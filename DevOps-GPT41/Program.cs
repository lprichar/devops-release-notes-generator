using System.Text.Json;
using System.Threading.Tasks;

namespace DevOps_GPT41;

internal abstract class Program
{
    static async Task Main()
    {
        var configData = new ConfigurationData();
        var connection = new Connection(configData.Org, configData.Pat);
        var dateTimeProvider = new UtcDateTimeProvider();
        var manager = new DevOpsManager(dateTimeProvider, connection, configData);
        var yaml = await manager.GenerateReleaseNotesYaml();
        if (yaml == null)
        {
            Console.WriteLine("No successful and completed builds found.");
            return;
        }
        Console.WriteLine(yaml);
    }
}