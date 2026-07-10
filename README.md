# Xg3.Auth

Caller authentication for .NET apps calling APIs **forwarded by the [xgress3](https://xgress3.com) gateway**.

**Product:** [xgress3.com](https://xgress3.com) · **Documentation:** [docs.xgress3.com](https://docs.xgress3.com)

Supports **.NET 6.0 and later** (`net6.0` and `net8.0` builds in the NuGet package).

## What is xgress3?

[xgress3](https://xgress3.com) is a zero-trust HTTP access layer for private services. A lightweight agent runs inside the private environment and establishes a persistent, outbound-only, mTLS-protected connection to the xgress3 control plane. No inbound ports are opened and no user-level shared credentials are distributed.

Each backend service is reachable through a stable tenant hostname of the form `https://{account-id}--{service-id}.{region}.xg3.io/{path}`. Requests terminate at the xgress3 gateway rather than inside the private network. The control plane authenticates and authorizes every request against explicit policy before forwarding it over the existing outbound connection to the agent, which acts as a transport-only forwarder. Requests are rejected by default unless explicitly permitted.

xgress3 controls access to the service; backend authentication and authorization are unchanged. Callers therefore supply two independent things: the credentials your backend already expects, and an xgress3 caller credential that the control plane validates. See [Ingress authentication](https://docs.xgress3.com/developers/ingress-authentication) in the documentation for the HTTP contract this package implements.

**Xg3.Auth** handles the caller side for .NET in **two supported modes** (pick one per `HttpClient` — never both on the same request):

- **JWT** — obtains a bearer token via OAuth client credentials, keeps it fresh, and supplies `X-Xg3-Authorization`
- **Service key** — supplies the static `X-Xg3-Service-Key` header (`xg3_sk_{keyId}.{secret}`)

Both modes expose your service URL and optional host rewrite for `HttpClient`, Refit, Flurl, and RestSharp.

## What this package does

| Concern | JWT path | Service key path |
|--------|----------|------------------|
| Options | `Xg3JwtAuthOptions` (`Xg3:JwtAuth`) | `Xg3ServiceKeyAuthOptions` (`Xg3:ServiceKeyAuth`) |
| Provider | `IXg3JwtAuthProvider` | `IXg3ServiceKeyAuthProvider` |
| Handler | `Xg3JwtAuthDelegatingHandler` | `Xg3ServiceKeyAuthDelegatingHandler` |
| DI | `AddXg3JwtAuth`, `AddXg3JwtAuthHandler` | `AddXg3ServiceKeyAuth`, `AddXg3ServiceKeyAuthHandler` |

Shared (non-auth): `IXg3GatewayServiceTarget` for `ServiceBaseAddress` / `ServiceBaseUrl`, plus optional `RewriteRequestHost` on both handlers.

**Important:** The gateway rejects requests that include **both** auth headers. Do not chain JWT and service-key handlers on the same `HttpClient`.

`ServiceId` must match the service ID from the xgress3 console (the DNS-safe id used in the public hostname and OAuth scope).

## Install

```bash
dotnet add package Xg3.Auth
```

## Quickstart

Pick **one auth mode per `HttpClient`** — the gateway rejects requests with both `X-Xg3-Authorization` and `X-Xg3-Service-Key`.

Working sample projects live under [`samples/`](samples/).

### JWT with dependency injection

Bind from configuration (typical ASP.NET Core host):

```json
// appsettings.json
{
  "Xg3": {
    "JwtAuth": {
      "Region": "au",
      "ClientId": "YOUR_CLIENT_ID",
      "ClientSecret": "YOUR_CLIENT_SECRET",
      "AccountId": "acme",
      "ServiceId": "api"
    }
  }
}
```

```csharp
using Xg3.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddXg3JwtAuth(builder.Configuration.GetSection(Xg3JwtAuthOptions.SectionName));
builder.Services.AddHttpClient("api")
    .AddXg3JwtAuthHandler()
    .ConfigureHttpClient((sp, client) =>
        client.BaseAddress = sp.GetRequiredService<IXg3JwtAuthProvider>().ServiceBaseAddress);

var app = builder.Build();
// var client = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient("api");
// await client.GetAsync("/health");
```

### JWT — inline instantiation (no DI)

```csharp
using Xg3.Auth;

var options = new Xg3JwtAuthOptions
{
    Region = "au",
    ClientId = "YOUR_CLIENT_ID",
    ClientSecret = "YOUR_CLIENT_SECRET",
    AccountId = "acme",
    ServiceId = "api"
};

using var tokenHttpClient = new HttpClient();
using var jwtProvider = new Xg3JwtAuthProvider(options, tokenHttpClient);

using var handler = new Xg3JwtAuthDelegatingHandler(jwtProvider)
{
    InnerHandler = new HttpClientHandler()
};
using var apiClient = new HttpClient(handler) { BaseAddress = jwtProvider.ServiceBaseAddress };

// await apiClient.GetAsync("/health");  // token minted and header attached automatically
```

### Service key with dependency injection

```json
// appsettings.json
{
  "Xg3": {
    "ServiceKeyAuth": {
      "Region": "au",
      "ServiceKey": "xg3_sk_YOUR_KEY_ID.YOUR_SECRET",
      "AccountId": "acme",
      "ServiceId": "api"
    }
  }
}
```

```csharp
using Xg3.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddXg3ServiceKeyAuth(
    builder.Configuration.GetSection(Xg3ServiceKeyAuthOptions.SectionName));
builder.Services.AddHttpClient("api")
    .AddXg3ServiceKeyAuthHandler()
    .ConfigureHttpClient((sp, client) =>
        client.BaseAddress = sp.GetRequiredService<IXg3ServiceKeyAuthProvider>().ServiceBaseAddress);

var app = builder.Build();
// var client = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient("api");
// await client.GetAsync("/health");
```

### Service key — inline instantiation (no DI)

```csharp
using Xg3.Auth;

var options = new Xg3ServiceKeyAuthOptions
{
    Region = "au",
    ServiceKey = "xg3_sk_YOUR_KEY_ID.YOUR_SECRET",
    AccountId = "acme",
    ServiceId = "api"
};

var keyProvider = new Xg3ServiceKeyAuthProvider(options);

using var handler = new Xg3ServiceKeyAuthDelegatingHandler(keyProvider)
{
    InnerHandler = new HttpClientHandler()
};
using var apiClient = new HttpClient(handler) { BaseAddress = keyProvider.ServiceBaseAddress };

// await apiClient.GetAsync("/health");  // X-Xg3-Service-Key attached automatically
```

### Manual headers (no `DelegatingHandler`)

Use any HTTP client library. Read the header from the provider and attach it yourself:

```csharp
using Xg3.Auth;

// JWT — mint token, then attach header yourself
var jwtOptions = new Xg3JwtAuthOptions
{
    Region = "au",
    ClientId = "YOUR_CLIENT_ID",
    ClientSecret = "YOUR_CLIENT_SECRET",
    AccountId = "acme",
    ServiceId = "api"
};
using var jwtProvider = new Xg3JwtAuthProvider(jwtOptions, new HttpClient());
await jwtProvider.EnsureValidTokenAsync();
var (jwtName, jwtValue) = jwtProvider.AuthorizationHeader;

using var jwtRequest = new HttpRequestMessage(HttpMethod.Get, jwtProvider.ServiceBaseUrl + "health");
jwtRequest.Headers.TryAddWithoutValidation(jwtName, jwtValue);
// await yourHttpClient.SendAsync(jwtRequest);

// Service key — static header, no token mint
var serviceKeyOptions = new Xg3ServiceKeyAuthOptions
{
    Region = "au",
    ServiceKey = "xg3_sk_YOUR_KEY_ID.YOUR_SECRET",
    AccountId = "acme",
    ServiceId = "api"
};
var keyProvider = new Xg3ServiceKeyAuthProvider(serviceKeyOptions);
var (keyName, keyValue) = keyProvider.ServiceKeyHeader;

using var keyRequest = new HttpRequestMessage(HttpMethod.Get, keyProvider.ServiceBaseUrl + "health");
keyRequest.Headers.TryAddWithoutValidation(keyName, keyValue);
// await yourHttpClient.SendAsync(keyRequest);
```

Register only the auth path your app uses. See [Configuration](#configuration) for the full options reference.

## Configuration

```json
{
  "Xg3": {
    "JwtAuth": {
      "Region": "au",
      "ClientId": "...",
      "ClientSecret": "...",
      "AccountId": "acme",
      "ServiceId": "api"
    },
    "ServiceKeyAuth": {
      "Region": "au",
      "ServiceKey": "xg3_sk_abc123.secretPartHere",
      "AccountId": "acme",
      "ServiceId": "api"
    }
  }
}
```

## Compatibility and dependencies

The package multi-targets **net6.0** and **net8.0**. NuGet selects the best assembly for your app's target framework.

**Microsoft.Extensions alignment:** the `net6.0` build depends on **Extensions 6.x**; the `net8.0` build depends on **Extensions 8.x**. This avoids forcing Extensions 8 onto ASP.NET Core 6 apps that already use Extensions 6 in-box.

Direct dependencies (per TFM group in the `.nupkg`):

| Package | net6.0 | net8.0 |
|---------|--------|--------|
| `Microsoft.Extensions.Configuration.Abstractions` | 6.0.0 | 8.0.0 |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 6.0.0 | 8.0.2 |
| `Microsoft.Extensions.Http` | 6.0.0 | 8.0.1 |
| `Microsoft.Extensions.Logging.Abstractions` | 6.0.4 | 8.0.2 |
| `Microsoft.Extensions.Options` | 6.0.0 | 8.0.2 |
| `Microsoft.Extensions.Options.ConfigurationExtensions` | 6.0.0 | 8.0.0 |

No Refit, Flurl, RestSharp, or ASP.NET framework references are shipped. DI integration requires `Microsoft.Extensions.Http` (already a direct dependency when you use `AddXg3*AuthHandler`).

**Manual use without DI:** construct `Xg3JwtAuthProvider` or `Xg3ServiceKeyAuthProvider` directly with your own `HttpClient` — you still reference the same package, but you control the host app's Extensions stack.

CI validates restore/build with four **net8.0** canary samples under `samples/` — direct instantiation and DI (config binding) for JWT and service key.

Do **not** pin `Microsoft.Extensions.Http` or other Extensions packages to a different major version than `Xg3.Auth`'s TFM-aligned dependency group — let the package supply the matching versions (see `samples/README.md`).

## Integrations

- [HttpClient](docs/integrations/httpclient.md)
- [Refit](docs/integrations/refit.md)
- [Flurl](docs/integrations/flurl.md)
- [RestSharp](docs/integrations/restsharp.md)

## Service URL

`https://{accountId}--{serviceId}.{regionalApex}/`

Example: `https://acme--api.au.xg3.io/`

## License

MIT — see [LICENSE](LICENSE).
