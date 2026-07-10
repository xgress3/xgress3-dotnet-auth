# Flurl integration

Create a `FlurlClient` from an `HttpClient` that already has an Xg3 auth handler in its pipeline.

## JWT

```csharp
var tokenProvider = /* IXg3JwtAuthProvider from DI */;
var recorder = new HttpClientHandler();
var authHandler = new Xg3JwtAuthDelegatingHandler(tokenProvider) { InnerHandler = recorder };
using var httpClient = new HttpClient(authHandler)
{
    BaseAddress = tokenProvider.ServiceBaseAddress
};

var flurl = new FlurlClient(httpClient);
await flurl.Request("health").GetAsync();
```

## Service key

```csharp
var keyProvider = /* IXg3ServiceKeyAuthProvider from DI */;
var authHandler = new Xg3ServiceKeyAuthDelegatingHandler(keyProvider) { InnerHandler = new HttpClientHandler() };
using var httpClient = new HttpClient(authHandler)
{
    BaseAddress = keyProvider.ServiceBaseAddress
};

var flurl = new FlurlClient(httpClient);
await flurl.Request("health").GetAsync();
```

## Host rewrite

Pass `RewriteRequestHost = true` (and optional `ClientBaseAddress`) in `Xg3GatewayHandlerOptions` when using a local development base URL.
