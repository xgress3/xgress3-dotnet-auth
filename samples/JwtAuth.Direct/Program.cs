using Xg3.Auth;

var options = new Xg3JwtAuthOptions
{
    Region = "au",
    ClientId = "client",
    ClientSecret = "secret",
    AccountId = "acme",
    ServiceId = "api"
};

using var tokenHttpClient = new HttpClient();
using var jwtProvider = new Xg3JwtAuthProvider(options, tokenHttpClient);

var (headerName, headerValue) = jwtProvider.AuthorizationHeader;
Console.WriteLine($"JWT service URL: {jwtProvider.ServiceBaseUrl}");
Console.WriteLine($"Authorization header: {headerName}");

using var handler = new Xg3JwtAuthDelegatingHandler(jwtProvider)
{
    InnerHandler = new HttpClientHandler()
};
using var apiClient = new HttpClient(handler) { BaseAddress = jwtProvider.ServiceBaseAddress };

// Outbound calls mint/attach the token automatically:
// var response = await apiClient.GetAsync("/health");

_ = headerValue;
