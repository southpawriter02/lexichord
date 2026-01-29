using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using Lexichord.Host.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Host;

/// <summary>
/// Unit tests for <see cref="UpdateService"/>.
/// </summary>
/// <remarks>
/// Test categories:
/// - Initialization: Verifies default state on construction
/// - Channel Switching: Verifies SetChannelAsync behavior
/// - Update Checking: Verifies CheckForUpdatesAsync stub behavior
/// - Events: Verifies event publication
///
/// Version: v0.1.6d
/// </remarks>
public class UpdateServiceTests : IDisposable
{
    private readonly Mock<IMediator> _mediator;
    private readonly Mock<ILogger<UpdateService>> _logger;
    private readonly string _testSettingsPath;

    public UpdateServiceTests()
    {
        _mediator = new Mock<IMediator>();
        _logger = new Mock<ILogger<UpdateService>>();

        // Use unique temp path for each test run to ensure isolation
        _testSettingsPath = Path.Combine(
            Path.GetTempPath(),
            "Lexichord.Tests",
            $"update-settings-{Guid.NewGuid()}.json");

        // Ensure parent directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(_testSettingsPath)!);
    }

    public void Dispose()
    {
        // Clean up test file after each test
        try
        {
            if (File.Exists(_testSettingsPath))
                File.Delete(_testSettingsPath);
        }
        catch { /* Ignore cleanup errors */ }
    }

    private UpdateService CreateService()
    {
        return new UpdateService(_mediator.Object, _logger.Object, _testSettingsPath);
    }

    #region Initialization Tests

    [Fact]
    public void Constructor_InitializesWithDefaultChannel_Stable()
    {
        // Arrange & Act
        var service = CreateService();

        // Assert
        Assert.Equal(UpdateChannel.Stable, service.CurrentChannel);
    }

    [Fact]
    public void Constructor_InitializesVersionInfo()
    {
        // Arrange & Act
        var service = CreateService();

        // Assert
        var version = service.GetVersionInfo();
        Assert.NotNull(version);
        Assert.False(string.IsNullOrEmpty(version.Version));
        Assert.False(string.IsNullOrEmpty(version.RuntimeInfo));
    }

    [Fact]
    public void CurrentVersion_ReturnsVersionString()
    {
        // Arrange
        var service = CreateService();

        // Act
        var version = service.CurrentVersion;

        // Assert
        Assert.NotNull(version);
        Assert.Matches(@"^\d+\.\d+\.\d+", version);
    }

    [Fact]
    public void LastCheckTime_InitializesToNull()
    {
        // Arrange & Act
        var service = CreateService();

        // Assert
        Assert.Null(service.LastCheckTime);
    }

    #endregion

    #region Channel Switching Tests

    [Fact]
    public async Task SetChannelAsync_ToInsider_UpdatesCurrentChannel()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.SetChannelAsync(UpdateChannel.Insider);

        // Assert
        Assert.Equal(UpdateChannel.Insider, service.CurrentChannel);
    }

    [Fact]
    public async Task SetChannelAsync_BackToStable_UpdatesCurrentChannel()
    {
        // Arrange
        var service = CreateService();
        await service.SetChannelAsync(UpdateChannel.Insider);

        // Act
        await service.SetChannelAsync(UpdateChannel.Stable);

        // Assert
        Assert.Equal(UpdateChannel.Stable, service.CurrentChannel);
    }

    [Fact]
    public async Task SetChannelAsync_SameChannel_DoesNotRaiseEvent()
    {
        // Arrange
        var service = CreateService();
        var eventRaised = false;
        service.ChannelChanged += (_, _) => eventRaised = true;

        // Act
        await service.SetChannelAsync(UpdateChannel.Stable);

        // Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public async Task SetChannelAsync_DifferentChannel_RaisesChannelChangedEvent()
    {
        // Arrange
        var service = CreateService();
        UpdateChannelChangedEventArgs? eventArgs = null;
        service.ChannelChanged += (_, args) => eventArgs = args;

        // Act
        await service.SetChannelAsync(UpdateChannel.Insider);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(UpdateChannel.Stable, eventArgs.OldChannel);
        Assert.Equal(UpdateChannel.Insider, eventArgs.NewChannel);
    }

    [Fact]
    public async Task SetChannelAsync_PublishesMediatREvent()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.SetChannelAsync(UpdateChannel.Insider);

        // Assert
        _mediator.Verify(m => m.Publish(
            It.Is<UpdateChannelChangedEvent>(e =>
                e.OldChannel == UpdateChannel.Stable &&
                e.NewChannel == UpdateChannel.Insider),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Update Checking Tests

    [Fact]
    public async Task CheckForUpdatesAsync_ReturnsNull_StubImplementation()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.CheckForUpdatesAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_UpdatesLastCheckTime()
    {
        // Arrange
        var service = CreateService();
        var beforeCheck = DateTime.UtcNow;

        // Act
        await service.CheckForUpdatesAsync();

        // Assert
        Assert.NotNull(service.LastCheckTime);
        Assert.True(service.LastCheckTime >= beforeCheck);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_PublishesUpdateCheckCompletedEvent()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.CheckForUpdatesAsync();

        // Assert
        _mediator.Verify(m => m.Publish(
            It.Is<UpdateCheckCompletedEvent>(e =>
                e.UpdateFound == false),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_CancellationThrows()
    {
        // Arrange
        var service = CreateService();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => service.CheckForUpdatesAsync(cts.Token));
    }

    #endregion

    #region VersionInfo Tests

    [Fact]
    public void GetVersionInfo_ReturnsConsistentValues()
    {
        // Arrange
        var service = CreateService();

        // Act
        var info1 = service.GetVersionInfo();
        var info2 = service.GetVersionInfo();

        // Assert
        Assert.Equal(info1, info2);
    }

    [Fact]
    public void GetVersionInfo_DisplayVersion_ContainsVersion()
    {
        // Arrange
        var service = CreateService();

        // Act
        var info = service.GetVersionInfo();

        // Assert
        Assert.Contains(info.Version, info.DisplayVersion);
    }

    [Fact]
    public void GetVersionInfo_RuntimeInfo_ContainsDotNet()
    {
        // Arrange
        var service = CreateService();

        // Act
        var info = service.GetVersionInfo();

        // Assert
        Assert.Contains(".NET", info.RuntimeInfo);
    }

    #endregion
}
