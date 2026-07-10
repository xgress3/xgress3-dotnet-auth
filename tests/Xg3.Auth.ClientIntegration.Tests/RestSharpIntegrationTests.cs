using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using RestSharp;
using RestSharp.Serializers.Json;
using Xg3.Auth.Internal;
using Xunit;
using Xunit.Abstractions;

namespace Xg3.Auth.ClientIntegration.Tests;

public sealed class RestSharpIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public RestSharpIntegrationTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task JwtHandler_AddsAuthorizationHeaderOnly()
    {
        _output.WriteLine("Arrange: RestSharp client with JWT handler");
        var recorded = await SendAsync(useJwt: true);

        _output.WriteLine($"Assert: uri={recorded.RequestUri}");
        Assert.True(recorded.Headers.Contains(Xg3AuthConstants.JwtAuthorizationHeaderName));
        Assert.False(recorded.Headers.Contains(Xg3AuthConstants.ServiceKeyHeaderName));
    }

    [Fact]
    public async Task ServiceKeyHandler_AddsServiceKeyHeaderOnly()
    {
        _output.WriteLine("Arrange: RestSharp client with service-key handler");
        var recorded = await SendAsync(useJwt: false);

        _output.WriteLine($"Assert: uri={recorded.RequestUri}");
        Assert.True(recorded.Headers.Contains(Xg3AuthConstants.ServiceKeyHeaderName));
        Assert.False(recorded.Headers.Contains(Xg3AuthConstants.JwtAuthorizationHeaderName));
    }

    private static async Task<HttpRequestMessage> SendAsync(bool useJwt)
    {
        var recorder = new RecordingHandler { InnerHandler = new HttpClientHandler() };
        DelegatingHandler authHandler;

        if (useJwt)
        {
            var tokenStub = new StubHttpMessageHandler(_ =>
                StubHttpMessageHandler.JsonResponse(HttpStatusCode.OK, new { access_token = "jwt", expires_in = 3600 }));
            var provider = new Xg3JwtAuthProvider(JwtOptions(), new HttpClient(tokenStub), NullLogger<Xg3JwtAuthProvider>.Instance);
            authHandler = new Xg3JwtAuthDelegatingHandler(provider) { InnerHandler = recorder };
        }
        else
        {
            var provider = new Xg3ServiceKeyAuthProvider(ServiceKeyOptions());
            authHandler = new Xg3ServiceKeyAuthDelegatingHandler(provider) { InnerHandler = recorder };
        }

        var options = new RestClientOptions("https://acme--api.au.xg3.io/")
        {
            ConfigureMessageHandler = _ => authHandler
        };

        using var client = new RestClient(options, configureSerialization: s => s.UseSystemTextJson());
        var request = new RestRequest("health");
        await client.ExecuteAsync(request);
        return recorder.RecordedRequests.Single();
    }

    private static Xg3JwtAuthOptions JwtOptions() => new()
    {
        Region = "au",
        ClientId = "client",
        ClientSecret = "secret",
        AccountId = "acme",
        ServiceId = "api"
    };

    private static Xg3ServiceKeyAuthOptions ServiceKeyOptions() => new()
    {
        Region = "au",
        ServiceKey = "xg3_sk_keyid.secret",
        AccountId = "acme",
        ServiceId = "api"
    };
}
