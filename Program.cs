using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text.Json;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var pat = config["pat"];
var org = config["org"];
var project = config["project"];

if (string.IsNullOrWhiteSpace(pat) || string.IsNullOrWhiteSpace(org) || string.IsNullOrWhiteSpace(project))
{
    Console.WriteLine("Missing required secrets: pat, org, or project.");
    return;
}

var baseUrl = $"https://dev.azure.com/{org}/{project}/_apis/";
using var http = new HttpClient();
var authToken = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{pat}"));
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

// 1. Get the pipeline ID for "CD"
var pipelinesUrl = $"{baseUrl}pipelines?api-version=7.2-preview.1";
var pipelinesResponse = await http.GetAsync(pipelinesUrl);
pipelinesResponse.EnsureSuccessStatusCode();
using var pipelinesStream = await pipelinesResponse.Content.ReadAsStreamAsync();
using var pipelinesDoc = await JsonDocument.ParseAsync(pipelinesStream);

var cdPipeline = pipelinesDoc.RootElement
    .GetProperty("value")
    .EnumerateArray()
    .FirstOrDefault(p => p.GetProperty("name").GetString() == "CD");

if (cdPipeline.ValueKind == JsonValueKind.Undefined)
{
    Console.WriteLine("Pipeline 'CD' not found.");
    return;
}

var pipelineId = cdPipeline.GetProperty("id").GetInt32();

// 2. Get the latest runs for the "CD" pipeline
var runsUrl = $"{baseUrl}pipelines/{pipelineId}/runs?api-version=7.2-preview.1";
var runsResponse = await http.GetAsync(runsUrl);
runsResponse.EnsureSuccessStatusCode();
using var runsStream = await runsResponse.Content.ReadAsStreamAsync();
using var runsDoc = await JsonDocument.ParseAsync(runsStream);

var runs = runsDoc.RootElement.GetProperty("value").EnumerateArray();

// 3. Find the latest successful run that completed all 4 steps
foreach (var run in runs)
{
    var runId = run.GetProperty("id").GetInt32();
    var runTitle = run.GetProperty("name").GetString();

    // Get run details (including stages and steps)
    var runDetailsUrl = $"{baseUrl}pipelines/{pipelineId}/runs/{runId}?api-version=7.2-preview.1";
    var runDetailsResponse = await http.GetAsync(runDetailsUrl);
    runDetailsResponse.EnsureSuccessStatusCode();
    using var runDetailsStream = await runDetailsResponse.Content.ReadAsStreamAsync();
    using var runDetailsDoc = await JsonDocument.ParseAsync(runDetailsStream);
        
    // Check status and steps
    var state = runDetailsDoc.RootElement.GetProperty("state").GetString();
    string? result = null;
    if (runDetailsDoc.RootElement.TryGetProperty("result", out var resultElement))
    {
        result = resultElement.GetString();
    }
    else
    {
        Console.WriteLine("Warning: 'result' property not found in run details. Dumping JSON for inspection:");
        Console.WriteLine(await runDetailsResponse.Content.ReadAsStringAsync());
    }

    if (state == "completed" && result == "succeeded")
    {
        // Try to get steps (jobs/tasks) - this structure may vary depending on pipeline type
        if (runDetailsDoc.RootElement.TryGetProperty("stages", out var stages))
        {
            foreach (var stage in stages.EnumerateArray())
            {
                if (stage.TryGetProperty("jobs", out var jobs))
                {
                    foreach (var job in jobs.EnumerateArray())
                    {
                        if (job.TryGetProperty("steps", out var steps))
                        {
                            if (steps.GetArrayLength() == 4)
                            {
                                Console.WriteLine($"Last successful 'CD' deployment to production: {runTitle}");
                                return;
                            }
                        }
                    }
                }
            }
        }
    }
}

Console.WriteLine("No successful 'CD' deployment to production with 4 steps found.");