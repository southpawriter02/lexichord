using global::Lexichord.Abstractions.Contracts;
using global::Lexichord.Abstractions.Events;
using Lexichord.Host.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

// Type alias with global qualifier for use in test methods
using LexichordUpdateOptions = global::Lexichord.Abstractions.Contracts.UpdateOptions;


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
/// - v0.1.7a additions: Tests for DownloadUpdatesAsync, ApplyUpdatesAndRestart,
///   IsUpdateReady, and UpdateProgress
///
/// Version: v0.1.7a
/// </remarks>
public class UpdateServiceTests : IDisposable
{
    private readonly Mock<IMediator> _mediator;
    private readonly Mock<ILogger<UpdateService>> _logger;
    private readonly string _testSettingsPath;
    private readonly LexichordUpdateOptions _options;

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

        // Default options for testing (empty URLs = dev mode, no update manager)
        _options = new LexichordUpdateOptions(
            StableUpdateUrl: string.Empty,
            InsiderUpdateUrl: string.Empty,
            AutoCheckOnStartup: false,
            AutoDownload: false);
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
        return new UpdateService(_mediator.Object, _logger.Object, _options, _testSettingsPath);
    }

    private UpdateService CreateService(LexichordUpdateOptions options)
    {
        return new UpdateService(_mediator.Object, _logger.Object, options, _testSettingsPath);
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

    [Fact]
    public void IsUpdateReady_InitializesToFalse()
    {
        // Arrange & Act
        var service = CreateService();

        // Assert
        Assert.False(service.IsUpdateReady);
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

    [Fact]
    public async Task SetChannelAsync_ResetsIsUpdateReady()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.SetChannelAsync(UpdateChannel.Insider);

        // Assert
        Assert.False(service.IsUpdateReady);
    }

    #endregion

    #region Update Checking Tests

    [Fact]
    public async Task CheckForUpdatesAsync_ReturnsNull_WhenNotInstalledViaVelopack()
    {
        // Arrange - Empty URLs means no Velopack UpdateManager
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

    #region v0.1.7a: DownloadUpdatesAsync Tests

    [Fact]
    public async Task DownloadUpdatesAsync_DoesNothing_WhenNotInstalled()
    {
        // Arrange
        var service = CreateService();
        var update = new UpdateInfo("1.0.0", "Notes", "url", DateTime.Now, false, 1000);

        // Act - Should not throw, just logs warning
        await service.DownloadUpdatesAsync(update);

        // Assert
        Assert.False(service.IsUpdateReady);
    }

    [Fact]
    public async Task DownloadUpdatesAsync_DoesNothing_WhenNoUpdateChecked()
    {
        // Arrange
        var service = CreateService();
        var update = new UpdateInfo("1.0.0", "Notes", "url", DateTime.Now, false, 1000);

        // Act - Should not throw, just logs warning
        await service.DownloadUpdatesAsync(update);

        // Assert
        Assert.False(service.IsUpdateReady);
    }

    #endregion

    #region v0.1.7a: ApplyUpdatesAndRestart Tests

    [Fact]
    public void ApplyUpdatesAndRestart_ThrowsWhenNoUpdateReady()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => service.ApplyUpdatesAndRestart());
        Assert.Contains("No update is ready", ex.Message);
    }

    #endregion

    #region v0.1.7a: UpdateProgress Event Tests

    [Fact]
    public void UpdateProgress_EventCanBeSubscribed()
    {
        // Arrange
        var service = CreateService();
        service.UpdateProgress += (_, _) => { /* Handler registered */ };

        // Assert - Just verify subscription works (event only fires during actual download)
        Assert.NotNull(service);
    }

    #endregion

    #region v0.1.7a: UpdateOptions Tests

    [Fact]
    public void GetUrlForChannel_ReturnsStableUrl_ForStableChannel()
    {
        // Arrange
        var options = new LexichordUpdateOptions(
            StableUpdateUrl: "https://stable.example.com",
            InsiderUpdateUrl: "https://insider.example.com",
            AutoCheckOnStartup: true,
            AutoDownload: false);

        // Act
        var url = options.GetUrlForChannel(UpdateChannel.Stable);

        // Assert
        Assert.Equal("https://stable.example.com", url);
    }

    [Fact]
    public void GetUrlForChannel_ReturnsInsiderUrl_ForInsiderChannel()
    {
        // Arrange
        var options = new LexichordUpdateOptions(
            StableUpdateUrl: "https://stable.example.com",
            InsiderUpdateUrl: "https://insider.example.com",
            AutoCheckOnStartup: true,
            AutoDownload: false);

        // Act
        var url = options.GetUrlForChannel(UpdateChannel.Insider);

        // Assert
        Assert.Equal("https://insider.example.com", url);
    }

    #endregion
}
