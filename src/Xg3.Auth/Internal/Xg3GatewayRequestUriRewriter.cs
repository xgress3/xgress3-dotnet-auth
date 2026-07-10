namespace Xg3.Auth.Internal;

internal static class Xg3GatewayRequestUriRewriter
{
    internal static void RewriteIfNeeded(
        HttpRequestMessage request,
        Uri serviceBaseAddress,
        Uri? clientBaseAddress)
    {
        var effectivePathAndQuery = ResolvePathAndQuery(request.RequestUri, clientBaseAddress);
        var normalizedPath = effectivePathAndQuery.StartsWith('/')
            ? effectivePathAndQuery
            : "/" + effectivePathAndQuery;

        var builder = new UriBuilder(serviceBaseAddress)
        {
            Path = normalizedPath,
            Query = string.Empty,
            Fragment = string.Empty
        };

        if (effectivePathAndQuery.Contains('?', StringComparison.Ordinal))
        {
            var queryIndex = effectivePathAndQuery.IndexOf('?', StringComparison.Ordinal);
            builder.Path = effectivePathAndQuery[..queryIndex];
            builder.Query = effectivePathAndQuery[(queryIndex + 1)..];
        }

        if (request.RequestUri is not null
            && request.RequestUri.IsAbsoluteUri
            && !string.IsNullOrEmpty(request.RequestUri.Fragment))
        {
            builder.Fragment = request.RequestUri.Fragment.TrimStart('#');
        }

        request.RequestUri = builder.Uri;
    }

    private static string ResolvePathAndQuery(Uri? requestUri, Uri? clientBaseAddress)
    {
        if (requestUri is null)
        {
            return "/";
        }

        if (requestUri.IsAbsoluteUri)
        {
            return requestUri.PathAndQuery;
        }

        if (clientBaseAddress is not null)
        {
            return new Uri(clientBaseAddress, requestUri).PathAndQuery;
        }

        return requestUri.OriginalString.StartsWith('/')
            ? requestUri.OriginalString
            : "/" + requestUri.OriginalString;
    }
}
