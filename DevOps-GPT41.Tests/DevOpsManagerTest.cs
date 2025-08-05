using Moq;
using Shouldly;

namespace DevOps_GPT41.Tests;

public class DevOpsManagerTest
{
    [Fact]
    public async Task GivenNoDeployments_WhenGenerateReleaseNotesJson_ThenReturnsNull()
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
        var result = await manager.GenerateReleaseNotesJson();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GivenOldBuild_WhenGenerateReleaseNotesJson_ThenGetPullRequestsCalledWithNullTo()
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
        await manager.GenerateReleaseNotesJson();

        // Assert
        mockConnection.Verify(x => x.GetPullRequests(
            configData.Repo,
            lastBuild,
            null), Times.Once);
    }

    [Fact]
    public async Task GivenPreviousDeploymentNullAndLatestDeploymentNotNull_WhenGenerateReleaseNotesJson_ThenThrowsException()
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
        var ex = await Record.ExceptionAsync(() => manager.GenerateReleaseNotesJson());

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

    private record TestConfigurationData(string repo, string org, string pat, string project) : ConfigurationData(repo, org, pat, project);
}
