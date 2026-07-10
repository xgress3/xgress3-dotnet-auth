using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Xg3.Auth;

/// <summary>
/// Dependency injection extensions for registering static service-key authentication
/// and its delegating handler.
/// </summary>
public static class Xg3ServiceKeyAuthServiceCollectionExtensions
{
    /// <summary>
    /// Registers service-key authentication, binding options from the <c>Xg3:ServiceKeyAuth</c> section of the supplied configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root or section containing the <c>Xg3:ServiceKeyAuth</c> section.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddXg3ServiceKeyAuth(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services.AddXg3ServiceKeyAuth(configuration.GetSection(Xg3ServiceKeyAuthOptions.SectionName));

    /// <summary>Registers service-key authentication, configuring options in code.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">A delegate that populates the options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddXg3ServiceKeyAuth(
        this IServiceCollection services,
        Action<Xg3ServiceKeyAuthOptions> configure)
    {
        services.AddOptions<Xg3ServiceKeyAuthOptions>()
            .Configure(configure)
            .PostConfigure(options => options.Validate());

        return AddXg3ServiceKeyAuthCore(services);
    }

    /// <summary>Registers service-key authentication, binding options from a specific configuration section.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationSection">The configuration section to bind.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddXg3ServiceKeyAuth(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        services.AddOptions<Xg3ServiceKeyAuthOptions>()
            .Bind(configurationSection)
            .PostConfigure(options => options.Validate());

        return AddXg3ServiceKeyAuthCore(services);
    }

    /// <summary>Adds the service-key delegating handler to a named or typed <see cref="HttpClient"/> registration.</summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <returns>The HTTP client builder for chaining.</returns>
    public static IHttpClientBuilder AddXg3ServiceKeyAuthHandler(this IHttpClientBuilder builder) =>
        builder.AddXg3ServiceKeyAuthHandler(_ => { });

    /// <summary>Adds the service-key delegating handler to a named or typed <see cref="HttpClient"/> registration.</summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <param name="configureHandler">A delegate that configures handler behavior, such as host rewriting.</param>
    /// <returns>The HTTP client builder for chaining.</returns>
    public static IHttpClientBuilder AddXg3ServiceKeyAuthHandler(
        this IHttpClientBuilder builder,
        Action<Xg3GatewayHandlerOptions> configureHandler)
    {
        builder.AddHttpMessageHandler(sp =>
        {
            var handlerOptions = new Xg3GatewayHandlerOptions();
            configureHandler(handlerOptions);
            return new Xg3ServiceKeyAuthDelegatingHandler(
                sp.GetRequiredService<IXg3ServiceKeyAuthProvider>(),
                handlerOptions);
        });

        return builder;
    }

    private static IServiceCollection AddXg3ServiceKeyAuthCore(IServiceCollection services)
    {
        services.AddSingleton<Xg3ServiceKeyAuthProvider>(sp =>
            new Xg3ServiceKeyAuthProvider(sp.GetRequiredService<IOptions<Xg3ServiceKeyAuthOptions>>().Value));
        services.AddSingleton<IXg3ServiceKeyAuthProvider>(sp => sp.GetRequiredService<Xg3ServiceKeyAuthProvider>());
        return services;
    }
}
