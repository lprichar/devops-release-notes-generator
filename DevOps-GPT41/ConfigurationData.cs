using Microsoft.Extensions.Configuration;

namespace DevOps_GPT41;

public record ConfigurationData : IConfigurationData
{
    public string Repo { get; }
    public string Org { get; }
    public string Pat { get; }
    public string Project { get; }

    public ConfigurationData()
    {
        var builder = new ConfigurationBuilder()
            .AddUserSecrets<ConfigurationData>();
        var configuration = builder.Build();

        Repo = configuration["repo"] ?? throw new Exception("Missing required configuration value: 'repo'.");
        Org = configuration["org"] ?? throw new Exception("Missing required configuration value: 'org'.");
        Pat = configuration["pat"] ?? throw new Exception("Missing required configuration value: 'pat'.");
        Project = configuration["project"] ?? throw new Exception("Missing required configuration value: 'project'.");
    }
}