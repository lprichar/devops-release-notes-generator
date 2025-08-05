using Moq;
using Shouldly;
using YamlDotNet.Serialization;

namespace DevOps_GPT41.Tests;

public class DevOpsManagerTest
{
    [Fact]
    public async Task GivenNoDeployments_WhenGenerateReleaseNotesYaml_ThenReturnsNull()
    {
        // Arrange
        var mockDateTimeProvider = new Mock<IDateTimeProvider>();
        mockDateTimeProvider.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var mockConnection = new Mock<IConnection>();
        mockConnection.Setup(x => x.GetLastTwoProductionDeployments(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(( (DateTime?)null, (DateTime?)null ));

        var configData = MakeConfigurationData();
        var manager = new DevOpsManager(mockDateTimeProvider.Object, mockConnection.Object, configData);

        // Act
        var result = await manager.GenerateReleaseNotesYaml();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GivenOldBuild_WhenGenerateReleaseNotesYaml_ThenGetPullRequestsCalledWithNullTo()
    {
        // Arrange
        var today = new DateTime(2025, 1, 3, 12, 0, 0, DateTimeKind.Utc);
        var lastBuild = new DateTime(2025, 1, 1, 13, 0, 0, DateTimeKind.Utc);
        var mockDateTimeProvider = new Mock<IDateTimeProvider>();
        mockDateTimeProvider.Setup(x => x.UtcNow).Returns(today);

        var mockConnection = new Mock<IConnection>();
        mockConnection.Setup(x => x.GetLastTwoProductionDeployments(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(( (DateTime?)null, (DateTime?)lastBuild ));
        mockConnection.Setup(x => x.GetPullRequests(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<Pr>());

        var configData = MakeConfigurationData();
        var manager = new DevOpsManager(mockDateTimeProvider.Object, mockConnection.Object, configData);

        // Act
        await manager.GenerateReleaseNotesYaml();

        // Assert
        mockConnection.Verify(x => x.GetPullRequests(
            configData.Repo,
            lastBuild,
            null), Times.Once);
    }

    [Fact]
    public async Task GivenPreviousDeploymentNullAndLatestDeploymentNotNull_WhenGenerateReleaseNotesYaml_ThenThrowsException()
    {
        // Arrange
        var mockDateTimeProvider = new Mock<IDateTimeProvider>();
        mockDateTimeProvider.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var mockConnection = new Mock<IConnection>();
        var latestDeployment = DateTime.UtcNow;
        mockConnection.Setup(x => x.GetLastTwoProductionDeployments(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(((DateTime?)null, (DateTime?)latestDeployment));

        var configData = MakeConfigurationData();
        var manager = new DevOpsManager(mockDateTimeProvider.Object, mockConnection.Object, configData);

        // Act
        var ex = await Record.ExceptionAsync(() => manager.GenerateReleaseNotesYaml());

        // Assert
        ex.ShouldNotBeNull();
        ex.ShouldBeOfType<InvalidOperationException>();
    }

    private static ConfigurationData MakeConfigurationData(
        string repo = "repo",
        string org = "org",
        string pat = "pat",
        string project = "project")
    {
        return new TestConfigurationData(repo, org, pat, project);
    }

    [Fact]
    public async Task GivenRecentBuild_WhenGenerateReleaseNotesYaml_ThenReturnsValidYaml()
    {
        // Arrange
        var today = new DateTime(2025, 1, 3, 12, 0, 0, DateTimeKind.Utc);
        var previousBuild = new DateTime(2025, 1, 2, 13, 0, 0, DateTimeKind.Utc);
        var latestBuild = new DateTime(2025, 1, 3, 11, 0, 0, DateTimeKind.Utc);
        var mockDateTimeProvider = new Mock<IDateTimeProvider>();
        mockDateTimeProvider.Setup(x => x.UtcNow).Returns(today);

        var mockConnection = new Mock<IConnection>();
        mockConnection.Setup(x => x.GetLastTwoProductionDeployments(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(( (DateTime?)previousBuild, (DateTime?)latestBuild ));
        
        var prList = new List<Pr>
        {
            new(123, new DateTime(2025, 1, 2, 14, 0, 0, DateTimeKind.Utc), "Fix bug", "Fixed an issue"),
            new(124, new DateTime(2025, 1, 3, 10, 0, 0, DateTimeKind.Utc), "Add feature", "Added new functionality")
        };
        mockConnection.Setup(x => x.GetPullRequests(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(prList);

        var configData = MakeConfigurationData();
        var manager = new DevOpsManager(mockDateTimeProvider.Object, mockConnection.Object, configData);

        // Act
        var result = await manager.GenerateReleaseNotesYaml();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        
        // Verify it's in YAML format (should contain dashes for list items and proper structure)
        result.ShouldContain("- id:");
        result.ShouldContain("  completionDate:");
        result.ShouldContain("  title:");
        result.ShouldContain("  body:");
        result.ShouldContain("Fix bug");
        result.ShouldContain("Add feature");
        result.ShouldNotContain("{");  // Should not contain JSON braces
        result.ShouldNotContain("[");  // Should not contain JSON square brackets
        
        // Verify the YAML structure is valid by checking it can be parsed without errors
        var parser = new YamlDotNet.Core.Parser(new StringReader(result));
        var validYaml = true;
        try
        {
            while (parser.MoveNext())
            {
                // Parse through the entire YAML to validate structure
            }
        }
        catch
        {
            validYaml = false;
        }
        validYaml.ShouldBeTrue("Generated output should be valid YAML");
    }

    private record TestConfigurationData(string repo, string org, string pat, string project) : ConfigurationData(repo, org, pat, project);
}
