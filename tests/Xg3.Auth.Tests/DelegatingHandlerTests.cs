using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xg3.Auth.Internal;
using Xg3.Auth.Tests.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Xg3.Auth.Tests;

public sealed class Xg3JwtAuthDelegatingHandlerTests
{
    private readonly ITestOutputHelper _output;

    public Xg3JwtAuthDelegatingHandlerTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task SendAsync_AddsJwtHeaderOnly()
    {
        _output.WriteLine("Arrange: JWT handler with recording inner handler");
        var tokenStub = new StubHttpMessageHandler(_ =>
            StubHttpMessageHandler.JsonResponse(HttpStatusCode.OK, new { access_token = "jwt", expires_in = 3600 }));
        using var provider = new Xg3JwtAuthProvider(CreateOptions(), new HttpClient(tokenStub), NullLogger<Xg3JwtAuthProvider>.Instance);

        var recorder = new RecordingHandler { InnerHandler = new HttpClientHandler() };
        var handler = new Xg3JwtAuthDelegatingHandler(provider)
        {
            InnerHandler = recorder
        };

        using var client = new HttpClient(handler)
        {
            BaseAddress = provider.ServiceBaseAddress
        };

        _output.WriteLine("Act: GET /health");
        await client.GetAsync("/health");

        var recorded = recorder.RecordedRequests.Single();
        _output.WriteLine($"Assert: jwt header present, service-key absent; uri={recorded.RequestUri}");

        Assert.True(recorded.Headers.Contains(Xg3AuthConstants.JwtAuthorizationHeaderName));
        Assert.False(recorded.Headers.Contains(Xg3AuthConstants.ServiceKeyHeaderName));
        Assert.Equal("Bearer jwt", recorded.Headers.GetValues(Xg3AuthConstants.JwtAuthorizationHeaderName).Single());
    }

    [Fact]
    public async Task SendAsync_WithRewrite_ReplacesAuthority()
    {
        _output.WriteLine("Arrange: rewrite enabled with localhost base");
        var tokenStub = new StubHttpMessageHandler(_ =>
            StubHttpMessageHandler.JsonResponse(HttpStatusCode.OK, new { access_token = "jwt", expires_in = 3600 }));
        using var provider = new Xg3JwtAuthProvider(CreateOptions(), new HttpClient(tokenStub), NullLogger<Xg3JwtAuthProvider>.Instance);

        var recorder = new RecordingHandler { InnerHandler = new HttpClientHandler() };
        var handler = new Xg3JwtAuthDelegatingHandler(provider, new Xg3GatewayHandlerOptions
        {
            RewriteRequestHost = true,
            ClientBaseAddress = new Uri("http://localhost:5000/")
        })
        {
            InnerHandler = recorder
        };

        using var client = new HttpClient(handler);
        _output.WriteLine("Act: GET localhost URL");
        await client.GetAsync("http://localhost:5000/api/v1/health");

        var recorded = recorder.RecordedRequests.Single();
        _output.WriteLine($"Assert: uri={recorded.RequestUri}");
        Assert.Equal("https://acme--api.au.xg3.io/api/v1/health", recorded.RequestUri!.AbsoluteUri);
    }

    private static Xg3JwtAuthOptions CreateOptions() => new()
    {
        Region = "au",
        ClientId = "client",
        ClientSecret = "secret",
        AccountId = "acme",
        ServiceId = "api"
    };
}

public sealed class Xg3ServiceKeyAuthDelegatingHandlerTests
{
    private readonly ITestOutputHelper _output;

