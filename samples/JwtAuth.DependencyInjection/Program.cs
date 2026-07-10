using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xg3.Auth;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var services = new ServiceCollection();
services.AddLogging();

// Configure via IOptions binding from appsettings.json (recommended for ASP.NET Core hosts):
services.AddXg3JwtAuth(configuration.GetSection(Xg3JwtAuthOptions.SectionName));

// Alternative without options binding — configure in code instead:
// services.AddXg3JwtAuth(o =>
// {
//     o.Region = "au";
//     o.ClientId = "client";
//     o.ClientSecret = "secret";
//     o.AccountId = "acme";
//     o.ServiceId = "api";
// });

services.AddHttpClient("api")
    .AddXg3JwtAuthHandler()
    .ConfigureHttpClient((sp, client) =>
        client.BaseAddress = sp.GetRequiredService<IXg3JwtAuthProvider>().ServiceBaseAddress);

using var provider = services.BuildServiceProvider();
var jwtProvider = provider.GetRequiredService<IXg3JwtAuthProvider>();
Console.WriteLine($"JWT (DI + config binding): {jwtProvider.ServiceBaseUrl}");
