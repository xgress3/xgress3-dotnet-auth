using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Xg3.Auth.Internal;
using Xg3.Auth.Tests.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Xg3.Auth.Tests;

public sealed class Xg3JwtAuthProviderTests
{
    private readonly ITestOutputHelper _output;

    public Xg3JwtAuthProviderTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task AccessToken_AfterMint_ReturnsBearerToken()
    {
        _output.WriteLine("Arrange: stub token endpoint returning access_token and expires_in=3600");
        var stub = new StubHttpMessageHandler(_ =>
            StubHttpMessageHandler.JsonResponse(HttpStatusCode.OK, new { access_token = "jwt-token", expires_in = 3600 }));
        var options = CreateOptions();

        _output.WriteLine("Act: construct provider and read AccessToken");
        using var provider = new Xg3JwtAuthProvider(options, new HttpClient(stub), NullLogger<Xg3JwtAuthProvider>.Instance);
        await provider.EnsureValidTokenAsync();
        var token = provider.AccessToken;

        _output.WriteLine($"Assert: token non-empty; header={provider.AuthorizationHeader.Name}");
        Assert.Equal("jwt-token", token);
        Assert.Equal(Xg3AuthConstants.JwtAuthorizationHeaderName, provider.AuthorizationHeader.Name);
        Assert.Equal("Bearer jwt-token", provider.AuthorizationHeader.Value);
    }

    [Fact]
    public async Task EnsureValidTokenAsync_RefreshesWhenNearExpiry()
    {
        _output.WriteLine("Arrange: clock and stub that returns sequential tokens");
        var clock = new FakeSystemClock { UtcNow = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var mintCount = 0;
        var stub = new StubHttpMessageHandler(_ =>
        {
            mintCount++;
            return StubHttpMessageHandler.JsonResponse(
                HttpStatusCode.OK,
                new { access_token = $"token-{mintCount}", expires_in = 3600 });
        });

        var options = CreateOptions();
        options.RefreshBeforeExpiry = TimeSpan.FromMinutes(6);

        using var provider = new Xg3JwtAuthProvider(
            options,
            new HttpClient(stub),
            ownsHttpClient: true,
            NullLogger<Xg3JwtAuthProvider>.Instance,
            clock);

        _output.WriteLine("Act: initial mint then advance clock past refresh threshold");
        await provider.EnsureValidTokenAsync();
        clock.Advance(TimeSpan.FromMinutes(55));
        await provider.EnsureValidTokenAsync();

        _output.WriteLine($"Assert: mintCount={mintCount}, token={provider.AccessToken}");
        Assert.Equal(2, mintCount);
        Assert.Equal("token-2", provider.AccessToken);
    }

    [Fact]
    public async Task MintTokenAsync_On429_RetriesAfterRetryAfter()
    {
        _output.WriteLine("Arrange: first response 429 with Retry-After, then 200");
        var attempts = 0;
        var stub = new StubHttpMessageHandler(_ =>
        {
            attempts++;
            if (attempts == 1)
            {
                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromMilliseconds(10));
                return response;
            }

            return StubHttpMessageHandler.JsonResponse(HttpStatusCode.OK, new { access_token = "after-retry", expires_in = 3600 });
        });

        using var provider = new Xg3JwtAuthProvider(CreateOptions(), new HttpClient(stub), NullLogger<Xg3JwtAuthProvider>.Instance);

        _output.WriteLine("Act: mint token");
        await provider.EnsureValidTokenAsync();

        _output.WriteLine($"Assert: attempts={attempts}, token={provider.AccessToken}");
        Assert.Equal(2, attempts);
        Assert.Equal("after-retry", provider.AccessToken);
    }

    [Fact]
    public async Task MintTokenAsync_OnFailure_ServesStaleTokenUntilExpired()
    {
        _output.WriteLine("Arrange: successful mint then failing refresh while token still valid");
        var clock = new FakeSystemClock { UtcNow = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var attempts = 0;
        var stub = new StubHttpMessageHandler(_ =>
        {
            attempts++;
            if (attempts == 1)
            {
                return StubHttpMessageHandler.JsonResponse(HttpStatusCode.OK, new { access_token = "stale-ok", expires_in = 3600 });
            }

            return StubHttpMessageHandler.JsonResponse(HttpStatusCode.BadRequest, new { error = "invalid_client" });
        });

        var options = CreateOptions();
        options.RefreshBeforeExpiry = TimeSpan.FromMinutes(6);

        using var provider = new Xg3JwtAuthProvider(
            options,
            new HttpClient(stub),
            ownsHttpClient: true,
            NullLogger<Xg3JwtAuthProvider>.Instance,
            clock);

        await provider.EnsureValidTokenAsync();
        clock.Advance(TimeSpan.FromMinutes(55));

        _output.WriteLine("Act: refresh fails but stale token still within expiry");
        var failedArgs = (Xg3TokenRefreshFailedEventArgs?)null;
        provider.TokenRefreshFailed += (_, e) => failedArgs = e;
        await provider.EnsureValidTokenAsync();
        provider.Dispose();

        _output.WriteLine($"Assert: stale served, attempts={attempts}");
        Assert.Equal(2, attempts);
        Assert.NotNull(failedArgs);
        Assert.True(failedArgs!.ServingStaleToken);
    }

    [Fact]
    public async Task TokenRefreshed_EventFiresOnSuccessfulMint()
    {
        _output.WriteLine("Arrange: provider with event handler");
        var stub = new StubHttpMessageHandler(_ =>
            StubHttpMessageHandler.JsonResponse(HttpStatusCode.OK, new { access_token = "evt", expires_in = 3600 }));
        using var provider = new Xg3JwtAuthProvider(CreateOptions(), new HttpClient(stub), NullLogger<Xg3JwtAuthProvider>.Instance);
        Xg3TokenRefreshedEventArgs? args = null;
        provider.TokenRefreshed += (_, e) => args = e;

        _output.WriteLine("Act: mint");
        await provider.EnsureValidTokenAsync();

        _output.WriteLine($"Assert: event fired, token={args?.AccessToken}");
        Assert.NotNull(args);
        Assert.Equal("evt", args!.AccessToken);
    }

    private static Xg3JwtAuthOptions CreateOptions() => new()
    {
        Region = "au",
        ClientId = "client-id",
        ClientSecret = "client-secret",
        AccountId = "acme",
        ServiceId = "api"
    };
}