    public Xg3ServiceKeyAuthDelegatingHandlerTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task SendAsync_AddsServiceKeyHeaderOnly()
    {
        _output.WriteLine("Arrange: service-key handler with recording inner handler");
        var provider = new Xg3ServiceKeyAuthProvider(new Xg3ServiceKeyAuthOptions
        {
            Region = "au",
            ServiceKey = "xg3_sk_keyid.secret",
            AccountId = "acme",
            ServiceId = "api"
        });

        var recorder = new RecordingHandler { InnerHandler = new HttpClientHandler() };
        var handler = new Xg3ServiceKeyAuthDelegatingHandler(provider) { InnerHandler = recorder };

        using var client = new HttpClient(handler) { BaseAddress = provider.ServiceBaseAddress };

        _output.WriteLine("Act: GET /health");
        await client.GetAsync("/health");

        var recorded = recorder.RecordedRequests.Single();
        _output.WriteLine($"Assert: service-key present, jwt absent; uri={recorded.RequestUri}");

        Assert.True(recorded.Headers.Contains(Xg3AuthConstants.ServiceKeyHeaderName));
        Assert.False(recorded.Headers.Contains(Xg3AuthConstants.JwtAuthorizationHeaderName));
        Assert.Equal("xg3_sk_keyid.secret", recorded.Headers.GetValues(Xg3AuthConstants.ServiceKeyHeaderName).Single());
    }

    [Fact]
    public async Task SendAsync_WithRewrite_ReplacesAuthority()
    {
        _output.WriteLine("Arrange: service-key handler with rewrite");
        var provider = new Xg3ServiceKeyAuthProvider(new Xg3ServiceKeyAuthOptions
        {
            Region = "au",
            ServiceKey = "xg3_sk_keyid.secret",
            AccountId = "acme",
            ServiceId = "api"
        });

        var recorder = new RecordingHandler { InnerHandler = new HttpClientHandler() };
        var handler = new Xg3ServiceKeyAuthDelegatingHandler(provider, new Xg3GatewayHandlerOptions
        {
            RewriteRequestHost = true,
            ClientBaseAddress = new Uri("http://localhost:5000/")
        })
        {
            InnerHandler = recorder
        };

        using var client = new HttpClient(handler);
        _output.WriteLine("Act: GET localhost URL");
        await client.GetAsync("http://localhost:5000/api/v1/health");

        var recorded = recorder.RecordedRequests.Single();
        _output.WriteLine($"Assert: uri={recorded.RequestUri}");
        Assert.Equal("https://acme--api.au.xg3.io/api/v1/health", recorded.RequestUri!.AbsoluteUri);
    }
}

public sealed class Xg3AuthServiceCollectionExtensionsTests
{
    private readonly ITestOutputHelper _output;

    public Xg3AuthServiceCollectionExtensionsTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void AddXg3JwtAuth_RegistersDistinctProviderType()
    {
        _output.WriteLine("Arrange: service collection with JWT options");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddXg3JwtAuth(o =>
        {
            o.Region = "au";
            o.ClientId = "client";
            o.ClientSecret = "secret";
            o.AccountId = "acme";
            o.ServiceId = "api";
        });

        _output.WriteLine("Act: build provider");
        var provider = services.BuildServiceProvider();

        _output.WriteLine("Assert: IXg3JwtAuthProvider resolved");
        Assert.IsAssignableFrom<IXg3JwtAuthProvider>(provider.GetRequiredService<IXg3JwtAuthProvider>());
        Assert.IsType<Xg3JwtAuthProvider>(provider.GetRequiredService<IXg3JwtAuthProvider>());
    }

    [Fact]
    public void AddXg3ServiceKeyAuth_RegistersDistinctProviderType()
    {
        _output.WriteLine("Arrange: service collection with service-key options");
        var services = new ServiceCollection();
        services.AddXg3ServiceKeyAuth(o =>
        {
            o.Region = "au";
            o.ServiceKey = "xg3_sk_keyid.secret";
            o.AccountId = "acme";
            o.ServiceId = "api";
        });

        _output.WriteLine("Act: build provider");
        var sp = services.BuildServiceProvider();

        _output.WriteLine("Assert: IXg3ServiceKeyAuthProvider resolved");
        Assert.IsAssignableFrom<IXg3ServiceKeyAuthProvider>(sp.GetRequiredService<IXg3ServiceKeyAuthProvider>());
        Assert.IsType<Xg3ServiceKeyAuthProvider>(sp.GetRequiredService<IXg3ServiceKeyAuthProvider>());
    }
}
