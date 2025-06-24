using Moq;
using Shouldly;
using Xunit;
using DevOps_GPT41;
using System;

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
