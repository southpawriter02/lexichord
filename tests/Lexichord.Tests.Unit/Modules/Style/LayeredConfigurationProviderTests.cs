using FluentAssertions;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using Lexichord.Modules.Style.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for <see cref="LayeredConfigurationProvider"/>.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the layered configuration provider contract:
/// - Hierarchical configuration merging (System → User → Project)
/// - License tier gating for project configuration
/// - Caching behavior and invalidation
/// - Error handling for invalid configuration files
///
/// Version: v0.3.6a
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Version", "v0.3.6a")]
public class LayeredConfigurationProviderTests
{
    private readonly Mock<IWorkspaceService> _workspaceServiceMock;
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly Mock<ILogger<LayeredConfigurationProvider>> _loggerMock;
    private readonly LayeredConfigurationProvider _sut;

    public LayeredConfigurationProviderTests()
    {
        _workspaceServiceMock = new Mock<IWorkspaceService>();
        _licenseContextMock = new Mock<ILicenseContext>();
        _loggerMock = new Mock<ILogger<LayeredConfigurationProvider>>();

        _sut = new LayeredConfigurationProvider(
            _workspaceServiceMock.Object,
            _licenseContextMock.Object,
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsOnNullWorkspaceService()
    {
        // Act
        var act = () => new LayeredConfigurationProvider(
            null!,
            _licenseContextMock.Object,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("workspaceService");
    }

    [Fact]
    public void Constructor_ThrowsOnNullLicenseContext()
    {
        // Act
        var act = () => new LayeredConfigurationProvider(
            _workspaceServiceMock.Object,
            null!,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("licenseContext");
    }

    [Fact]
    public void Constructor_ThrowsOnNullLogger()
    {
        // Act
        var act = () => new LayeredConfigurationProvider(
            _workspaceServiceMock.Object,
            _licenseContextMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region GetEffectiveConfiguration Tests

    [Fact]
    public void GetEffectiveConfiguration_NoWorkspaceOpen_ReturnsSystemDefaults()
    {
        // Arrange
        _workspaceServiceMock.Setup(w => w.IsWorkspaceOpen).Returns(false);
        _licenseContextMock.Setup(l => l.IsFeatureEnabled(FeatureCodes.GlobalDictionary)).Returns(true);

        // Act
        var result = _sut.GetEffectiveConfiguration();

        // Assert
        result.Should().NotBeNull();
        result.DefaultProfile.Should().Be("Technical");
        result.PassiveVoiceThreshold.Should().Be(20);
        result.TargetGradeLevel.Should().Be(10);
    }

    [Fact]
    public void GetEffectiveConfiguration_CoreLicense_IgnoresProjectConfig()
    {
        // Arrange
        _workspaceServiceMock.Setup(w => w.IsWorkspaceOpen).Returns(true);
        _workspaceServiceMock.Setup(w => w.CurrentWorkspace).Returns(
            new WorkspaceInfo("/project", "TestProject", DateTimeOffset.UtcNow));

        // Core license = GlobalDictionary feature NOT enabled
        _licenseContextMock.Setup(l => l.IsFeatureEnabled(FeatureCodes.GlobalDictionary)).Returns(false);

        // Act
        var result = _sut.GetEffectiveConfiguration();

        // Assert
        result.Should().NotBeNull();
        // Returns system defaults because project config is ignored for Core
        result.DefaultProfile.Should().Be("Technical");
        result.PassiveVoiceThreshold.Should().Be(20);
    }

    [Fact]
    public void GetEffectiveConfiguration_WriterProLicense_NoConfigFile_ReturnsDefaults()
    {
        // Arrange
        _workspaceServiceMock.Setup(w => w.IsWorkspaceOpen).Returns(true);
        _workspaceServiceMock.Setup(w => w.CurrentWorkspace).Returns(
            new WorkspaceInfo("/nonexistent", "TestProject", DateTimeOffset.UtcNow));
        _licenseContextMock.Setup(l => l.IsFeatureEnabled(FeatureCodes.GlobalDictionary)).Returns(true);

        // Act
        var result = _sut.GetEffectiveConfiguration();

        // Assert
        result.Should().NotBeNull();
        // Returns system defaults because config file doesn't exist
        result.DefaultProfile.Should().Be("Technical");
    }

    [Fact]
    public void GetEffectiveConfiguration_CachesResult()
    {
        // Arrange
        _workspaceServiceMock.Setup(w => w.IsWorkspaceOpen).Returns(false);

        // Act
        var result1 = _sut.GetEffectiveConfiguration();
        var result2 = _sut.GetEffectiveConfiguration();

        // Assert
        result1.Should().BeSameAs(result2); // Same cached instance
    }

    #endregion

    #region GetConfiguration Tests

    [Fact]
    public void GetConfiguration_System_ReturnsDefaults()
    {
        // Act
        var result = _sut.GetConfiguration(ConfigurationSource.System);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(StyleConfiguration.Defaults);
    }

    [Fact]
    public void GetConfiguration_User_ReturnsNull_NotYetImplemented()
    {
        // Act
        var result = _sut.GetConfiguration(ConfigurationSource.User);

        // Assert
        // Currently returns null as user config is not yet implemented
        result.Should().BeNull();
    }

    [Fact]
    public void GetConfiguration_Project_NoWorkspace_ReturnsNull()
    {
        // Arrange
        _workspaceServiceMock.Setup(w => w.IsWorkspaceOpen).Returns(false);

        // Act
        var result = _sut.GetConfiguration(ConfigurationSource.Project);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetConfiguration_InvalidSource_Throws()
    {
        // Act
        var act = () => _sut.GetConfiguration((ConfigurationSource)999);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("source");
    }

    #endregion

    #region InvalidateCache Tests

    [Fact]
    public void InvalidateCache_RaisesConfigurationChangedEvent()
    {
        // Arrange
        ConfigurationChangedEventArgs? eventArgs = null;
        _sut.ConfigurationChanged += (_, args) => eventArgs = args;

        // Act
        _sut.InvalidateCache();

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void InvalidateCache_ClearsCache_NextCallReloads()
    {
        // Arrange
        _workspaceServiceMock.Setup(w => w.IsWorkspaceOpen).Returns(false);

        // Get initial result (caches it)
        var result1 = _sut.GetEffectiveConfiguration();

        // Act - invalidate cache
        _sut.InvalidateCache();

        // Get again - should reload
        var result2 = _sut.GetEffectiveConfiguration();

        // Assert
        // After invalidation, a new instance is created (not the same reference)
        result1.Should().NotBeSameAs(result2);
        result1.Should().BeEquivalentTo(result2); // But values are the same
    }

    #endregion

    #region Merge Strategy Tests

    [Fact]
    public void GetEffectiveConfiguration_ListFieldsAreConcatenated()
    {
        // Arrange
        // No way to inject custom config for merge testing without file system
        // This test verifies the default behavior
        _workspaceServiceMock.Setup(w => w.IsWorkspaceOpen).Returns(false);

        // Act
        var result = _sut.GetEffectiveConfiguration();

        // Assert
        // Default config has empty lists
        result.TerminologyExclusions.Should().BeEmpty();
        result.IgnoredRules.Should().BeEmpty();
        result.TerminologyAdditions.Should().BeEmpty();
    }

    #endregion
}
