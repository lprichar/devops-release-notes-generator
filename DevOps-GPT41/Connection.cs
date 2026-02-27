using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace DevOps_GPT41;

public record Pr(DateTime CompletionDate, string Title, string Body);

public interface IConnection
{
    Task<IEnumerable<GitRepository>> GetRepositoriesAsync();
    Task<List<Pr>> GetPullRequests(string repoName, DateTime from, DateTime? to = null);
    Task<(DateTime? Previous, DateTime? Latest)> GetLastTwoProductionDeployments(string project, string pipelineName);
}

public class Connection : IConnection
{
    private readonly VssConnection _vssConnection;

    public Connection(string org, string pat)
    {
        Uri orgUrl = new Uri($"https://dev.azure.com/{org}");
        VssCredentials credentials = new VssBasicCredential(string.Empty, pat);
        _vssConnection = new VssConnection(orgUrl, credentials);
    }

    public async Task<IEnumerable<GitRepository>> GetRepositoriesAsync()
    {
        var gitClient = _vssConnection.GetClient<GitHttpClient>();
        return await gitClient.GetRepositoriesAsync();
    }

    public async Task<GitRepository?> GetRepositoryByName(string repoName)
    {
        var gitClient = _vssConnection.GetClient<GitHttpClient>();
        var repositories = await gitClient.GetRepositoriesAsync();
        return repositories.FirstOrDefault(r => r.Name.Equals(repoName, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<IEnumerable<GitPullRequest>> GetPullRequestsAsync(Guid repositoryId, GitPullRequestSearchCriteria criteria)
    {
        var gitClient = _vssConnection.GetClient<GitHttpClient>();
        return await gitClient.GetPullRequestsAsync(repositoryId, criteria);
    }

    private async Task<List<BuildDefinitionReference>> GetDefinitionsAsync(string project)
    {
        var buildClient = _vssConnection.GetClient<BuildHttpClient>();
        return await buildClient.GetDefinitionsAsync(project);
    }

    private async Task<IEnumerable<Build>> GetBuildsAsync(string project, IEnumerable<int> definitions, BuildQueryOrder queryOrder, int top)
    {
        var buildClient = _vssConnection.GetClient<BuildHttpClient>();
        return await buildClient.GetBuildsAsync(project, definitions, queryOrder: queryOrder, top: top);
    }

    public async Task<List<Pr>> GetPullRequests(string repoName, DateTime from, DateTime? to = null)
    {
        var gitClient = _vssConnection.GetClient<GitHttpClient>();
        var repository = await GetRepositoryByName(repoName);
        if (repository == null)
            throw new Exception($"Repository '{repoName}' not found.");
        var repositoryId = repository.Id;
        var pullRequests = await GetPullRequestsAsync(
            repositoryId,
            new GitPullRequestSearchCriteria
            {
                Status = PullRequestStatus.All
            }
        );
        var filtered = pullRequests
            .Where(pr => pr.Status != PullRequestStatus.Abandoned)
            .Select(pr =>
            {
                var closedDate = pr.ClosedDate;
                var closedDateUtc = closedDate.Kind == DateTimeKind.Utc ? closedDate : TimeZoneInfo.ConvertTimeToUtc(closedDate);
                return new { pr.PullRequestId, closedDateUtc, pr.Title };
            })
            .Where(x => x.closedDateUtc > from && (to == null || x.closedDateUtc <= to))
            .OrderBy(x => x.closedDateUtc)
            .ToList();

        var result = new List<Pr>();
        foreach (var prInfo in filtered)
        {
            var fullPr = await gitClient.GetPullRequestAsync(repositoryId, prInfo.PullRequestId);
            result.Add(new Pr(
                prInfo.closedDateUtc,
                fullPr.Title ?? string.Empty,
                fullPr.Description ?? string.Empty));
        }
        return result;
    }

    public async Task<(DateTime? Previous, DateTime? Latest)> GetLastTwoProductionDeployments(string project, string pipelineName)
    {
        var definitions = await GetDefinitionsAsync(project);
        if (!definitions.Any())
        {
            Console.WriteLine($"Pipeline '{pipelineName}' not found.");
            return (null, null);
        }
        var definition = definitions.FirstOrDefault(d => d.Name.Equals(pipelineName, StringComparison.OrdinalIgnoreCase));
        if (definition == null)
        {
            Console.WriteLine($"Pipeline '{pipelineName}' not found.");
            return (null, null);
        }
        var builds = await GetBuildsAsync(
            project,
            new List<int> { definition.Id },
            BuildQueryOrder.StartTimeDescending,
            top: 100
        );
        var deployments = builds
            .Where(b => b is { Status: BuildStatus.Completed, Result: BuildResult.Succeeded })
            .OrderByDescending(b => b.StartTime)
            .Take(2)
            .ToList();
        return (deployments.Count == 2 ? deployments[1].StartTime : null, deployments.FirstOrDefault()?.StartTime);
    }
}