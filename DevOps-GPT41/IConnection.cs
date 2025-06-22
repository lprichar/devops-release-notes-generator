using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace DevOps_GPT41;

public interface IConnection
{
    Task<IEnumerable<GitRepository>> GetRepositoriesAsync();
    Task<GitRepository?> GetRepositoryByName(string repoName);
    Task<List<Pr>> GetPullRequests(Guid repositoryId, DateTime from, DateTime? to = null);
    Task<(DateTime? Previous, DateTime? Latest)> GetLastTwoProductionDeployments(string project, string pipelineName);
}