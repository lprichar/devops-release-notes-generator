using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

Console.WriteLine("Azure DevOps API Client Example");
Console.WriteLine("===============================");

var host = CreateHostBuilder(args).Build();
await RunAzureDevOpsClientAsync(host.Services);

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((_, config) =>
        {
            config.AddUserSecrets<Program>();
        })
        .ConfigureServices((_, services) =>
        {
            services.AddTransient<IAzureDevOpsClient, AzureDevOpsClient>();
        });

static async Task RunAzureDevOpsClientAsync(IServiceProvider services)
{
    var client = services.GetRequiredService<IAzureDevOpsClient>();
    var config = services.GetRequiredService<IConfiguration>();
    
    // Display configuration values (PAT is masked for security)
    Console.WriteLine("Configuration:");
    Console.WriteLine($"- Organization: {config["org"] ?? "Not set"}");
    Console.WriteLine($"- Project: {config["project"] ?? "Not set"}");
    Console.WriteLine($"- Repository: {config["repo"] ?? "Not set"}");
    Console.WriteLine($"- PAT: {(string.IsNullOrEmpty(config["pat"]) ? "Not set" : "******")}");
    Console.WriteLine();
    
    try
    {
        Console.WriteLine("Connecting to Azure DevOps...");
        await client.ConnectAsync();
        
        Console.WriteLine("Connected successfully!");
        Console.WriteLine("Fetching projects...");
        
        var projects = await client.GetProjectsAsync();
        
        Console.WriteLine($"Found {projects.Count} projects:");
        foreach (var project in projects)
        {
            Console.WriteLine($"- {project.Name} ({project.Id})");
        }
        
        // Show details for the configured project if available
        var configuredProject = config["project"];
        if (!string.IsNullOrEmpty(configuredProject))
        {
            var projectDetails = projects.FirstOrDefault(p => 
                p.Name.Equals(configuredProject, StringComparison.OrdinalIgnoreCase));
                
            if (projectDetails != null)
            {
                Console.WriteLine($"\nDetails for configured project '{configuredProject}':");
                Console.WriteLine($"- ID: {projectDetails.Id}");
                Console.WriteLine($"- Description: {projectDetails.Description ?? "No description"}");
                Console.WriteLine($"- State: {projectDetails.State}");
            }
            else
            {
                Console.WriteLine($"\nConfigured project '{configuredProject}' not found.");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

public interface IAzureDevOpsClient
{
    Task ConnectAsync();
    Task<IReadOnlyList<TeamProjectReference>> GetProjectsAsync();
}

public class AzureDevOpsClient : IAzureDevOpsClient
{
    private readonly IConfiguration _configuration;
    private VssConnection? _connection;
    
    public AzureDevOpsClient(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task ConnectAsync()
    {
        // Get settings from user secrets with exact key names
        var org = _configuration["org"] 
            ?? throw new InvalidOperationException("Azure DevOps organization not found in configuration");
            
        var pat = _configuration["pat"] 
            ?? throw new InvalidOperationException("Azure DevOps personal access token not found in configuration");
            
        // Project and repo are used for display but not required for initial connection
        var project = _configuration["project"];
        var repo = _configuration["repo"];
        
        // Create the organization URL from the organization name
        var orgUrl = $"https://dev.azure.com/{org}";
        
        // Create credentials using PAT
        var credentials = new VssBasicCredential(string.Empty, pat);
        
        // Create a connection to Azure DevOps
        _connection = new VssConnection(new Uri(orgUrl), credentials);
        
        // Test the connection by getting a client
        await _connection.ConnectAsync();
        
        Console.WriteLine($"Connected to organization: {org}");
        if (!string.IsNullOrEmpty(project))
            Console.WriteLine($"Selected project: {project}");
        if (!string.IsNullOrEmpty(repo))
            Console.WriteLine($"Selected repository: {repo}");
    }
    
    public async Task<IReadOnlyList<TeamProjectReference>> GetProjectsAsync()
    {
        if (_connection is null)
            throw new InvalidOperationException("Not connected to Azure DevOps. Call ConnectAsync() first.");
            
        var projectClient = _connection.GetClient<ProjectHttpClient>();
        var projects = await projectClient.GetProjects();
        
        return projects;
    }
}