using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Modules.Style.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for <see cref="OverrideMenuViewModel"/>.
/// </summary>
/// <remarks>
/// Tests cover license checking, workspace validation, and command execution.
///
/// Version: v0.3.6c
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.3.6c")]
public class OverrideMenuViewModelTests
{
    private readonly Mock<ILicenseContext> _mockLicense;
    private readonly Mock<IWorkspaceService> _mockWorkspace;
    private readonly Mock<IProjectConfigurationWriter> _mockConfigWriter;
    private readonly Mock<ILintingOrchestrator> _mockOrchestrator;

    public OverrideMenuViewModelTests()
    {
        _mockLicense = new Mock<ILicenseContext>();
        _mockWorkspace = new Mock<IWorkspaceService>();
        _mockConfigWriter = new Mock<IProjectConfigurationWriter>();
        _mockOrchestrator = new Mock<ILintingOrchestrator>();
    }

    #region CanIgnoreRule Tests

    [Fact]
    public void CanIgnoreRule_WriterProLicenseAndWorkspaceOpen_ReturnsTrue()
    {
        // Arrange
        _mockLicense.Setup(l => l.IsFeatureEnabled(It.IsAny<string>())).Returns(true);
        _mockWorkspace.Setup(w => w.IsWorkspaceOpen).Returns(true);

        var vm = CreateViewModel();

        // Act & Assert
        vm.CanIgnoreRule.Should().BeTrue();
    }

    [Fact]
    public void CanIgnoreRule_CoreLicense_ReturnsFalse()
    {
        // Arrange
        _mockLicense.Setup(l => l.IsFeatureEnabled(It.IsAny<string>())).Returns(false);
        _mockWorkspace.Setup(w => w.IsWorkspaceOpen).Returns(true);

        var vm = CreateViewModel();

        // Act & Assert
        vm.CanIgnoreRule.Should().BeFalse();
    }

    [Fact]
    public void CanIgnoreRule_NoWorkspaceOpen_ReturnsFalse()
    {
        // Arrange
        _mockLicense.Setup(l => l.IsFeatureEnabled(It.IsAny<string>())).Returns(true);
        _mockWorkspace.Setup(w => w.IsWorkspaceOpen).Returns(false);

        var vm = CreateViewModel();

        // Act & Assert
        vm.CanIgnoreRule.Should().BeFalse();
    }

    [Fact]
    public void CanIgnoreRule_NoLicenseAndNoWorkspace_ReturnsFalse()
    {
        // Arrange
        _mockLicense.Setup(l => l.IsFeatureEnabled(It.IsAny<string>())).Returns(false);
        _mockWorkspace.Setup(w => w.IsWorkspaceOpen).Returns(false);

        var vm = CreateViewModel();

        // Act & Assert
        vm.CanIgnoreRule.Should().BeFalse();
    }

    #endregion

    #region RequiresLicenseUpgrade Tests

    [Fact]
    public void RequiresLicenseUpgrade_CoreLicense_ReturnsTrue()
    {
        // Arrange
        _mockLicense.Setup(l => l.IsFeatureEnabled(It.IsAny<string>())).Returns(false);

        var vm = CreateViewModel();

        // Act & Assert
        vm.RequiresLicenseUpgrade.Should().BeTrue();
    }

    [Fact]
    public void RequiresLicenseUpgrade_WriterProLicense_ReturnsFalse()
    {
        // Arrange
        _mockLicense.Setup(l => l.IsFeatureEnabled(It.IsAny<string>())).Returns(true);

        var vm = CreateViewModel();

        // Act & Assert
        vm.RequiresLicenseUpgrade.Should().BeFalse();
    }

    #endregion

    #region IsLicensedButNoWorkspace Tests

    [Fact]
    public void IsLicensedButNoWorkspace_LicensedNoWorkspace_ReturnsTrue()
    {
        // Arrange
        _mockLicense.Setup(l => l.IsFeatureEnabled(It.IsAny<string>())).Returns(true);
        _mockWorkspace.Setup(w => w.IsWorkspaceOpen).Returns(false);

        var vm = CreateViewModel();

        // Act & Assert
        vm.IsLicensedButNoWorkspace.Should().BeTrue();
    }

