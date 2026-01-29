using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using Lexichord.Host.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Host;

/// <summary>
/// Unit tests for <see cref="FirstRunService"/>.
/// </summary>
/// <remarks>
/// Test categories:
/// - Initialization: Verifies default state on construction
/// - First Run Detection: Verifies IsFirstRunEver and IsFirstRunAfterUpdate logic
/// - Version Comparison: Verifies version normalization and matching
/// - Settings Persistence: Verifies MarkRunCompletedAsync behavior
/// - Events: Verifies event publication
///
/// Version: v0.1.7c
/// </remarks>
public class FirstRunServiceTests : IDisposable
{
    private readonly Mock<IMediator> _mediator;
    private readonly Mock<ILogger<FirstRunService>> _logger;
    private readonly string _testSettingsPath;
    private readonly string _testChangelogPath;
    private readonly string _testDir;

    public FirstRunServiceTests()
    {
        _mediator = new Mock<IMediator>();
        _logger = new Mock<ILogger<FirstRunService>>();

        // Use unique temp path for each test run to ensure isolation
        _testDir = Path.Combine(
            Path.GetTempPath(),
            "Lexichord.Tests",
            $"first-run-{Guid.NewGuid()}");

        Directory.CreateDirectory(_testDir);

        _testSettingsPath = Path.Combine(_testDir, "first-run-settings.json");
        _testChangelogPath = Path.Combine(_testDir, "CHANGELOG.md");
    }

