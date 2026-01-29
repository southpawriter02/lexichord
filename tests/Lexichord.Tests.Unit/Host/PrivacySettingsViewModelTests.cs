using Lexichord.Abstractions.Contracts;
using Lexichord.Host.Settings;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Host;

/// <summary>
/// Unit tests for PrivacySettingsViewModel.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify:
/// - Initial state matches telemetry service state
/// - Toggle calls correct service methods
/// - Property change notifications fire correctly
/// </remarks>
public class PrivacySettingsViewModelTests
{
    private readonly Mock<ITelemetryService> _mockTelemetryService;
    private readonly Mock<ILogger<PrivacySettingsViewModel>> _mockLogger;

    public PrivacySettingsViewModelTests()
    {
        _mockTelemetryService = new Mock<ITelemetryService>();
        _mockLogger = new Mock<ILogger<PrivacySettingsViewModel>>();
    }

    private PrivacySettingsViewModel CreateViewModel(bool isEnabled = false)
    {
        _mockTelemetryService.Setup(s => s.IsEnabled).Returns(isEnabled);
        return new PrivacySettingsViewModel(
            _mockTelemetryService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void CrashReportingEnabled_WhenServiceDisabled_ReturnsFalse()
    {
        // Arrange
        var sut = CreateViewModel(isEnabled: false);

        // Assert
        sut.CrashReportingEnabled.Should().BeFalse();
    }

    [Fact]
    public void CrashReportingEnabled_WhenServiceEnabled_ReturnsTrue()
    {
        // Arrange
        var sut = CreateViewModel(isEnabled: true);

        // Assert
        sut.CrashReportingEnabled.Should().BeTrue();
    }

    [Fact]
    public void SettingCrashReportingEnabled_ToTrue_CallsServiceEnable()
    {
        // Arrange
        var sut = CreateViewModel(isEnabled: false);

        // Act
        sut.CrashReportingEnabled = true;

        // Assert
        _mockTelemetryService.Verify(s => s.Enable(), Times.Once);
    }

    [Fact]
    public void SettingCrashReportingEnabled_ToFalse_CallsServiceDisable()
    {
        // Arrange
        var sut = CreateViewModel(isEnabled: true);

        // Act
        sut.CrashReportingEnabled = false;

        // Assert
        _mockTelemetryService.Verify(s => s.Disable(), Times.Once);
    }

    [Fact]
    public void SettingCrashReportingEnabled_RaisesPropertyChanged()
    {
        // Arrange
        var sut = CreateViewModel(isEnabled: false);
        var propertyChangedRaised = false;
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(sut.CrashReportingEnabled))
                propertyChangedRaised = true;
        };

        // Act
        sut.CrashReportingEnabled = true;

        // Assert
        propertyChangedRaised.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithNullTelemetryService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new PrivacySettingsViewModel(null!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("telemetryService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new PrivacySettingsViewModel(_mockTelemetryService.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("logger");
    }

    [Fact]
    public void LearnMoreCommand_CanExecute_ReturnsTrue()
    {
        // Arrange
        var sut = CreateViewModel();

        // Assert
        sut.LearnMoreCommand.CanExecute(null).Should().BeTrue();
    }
}
