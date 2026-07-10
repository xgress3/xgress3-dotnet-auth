using Xg3.Auth;

var options = new Xg3ServiceKeyAuthOptions
{
    Region = "au",
    ServiceKey = "xg3_sk_keyid.secret",
    AccountId = "acme",
    ServiceId = "api"
};

var keyProvider = new Xg3ServiceKeyAuthProvider(options);

var (headerName, headerValue) = keyProvider.ServiceKeyHeader;
Console.WriteLine($"Service key URL: {keyProvider.ServiceBaseUrl}");
Console.WriteLine($"Service key header: {headerName}");

using var handler = new Xg3ServiceKeyAuthDelegatingHandler(keyProvider)
{
    InnerHandler = new HttpClientHandler()
};
using var apiClient = new HttpClient(handler) { BaseAddress = keyProvider.ServiceBaseAddress };

// Outbound calls attach X-Xg3-Service-Key automatically:
// var response = await apiClient.GetAsync("/health");

_ = headerValue;
