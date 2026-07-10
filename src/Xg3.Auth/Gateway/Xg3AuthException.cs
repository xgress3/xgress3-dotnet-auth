namespace Xg3.Auth;

/// <summary>Represents an error raised while acquiring or applying xgress3 caller credentials.</summary>
public sealed class Xg3AuthException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="Xg3AuthException"/> class with a message.</summary>
    /// <param name="message">A description of the failure.</param>
    public Xg3AuthException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="Xg3AuthException"/> class with a message and inner exception.</summary>
    /// <param name="message">A description of the failure.</param>
    /// <param name="innerException">The underlying exception that caused this failure.</param>
    public Xg3AuthException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>The <c>error</c> code returned by the token endpoint, when available.</summary>
    public string? OAuthError { get; init; }

    /// <summary>The <c>error_description</c> returned by the token endpoint, when available.</summary>
    public string? OAuthErrorDescription { get; init; }

    /// <summary>The HTTP status code returned by the token endpoint, when the failure originated from an HTTP response.</summary>
    public int? StatusCode { get; init; }
}
