using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xg3.Auth.Internal;

namespace Xg3.Auth;

/// <summary>
/// An <see cref="IXg3JwtAuthProvider"/> that mints and caches JWT access tokens using the OAuth
/// client-credentials grant, refreshing them proactively before they expire.
/// </summary>
public sealed class Xg3JwtAuthProvider : IXg3JwtAuthProvider
{
    private readonly Xg3JwtAuthOptions _options;
    private readonly OAuthTokenClient _tokenClient;
    private readonly ISystemClock _clock;
    private readonly ILogger<Xg3JwtAuthProvider> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly bool _ownsHttpClient;
    private readonly HttpClient? _ownedHttpClient;

    private readonly Uri _tokenEndpoint;
    private readonly string _scope;

    private string? _accessToken;
    private DateTimeOffset? _expiresAt;
    private PeriodicTimer? _refreshTimer;
    private CancellationTokenSource? _refreshCts;
    private Task? _refreshLoopTask;
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="Xg3JwtAuthProvider"/> class.</summary>
    /// <param name="options">The JWT authentication options. Validated on construction.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> used to call the token endpoint. Not disposed by this provider.</param>
    /// <param name="logger">An optional logger. When omitted, logging is disabled.</param>
    public Xg3JwtAuthProvider(
        Xg3JwtAuthOptions options,
        HttpClient httpClient,
        ILogger<Xg3JwtAuthProvider>? logger = null)
        : this(options, httpClient, ownsHttpClient: false, logger, clock: null)
    {
    }

    internal Xg3JwtAuthProvider(
        Xg3JwtAuthOptions options,
        HttpClient httpClient,
        bool ownsHttpClient,
        ILogger<Xg3JwtAuthProvider>? logger,
        ISystemClock? clock)
    {
        options.Validate();
        _options = options;
        _ownsHttpClient = ownsHttpClient;
        if (ownsHttpClient)
        {
            _ownedHttpClient = httpClient;
        }

        _tokenClient = new OAuthTokenClient(httpClient, logger ?? NullLogger<Xg3JwtAuthProvider>.Instance);
        _clock = clock ?? SystemClock.Instance;
        _logger = logger ?? NullLogger<Xg3JwtAuthProvider>.Instance;

        ServiceBaseAddress = Xg3AuthEndpoints.ResolveServiceBaseAddress(
            options.AccountId,
            options.ServiceId,
            options.Region);
        ServiceBaseUrl = ServiceBaseAddress.AbsoluteUri;

        _tokenEndpoint = Xg3AuthEndpoints.ResolveTokenEndpoint(options.Region);
        _scope = Xg3AuthScope.Format(options.AccountId, options.ServiceId);
    }

    /// <inheritdoc />
    public Uri ServiceBaseAddress { get; }

    /// <inheritdoc />
    public string ServiceBaseUrl { get; }

    /// <inheritdoc />
    public DateTimeOffset? ExpiresAt
    {
        get
        {
            ThrowIfDisposed();
            return _expiresAt;
        }
    }

    /// <inheritdoc />
    public event EventHandler<Xg3TokenRefreshedEventArgs>? TokenRefreshed;

    /// <inheritdoc />
    public event EventHandler<Xg3TokenRefreshFailedEventArgs>? TokenRefreshFailed;

    /// <inheritdoc />
    public string AccessToken
    {
        get
        {
            ThrowIfDisposed();
            EnsureValidTokenAsync(CancellationToken.None).GetAwaiter().GetResult();
            return _accessToken ?? throw new Xg3AuthException("Access token is not available.");
        }
    }

    /// <inheritdoc />
    public (string Name, string Value) AuthorizationHeader
    {
        get
        {
            var token = AccessToken;
            return (Xg3AuthConstants.JwtAuthorizationHeaderName, $"Bearer {token}");
        }
    }

    /// <inheritdoc />
    public async Task EnsureValidTokenAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!NeedsRefresh())
        {
            return;
        }

        await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!NeedsRefresh())
            {
                return;
            }

            await MintTokenCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <summary>Stops background refresh and releases resources held by the provider.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        _refreshTimer?.Dispose();

        if (_refreshLoopTask is not null)
        {
            try
            {
                _refreshLoopTask.Wait(TimeSpan.FromSeconds(2));
            }
            catch
            {
                // Ignore shutdown errors; the loop is being torn down.
            }
        }

        _refreshLock.Dispose();

        if (_ownsHttpClient)
        {
            _ownedHttpClient?.Dispose();
        }
    }

    private bool NeedsRefresh()
    {
        if (_accessToken is null || _expiresAt is null)
        {
            return true;
        }

        return _clock.UtcNow >= _expiresAt.Value - _options.RefreshBeforeExpiry;
    }

    private async Task MintTokenCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            var request = new OAuthTokenRequest
            {
                ClientId = _options.ClientId,
                ClientSecret = _options.ClientSecret,
                Scope = _scope
            };

            var (accessToken, expiresIn) = await _tokenClient
                .MintTokenAsync(_tokenEndpoint, request, cancellationToken)
                .ConfigureAwait(false);

            _accessToken = accessToken;
            _expiresAt = _clock.UtcNow.AddSeconds(expiresIn);

            _logger.LogInformation(
                "Minted JWT for account {AccountId} service {ServiceId}; expires at {ExpiresAt}",
                _options.AccountId,
                _options.ServiceId,
                _expiresAt);

            TokenRefreshed?.Invoke(this, new Xg3TokenRefreshedEventArgs(accessToken, _expiresAt.Value));
            EnsureRefreshLoopStarted();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "JWT refresh failed for client {ClientId}", _options.ClientId);

            TokenRefreshFailed?.Invoke(
                this,
                new Xg3TokenRefreshFailedEventArgs(ex, servingStaleToken: CanServeStaleToken()));

            if (!CanServeStaleToken())
            {
                if (ex is Xg3AuthException)
                {
                    throw;
                }

                throw new Xg3AuthException("Failed to mint JWT access token.", ex);
            }
        }
    }

    private bool CanServeStaleToken() =>
        _accessToken is not null && _expiresAt is not null && _clock.UtcNow < _expiresAt.Value;

    private void EnsureRefreshLoopStarted()
    {
        if (_refreshTimer is not null)
        {
            return;
        }

        _refreshCts = new CancellationTokenSource();
        _refreshTimer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        _refreshLoopTask = RunRefreshLoopAsync(_refreshCts.Token);
    }

    private async Task RunRefreshLoopAsync(CancellationToken cancellationToken)
    {
        while (_refreshTimer is not null && await _refreshTimer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!NeedsRefresh())
            {
                continue;
            }

            try
            {
                await EnsureValidTokenAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Background JWT refresh tick failed");
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(Xg3JwtAuthProvider));
        }
    }
}