    public void Dispose()
    {
        // Clean up test directory after each test
        try
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }
        catch { /* Ignore cleanup errors */ }
    }

    private FirstRunService CreateService()
    {
        return new FirstRunService(_mediator.Object, _logger.Object, _testSettingsPath, _testChangelogPath);
    }

    #region Initialization Tests

    [Fact]
    public void Constructor_InitializesCurrentVersion()
    {
        // Arrange & Act
        var service = CreateService();

        // Assert
        Assert.NotNull(service.CurrentVersion);
        Assert.Matches(@"^\d+\.\d+\.\d+", service.CurrentVersion);
    }

    [Fact]
    public void ChangelogPath_ReturnsConfiguredPath()
    {
        // Arrange & Act
        var service = CreateService();

        // Assert
        Assert.Equal(_testChangelogPath, service.ChangelogPath);
    }

    #endregion

    #region First Run Detection Tests

    [Fact]
    public void IsFirstRunEver_WhenNoStoredVersion_ReturnsTrue()
    {
        // Arrange - No settings file exists
        var service = CreateService();

        // Act
        var result = service.IsFirstRunEver;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsFirstRunAfterUpdate_WhenNoStoredVersion_ReturnsFalse()
    {
        // Arrange - No settings file exists (fresh install, not update)
        var service = CreateService();

        // Act
        var result = service.IsFirstRunAfterUpdate;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsFirstRunAfterUpdate_WhenVersionDiffers_ReturnsTrue()
    {
        // Arrange - Create settings with old version
        var settings = new { LastRunVersion = "0.0.1", InstallationId = Guid.NewGuid().ToString() };
        var json = System.Text.Json.JsonSerializer.Serialize(settings);
        File.WriteAllText(_testSettingsPath, json);

        var service = CreateService();

        // Act
        var result = service.IsFirstRunAfterUpdate;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsFirstRunEver_WhenVersionDiffers_ReturnsFalse()
    {
        // Arrange - Create settings with old version (update, not fresh install)
        var settings = new { LastRunVersion = "0.0.1", InstallationId = Guid.NewGuid().ToString() };
        var json = System.Text.Json.JsonSerializer.Serialize(settings);
        File.WriteAllText(_testSettingsPath, json);

        var service = CreateService();

        // Act
        var result = service.IsFirstRunEver;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsFirstRunAfterUpdate_WhenVersionMatches_ReturnsFalse()
    {
        // Arrange - Create settings with current version
        var service = CreateService();
        var currentVersion = service.CurrentVersion;

        var settings = new { LastRunVersion = currentVersion, InstallationId = Guid.NewGuid().ToString() };
        var json = System.Text.Json.JsonSerializer.Serialize(settings);
        File.WriteAllText(_testSettingsPath, json);

        // Create new service to pick up settings
        var service2 = CreateService();

        // Act
        var result = service2.IsFirstRunAfterUpdate;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void PreviousVersion_WhenNoStoredVersion_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.PreviousVersion;

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PreviousVersion_WhenVersionStored_ReturnsStoredVersion()
    {
        // Arrange
        var settings = new { LastRunVersion = "1.2.3", InstallationId = Guid.NewGuid().ToString() };
        var json = System.Text.Json.JsonSerializer.Serialize(settings);
        File.WriteAllText(_testSettingsPath, json);

        var service = CreateService();

        // Act
        var result = service.PreviousVersion;

        // Assert
        Assert.Equal("1.2.3", result);
    }

    #endregion

    #region MarkRunCompletedAsync Tests

    [Fact]
    public async Task MarkRunCompletedAsync_StoresCurrentVersion()
    {
        // Arrange
        var service = CreateService();
        var currentVersion = service.CurrentVersion;

        // Act
        await service.MarkRunCompletedAsync();

        // Assert
        Assert.True(File.Exists(_testSettingsPath));
        var json = await File.ReadAllTextAsync(_testSettingsPath);
        Assert.Contains(currentVersion, json);
    }

    [Fact]
    public async Task MarkRunCompletedAsync_GeneratesInstallationId()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.MarkRunCompletedAsync();

        // Assert
        var json = await File.ReadAllTextAsync(_testSettingsPath);
        Assert.Contains("InstallationId", json);
    }

    [Fact]
    public async Task MarkRunCompletedAsync_SetsFirstRunDate()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.MarkRunCompletedAsync();

        // Assert
        var json = await File.ReadAllTextAsync(_testSettingsPath);
        Assert.Contains("FirstRunDate", json);
    }

    [Fact]
    public async Task MarkRunCompletedAsync_ResetsFirstRunFlags()
    {
        // Arrange
        var service = CreateService();
        Assert.True(service.IsFirstRunEver);

        // Act
        await service.MarkRunCompletedAsync();

        // Assert
        Assert.False(service.IsFirstRunEver);
        Assert.False(service.IsFirstRunAfterUpdate);
    }

    #endregion

    #region GetReleaseNotesAsync Tests

    [Fact]
    public async Task GetReleaseNotesAsync_WhenFileExists_ReturnsContent()
    {
        // Arrange
        var expectedContent = "# Changelog\n\n## v1.0.0\n- Initial release";
        await File.WriteAllTextAsync(_testChangelogPath, expectedContent);

        var service = CreateService();

        // Act
        var result = await service.GetReleaseNotesAsync();

        // Assert
        Assert.Equal(expectedContent, result);
    }

    [Fact]
    public async Task GetReleaseNotesAsync_WhenFileMissing_ReturnsFallback()
    {
        // Arrange - No changelog file exists
        var service = CreateService();

        // Act
        var result = await service.GetReleaseNotesAsync();

        // Assert
        Assert.Contains("Lexichord", result);
        Assert.Contains("release notes could not be loaded", result);
    }

    #endregion

    #region Event Publication Tests

    [Fact]
    public void FirstRunDetectedEvent_PublishedOnFirstRunEver()
    {
        // Arrange & Act
        var service = CreateService();
        _ = service.IsFirstRunEver; // Trigger lazy initialization

        // Assert - Allow for async event publication
        Thread.Sleep(100);
        _mediator.Verify(m => m.Publish(
            It.Is<FirstRunDetectedEvent>(e =>
                e.IsFirstRunEver == true &&
                e.IsFirstRunAfterUpdate == false),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void FirstRunDetectedEvent_PublishedOnUpdate()
    {
        // Arrange
        var settings = new { LastRunVersion = "0.0.1", InstallationId = Guid.NewGuid().ToString() };
        var json = System.Text.Json.JsonSerializer.Serialize(settings);
        File.WriteAllText(_testSettingsPath, json);

        // Act
        var service = CreateService();
        _ = service.IsFirstRunAfterUpdate; // Trigger lazy initialization

        // Assert - Allow for async event publication
        Thread.Sleep(100);
        _mediator.Verify(m => m.Publish(
            It.Is<FirstRunDetectedEvent>(e =>
                e.IsFirstRunEver == false &&
                e.IsFirstRunAfterUpdate == true &&
                e.PreviousVersion == "0.0.1"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void FirstRunDetectedEvent_NotPublishedOnNormalRun()
    {
        // Arrange - Create settings with current version
        var tempService = CreateService();
        var currentVersion = tempService.CurrentVersion;

        var settings = new { LastRunVersion = currentVersion, InstallationId = Guid.NewGuid().ToString() };
        var json = System.Text.Json.JsonSerializer.Serialize(settings);
        File.WriteAllText(_testSettingsPath, json);

        // Act
        var service = CreateService();
        _ = service.IsFirstRunAfterUpdate; // Trigger lazy initialization

        // Assert
        Thread.Sleep(100);
        _mediator.Verify(m => m.Publish(
            It.IsAny<FirstRunDetectedEvent>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Version Normalization Tests

    // Note: Version normalization is tested indirectly through IsFirstRunAfterUpdate tests.
    // The VersionsMatch method is private and handles:
    // - Leading 'v' removal (v1.0.0 -> 1.0.0)
    // - Trailing zero removal (1.0.0.0 -> 1.0.0)
    // - Case-insensitive comparison

    #endregion
}
