using System.Text.Json;

namespace DevOps_GPT41;

internal abstract class Program
{
    static async Task Main()
    {
        var configData = new ConfigurationData();
        var connection = new Connection(configData.Org, configData.Pat);
        var (previousDeployment, latestDeployment) = await connection.GetLastTwoProductionDeployments(configData.Project, "CD");

        if (previousDeployment == null || latestDeployment == null)
        {
            Console.WriteLine("Fewer than two successful and completed builds found.");
            return;
        }

        var targetRepo = await connection.GetRepositoryByName(configData.Repo);
        if (targetRepo == null)
        {
            throw new Exception("Repository not found.");
        }

        var prList = await connection.GetPullRequestsBetween(targetRepo.Id, previousDeployment.Value, latestDeployment.Value);
        var json = JsonSerializer.Serialize(prList, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine(json);
    }
}