namespace Xg3.Auth;

/// <summary>Provides the data for the <see cref="IXg3JwtAuthProvider.TokenRefreshFailed"/> event.</summary>
public sealed class Xg3TokenRefreshFailedEventArgs : EventArgs
{
    /// <summary>Initializes a new instance of the <see cref="Xg3TokenRefreshFailedEventArgs"/> class.</summary>
    /// <param name="exception">The exception that caused the refresh to fail.</param>
    /// <param name="servingStaleToken">
    /// <see langword="true"/> if a previously issued, not-yet-expired token remains in use despite the failure.
    /// </param>
    public Xg3TokenRefreshFailedEventArgs(Exception exception, bool servingStaleToken)
    {
        Exception = exception;
        ServingStaleToken = servingStaleToken;
    }

    /// <summary>The exception that caused the refresh to fail.</summary>
    public Exception Exception { get; }

    /// <summary>
    /// <see langword="true"/> if a previously issued, not-yet-expired token remains in use despite the failure.
    /// </summary>
    public bool ServingStaleToken { get; }
}
