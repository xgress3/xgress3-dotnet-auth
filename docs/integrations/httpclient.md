# HttpClient integration

Use one auth handler per `HttpClient` pipeline. Never chain JWT and service-key handlers on the same client.

## JWT

```csharp
services.AddXg3JwtAuth(configuration);
services.AddHttpClient<MyApiClient>()
    .AddXg3JwtAuthHandler()
    .ConfigureHttpClient((sp, client) =>
        client.BaseAddress = sp.GetRequiredService<IXg3JwtAuthProvider>().ServiceBaseAddress);

// Outbound requests include: X-Xg3-Authorization: Bearer {token}
```

### Host rewrite (JWT)

When your app uses a local base address during development:

```csharp
services.AddHttpClient<MyApiClient>(c => c.BaseAddress = new Uri("http://localhost:5000/"))
    .AddXg3JwtAuthHandler(o =>
    {
        o.RewriteRequestHost = true;
        o.ClientBaseAddress = new Uri("http://localhost:5000/");
    });
```

## Service key

```csharp
services.AddXg3ServiceKeyAuth(configuration);
services.AddHttpClient<MyApiClient>()
    .AddXg3ServiceKeyAuthHandler()
    .ConfigureHttpClient((sp, client) =>
        client.BaseAddress = sp.GetRequiredService<IXg3ServiceKeyAuthProvider>().ServiceBaseAddress);

// Outbound requests include: X-Xg3-Service-Key: xg3_sk_{keyId}.{secret}
```

### Host rewrite (service key)

```csharp
services.AddHttpClient<MyApiClient>(c => c.BaseAddress = new Uri("http://localhost:5000/"))
    .AddXg3ServiceKeyAuthHandler(o =>
    {
        o.RewriteRequestHost = true;
        o.ClientBaseAddress = new Uri("http://localhost:5000/");
    });
```

## Manual headers

```csharp
// JWT
var (jwtName, jwtValue) = jwtProvider.AuthorizationHeader;

// Service key
var (keyName, keyValue) = serviceKeyProvider.ServiceKeyHeader;
```
