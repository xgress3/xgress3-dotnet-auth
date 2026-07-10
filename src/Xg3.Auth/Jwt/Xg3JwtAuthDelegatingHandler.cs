using Xg3.Auth.Internal;

namespace Xg3.Auth;

/// <summary>
/// A <see cref="DelegatingHandler"/> that attaches a valid JWT authorization header to each outgoing
/// request, refreshing the token as needed and optionally rewriting the request host.
/// </summary>
public sealed class Xg3JwtAuthDelegatingHandler : DelegatingHandler
{
    private readonly IXg3JwtAuthProvider _provider;
    private readonly Xg3GatewayHandlerOptions _handlerOptions;

    /// <summary>Initializes a new instance of the <see cref="Xg3JwtAuthDelegatingHandler"/> class.</summary>
    /// <param name="provider">The JWT provider that supplies and refreshes tokens.</param>
    /// <param name="handlerOptions">Optional handler behavior, such as host rewriting.</param>
    public Xg3JwtAuthDelegatingHandler(
        IXg3JwtAuthProvider provider,
        Xg3GatewayHandlerOptions? handlerOptions = null)
    {
        _provider = provider;
        _handlerOptions = handlerOptions ?? new Xg3GatewayHandlerOptions();
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_handlerOptions.RewriteRequestHost)
        {
            Xg3GatewayRequestUriRewriter.RewriteIfNeeded(
                request,
                _provider.ServiceBaseAddress,
                _handlerOptions.ClientBaseAddress);
        }

        await _provider.EnsureValidTokenAsync(cancellationToken).ConfigureAwait(false);

        var (name, value) = _provider.AuthorizationHeader;
        if (!request.Headers.Contains(name))
        {
            request.Headers.TryAddWithoutValidation(name, value);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
