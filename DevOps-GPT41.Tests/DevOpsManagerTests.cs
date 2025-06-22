using DevOps_GPT41;
using Moq;
using Xunit;

namespace DevOps_GPT41.Tests;

public class DevOpsManagerTests
{
    [Fact]
    public void IsRecentDeployment_WhenDeploymentWithinLast24Hours_ReturnsTrue()
    {
        // Arrange
        var fixedDateTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var mockDateTimeProvider = new Mock<IDateTimeProvider>();
        mockDateTimeProvider.Setup(p => p.UtcNow).Returns(fixedDateTime);
        
        var mockConnection = new Mock<IConnection>();
        var mockConfig = new Mock<IConfigurationData>();
        
        var devOpsManager = new DevOpsManager(mockConnection.Object, mockConfig.Object, mockDateTimeProvider.Object);
        
        // Deployment is 10 hours ago (within 24 hour window)
        var deploymentTime = fixedDateTime.AddHours(-10);
        
        // Act
        var result = devOpsManager.IsRecentDeployment(deploymentTime);
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public void IsRecentDeployment_WhenDeploymentOlderThan24Hours_ReturnsFalse()
    {
        // Arrange
        var fixedDateTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var mockDateTimeProvider = new Mock<IDateTimeProvider>();
        mockDateTimeProvider.Setup(p => p.UtcNow).Returns(fixedDateTime);
        
        var mockConnection = new Mock<IConnection>();
        var mockConfig = new Mock<IConfigurationData>();
        
        var devOpsManager = new DevOpsManager(mockConnection.Object, mockConfig.Object, mockDateTimeProvider.Object);
        
        // Deployment is 25 hours ago (outside 24 hour window)
        var deploymentTime = fixedDateTime.AddHours(-25);
        
        // Act
        var result = devOpsManager.IsRecentDeployment(deploymentTime);
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public void IsRecentDeployment_WhenDeploymentExactly24HoursAgo_ReturnsFalse()
    {
        // Arrange
        var fixedDateTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var mockDateTimeProvider = new Mock<IDateTimeProvider>();
        mockDateTimeProvider.Setup(p => p.UtcNow).Returns(fixedDateTime);
        
        var mockConnection = new Mock<IConnection>();
        var mockConfig = new Mock<IConfigurationData>();
        
        var devOpsManager = new DevOpsManager(mockConnection.Object, mockConfig.Object, mockDateTimeProvider.Object);
        
        // Deployment is exactly 24 hours ago (boundary condition)
        var deploymentTime = fixedDateTime.AddHours(-24);
        
        // Act
        var result = devOpsManager.IsRecentDeployment(deploymentTime);
        
        // Assert
        Assert.False(result);
    }
}