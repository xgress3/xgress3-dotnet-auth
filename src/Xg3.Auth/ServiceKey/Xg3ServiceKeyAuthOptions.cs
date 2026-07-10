using Xg3.Auth.Internal;

namespace Xg3.Auth;

/// <summary>Configuration for static service-key authentication against the xgress3 gateway.</summary>
public sealed class Xg3ServiceKeyAuthOptions
{
    /// <summary>The configuration section name (<c>Xg3:ServiceKeyAuth</c>) used when binding from <c>IConfiguration</c>.</summary>
    public const string SectionName = "Xg3:ServiceKeyAuth";

    /// <summary>The regional label of the xgress3 gateway (for example, <c>au</c>).</summary>
    public string Region { get; set; } = "au";

    /// <summary>The full service key value, in the form <c>xg3_sk_{keyId}.{secret}</c>.</summary>
    public string ServiceKey { get; set; } = "";

    /// <summary>The xgress3 account identifier that owns the target service.</summary>
    public string AccountId { get; set; } = "";

    /// <summary>The identifier of the target backend service.</summary>
    public string ServiceId { get; set; } = "";

    /// <summary>Validates that all required options are present and well formed.</summary>
    /// <exception cref="InvalidOperationException">Thrown when a required value is missing or invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Region))
        {
            throw new InvalidOperationException("Xg3 service-key auth Region is required.");
        }

        if (string.IsNullOrWhiteSpace(ServiceKey))
        {
            throw new InvalidOperationException("Xg3 service-key auth ServiceKey is required.");
        }

        if (!ServiceKey.StartsWith(Xg3AuthConstants.ServiceKeyPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Xg3 service-key auth ServiceKey must start with '{Xg3AuthConstants.ServiceKeyPrefix}'.");
        }

        var dotIndex = ServiceKey.IndexOf('.', Xg3AuthConstants.ServiceKeyPrefix.Length);
        if (dotIndex <= Xg3AuthConstants.ServiceKeyPrefix.Length || dotIndex >= ServiceKey.Length - 1)
        {
            throw new InvalidOperationException(
                "Xg3 service-key auth ServiceKey must contain a '.' separating key id and secret.");
        }

        if (string.IsNullOrWhiteSpace(AccountId))
        {
            throw new InvalidOperationException("Xg3 service-key auth AccountId is required.");
        }

        if (string.IsNullOrWhiteSpace(ServiceId))
        {
            throw new InvalidOperationException("Xg3 service-key auth ServiceId is required.");
        }
    }
}
