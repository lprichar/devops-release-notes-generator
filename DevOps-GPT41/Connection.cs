using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace DevOps_GPT41;

public class Connection
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

    public async Task<IEnumerable<GitPullRequest>> GetPullRequestsBetween(Guid repositoryId, DateTime from, DateTime to)
    {
        var pullRequests = await GetPullRequestsAsync(
            repositoryId,
            new GitPullRequestSearchCriteria
            {
                Status = PullRequestStatus.All
            }
        );
        return pullRequests.Where(pr =>
        {
            if (pr.Status == PullRequestStatus.Abandoned)
                return false;
            // ClosedDate is non-nullable, so just use it directly
            var closedDate = pr.ClosedDate;
            var closedDateUtc = closedDate.Kind == DateTimeKind.Utc ? closedDate : TimeZoneInfo.ConvertTimeToUtc(closedDate);
            return closedDateUtc > from && closedDateUtc <= to;
        });
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