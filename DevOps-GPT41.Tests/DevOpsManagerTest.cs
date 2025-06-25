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
    public async Task GivenOldBuild_WhenGenerateReleaseNotesJson_ThenGetPullRequestsCalledWithCorrectFromAndTo()
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
            .ReturnsAsync(Array.Empty<PullRequest>());

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

    private static ConfigurationData MakeConfigurationData(
        string repo = "repo",
        string org = "org",
        string pat = "pat",
        string project = "project")
    {
        var mock = new Mock<ConfigurationData>();
        mock.SetupGet(x => x.Repo).Returns(repo);
        mock.SetupGet(x => x.Org).Returns(org);
        mock.SetupGet(x => x.Pat).Returns(pat);
        mock.SetupGet(x => x.Project).Returns(project);
        return mock.Object;
    }
}
