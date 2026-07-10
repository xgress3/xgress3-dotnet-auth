using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xg3.Auth.Internal;

namespace Xg3.Auth;

/// <summary>
/// Dependency injection extensions for registering JWT (OAuth client-credentials) authentication
/// and its delegating handler.
/// </summary>
public static class Xg3JwtAuthServiceCollectionExtensions
{
    /// <summary>
    /// Registers JWT authentication, binding options from the <c>Xg3:JwtAuth</c> section of the supplied configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root or section containing the <c>Xg3:JwtAuth</c> section.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddXg3JwtAuth(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services.AddXg3JwtAuth(configuration.GetSection(Xg3JwtAuthOptions.SectionName));

    /// <summary>Registers JWT authentication, configuring options in code.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">A delegate that populates the options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddXg3JwtAuth(
        this IServiceCollection services,
        Action<Xg3JwtAuthOptions> configure)
    {
        services.AddOptions<Xg3JwtAuthOptions>()
            .Configure(configure)
            .PostConfigure(options => options.Validate());

        return AddXg3JwtAuthCore(services);
    }

    /// <summary>Registers JWT authentication, binding options from a specific configuration section.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationSection">The configuration section to bind.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddXg3JwtAuth(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        services.AddOptions<Xg3JwtAuthOptions>()
            .Bind(configurationSection)
            .PostConfigure(options => options.Validate());

        return AddXg3JwtAuthCore(services);
    }

    /// <summary>Adds the JWT delegating handler to a named or typed <see cref="HttpClient"/> registration.</summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <returns>The HTTP client builder for chaining.</returns>
    public static IHttpClientBuilder AddXg3JwtAuthHandler(this IHttpClientBuilder builder) =>
        builder.AddXg3JwtAuthHandler(_ => { });

    /// <summary>Adds the JWT delegating handler to a named or typed <see cref="HttpClient"/> registration.</summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <param name="configureHandler">A delegate that configures handler behavior, such as host rewriting.</param>
    /// <returns>The HTTP client builder for chaining.</returns>
    public static IHttpClientBuilder AddXg3JwtAuthHandler(
        this IHttpClientBuilder builder,
        Action<Xg3GatewayHandlerOptions> configureHandler)
    {
        builder.AddHttpMessageHandler(sp =>
        {
            var handlerOptions = new Xg3GatewayHandlerOptions();
            configureHandler(handlerOptions);
            return new Xg3JwtAuthDelegatingHandler(
                sp.GetRequiredService<IXg3JwtAuthProvider>(),
                handlerOptions);
        });

        return builder;
    }

    private static IServiceCollection AddXg3JwtAuthCore(IServiceCollection services)
    {
        services.AddHttpClient(Xg3AuthConstants.TokenHttpClientName);

        services.AddSingleton<Xg3JwtAuthProvider>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<Xg3JwtAuthOptions>>().Value;
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(Xg3AuthConstants.TokenHttpClientName);
            var logger = sp.GetRequiredService<ILogger<Xg3JwtAuthProvider>>();
            return new Xg3JwtAuthProvider(options, httpClient, logger);
        });

        services.AddSingleton<IXg3JwtAuthProvider>(sp => sp.GetRequiredService<Xg3JwtAuthProvider>());
        return services;
    }
}
