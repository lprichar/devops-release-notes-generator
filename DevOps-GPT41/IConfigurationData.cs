namespace DevOps_GPT41;

public interface IConfigurationData
{
    string Repo { get; }
    string Org { get; }
    string Pat { get; }
    string Project { get; }
}