# RestSharp integration

RestSharp 112+ can use a custom `HttpMessageHandler` via `RestClientOptions.ConfigureMessageHandler`.

## JWT

```csharp
var tokenProvider = /* IXg3JwtAuthProvider from DI */;
var authHandler = new Xg3JwtAuthDelegatingHandler(tokenProvider) { InnerHandler = new HttpClientHandler() };

var options = new RestClientOptions(tokenProvider.ServiceBaseUrl)
{
    ConfigureMessageHandler = _ => authHandler
};

using var client = new RestClient(options);
await client.ExecuteAsync(new RestRequest("health"));
```

## Service key

```csharp
var keyProvider = /* IXg3ServiceKeyAuthProvider from DI */;
var authHandler = new Xg3ServiceKeyAuthDelegatingHandler(keyProvider) { InnerHandler = new HttpClientHandler() };

var options = new RestClientOptions(keyProvider.ServiceBaseUrl)
{
    ConfigureMessageHandler = _ => authHandler
};

using var client = new RestClient(options);
await client.ExecuteAsync(new RestRequest("health"));
```

## Manual fallback

If you cannot inject a handler, add the header explicitly:

```csharp
var (name, value) = keyProvider.ServiceKeyHeader;
request.AddOrUpdateHeader(name, value);
```
