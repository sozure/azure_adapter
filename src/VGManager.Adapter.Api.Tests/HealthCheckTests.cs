using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using VGManager.Adapter.Api.HealthChecks;

namespace VGManager.Adapter.Api.Tests;

public class HealthCheckTests
{
    private StartupHealthCheck _startupHealthCheck = null!;
    private Mock<IHostApplicationLifetime> _applicationLifetimeMock = null!;

    [SetUp]
    public void Setup()
    {
        _applicationLifetimeMock = new Mock<IHostApplicationLifetime>();
        _startupHealthCheck = new StartupHealthCheck(_applicationLifetimeMock.Object);
    }

    [TestCase(false, HealthStatus.Unhealthy, TestName = "CheckHealthAsync_ReturnsUnhealthy")]
    [TestCase(true, HealthStatus.Healthy, TestName = "CheckHealthAsync_ReturnsHealthy")]
    public async Task CheckHealthAsync_ReturnsHealthStatus(bool canceled, HealthStatus healthStatus)
    {
        //Arrange
        var cancellationToken = new CancellationToken(canceled);
        _applicationLifetimeMock.Setup(x => x.ApplicationStarted).Returns(cancellationToken);
        var healthCheckContext = new HealthCheckContext();

        //Act
        _startupHealthCheck.RegisterStartupReadiness();
        var result = await _startupHealthCheck.CheckHealthAsync(healthCheckContext, cancellationToken);

        //Assert
        _applicationLifetimeMock.Verify(x => x.ApplicationStarted, Times.Once);
        result.Status.Should().Be(healthStatus);
    }

    [Test]
    public void HealthCheckSettings_CheckPort()
    {
        //Arrange
        var configurationBuilder = new ConfigurationBuilder().AddJsonFile("appsettings.Development.json");
        var configurationRoot = configurationBuilder.Build();
        var expectedPort = 8080;

        //Act
        var healthChecksSettingsResult = configurationRoot.GetSection(Constants.SettingKeys.HealthChecksSettings).Get<HealthChecksSettings>();

        //Assert
        healthChecksSettingsResult?.Port.Should().Be(expectedPort);
    }
}
