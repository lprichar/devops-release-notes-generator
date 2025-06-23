using System.Text.Json;

namespace DevOps_GPT41;

public class DevOpsManager
{
    private readonly ConfigurationData _configData;
    private readonly Connection _connection;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DevOpsManager(ConfigurationData configData, Connection connection, IDateTimeProvider dateTimeProvider)
    {
        _configData = configData;
        _connection = connection;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<string> GetPullRequestsJsonAsync()
    {
        var (previousDeployment, latestDeployment) = await _connection.GetLastTwoProductionDeployments(_configData.Project, "CD");

        if (latestDeployment == null)
        {
            return "No successful and completed builds found.";
        }

        var targetRepo = await _connection.GetRepositoryByName(_configData.Repo);
        if (targetRepo == null)
        {
            throw new Exception("Repository not found.");
        }

        var prList = latestDeployment.Value > _dateTimeProvider.UtcNow.AddHours(-24)
            ? await _connection.GetPullRequests(targetRepo.Id, previousDeployment.Value, latestDeployment.Value)
            : await _connection.GetPullRequests(targetRepo.Id, latestDeployment.Value);

        return JsonSerializer.Serialize(prList, new JsonSerializerOptions { WriteIndented = true });
    }
}
