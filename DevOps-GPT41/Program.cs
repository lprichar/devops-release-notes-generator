using System;
using System.Text.Json;

namespace DevOps_GPT41;

internal abstract class Program
{
    static async Task Main()
    {
        var configData = new ConfigurationData();
        var connection = new Connection(configData.Org, configData.Pat);
        var dateTimeProvider = new DateTimeProvider();
        var devOpsManager = new DevOpsManager(configData, connection, dateTimeProvider);

        var json = await devOpsManager.GetPullRequestsJsonAsync();
        Console.WriteLine(json);
    }
}

public class DevOpsManager
{
    private readonly IConfigurationData _configData;
    private readonly IConnection _connection;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DevOpsManager(IConfigurationData configData, IConnection connection, IDateTimeProvider dateTimeProvider)
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

        var prList = latestDeployment.Value > _dateTimeProvider.GetUtcNow().AddHours(-24)
            ? await _connection.GetPullRequests(targetRepo.Id, previousDeployment.Value, latestDeployment.Value)
            : await _connection.GetPullRequests(targetRepo.Id, latestDeployment.Value);

        return JsonSerializer.Serialize(prList, new JsonSerializerOptions { WriteIndented = true });
    }
}

public class GitRepository
{
    public Guid Id { get; set; }
}

public interface IConnection
{
    Task<(DateTime? Previous, DateTime? Latest)> GetLastTwoProductionDeployments(string project, string pipelineName);
    Task<Microsoft.TeamFoundation.SourceControl.WebApi.GitRepository?> GetRepositoryByName(string repoName);
    Task<IEnumerable<Microsoft.TeamFoundation.SourceControl.WebApi.GitRepository>> GetRepositoriesAsync();
    Task<List<Pr>> GetPullRequests(Guid repositoryId, DateTime from, DateTime? to = null);
}