using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.SourceControl.WebApi;
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

if (string.IsNullOrEmpty(repo) || string.IsNullOrEmpty(org) || string.IsNullOrEmpty(pat))
{
    Console.WriteLine("Missing required configuration values: 'repo', 'org', or 'pat'.");
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