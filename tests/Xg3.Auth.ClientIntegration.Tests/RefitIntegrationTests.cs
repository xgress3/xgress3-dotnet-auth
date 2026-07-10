using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Refit;
using Xg3.Auth.Internal;
using Xunit;
using Xunit.Abstractions;

namespace Xg3.Auth.ClientIntegration.Tests;

public interface IHealthApi
{
    [Get("/health")]
    Task<HttpResponseMessage> GetHealthAsync();
}

public sealed class RefitIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public RefitIntegrationTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task JwtHandler_AddsAuthorizationHeaderOnly()
    {
        _output.WriteLine("Arrange: Refit client with JWT handler and recording handler");
        var recorded = await SendWithPipelineAsync(useJwt: true);

        _output.WriteLine($"Assert: uri={recorded.RequestUri}");
        Assert.True(recorded.Headers.Contains(Xg3AuthConstants.JwtAuthorizationHeaderName));
        Assert.False(recorded.Headers.Contains(Xg3AuthConstants.ServiceKeyHeaderName));
    }

    [Fact]
    public async Task ServiceKeyHandler_AddsServiceKeyHeaderOnly()
    {
        _output.WriteLine("Arrange: Refit client with service-key handler and recording handler");
        var recorded = await SendWithPipelineAsync(useJwt: false);

        _output.WriteLine($"Assert: uri={recorded.RequestUri}");
        Assert.True(recorded.Headers.Contains(Xg3AuthConstants.ServiceKeyHeaderName));
        Assert.False(recorded.Headers.Contains(Xg3AuthConstants.JwtAuthorizationHeaderName));
    }

    private async Task<HttpRequestMessage> SendWithPipelineAsync(bool useJwt)
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

        using var httpClient = new HttpClient(authHandler) { BaseAddress = new Uri("https://acme--api.au.xg3.io/") };
        var api = RestService.For<IHealthApi>(httpClient);
        await api.GetHealthAsync();
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

internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
        _responder = responder;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(_responder(request));

    public static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, object body) =>
        new(statusCode)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json")
        };
}

internal sealed class RecordingHandler : DelegatingHandler
{
    public List<HttpRequestMessage> RecordedRequests { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        RecordedRequests.Add(clone);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
