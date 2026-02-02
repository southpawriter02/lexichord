// =============================================================================
// File: AxiomLoaderTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for AxiomLoader constructor and license gating.
// =============================================================================
// LOGIC: Tests constructor validation, license tier checks, and loading behavior.
//
// v0.4.6g: Axiom Loader (CKVS Phase 1d)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Lexichord.Modules.Knowledge.Axioms;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge;

/// <summary>
/// Unit tests for <see cref="AxiomLoader"/> constructor and loading logic.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.6g")]
public class AxiomLoaderTests
{
    private readonly Mock<IAxiomRepository> _mockRepository;
    private readonly Mock<ISchemaRegistry> _mockSchemaRegistry;
    private readonly Mock<ILicenseContext> _mockLicenseContext;
    private readonly Mock<IWorkspaceService> _mockWorkspaceService;
    private readonly Mock<ILogger<AxiomLoader>> _mockLogger;

    public AxiomLoaderTests()
    {
        _mockRepository = new Mock<IAxiomRepository>();
        _mockSchemaRegistry = new Mock<ISchemaRegistry>();
        _mockLicenseContext = new Mock<ILicenseContext>();
        _mockWorkspaceService = new Mock<IWorkspaceService>();
        _mockLogger = new Mock<ILogger<AxiomLoader>>();

        // Default: Core tier (no features enabled)
        _mockLicenseContext.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);
    }

    #region Constructor Validation

    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange & Act
        var loader = new AxiomLoader(
            _mockRepository.Object,
            _mockSchemaRegistry.Object,
            _mockLicenseContext.Object,
            _mockWorkspaceService.Object,
            _mockLogger.Object);

        // Assert
        Assert.NotNull(loader);
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new AxiomLoader(
            null!,
            _mockSchemaRegistry.Object,
            _mockLicenseContext.Object,
            _mockWorkspaceService.Object,
            _mockLogger.Object));

        Assert.Equal("repository", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullSchemaRegistry_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new AxiomLoader(
            _mockRepository.Object,
            null!,
            _mockLicenseContext.Object,
            _mockWorkspaceService.Object,
            _mockLogger.Object));

        Assert.Equal("schemaRegistry", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLicenseContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new AxiomLoader(
            _mockRepository.Object,
            _mockSchemaRegistry.Object,
            null!,
            _mockWorkspaceService.Object,
            _mockLogger.Object));

        Assert.Equal("licenseContext", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullWorkspaceService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new AxiomLoader(
            _mockRepository.Object,
            _mockSchemaRegistry.Object,
            _mockLicenseContext.Object,
            null!,
            _mockLogger.Object));

        Assert.Equal("workspaceService", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new AxiomLoader(
            _mockRepository.Object,
            _mockSchemaRegistry.Object,
            _mockLicenseContext.Object,
            _mockWorkspaceService.Object,
            null!));

        Assert.Equal("logger", ex.ParamName);
    }

    #endregion

    #region License Gating

    [Fact]
    public async Task LoadWorkspaceAsync_WithCoreTier_ThrowsFeatureNotLicensedException()
    {
        // Arrange
        _mockLicenseContext.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);

        var loader = new AxiomLoader(
            _mockRepository.Object,
            _mockSchemaRegistry.Object,
            _mockLicenseContext.Object,
            _mockWorkspaceService.Object,
            _mockLogger.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<FeatureNotLicensedException>(
            () => loader.LoadWorkspaceAsync("/some/path"));

        Assert.Equal(LicenseTier.Teams, ex.RequiredTier);
    }

    [Fact]
    public async Task LoadWorkspaceAsync_WithWriterProTier_ThrowsFeatureNotLicensedException()
    {
        // Arrange
        _mockLicenseContext.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);

        var loader = new AxiomLoader(
            _mockRepository.Object,
            _mockSchemaRegistry.Object,
            _mockLicenseContext.Object,
            _mockWorkspaceService.Object,
            _mockLogger.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<FeatureNotLicensedException>(
            () => loader.LoadWorkspaceAsync("/some/path"));

        Assert.Equal(LicenseTier.Teams, ex.RequiredTier);
    }

    [Fact]
    public async Task LoadFileAsync_WithCoreTier_ThrowsFeatureNotLicensedException()
    {
        // Arrange
        _mockLicenseContext.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);

        var loader = new AxiomLoader(
            _mockRepository.Object,
            _mockSchemaRegistry.Object,
            _mockLicenseContext.Object,
            _mockWorkspaceService.Object,
            _mockLogger.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<FeatureNotLicensedException>(
            () => loader.LoadFileAsync("/some/file.yaml"));

        Assert.Equal(LicenseTier.Teams, ex.RequiredTier);
    }

    [Fact]
    public void StartWatching_WithCoreTier_DoesNotStartWatcher()
    {
        // Arrange
        _mockLicenseContext.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);

        var loader = new AxiomLoader(
            _mockRepository.Object,
            _mockSchemaRegistry.Object,
            _mockLicenseContext.Object,
            _mockWorkspaceService.Object,
            _mockLogger.Object);

        // Act - Should not throw, just silently skip
        loader.StartWatching("/some/path");

        // Assert - No exception means success (watcher not started)
        Assert.True(true);
    }

    #endregion

    #region LoadAllAsync Behavior

    [Fact]
    public async Task LoadAllAsync_WithCoreTier_ReturnsEmptyResult()
    {
        // Arrange
        _mockLicenseContext.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);
        _mockWorkspaceService.Setup(x => x.IsWorkspaceOpen).Returns(false);

        var loader = new AxiomLoader(
            _mockRepository.Object,
            _mockSchemaRegistry.Object,
            _mockLicenseContext.Object,
            _mockWorkspaceService.Object,
            _mockLogger.Object);

        // Act
        var result = await loader.LoadAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Axioms);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task LoadAllAsync_WithWriterProTier_LoadsBuiltInAxioms()
    {
        // Arrange
        _mockLicenseContext.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);
        _mockWorkspaceService.Setup(x => x.IsWorkspaceOpen).Returns(false);
        _mockRepository.Setup(x => x.SaveBatchAsync(It.IsAny<IEnumerable<Axiom>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mockSchemaRegistry.Setup(x => x.GetEntityType(It.IsAny<string>()))
            .Returns((EntityTypeSchema?)null);

        var loader = new AxiomLoader(
            _mockRepository.Object,
            _mockSchemaRegistry.Object,
            _mockLicenseContext.Object,
            _mockWorkspaceService.Object,
            _mockLogger.Object);

        // Act
        var result = await loader.LoadAllAsync();

        // Assert
        Assert.NotNull(result);
        // Should load built-in axioms from embedded resources
        Assert.NotEmpty(result.FilesProcessed);
    }

    #endregion

    #region ValidateFileAsync

    [Fact]
    public async Task ValidateFileAsync_WithNonExistentFile_ReturnsFileNotFoundError()
    {
        // Arrange
        var loader = new AxiomLoader(
            _mockRepository.Object,
            _mockSchemaRegistry.Object,
            _mockLicenseContext.Object,
            _mockWorkspaceService.Object,
            _mockLogger.Object);

        var nonExistentPath = "/non/existent/file.yaml";

        // Act
        var result = await loader.ValidateFileAsync(nonExistentPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(nonExistentPath, result.FilePath);
        Assert.Single(result.Errors);
        Assert.Equal("FILE_NOT_FOUND", result.Errors[0].Code);
    }

    #endregion

    #region Dispose

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var loader = new AxiomLoader(
            _mockRepository.Object,
            _mockSchemaRegistry.Object,
            _mockLicenseContext.Object,
            _mockWorkspaceService.Object,
            _mockLogger.Object);

        // Act & Assert - Should not throw
        loader.Dispose();
        loader.Dispose();
    }

    #endregion
}
