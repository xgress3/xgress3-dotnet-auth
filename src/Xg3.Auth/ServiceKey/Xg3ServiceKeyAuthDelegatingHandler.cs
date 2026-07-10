using Xg3.Auth.Internal;

namespace Xg3.Auth;

/// <summary>
/// A <see cref="DelegatingHandler"/> that attaches the service-key header to each outgoing request
/// and optionally rewrites the request host.
/// </summary>
public sealed class Xg3ServiceKeyAuthDelegatingHandler : DelegatingHandler
{
    private readonly IXg3ServiceKeyAuthProvider _provider;
    private readonly Xg3GatewayHandlerOptions _handlerOptions;

    /// <summary>Initializes a new instance of the <see cref="Xg3ServiceKeyAuthDelegatingHandler"/> class.</summary>
    /// <param name="provider">The provider that supplies the service key.</param>
    /// <param name="handlerOptions">Optional handler behavior, such as host rewriting.</param>
    public Xg3ServiceKeyAuthDelegatingHandler(
        IXg3ServiceKeyAuthProvider provider,
        Xg3GatewayHandlerOptions? handlerOptions = null)
    {
        _provider = provider;
        _handlerOptions = handlerOptions ?? new Xg3GatewayHandlerOptions();
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_handlerOptions.RewriteRequestHost)
        {
            Xg3GatewayRequestUriRewriter.RewriteIfNeeded(
                request,
                _provider.ServiceBaseAddress,
                _handlerOptions.ClientBaseAddress);
        }

        var (name, value) = _provider.ServiceKeyHeader;
        if (!request.Headers.Contains(name))
        {
            request.Headers.TryAddWithoutValidation(name, value);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
