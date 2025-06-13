using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System.Net;

namespace AzureDevOpsSample
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Build configuration
            IConfiguration configuration = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .Build();

            // Retrieve values from user secrets
            string pat = configuration["pat"];
            string organizationUrl = $"https://dev.azure.com/{configuration["org"]}";
            string projectName = configuration["project"];
            string repoName = configuration["repo"];

            try
            {
                // Use personal access token for authentication
                VssBasicCredential credentials = new(new NetworkCredential(string.Empty, pat));

                // Connect to Azure DevOps
                var connection = new VssConnection(new Uri(organizationUrl), credentials);

                // Get a Git client
                using (var gitClient = connection.GetClient<GitHttpClient>())
                {
                    // Get project id
                    TeamProjectReference project = await connection.GetClient<ProjectHttpClient>().GetProject(projectName);
                    Guid projectId = project.Id;

                    // Get repositories
                    var repositories = await gitClient.GetRepositoriesAsync(projectId);

                    Console.WriteLine("Repositories:");
                    foreach (var repo in repositories)
                    {
                        Console.WriteLine($"- {repo.Name}");
                    }

                    // Find the repository by name
                    var repository = repositories.FirstOrDefault(r => r.Name.Equals(repoName, StringComparison.OrdinalIgnoreCase));
                    if (repository != null)
                    {
                        // Get the build client
                        using (var buildClient = connection.GetClient<BuildHttpClient>())
                        {
                            // Define build query parameters
                                var buildDefinitions = await buildClient.GetDefinitionsAsync(projectName, repositoryId: repository.Id);
                            if (buildDefinitions.Any())
                            {
                                var buildDefinition = buildDefinitions.First();

                                // Retrieve the 2 most recent builds
                                var builds = await buildClient.GetBuildsAsync(projectId, definitions: new[] { buildDefinition.Id }, top: 2);

                                Console.WriteLine($"\nRecent builds for repository '{repoName}':");
                                foreach (var build in builds)
                                {
                                    Console.WriteLine($"- Build #{build.BuildNumber}: Status = {build.Status}, Result = {build.Result}, Started = {build.StartTime}, Finished = {build.FinishTime}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"No build definitions found for repository '{repoName}'.");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Repository '{repoName}' not found.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}