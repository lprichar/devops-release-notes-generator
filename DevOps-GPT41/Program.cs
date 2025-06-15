using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text.Json;

class Program
{
    static async Task Main(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .Build();

        // Retrieve values from configuration
        string repo = configuration["repo"] ?? throw new ArgumentNullException("repo", "Repository name is not configured.");
        string org = configuration["org"] ?? throw new ArgumentNullException("org", "Organization name is not configured.");
        string pat = configuration["pat"] ?? throw new ArgumentNullException("pat", "Personal Access Token is not configured.");

        Console.WriteLine($"Repository: {repo}, Organization: {org}");

        try
        {
            // Authenticate and retrieve repository data
            var repositoryData = await GetRepositoryDataAsync(org, repo, pat);
            Console.WriteLine(JsonSerializer.Serialize(repositoryData, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static async Task<object> GetRepositoryDataAsync(string organization, string repository, string personalAccessToken)
    {
        using var client = new HttpClient();

        // Set up authentication
        var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{personalAccessToken}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        // Azure DevOps REST API URL for repository
        string url = $"https://dev.azure.com/{organization}/_apis/git/repositories/{repository}?api-version=7.0";

        // Make the request
        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error Response: {errorContent}");
            response.EnsureSuccessStatusCode();
        }

        // Parse and return the response
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<object>(responseContent);
    }
}