    [Fact]
    public void IsLicensedButNoWorkspace_LicensedWithWorkspace_ReturnsFalse()
    {
        // Arrange
        _mockLicense.Setup(l => l.IsFeatureEnabled(It.IsAny<string>())).Returns(true);
        _mockWorkspace.Setup(w => w.IsWorkspaceOpen).Returns(true);

        var vm = CreateViewModel();

        // Act & Assert
        vm.IsLicensedButNoWorkspace.Should().BeFalse();
    }

    #endregion

    #region ConfigurationFilePath Tests

    [Fact]
    public void ConfigurationFilePath_ReturnsWriterPath()
    {
        // Arrange
        _mockConfigWriter.Setup(cw => cw.GetConfigurationFilePath())
            .Returns("/test/path/.lexichord/style.yaml");

        var vm = CreateViewModel();

        // Act
        var path = vm.ConfigurationFilePath;

        // Assert
        path.Should().Be("/test/path/.lexichord/style.yaml");
    }

    [Fact]
    public void ConfigurationFilePath_NoWorkspace_ReturnsNull()
    {
        // Arrange
        _mockConfigWriter.Setup(cw => cw.GetConfigurationFilePath())
            .Returns((string?)null);

        var vm = CreateViewModel();

        // Act
        var path = vm.ConfigurationFilePath;

        // Assert
        path.Should().BeNull();
    }

    #endregion

    #region ShowConfirmationDialogs Tests

    [Fact]
    public void ShowConfirmationDialogs_DefaultsToTrue()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act & Assert
        vm.ShowConfirmationDialogs.Should().BeTrue();
    }

    [Fact]
    public void ShowConfirmationDialogs_CanBeSet()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.ShowConfirmationDialogs = false;

        // Assert
        vm.ShowConfirmationDialogs.Should().BeFalse();
    }

    #endregion

    #region IsRuleCurrentlyIgnored Tests

    [Fact]
    public void IsRuleCurrentlyIgnored_DelegatesToConfigWriter()
    {
        // Arrange
        _mockConfigWriter.Setup(cw => cw.IsRuleIgnored("TERM-001")).Returns(true);

        var vm = CreateViewModel();

        // Act
        var result = vm.IsRuleCurrentlyIgnored("TERM-001");

        // Assert
        result.Should().BeTrue();
        _mockConfigWriter.Verify(cw => cw.IsRuleIgnored("TERM-001"), Times.Once);
    }

    #endregion

    #region IsTermCurrentlyExcluded Tests

    [Fact]
    public void IsTermCurrentlyExcluded_DelegatesToConfigWriter()
    {
        // Arrange
        _mockConfigWriter.Setup(cw => cw.IsTermExcluded("whitelist")).Returns(true);

        var vm = CreateViewModel();

        // Act
        var result = vm.IsTermCurrentlyExcluded("whitelist");

        // Assert
        result.Should().BeTrue();
        _mockConfigWriter.Verify(cw => cw.IsTermExcluded("whitelist"), Times.Once);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullLicenseContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OverrideMenuViewModel(
            null!,
            _mockWorkspace.Object,
            _mockConfigWriter.Object,
            _mockOrchestrator.Object));
    }

    [Fact]
    public void Constructor_NullWorkspaceService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OverrideMenuViewModel(
            _mockLicense.Object,
            null!,
            _mockConfigWriter.Object,
            _mockOrchestrator.Object));
    }

    [Fact]
    public void Constructor_NullConfigWriter_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OverrideMenuViewModel(
            _mockLicense.Object,
            _mockWorkspace.Object,
            null!,
            _mockOrchestrator.Object));
    }

    [Fact]
    public void Constructor_NullOrchestrator_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OverrideMenuViewModel(
            _mockLicense.Object,
            _mockWorkspace.Object,
            _mockConfigWriter.Object,
            null!));
    }

    [Fact]
    public void Constructor_NullLogger_DoesNotThrow()
    {
        // Act & Assert - should succeed without logger
        var vm = new OverrideMenuViewModel(
            _mockLicense.Object,
            _mockWorkspace.Object,
            _mockConfigWriter.Object,
            _mockOrchestrator.Object,
            logger: null);

        vm.Should().NotBeNull();
    }

    #endregion

    private OverrideMenuViewModel CreateViewModel()
    {
        return new OverrideMenuViewModel(
            _mockLicense.Object,
            _mockWorkspace.Object,
            _mockConfigWriter.Object,
            _mockOrchestrator.Object,
            NullLogger<OverrideMenuViewModel>.Instance);
    }
}
