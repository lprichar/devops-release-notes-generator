using Microsoft.TeamFoundation.SourceControl.WebApi;
using System.Text.Json;

namespace DevOps_GPT41;

public class DevOpsManager
{
    private readonly IConnection _connection;
    private readonly IConfigurationData _configData;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DevOpsManager(
        IConnection connection,
        IConfigurationData configData,
        IDateTimeProvider dateTimeProvider)
    {
        _connection = connection;
        _configData = configData;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<string> GenerateReleaseNotesAsync(string pipelineName = "CD")
    {
        var (previousDeployment, latestDeployment) = await _connection.GetLastTwoProductionDeployments(_configData.Project, pipelineName);

        if (latestDeployment == null)
        {
            return "No successful and completed builds found.";
        }

        var targetRepo = await _connection.GetRepositoryByName(_configData.Repo);
        if (targetRepo == null)
        {
            throw new Exception("Repository not found.");
        }

        var prList = IsRecentDeployment(latestDeployment.Value)
            ? await _connection.GetPullRequests(targetRepo.Id, previousDeployment.Value, latestDeployment.Value)
            : await _connection.GetPullRequests(targetRepo.Id, latestDeployment.Value);

        return JsonSerializer.Serialize(prList, new JsonSerializerOptions { WriteIndented = true });
    }

    public bool IsRecentDeployment(DateTime deploymentTime)
    {
        return deploymentTime > _dateTimeProvider.UtcNow.AddHours(-24);
    }
}