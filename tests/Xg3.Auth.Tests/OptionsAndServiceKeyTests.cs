using Microsoft.Extensions.Logging.Abstractions;
using Xg3.Auth.Internal;
using Xg3.Auth.Tests.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Xg3.Auth.Tests;

public sealed class Xg3JwtAuthOptionsTests
{
    private readonly ITestOutputHelper _output;

    public Xg3JwtAuthOptionsTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void Validate_ThrowsWhenClientIdMissing()
    {
        _output.WriteLine("Arrange: options with empty ClientId");
        var options = ValidOptions();
        options.ClientId = "";

        _output.WriteLine("Act/Assert: Validate throws");
        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate());
        _output.WriteLine($"Assert: message={ex.Message}");
        Assert.Contains("ClientId", ex.Message, StringComparison.Ordinal);
    }

    private static Xg3JwtAuthOptions ValidOptions() => new()
    {
        Region = "au",
        ClientId = "client",
        ClientSecret = "secret",
        AccountId = "acme",
        ServiceId = "api"
    };
}

public sealed class Xg3ServiceKeyAuthOptionsTests
{
    private readonly ITestOutputHelper _output;

    public Xg3ServiceKeyAuthOptionsTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void Validate_RejectsMissingPrefix()
    {
        _output.WriteLine("Arrange: service key without xg3_sk_ prefix");
        var options = ValidOptions();
        options.ServiceKey = "bad.key";

        _output.WriteLine("Act/Assert: Validate throws");
        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate());
        _output.WriteLine($"Assert: message={ex.Message}");
        Assert.Contains("xg3_sk_", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_RejectsMissingDotSeparator()
    {
        _output.WriteLine("Arrange: service key without dot separator");
        var options = ValidOptions();
        options.ServiceKey = "xg3_sk_abc123";

        _output.WriteLine("Act/Assert: Validate throws");
        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate());
        _output.WriteLine($"Assert: message={ex.Message}");
        Assert.Contains("'.'", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_AcceptsWellFormedKey()
    {
        _output.WriteLine("Arrange: well-formed service key");
        var options = ValidOptions();

        _output.WriteLine("Act: Validate");
        var exception = Record.Exception(() => options.Validate());

        _output.WriteLine("Assert: no exception");
        Assert.Null(exception);
    }

    private static Xg3ServiceKeyAuthOptions ValidOptions() => new()
    {
        Region = "au",
        ServiceKey = "xg3_sk_abc123.secretPart",
        AccountId = "acme",
        ServiceId = "api"
    };
}

public sealed class Xg3ServiceKeyAuthProviderTests
{
    private readonly ITestOutputHelper _output;

    public Xg3ServiceKeyAuthProviderTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void ServiceKeyHeader_ReturnsExpectedHeader()
    {
        _output.WriteLine("Arrange: service key provider");
        var provider = new Xg3ServiceKeyAuthProvider(new Xg3ServiceKeyAuthOptions
        {
            Region = "au",
            ServiceKey = "xg3_sk_keyid.secret",
            AccountId = "acme",
            ServiceId = "api"
        });

        _output.WriteLine("Act: read ServiceKeyHeader");
        var (name, value) = provider.ServiceKeyHeader;

        _output.WriteLine($"Assert: name={name}, value starts with xg3_sk_");
        Assert.Equal(Xg3AuthConstants.ServiceKeyHeaderName, name);
        Assert.Equal("xg3_sk_keyid.secret", value);
    }

    [Fact]
    public void ServiceBaseUrl_MatchesJwtProviderForSameTarget()
    {
        _output.WriteLine("Arrange: JWT and service-key providers with same account/service/region");
        var jwtOptions = new Xg3JwtAuthOptions
        {
            Region = "au",
            ClientId = "client",
            ClientSecret = "secret",
            AccountId = "acme",
            ServiceId = "api"
        };

        var keyProvider = new Xg3ServiceKeyAuthProvider(new Xg3ServiceKeyAuthOptions
        {
            Region = "au",
            ServiceKey = "xg3_sk_keyid.secret",
            AccountId = "acme",
            ServiceId = "api"
        });

        using var jwtProvider = new Xg3JwtAuthProvider(
            jwtOptions,
            new HttpClient(new StubHttpMessageHandler(_ => StubHttpMessageHandler.JsonResponse(
                System.Net.HttpStatusCode.OK,
                new { access_token = "tok", expires_in = 3600 }))),
            NullLogger<Xg3JwtAuthProvider>.Instance);

        _output.WriteLine("Act: compare ServiceBaseUrl");
        _output.WriteLine($"Assert: jwt={jwtProvider.ServiceBaseUrl}, key={keyProvider.ServiceBaseUrl}");
        Assert.Equal(jwtProvider.ServiceBaseUrl, keyProvider.ServiceBaseUrl);
    }
}
