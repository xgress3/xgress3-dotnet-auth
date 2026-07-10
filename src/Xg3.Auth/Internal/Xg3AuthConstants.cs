namespace Xg3.Auth.Internal;

internal static class Xg3AuthConstants
{
    internal const string JwtAuthorizationHeaderName = "X-Xg3-Authorization";
    internal const string ServiceKeyHeaderName = "X-Xg3-Service-Key";
    internal const string GrantType = "client_credentials";
    internal const string DefaultDomainSuffix = "xg3.io";
    internal const string ServiceKeyPrefix = "xg3_sk_";
    internal const string TokenHttpClientName = "Xg3.Auth.TokenClient";
}
