using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

// Load configuration
var builder = new ConfigurationBuilder()
    .AddUserSecrets<Program>();
var configuration = builder.Build();

// Retrieve configuration values
string? repo = configuration["repo"];
string? org = configuration["org"];
string? pat = configuration["pat"];
string? project = configuration["project"];

if (string.IsNullOrEmpty(repo) || string.IsNullOrEmpty(org) || string.IsNullOrEmpty(pat) || string.IsNullOrEmpty(project))
{
    Console.WriteLine("Missing required configuration values: 'repo', 'org', 'pat', or 'project'.");
    return;
}

// Authenticate with Azure DevOps
Uri orgUrl = new Uri($"https://dev.azure.com/{org}");
VssCredentials credentials = new VssBasicCredential(string.Empty, pat);
using var connection = new VssConnection(orgUrl, credentials);

// Retrieve repository data
var gitClient = connection.GetClient<GitHttpClient>();
var repositories = await gitClient.GetRepositoriesAsync();
var targetRepo = repositories.FirstOrDefault(r => r.Name.Equals(repo, StringComparison.OrdinalIgnoreCase));

if (targetRepo != null)
{
    Console.WriteLine($"Repository found: {targetRepo.Name}");
    Console.WriteLine($"ID: {targetRepo.Id}");
    Console.WriteLine($"URL: {targetRepo.RemoteUrl}");
}
else
{
    Console.WriteLine("Repository not found.");
}

// Retrieve Build Data
var buildClient = connection.GetClient<BuildHttpClient>();

var definitions = await buildClient.GetDefinitionsAsync(project: project);

if (definitions == null || definitions.Count == 0)
{
    Console.WriteLine("Pipeline 'CD' not found.");
    return;
}

var definition = definitions.FirstOrDefault(d => d.Name.Equals("CD", StringComparison.OrdinalIgnoreCase));

if (definition == null)
{
    Console.WriteLine("Pipeline 'CD' not found.");
    return;
}

var definitionId = definition.Id;
var builds = await buildClient.GetBuildsAsync(
    project: project,
    definitions: new List<int> { definitionId },
    queryOrder: BuildQueryOrder.StartTimeDescending,
    top: 10
);

// Find the First Successful and Completed Build
var firstSuccessfulBuild = builds.FirstOrDefault(b => b.Status == BuildStatus.Completed && b.Result == BuildResult.Succeeded);
if (firstSuccessfulBuild != null)
{
    Console.WriteLine("First Successful and Completed Build:");
    Console.WriteLine($"Build ID: {firstSuccessfulBuild.Id}, Status: {firstSuccessfulBuild.Status}, Result: {firstSuccessfulBuild.Result}, Completed: {firstSuccessfulBuild.FinishTime}");
}
else
{
    Console.WriteLine("No successful and completed builds found.");
}