# Refit integration

Refit builds on `HttpClient`. Configure auth on the underlying client, then pass it to `RestService.For<T>()`.

## JWT

```csharp
services.AddXg3JwtAuth(o =>
{
    o.Region = "au";
    o.ClientId = Environment.GetEnvironmentVariable("XG3_CLIENT_ID")!;
    o.ClientSecret = Environment.GetEnvironmentVariable("XG3_CLIENT_SECRET")!;
    o.AccountId = "acme";
    o.ServiceId = "api";
});

services.AddHttpClient("refit-jwt")
    .AddXg3JwtAuthHandler()
    .ConfigureHttpClient((sp, client) =>
        client.BaseAddress = sp.GetRequiredService<IXg3JwtAuthProvider>().ServiceBaseAddress);

var httpClient = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("refit-jwt");
var api = RestService.For<IMyApi>(httpClient);
```

## Service key

```csharp
services.AddXg3ServiceKeyAuth(o =>
{
    o.Region = "au";
    o.ServiceKey = Environment.GetEnvironmentVariable("XG3_SERVICE_KEY")!;
    o.AccountId = "acme";
    o.ServiceId = "api";
});

services.AddHttpClient("refit-key")
    .AddXg3ServiceKeyAuthHandler()
    .ConfigureHttpClient((sp, client) =>
        client.BaseAddress = sp.GetRequiredService<IXg3ServiceKeyAuthProvider>().ServiceBaseAddress);
```

## Interface example

```csharp
public interface IMyApi
{
    [Get("/health")]
    Task<HttpResponseMessage> GetHealthAsync();
}
```
