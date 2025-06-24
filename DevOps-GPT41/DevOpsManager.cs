using System.Text.Json;

namespace DevOps_GPT41;

public class DevOpsManager(IDateTimeProvider dateTimeProvider, IConnection connection, ConfigurationData configData)
{
    public async Task<string?> GenerateReleaseNotesJson()
    {
        var (previousDeployment, latestDeployment) = await connection.GetLastTwoProductionDeployments(configData.Project, "CD");

        if (latestDeployment == null)
        {
            return null;
        }

        var isRecent = latestDeployment.Value > dateTimeProvider.UtcNow.AddHours(-24);
        var from = isRecent ? previousDeployment.Value : latestDeployment.Value;
        DateTime? to = isRecent ? latestDeployment.Value : null;

        var prList = await connection.GetPullRequests(configData.Repo, from, to);

        return JsonSerializer.Serialize(prList, new JsonSerializerOptions { WriteIndented = true });
    }
}
