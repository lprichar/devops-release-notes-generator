using System.Text.Json;

namespace DevOps_GPT41;

internal abstract class Program
{
    static async Task Main()
    {
        var configData = new ConfigurationData();
        var connection = new Connection(configData.Org, configData.Pat);
        var dateTimeProvider = new SystemDateTimeProvider();
        var manager = new DevOpsManager(configData, connection, dateTimeProvider);

        try
        {
            var result = await manager.GetPullRequestsJsonAsync();
            Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}