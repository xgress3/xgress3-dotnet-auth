using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xg3.Auth;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var services = new ServiceCollection();

// Configure via IOptions binding from appsettings.json (recommended for ASP.NET Core hosts):
services.AddXg3ServiceKeyAuth(configuration.GetSection(Xg3ServiceKeyAuthOptions.SectionName));

// Alternative without options binding — configure in code instead:
// services.AddXg3ServiceKeyAuth(o =>
// {
//     o.Region = "au";
//     o.ServiceKey = "xg3_sk_keyid.secret";
//     o.AccountId = "acme";
//     o.ServiceId = "api";
// });

services.AddHttpClient("api")
    .AddXg3ServiceKeyAuthHandler()
    .ConfigureHttpClient((sp, client) =>
        client.BaseAddress = sp.GetRequiredService<IXg3ServiceKeyAuthProvider>().ServiceBaseAddress);

using var provider = services.BuildServiceProvider();
var keyProvider = provider.GetRequiredService<IXg3ServiceKeyAuthProvider>();
Console.WriteLine($"Service key (DI + config binding): {keyProvider.ServiceBaseUrl}");
