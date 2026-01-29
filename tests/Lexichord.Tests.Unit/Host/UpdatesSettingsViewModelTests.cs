using Lexichord.Abstractions.Contracts;
using Lexichord.Host.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Host;

/// <summary>
/// Unit tests for <see cref="UpdatesSettingsViewModel"/>.
/// </summary>
/// <remarks>
/// Test categories:
/// - Initialization: Verifies ViewModel state matches service state
/// - Channel Selection: Verifies selection commands update service
/// - Update Checking: Verifies check command behavior
/// - Display Properties: Verifies formatted display values
///
/// Version: v0.1.6d
/// </remarks>
public class UpdatesSettingsViewModelTests
{
    private readonly Mock<IUpdateService> _updateService;
    private readonly Mock<ILogger<UpdatesSettingsViewModel>> _logger;

    public UpdatesSettingsViewModelTests()
    {
        _updateService = new Mock<IUpdateService>();
        _logger = new Mock<ILogger<UpdatesSettingsViewModel>>();

        // Setup default return values
        _updateService.Setup(s => s.CurrentChannel).Returns(UpdateChannel.Stable);
        _updateService.Setup(s => s.CurrentVersion).Returns("1.0.0");
        _updateService.Setup(s => s.GetVersionInfo()).Returns(new VersionInfo(
            "1.0.0",
            "1.0.0.0",
            DateTime.UtcNow,
            "abc1234",
            "main",
            false,
            ".NET 8.0"
        ));
    }

    private UpdatesSettingsViewModel CreateViewModel()
    {
        return new UpdatesSettingsViewModel(
            _updateService.Object,
            _logger.Object);
    }

    #region Initialization Tests

