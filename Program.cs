using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

Console.WriteLine("Connecting to Azure DevOps...");

// Load secrets from user secrets
var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

string personalAccessToken = configuration["pat"];
string organization = configuration["org"];
string project = configuration["project"];
string repository = configuration["repo"];

if (string.IsNullOrEmpty(personalAccessToken) || string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project) || string.IsNullOrEmpty(repository))
{
    Console.WriteLine("One or more required secrets (pat, org, project, repo) are missing.");
    return;
}

// Construct the Azure DevOps organization URL
string azureDevOpsUrl = $"https://dev.azure.com/{organization}";

// Create a connection to Azure DevOps
var credentials = new VssBasicCredential(string.Empty, personalAccessToken);
var connection = new VssConnection(new Uri(azureDevOpsUrl), credentials);

try
{
    // Get a GitHttpClient to interact with repositories
    var gitClient = connection.GetClient<GitHttpClient>();

    // Retrieve the repository details
    var repo = await gitClient.GetRepositoryAsync(project, repository);

    Console.WriteLine($"Connected successfully to repository '{repo.Name}' in project '{project}'!");
    Console.WriteLine($"Repository ID: {repo.Id}");
    Console.WriteLine($"Default Branch: {repo.DefaultBranch}");
    Console.WriteLine($"Remote URL: {repo.RemoteUrl}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error connecting to Azure DevOps: {ex.Message}");
}