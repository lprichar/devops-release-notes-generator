using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var pat = config["pat"];
var org = config["org"];
var repoName = config["repo"];

if (string.IsNullOrWhiteSpace(pat))
{
    Console.WriteLine("No PAT found in user-secrets under the key 'pat'.");
    return;
}
if (string.IsNullOrWhiteSpace(org))
{
    Console.WriteLine("No organization found in user-secrets under the key 'org'.");
    return;
}
if (string.IsNullOrWhiteSpace(repoName))
{
    Console.WriteLine("No repository name found in user-secrets under the key 'repo'.");
    return;
}

var organizationUrl = $"https://dev.azure.com/{org}";

var connection = new VssConnection(new Uri(organizationUrl), new VssBasicCredential(string.Empty, pat));

try
{
    var gitClient = connection.GetClient<GitHttpClient>();
    var repo = await gitClient.GetRepositoryAsync(repoName);

    Console.WriteLine($"Repository info for '{repoName}':");
    Console.WriteLine($"- Name: {repo.Name}");
    Console.WriteLine($"- Id: {repo.Id}");
    Console.WriteLine($"- Default branch: {repo.DefaultBranch}");
    Console.WriteLine($"- Remote URL: {repo.RemoteUrl}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error connecting to Azure DevOps: {ex.Message}");
}