# Consumer sample apps

Four **net8.0** samples restore **Xg3.Auth** from a local pack output after `dotnet pack`. CI does this via `tools/validate-package.sh`.

| Project | Integration | What it demonstrates |
|---------|-------------|-------------------|
| `JwtAuth.Direct` | No DI | `new Xg3JwtAuthProvider(...)`, manual `Xg3JwtAuthDelegatingHandler` |
| `SecretKeyAuth.Direct` | No DI | `new Xg3ServiceKeyAuthProvider(...)`, manual `Xg3ServiceKeyAuthDelegatingHandler` |
| `JwtAuth.DependencyInjection` | DI | `AddXg3JwtAuth` + `AddXg3JwtAuthHandler` with **config binding** (`appsettings.json`) |
| `SecretKeyAuth.DependencyInjection` | DI | `AddXg3ServiceKeyAuth` + `AddXg3ServiceKeyAuthHandler` with **config binding** |

The DI samples include a **commented-out** block showing the equivalent `Action<>` code configuration (without options binding).

Each sample uses one auth mode only — matching real usage (never both headers on the same `HttpClient`).

CI runs `tools/validate-package.sh` after tests.

```bash
dotnet pack src/Xg3.Auth -c Release -o ./artifacts
dotnet restore samples/JwtAuth.Direct --source ./artifacts --source https://api.nuget.org/v3/index.json
dotnet build samples/JwtAuth.Direct -c Release --no-restore
# repeat restore/build for the other samples, or run:
./tools/validate-package.sh
```
