namespace Xg3.Auth;

/// <summary>
/// Exposes the public xgress3 service URL for a configured account and service. The URL follows the
/// pattern <c>https://{accountId}--{serviceId}.{region}.xg3.io/</c> and is shared by both authentication modes.
/// </summary>
public interface IXg3GatewayServiceTarget
{
    /// <summary>
    /// The absolute base address of the service, including a trailing slash
    /// (for example, <c>https://acme--api.au.xg3.io/</c>). Suitable for <see cref="System.Net.Http.HttpClient.BaseAddress"/>.
    /// </summary>
    Uri ServiceBaseAddress { get; }

    /// <summary>The <see cref="ServiceBaseAddress"/> as an absolute URI string.</summary>
    string ServiceBaseUrl { get; }
}
