using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Xg3.Auth.Internal;

internal sealed class OAuthTokenClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public OAuthTokenClient(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<(string AccessToken, int ExpiresIn)> MintTokenAsync(
        Uri tokenEndpoint,
        OAuthTokenRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await SendWithOptionalRetryAsync(tokenEndpoint, request, cancellationToken)
            .ConfigureAwait(false);

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            var success = JsonSerializer.Deserialize<OAuthTokenSuccessResponse>(body, JsonOptions);
            if (success is null || string.IsNullOrWhiteSpace(success.AccessToken))
            {
                throw new Xg3AuthException("Token endpoint returned an empty access_token.");
            }

            if (success.ExpiresIn <= 0)
            {
                throw new Xg3AuthException("Token endpoint returned an invalid expires_in value.");
            }

            return (success.AccessToken, success.ExpiresIn);
        }

        OAuthTokenErrorResponse? error = null;
        try
        {
            error = JsonSerializer.Deserialize<OAuthTokenErrorResponse>(body, JsonOptions);
        }
        catch (JsonException)
        {
            // ignore parse failure
        }

        var message = error?.ErrorDescription ?? error?.Error ?? $"Token mint failed with HTTP {(int)response.StatusCode}.";
        throw new Xg3AuthException(message)
        {
            OAuthError = error?.Error,
            OAuthErrorDescription = error?.ErrorDescription,
            StatusCode = (int)response.StatusCode
        };
    }

    private async Task<HttpResponseMessage> SendWithOptionalRetryAsync(
        Uri tokenEndpoint,
        OAuthTokenRequest request,
        CancellationToken cancellationToken)
    {
        var response = await PostAsync(tokenEndpoint, request, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode != HttpStatusCode.TooManyRequests)
        {
            return response;
        }

        if (response.Headers.RetryAfter?.Delta is TimeSpan delay)
        {
            _logger.LogWarning("Token endpoint rate limited; retrying after {Delay}", delay);
            response.Dispose();
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            return await PostAsync(tokenEndpoint, request, cancellationToken).ConfigureAwait(false);
        }

        return response;
    }

    private Task<HttpResponseMessage> PostAsync(
        Uri tokenEndpoint,
        OAuthTokenRequest request,
        CancellationToken cancellationToken) =>
        _httpClient.PostAsJsonAsync(tokenEndpoint, request, cancellationToken);
}
