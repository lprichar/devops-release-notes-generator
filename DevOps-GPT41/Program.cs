using System.Text.Json;

namespace DevOps_GPT41;

internal abstract class Program
{
    static async Task Main()
    {
        var configData = new ConfigurationData();
        var connection = new Connection(configData.Org, configData.Pat);
        var (previousDeployment, latestDeployment) = await connection.GetLastTwoProductionDeployments(configData.Project, "CD");

        if (latestDeployment == null)
        {
            Console.WriteLine("No successful and completed builds found.");
            return;
        }

        var targetRepo = await connection.GetRepositoryByName(configData.Repo);
        if (targetRepo == null)
        {
            throw new Exception("Repository not found.");
        }

        var prList = latestDeployment.Value > DateTime.UtcNow.AddHours(-24)
            ? await connection.GetPullRequests(targetRepo.Id, previousDeployment.Value, latestDeployment.Value)
            : await connection.GetPullRequests(targetRepo.Id, latestDeployment.Value);

        var json = JsonSerializer.Serialize(prList, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine(json);
    }
}