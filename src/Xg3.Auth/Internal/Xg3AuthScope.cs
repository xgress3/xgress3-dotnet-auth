namespace Xg3.Auth.Internal;

internal static class Xg3AuthScope
{
    internal static string Format(string accountId, string serviceId) =>
        $"account:{accountId} service:{serviceId}";
}
