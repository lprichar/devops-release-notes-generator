using Microsoft.Extensions.Configuration;

namespace DevOps_GPT41;

public record ConfigurationData
{
    public virtual string Repo { get; }
    public virtual string Org { get; }
    public virtual string Pat { get; }
    public virtual string Project { get; }

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