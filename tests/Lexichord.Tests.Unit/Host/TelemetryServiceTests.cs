using Lexichord.Abstractions.Contracts;
using Lexichord.Host.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lexichord.Tests.Unit.Host;

/// <summary>
/// Unit tests for TelemetryService.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify:
/// - PII scrubbing for Windows/macOS/Linux paths
/// - Email address scrubbing
/// - Enable/disable state persistence
/// - Disabled service doesn't transmit
///
/// NOTE: These tests don't verify actual Sentry transmission,
/// which would require integration tests.
/// </remarks>
public class TelemetryServiceTests : IDisposable
{
    private readonly Mock<ILogger<TelemetryService>> _mockLogger;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly string _tempSettingsPath;
    private TelemetryService _sut;

    public TelemetryServiceTests()
    {
        _mockLogger = new Mock<ILogger<TelemetryService>>();
        _mockMediator = new Mock<IMediator>();
        _mockConfiguration = new Mock<IConfiguration>();

        // Configure no DSN for testing (won't try to connect to Sentry)
        _mockConfiguration.Setup(c => c["Sentry:Dsn"]).Returns((string?)null);

        _tempSettingsPath = Path.Combine(Path.GetTempPath(), $"test-telemetry-{Guid.NewGuid()}.json");
        _sut = CreateService();
    }

    public void Dispose()
    {
        _sut.Dispose();
        if (File.Exists(_tempSettingsPath))
            File.Delete(_tempSettingsPath);
        GC.SuppressFinalize(this);
    }

    private TelemetryService CreateService()
    {
        return new TelemetryService(
            _mockLogger.Object,
            _mockMediator.Object,
            _mockConfiguration.Object,
            _tempSettingsPath);
    }

    #region PII Scrubbing Tests

    [Fact]
    public void ScrubPii_WindowsPath_ReplacesWithPlaceholder()
    {
        // Arrange
        const string input = @"Error at C:\Users\john.doe\Documents\project\file.txt";

        // Act
        var result = TelemetryService.ScrubPii(input);

        // Assert
        result.Should().NotContain("john.doe");
        result.Should().Contain("[USER_PATH]");
    }

    [Fact]
    public void ScrubPii_MacOsPath_ReplacesWithPlaceholder()
    {
        // Arrange
        const string input = "/Users/jane.smith/Library/Application Support/Lexichord/data.db";

        // Act
        var result = TelemetryService.ScrubPii(input);

        // Assert
        result.Should().NotContain("jane.smith");
        result.Should().Contain("[USER_PATH]");
    }

    [Fact]
    public void ScrubPii_LinuxPath_ReplacesWithPlaceholder()
    {
        // Arrange
        const string input = "/home/developer/projects/lexichord/config.json";

        // Act
        var result = TelemetryService.ScrubPii(input);

        // Assert
        result.Should().NotContain("developer");
        result.Should().Contain("[USER_PATH]");
    }

    [Fact]
    public void ScrubPii_Email_ReplacesWithPlaceholder()
    {
        // Arrange
        const string input = "Error sending email to user@example.com";

        // Act
        var result = TelemetryService.ScrubPii(input);

        // Assert
        result.Should().NotContain("user@example.com");
        result.Should().Contain("[EMAIL]");
    }

    [Fact]
    public void ScrubPii_MultiplePiiItems_ReplacesAll()
    {
        // Arrange
        const string input = @"Error at C:\Users\admin\file.txt, email: admin@company.org and /home/root/data";

        // Act
        var result = TelemetryService.ScrubPii(input);

        // Assert
        result.Should().NotContain("admin");
        result.Should().NotContain("root");
        result.Should().NotContain("admin@company.org");
        result.Should().Contain("[USER_PATH]");
        result.Should().Contain("[EMAIL]");
    }

