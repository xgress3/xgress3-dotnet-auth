namespace Xg3.Auth;

/// <summary>Provides static service-key credentials for xgress3 gateway authentication.</summary>
public interface IXg3ServiceKeyAuthProvider : IXg3GatewayServiceTarget
{
    /// <summary>The full service key value, in the form <c>xg3_sk_{keyId}.{secret}</c>.</summary>
    string ServiceKey { get; }

    /// <summary>
    /// The header name and value to attach to outgoing requests
    /// (for example, <c>("X-Xg3-Service-Key", "xg3_sk_...")</c>).
    /// </summary>
    (string Name, string Value) ServiceKeyHeader { get; }
}
