using Microsoft.Extensions.Configuration;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var pat = config["pat"];

if (string.IsNullOrWhiteSpace(pat))
{
    Console.WriteLine("No PAT found in user-secrets under the key 'pat'.");
}
else
{
    Console.WriteLine($"PAT from user-secrets: {pat}");
}