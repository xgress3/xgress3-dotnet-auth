using Xg3.Auth.Internal;

namespace Xg3.Auth;

/// <summary>An <see cref="IXg3ServiceKeyAuthProvider"/> backed by a static, pre-issued service key.</summary>
public sealed class Xg3ServiceKeyAuthProvider : IXg3ServiceKeyAuthProvider
{
    /// <summary>Initializes a new instance of the <see cref="Xg3ServiceKeyAuthProvider"/> class.</summary>
    /// <param name="options">The service-key authentication options. Validated on construction.</param>
    public Xg3ServiceKeyAuthProvider(Xg3ServiceKeyAuthOptions options)
    {
        options.Validate();
        Options = options;
        ServiceKey = options.ServiceKey;
        ServiceBaseAddress = Xg3AuthEndpoints.ResolveServiceBaseAddress(
            options.AccountId,
            options.ServiceId,
            options.Region);
        ServiceBaseUrl = ServiceBaseAddress.AbsoluteUri;
    }

    internal Xg3ServiceKeyAuthOptions Options { get; }

    /// <inheritdoc />
    public string ServiceKey { get; }

    /// <inheritdoc />
    public Uri ServiceBaseAddress { get; }

    /// <inheritdoc />
    public string ServiceBaseUrl { get; }

    /// <inheritdoc />
    public (string Name, string Value) ServiceKeyHeader =>
        (Xg3AuthConstants.ServiceKeyHeaderName, ServiceKey);
}
