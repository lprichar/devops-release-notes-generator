using Azure.Identity;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.Extensions.Configuration;

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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}