namespace Xg3.Auth;

/// <summary>Provides the data for the <see cref="IXg3JwtAuthProvider.TokenRefreshed"/> event.</summary>
public sealed class Xg3TokenRefreshedEventArgs : EventArgs
{
    /// <summary>Initializes a new instance of the <see cref="Xg3TokenRefreshedEventArgs"/> class.</summary>
    /// <param name="accessToken">The newly acquired access token.</param>
    /// <param name="expiresAt">The absolute expiry time of the new token.</param>
    public Xg3TokenRefreshedEventArgs(string accessToken, DateTimeOffset expiresAt)
    {
        AccessToken = accessToken;
        ExpiresAt = expiresAt;
    }

    /// <summary>The newly acquired access token.</summary>
    public string AccessToken { get; }

    /// <summary>The absolute expiry time of the new token.</summary>
    public DateTimeOffset ExpiresAt { get; }
}