    [Fact]
    public void ScrubPii_NoPii_ReturnsOriginal()
    {
        // Arrange
        const string input = "Generic error message without any PII";

        // Act
        var result = TelemetryService.ScrubPii(input);

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    public void ScrubPii_NullInput_ReturnsEmptyString()
    {
        // Act
        var result = TelemetryService.ScrubPii(null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ScrubPii_EmptyInput_ReturnsEmptyString()
    {
        // Act
        var result = TelemetryService.ScrubPii(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Enable/Disable Tests

    [Fact]
    public void IsEnabled_InitialState_ReturnsFalse()
    {
        // Assert
        _sut.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Enable_WhenDisabled_SetsIsEnabledTrue()
    {
        // Arrange
        _sut.IsEnabled.Should().BeFalse();

        // Act
        _sut.Enable();

        // Assert (degraded mode, but still marked as enabled)
        _sut.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Disable_WhenEnabled_SetsIsEnabledFalse()
    {
        // Arrange
        _sut.Enable();
        _sut.IsEnabled.Should().BeTrue();

        // Act
        _sut.Disable();

        // Assert
        _sut.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Enable_CalledTwice_DoesNotThrow()
    {
        // Act & Assert
        var act = () =>
        {
            _sut.Enable();
            _sut.Enable();
        };

        act.Should().NotThrow();
        _sut.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Disable_CalledTwice_DoesNotThrow()
    {
        // Arrange
        _sut.Enable();

        // Act & Assert
        var act = () =>
        {
            _sut.Disable();
            _sut.Disable();
        };

        act.Should().NotThrow();
        _sut.IsEnabled.Should().BeFalse();
    }

    #endregion

    #region Operation When Disabled Tests

    [Fact]
    public void CaptureException_WhenDisabled_DoesNotThrow()
    {
        // Arrange
        _sut.IsEnabled.Should().BeFalse();
        var exception = new InvalidOperationException("Test");

        // Act & Assert
        var act = () => _sut.CaptureException(exception);
        act.Should().NotThrow();
    }

    [Fact]
    public void CaptureMessage_WhenDisabled_DoesNotThrow()
    {
        // Arrange
        _sut.IsEnabled.Should().BeFalse();

        // Act & Assert
        var act = () => _sut.CaptureMessage("Test message");
        act.Should().NotThrow();
    }

    [Fact]
    public void AddBreadcrumb_WhenDisabled_DoesNotThrow()
    {
        // Arrange
        _sut.IsEnabled.Should().BeFalse();

        // Act & Assert
        var act = () => _sut.AddBreadcrumb("Test breadcrumb");
        act.Should().NotThrow();
    }

    [Fact]
    public void BeginScope_WhenDisabled_ReturnsNoOpDisposable()
    {
        // Arrange
        _sut.IsEnabled.Should().BeFalse();

        // Act
        using var scope = _sut.BeginScope("test-operation");

        // Assert
        scope.Should().NotBeNull();
    }

    [Fact]
    public void Flush_WhenDisabled_DoesNotThrow()
    {
        // Arrange
        _sut.IsEnabled.Should().BeFalse();

        // Act & Assert
        var act = () => _sut.Flush(TimeSpan.FromSeconds(1));
        act.Should().NotThrow();
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public void Dispose_WhenNotEnabled_DoesNotThrow()
    {
        // Arrange
        _sut = CreateService();

        // Act & Assert
        var act = () => _sut.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_WhenCalledTwice_DoesNotThrow()
    {
        // Arrange
        _sut = CreateService();

        // Act & Assert
        var act = () =>
        {
            _sut.Dispose();
            _sut.Dispose();
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void Enable_AfterDispose_DoesNotThrow()
    {
        // Arrange
        _sut = CreateService();
        _sut.Dispose();

        // Act & Assert (should not throw, but won't enable)
        var act = () => _sut.Enable();
        act.Should().NotThrow();
    }

    #endregion

    #region Settings Persistence Tests

    [Fact]
    public void Enable_PersistsSettingsToFile()
    {
        // Arrange
        File.Exists(_tempSettingsPath).Should().BeFalse();

        // Act
        _sut.Enable();

        // Assert
        File.Exists(_tempSettingsPath).Should().BeTrue();
        var content = File.ReadAllText(_tempSettingsPath);
        content.Should().Contain("\"CrashReportingEnabled\": true");
    }

    [Fact]
    public void Disable_AfterEnable_UpdatesSettingsFile()
    {
        // Arrange
        _sut.Enable();
        var contentAfterEnable = File.ReadAllText(_tempSettingsPath);
        contentAfterEnable.Should().Contain("\"CrashReportingEnabled\": true");

        // Act
        _sut.Disable();

        // Assert
        var contentAfterDisable = File.ReadAllText(_tempSettingsPath);
        contentAfterDisable.Should().Contain("\"CrashReportingEnabled\": false");
    }

    [Fact]
    public void NewInstance_LoadsPreviouslyEnabledState()
    {
        // Arrange - enable and dispose
        _sut.Enable();
        _sut.Dispose();

        // Act - create new instance
        _sut = CreateService();

        // Assert
        _sut.IsEnabled.Should().BeTrue();
    }

    #endregion

    #region Capture With Tags Tests

    [Fact]
    public void CaptureException_WithTags_WhenEnabled_DoesNotThrow()
    {
        // Arrange
        _sut.Enable();
        var exception = new InvalidOperationException("Test");
        var tags = new Dictionary<string, string>
        {
            ["component"] = "test",
            ["version"] = "1.0.0"
        };

        // Act & Assert
        var act = () => _sut.CaptureException(exception, tags);
        act.Should().NotThrow();
    }

    [Fact]
    public void CaptureMessage_WithLevel_WhenEnabled_DoesNotThrow()
    {
        // Arrange
        _sut.Enable();

        // Act & Assert
        var act = () => _sut.CaptureMessage("Warning message", TelemetryLevel.Warning);
        act.Should().NotThrow();
    }

    #endregion
}
