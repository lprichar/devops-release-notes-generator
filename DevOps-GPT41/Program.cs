namespace DevOps_GPT41;

internal abstract class Program
{
    static async Task Main()
    {
        var configData = new ConfigurationData();

        var connection = new Connection(configData.Org, configData.Pat);
        var lastProductionDeploymentStartTime = await connection.GetLastProductionDeployment(configData.Project, "CD");

        if (lastProductionDeploymentStartTime == null)
        {
            Console.WriteLine("No successful and completed builds found.");
            return;
        }

        var targetRepo = await connection.GetRepositoryByName(configData.Repo);

        if (targetRepo == null)
        {
            throw new Exception("Repository not found.");
        }

        var prs = await connection.GetPullRequestsSince(targetRepo.Id, lastProductionDeploymentStartTime.Value);

        Console.WriteLine("Pull Requests Created After Last Production Deployment Start Time:");
        foreach (var pr in prs)
        {
            Console.WriteLine($"PR ID: {pr.PullRequestId}, Title: {pr.Title}, Created: {pr.CreationDate}");
        }
    }
}