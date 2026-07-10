using System.Text.Json.Serialization;

namespace Xg3.Auth.Internal;

internal sealed class OAuthTokenRequest
{
    [JsonPropertyName("grant_type")]
    public string GrantType { get; set; } = Xg3AuthConstants.GrantType;

    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = "";

    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; set; } = "";

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = "";
}

internal sealed class OAuthTokenSuccessResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}

internal sealed class OAuthTokenErrorResponse
{
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }
}