    [Fact]
    public void Constructor_InitializesFromService_StableChannel()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.True(viewModel.IsStable);
        Assert.False(viewModel.IsInsider);
    }

    [Fact]
    public void Constructor_InitializesFromService_InsiderChannel()
    {
        // Arrange
        _updateService.Setup(s => s.CurrentChannel).Returns(UpdateChannel.Insider);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.False(viewModel.IsStable);
        Assert.True(viewModel.IsInsider);
    }

    [Fact]
    public void Constructor_InitializesVersionInfo()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.NotNull(viewModel.VersionInfo);
        Assert.Equal("1.0.0", viewModel.VersionInfo.Version);
    }

    [Fact]
    public void Constructor_InitializesLastCheckTime()
    {
        // Arrange
        var lastCheck = DateTime.UtcNow.AddHours(-1);
        _updateService.Setup(s => s.LastCheckTime).Returns(lastCheck);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.Equal(lastCheck, viewModel.LastCheckTime);
    }

    #endregion

    #region Channel Selection Tests

    [Fact]
    public async Task SelectStableCommand_CallsSetChannelAsync()
    {
        // Arrange
        _updateService.Setup(s => s.CurrentChannel).Returns(UpdateChannel.Insider);
        var viewModel = CreateViewModel();

        // Act
        await viewModel.SelectStableCommand.ExecuteAsync(null);

        // Assert
        _updateService.Verify(s => s.SetChannelAsync(UpdateChannel.Stable), Times.Once);
    }

    [Fact]
    public async Task SelectInsiderCommand_CallsSetChannelAsync()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        await viewModel.SelectInsiderCommand.ExecuteAsync(null);

        // Assert
        _updateService.Verify(s => s.SetChannelAsync(UpdateChannel.Insider), Times.Once);
    }

    [Fact]
    public async Task SelectStableCommand_WhenAlreadyStable_DoesNotCallService()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        await viewModel.SelectStableCommand.ExecuteAsync(null);

        // Assert
        _updateService.Verify(s => s.SetChannelAsync(It.IsAny<UpdateChannel>()), Times.Never);
    }

    [Fact]
    public async Task SelectInsiderCommand_WhenAlreadyInsider_DoesNotCallService()
    {
        // Arrange
        _updateService.Setup(s => s.CurrentChannel).Returns(UpdateChannel.Insider);
        var viewModel = CreateViewModel();

        // Act
        await viewModel.SelectInsiderCommand.ExecuteAsync(null);

        // Assert
        _updateService.Verify(s => s.SetChannelAsync(It.IsAny<UpdateChannel>()), Times.Never);
    }

    [Fact]
    public async Task SelectInsiderCommand_UpdatesSelectionState()
    {
        // Arrange
        var viewModel = CreateViewModel();
        Assert.True(viewModel.IsStable);

        // Act
        await viewModel.SelectInsiderCommand.ExecuteAsync(null);

        // Assert
        Assert.False(viewModel.IsStable);
        Assert.True(viewModel.IsInsider);
    }

    #endregion

    #region SelectedChannel Property Tests

    [Fact]
    public void SelectedChannel_ReturnsStable_WhenIsStable()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var channel = viewModel.SelectedChannel;

        // Assert
        Assert.Equal(UpdateChannel.Stable, channel);
    }

    [Fact]
    public async Task SelectedChannel_ReturnsInsider_WhenIsInsider()
    {
        // Arrange
        var viewModel = CreateViewModel();
        await viewModel.SelectInsiderCommand.ExecuteAsync(null);

        // Act
        var channel = viewModel.SelectedChannel;

        // Assert
        Assert.Equal(UpdateChannel.Insider, channel);
    }

    #endregion

    #region Check for Updates Tests

    [Fact]
    public async Task CheckForUpdatesCommand_SetsIsCheckingForUpdates()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var wasChecking = false;

        _updateService.Setup(s => s.CheckForUpdatesAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                wasChecking = viewModel.IsCheckingForUpdates;
                await Task.Delay(10);
                return null;
            });

        // Act
        await viewModel.CheckForUpdatesCommand.ExecuteAsync(null);

        // Assert
        Assert.True(wasChecking);
        Assert.False(viewModel.IsCheckingForUpdates);
    }

    [Fact]
    public async Task CheckForUpdatesCommand_CallsService()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        await viewModel.CheckForUpdatesCommand.ExecuteAsync(null);

        // Assert
        _updateService.Verify(
            s => s.CheckForUpdatesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckForUpdatesCommand_UpdatesLastCheckTime()
    {
        // Arrange
        var newCheckTime = DateTime.UtcNow;
        _updateService.Setup(s => s.LastCheckTime).Returns(newCheckTime);
        var viewModel = CreateViewModel();

        // Act
        await viewModel.CheckForUpdatesCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(newCheckTime, viewModel.LastCheckTime);
    }

    [Fact]
    public async Task CheckForUpdatesCommand_NoUpdate_SetsUpToDateStatus()
    {
        // Arrange
        _updateService.Setup(s => s.CheckForUpdatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((UpdateInfo?)null);
        var viewModel = CreateViewModel();

        // Act
        await viewModel.CheckForUpdatesCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains("up to date", viewModel.UpdateStatus, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckForUpdatesCommand_UpdateAvailable_SetsVersionStatus()
    {
        // Arrange
        var updateInfo = new UpdateInfo(
            "2.0.0",
            "New features!",
            "https://example.com/download",
            DateTime.UtcNow
        );
        _updateService.Setup(s => s.CheckForUpdatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(updateInfo);
        var viewModel = CreateViewModel();

        // Act
        await viewModel.CheckForUpdatesCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains("2.0.0", viewModel.UpdateStatus!);
    }

    [Fact]
    public async Task CheckForUpdatesCommand_OnError_SetsCancelledStatus()
    {
        // Arrange
        _updateService.Setup(s => s.CheckForUpdatesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Network error"));
        var viewModel = CreateViewModel();

        // Act
        await viewModel.CheckForUpdatesCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains("failed", viewModel.UpdateStatus!, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Display Properties Tests

    [Fact]
    public void ChannelDescription_Stable_ReturnsStableDescription()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var description = viewModel.ChannelDescription;

        // Assert
        Assert.Contains("Recommended", description);
    }

    [Fact]
    public async Task ChannelDescription_Insider_ReturnsInsiderDescription()
    {
        // Arrange
        var viewModel = CreateViewModel();
        await viewModel.SelectInsiderCommand.ExecuteAsync(null);

        // Act
        var description = viewModel.ChannelDescription;

        // Assert
        Assert.Contains("early access", description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LastCheckDisplay_WhenNull_ReturnsNeverChecked()
    {
        // Arrange
        _updateService.Setup(s => s.LastCheckTime).Returns((DateTime?)null);
        var viewModel = CreateViewModel();

        // Act
        var display = viewModel.LastCheckDisplay;

        // Assert
        Assert.Equal("Never checked", display);
    }

    [Fact]
    public void LastCheckDisplay_WhenRecent_ReturnsJustNow()
    {
        // Arrange
        _updateService.Setup(s => s.LastCheckTime).Returns(DateTime.UtcNow);
        var viewModel = CreateViewModel();

        // Act
        var display = viewModel.LastCheckDisplay;

        // Assert
        Assert.Equal("Just now", display);
    }

    #endregion
}
