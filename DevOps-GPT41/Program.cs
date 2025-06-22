using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevOps_GPT41;

internal abstract class Program
{
    static async Task Main()
    {
        var host = CreateHostBuilder().Build();

        var devOpsManager = host.Services.GetRequiredService<DevOpsManager>();
        var result = await devOpsManager.GenerateReleaseNotesAsync();
        Console.WriteLine(result);
    }

    private static IHostBuilder CreateHostBuilder() =>
        Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                // Register dependencies
                services.AddSingleton<IConfigurationData, ConfigurationData>();
                services.AddSingleton<IDateTimeProvider, DefaultDateTimeProvider>();
                services.AddSingleton<IConnection>(provider =>
                {
                    var config = provider.GetRequiredService<IConfigurationData>();
                    return new Connection(config.Org, config.Pat);
                });
                services.AddSingleton<DevOpsManager>();
            });
}