namespace Xg3.Auth;

/// <summary>Configuration for OAuth client-credentials JWT authentication against the xgress3 gateway.</summary>
public sealed class Xg3JwtAuthOptions
{
    /// <summary>The configuration section name (<c>Xg3:JwtAuth</c>) used when binding from <c>IConfiguration</c>.</summary>
    public const string SectionName = "Xg3:JwtAuth";

    /// <summary>The regional label of the xgress3 gateway (for example, <c>au</c>).</summary>
    public string Region { get; set; } = "au";

    /// <summary>The OAuth client identifier issued for the service.</summary>
    public string ClientId { get; set; } = "";

    /// <summary>The OAuth client secret issued for the service.</summary>
    public string ClientSecret { get; set; } = "";

    /// <summary>The xgress3 account identifier that owns the target service.</summary>
    public string AccountId { get; set; } = "";

    /// <summary>The identifier of the target backend service.</summary>
    public string ServiceId { get; set; } = "";

    /// <summary>
    /// How long before token expiry a proactive refresh should occur. Defaults to six minutes.
    /// </summary>
    public TimeSpan RefreshBeforeExpiry { get; set; } = TimeSpan.FromMinutes(6);

    /// <summary>Validates that all required options are present and well formed.</summary>
    /// <exception cref="InvalidOperationException">Thrown when a required value is missing or invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Region))
        {
            throw new InvalidOperationException("Xg3 JWT auth Region is required.");
        }

        if (string.IsNullOrWhiteSpace(ClientId))
        {
            throw new InvalidOperationException("Xg3 JWT auth ClientId is required.");
        }

        if (string.IsNullOrWhiteSpace(ClientSecret))
        {
            throw new InvalidOperationException("Xg3 JWT auth ClientSecret is required.");
        }

        if (string.IsNullOrWhiteSpace(AccountId))
        {
            throw new InvalidOperationException("Xg3 JWT auth AccountId is required.");
        }

        if (string.IsNullOrWhiteSpace(ServiceId))
        {
            throw new InvalidOperationException("Xg3 JWT auth ServiceId is required.");
        }

        if (RefreshBeforeExpiry < TimeSpan.Zero)
        {
            throw new InvalidOperationException("Xg3 JWT auth RefreshBeforeExpiry cannot be negative.");
        }
    }
}
