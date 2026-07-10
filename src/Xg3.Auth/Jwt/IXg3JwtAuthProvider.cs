namespace Xg3.Auth;

/// <summary>
/// Provides OAuth client-credentials JWT bearer tokens for xgress3 gateway authentication,
/// refreshing them proactively before expiry.
/// </summary>
public interface IXg3JwtAuthProvider : IXg3GatewayServiceTarget, IDisposable
{
    /// <summary>
    /// The current access token, acquiring or refreshing it synchronously if required.
    /// Prefer <see cref="EnsureValidTokenAsync"/> in asynchronous code paths.
    /// </summary>
    string AccessToken { get; }

    /// <summary>
    /// The authorization header name and value to attach to outgoing requests
    /// (for example, <c>("X-Xg3-Authorization", "Bearer &lt;token&gt;")</c>).
    /// </summary>
    (string Name, string Value) AuthorizationHeader { get; }

    /// <summary>The absolute expiry time of the current access token, or <see langword="null"/> if none has been acquired.</summary>
    DateTimeOffset? ExpiresAt { get; }

    /// <summary>Raised after a token is successfully minted or refreshed.</summary>
    event EventHandler<Xg3TokenRefreshedEventArgs>? TokenRefreshed;

    /// <summary>Raised when a token refresh attempt fails.</summary>
    event EventHandler<Xg3TokenRefreshFailedEventArgs>? TokenRefreshFailed;

    /// <summary>
    /// Ensures a valid, unexpired access token is available, minting or refreshing it if necessary.
    /// Concurrent callers share a single refresh operation.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task EnsureValidTokenAsync(CancellationToken cancellationToken = default);
}
