namespace Xg3.Auth;

/// <summary>Options that control how an xgress3 delegating handler rewrites outgoing requests.</summary>
public sealed class Xg3GatewayHandlerOptions
{
    /// <summary>
    /// When <see langword="true"/>, the handler replaces the authority (scheme, host, and port) of each
    /// outgoing request with the configured service base address, preserving the path, query, and fragment.
    /// This lets an application address a local base URL during development while still routing through the gateway.
    /// </summary>
    public bool RewriteRequestHost { get; set; }

    /// <summary>
    /// The <see cref="System.Net.Http.HttpClient"/> base address used to resolve relative request URIs
    /// before rewriting. Only required when <see cref="RewriteRequestHost"/> is enabled and requests use relative URIs.
    /// </summary>
    public Uri? ClientBaseAddress { get; set; }
}
