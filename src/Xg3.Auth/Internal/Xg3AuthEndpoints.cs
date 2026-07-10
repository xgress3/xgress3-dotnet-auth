namespace Xg3.Auth.Internal;

internal static class Xg3AuthEndpoints
{
    internal static string ResolveRegionalApexHost(string region) =>
        ResolveRegionalApexHost(region, Xg3AuthConstants.DefaultDomainSuffix);

    internal static string ResolveRegionalApexHost(string region, string domainSuffix)
    {
        if (string.IsNullOrWhiteSpace(region))
        {
            throw new ArgumentException("Region is required.", nameof(region));
        }

        if (string.IsNullOrWhiteSpace(domainSuffix))
        {
            throw new ArgumentException("Regional apex domain suffix is required.", nameof(domainSuffix));
        }

        var normalizedRegion = region.Trim().ToLowerInvariant();
        var suffix = domainSuffix.Trim().TrimStart('.').ToLowerInvariant();

        return suffix.StartsWith($"{normalizedRegion}.", StringComparison.Ordinal)
            ? suffix
            : $"{normalizedRegion}.{suffix}";
    }

    internal static string FormatServiceHostname(string accountId, string serviceId, string regionalApexHost) =>
        $"{accountId.Trim().ToLowerInvariant()}--{serviceId}.{regionalApexHost}";

    internal static Uri ResolveServiceBaseAddress(string accountId, string serviceId, string region)
    {
        var apex = ResolveRegionalApexHost(region);
        var host = FormatServiceHostname(accountId, serviceId, apex);
        return new Uri($"https://{host}/");
    }

    internal static Uri ResolveTokenEndpoint(string region)
    {
        var apex = ResolveRegionalApexHost(region);
        return new Uri($"https://{apex}/oauth/token");
    }
}
