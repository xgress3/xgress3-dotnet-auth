using Xg3.Auth.Internal;
using Xg3.Auth.Tests.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Xg3.Auth.Tests;

public sealed class Xg3AuthScopeTests
{
    private readonly ITestOutputHelper _output;

    public Xg3AuthScopeTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void Format_ReturnsExpectedScopeString()
    {
        _output.WriteLine("Arrange: accountId=acme, serviceId=api");
        const string accountId = "acme";
        const string serviceId = "api";

        _output.WriteLine("Act: format scope");
        var scope = Xg3AuthScope.Format(accountId, serviceId);

        _output.WriteLine($"Assert: scope={scope}");
        Assert.Equal("account:acme service:api", scope);
    }
}

public sealed class Xg3AuthEndpointsTests
{
    private readonly ITestOutputHelper _output;

    public Xg3AuthEndpointsTests(ITestOutputHelper output) => _output = output;

    [Theory]
    [InlineData("au", "xg3.io", "au.xg3.io")]
    [InlineData("staging", "xg3.io", "staging.xg3.io")]
    [InlineData("au", "au.xg3.io", "au.xg3.io")]
    public void ResolveRegionalApexHost_ReturnsNormalizedHost(string region, string suffix, string expected)
    {
        _output.WriteLine($"Arrange: region={region}, suffix={suffix}");
        _output.WriteLine("Act: resolve regional apex host");
        var host = Xg3AuthEndpoints.ResolveRegionalApexHost(region, suffix);
        _output.WriteLine($"Assert: host={host}");
        Assert.Equal(expected, host);
    }

    [Fact]
    public void ResolveServiceBaseAddress_ReturnsHttpsServiceUrl()
    {
        _output.WriteLine("Arrange: acme/api in au region");
        _output.WriteLine("Act: resolve service base address");
        var uri = Xg3AuthEndpoints.ResolveServiceBaseAddress("acme", "api", "au");
        _output.WriteLine($"Assert: uri={uri}");
        Assert.Equal("https://acme--api.au.xg3.io/", uri.AbsoluteUri);
    }

    [Fact]
    public void ResolveTokenEndpoint_UsesRegionalApex()
    {
        _output.WriteLine("Arrange: au region");
        _output.WriteLine("Act: resolve token endpoint");
        var uri = Xg3AuthEndpoints.ResolveTokenEndpoint("au");
        _output.WriteLine($"Assert: uri={uri}");
        Assert.Equal("https://au.xg3.io/oauth/token", uri.AbsoluteUri);
    }
}

public sealed class Xg3GatewayRequestUriRewriterTests
{
    private readonly ITestOutputHelper _output;

    public Xg3GatewayRequestUriRewriterTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void RewriteIfNeeded_ReplacesLocalhostAuthorityWithServiceBase()
    {
        _output.WriteLine("Arrange: localhost absolute URI and service base");
        var serviceBase = new Uri("https://acme--api.au.xg3.io/");
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5000/api/v1/health?x=1");

        _output.WriteLine("Act: rewrite request host");
        Xg3GatewayRequestUriRewriter.RewriteIfNeeded(request, serviceBase, clientBaseAddress: null);

        _output.WriteLine($"Assert: uri={request.RequestUri}");
        Assert.Equal("https://acme--api.au.xg3.io/api/v1/health?x=1", request.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public void RewriteIfNeeded_ResolvesRelativeUriAgainstClientBaseAddress()
    {
        _output.WriteLine("Arrange: relative path and client base address");
        var serviceBase = new Uri("https://acme--api.au.xg3.io/");
        var clientBase = new Uri("http://localhost:5000/api/");
        var request = new HttpRequestMessage(HttpMethod.Get, "v1/health");

        _output.WriteLine("Act: rewrite request host");
        Xg3GatewayRequestUriRewriter.RewriteIfNeeded(request, serviceBase, clientBase);

        _output.WriteLine($"Assert: uri={request.RequestUri}");
        Assert.Equal("https://acme--api.au.xg3.io/api/v1/health", request.RequestUri!.AbsoluteUri);
    }
}
