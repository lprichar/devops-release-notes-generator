using System.Text.Json;

namespace DevOps_GPT41;

public class DevOpsManager(IDateTimeProvider dateTimeProvider, Connection connection, ConfigurationData configData)
{
    public async Task<string?> GenerateReleaseNotesJson()
    {
        var (previousDeployment, latestDeployment) = await connection.GetLastTwoProductionDeployments(configData.Project, "CD");

        if (latestDeployment == null)
        {
            return null;
        }

        var targetRepo = await connection.GetRepositoryByName(configData.Repo);
        if (targetRepo == null)
        {
            throw new Exception("Repository not found.");
        }

        var prList = latestDeployment.Value > dateTimeProvider.UtcNow.AddHours(-24)
            ? await connection.GetPullRequests(targetRepo.Id, previousDeployment.Value, latestDeployment.Value)
            : await connection.GetPullRequests(targetRepo.Id, latestDeployment.Value);

        return JsonSerializer.Serialize(prList, new JsonSerializerOptions { WriteIndented = true });
    }
}
