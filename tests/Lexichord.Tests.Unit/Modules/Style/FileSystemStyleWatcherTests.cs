using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using Lexichord.Modules.Style.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for FileSystemStyleWatcher.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the FileSystemStyleWatcher contract:
/// - License tier gating (WriterPro required)
/// - Start/Stop/Dispose lifecycle
/// - Path validation
/// - Event publishing
/// - Debounce delay configuration
///
/// Note: Tests that involve actual FileSystemWatcher behavior
/// are integration tests and are in a separate test class.
/// </remarks>
public class FileSystemStyleWatcherTests : IDisposable
{
    private readonly Mock<IStyleSheetLoader> _loaderMock;
    private readonly Mock<IStyleEngine> _engineMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly Mock<ILogger<FileSystemStyleWatcher>> _loggerMock;
    private readonly FileSystemStyleWatcher _sut;
    private readonly string _testDirectory;

    public FileSystemStyleWatcherTests()
    {
        _loaderMock = new Mock<IStyleSheetLoader>();
        _engineMock = new Mock<IStyleEngine>();
        _mediatorMock = new Mock<IMediator>();
        _licenseContextMock = new Mock<ILicenseContext>();
        _loggerMock = new Mock<ILogger<FileSystemStyleWatcher>>();

        // LOGIC: Default to WriterPro tier for most tests
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);

        // LOGIC: Setup default engine behavior
        _engineMock.Setup(x => x.GetActiveStyleSheet()).Returns(StyleSheet.Empty);

        _sut = new FileSystemStyleWatcher(
            _loaderMock.Object,
            _engineMock.Object,
            _mediatorMock.Object,
            _licenseContextMock.Object,
            _loggerMock.Object);

        // LOGIC: Create a unique temporary directory for each test
        _testDirectory = Path.Combine(Path.GetTempPath(), $"lexichord_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        _sut.Dispose();
        
        // LOGIC: Clean up test directory
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch
        {
            // LOGIC: Ignore cleanup failures in tests
        }
    }

    #region License Tier Tests

    [Fact]
    public void StartWatching_DoesNotStart_WhenCoreTier()
    {
        // Arrange
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);

        // Act
        _sut.StartWatching(_testDirectory);

        // Assert
        _sut.IsWatching.Should().BeFalse("Core tier should not enable watching");
        _sut.WatchedPath.Should().BeNull();
    }

    [Fact]
    public void StartWatching_Starts_WhenWriterProTier()
    {
        // Arrange
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);

        // Act
        _sut.StartWatching(_testDirectory);

        // Assert
        _sut.IsWatching.Should().BeTrue();
        _sut.WatchedPath.Should().Contain(".lexichord");
    }

    [Fact]
    public void StartWatching_Starts_WhenTeamsTier()
    {
        // Arrange
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Teams);

        // Act
        _sut.StartWatching(_testDirectory);

        // Assert
        _sut.IsWatching.Should().BeTrue("Teams tier should enable watching");
    }

    [Fact]
    public void StartWatching_Starts_WhenEnterpriseTier()
    {
        // Arrange
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Enterprise);

        // Act
        _sut.StartWatching(_testDirectory);

        // Assert
        _sut.IsWatching.Should().BeTrue("Enterprise tier should enable watching");
    }

    #endregion

    #region Lifecycle Tests

    [Fact]
    public void IsWatching_IsFalse_ByDefault()
    {
        _sut.IsWatching.Should().BeFalse();
    }

    [Fact]
    public void WatchedPath_IsNull_ByDefault()
    {
        _sut.WatchedPath.Should().BeNull();
    }

    [Fact]
    public void StartWatching_SetsIsWatchingTrue()
    {
        // Act
        _sut.StartWatching(_testDirectory);

        // Assert
        _sut.IsWatching.Should().BeTrue();
    }

    [Fact]
    public void StartWatching_SetsWatchedPath()
    {
        // Act
        _sut.StartWatching(_testDirectory);

        // Assert
        _sut.WatchedPath.Should().NotBeNull();
        _sut.WatchedPath.Should().EndWith(".lexichord");
    }

    [Fact]
    public void StartWatching_CreatesLexichordDirectory()
    {
        // Arrange
        var lexichordDir = Path.Combine(_testDirectory, ".lexichord");
        Directory.Exists(lexichordDir).Should().BeFalse("precondition: directory should not exist");

        // Act
        _sut.StartWatching(_testDirectory);

        // Assert
        Directory.Exists(lexichordDir).Should().BeTrue();
    }

    [Fact]
    public void StopWatching_SetsIsWatchingFalse()
    {
        // Arrange
        _sut.StartWatching(_testDirectory);
        _sut.IsWatching.Should().BeTrue("precondition");

        // Act
        _sut.StopWatching();

        // Assert
        _sut.IsWatching.Should().BeFalse();
    }

    [Fact]
    public void StopWatching_ClearsWatchedPath()
    {
        // Arrange
        _sut.StartWatching(_testDirectory);
        _sut.WatchedPath.Should().NotBeNull("precondition");

        // Act
        _sut.StopWatching();

        // Assert
        _sut.WatchedPath.Should().BeNull();
    }

    [Fact]
    public void StopWatching_IsIdempotent()
    {
        // Act - calling stop multiple times should not throw
        _sut.StopWatching();
        _sut.StopWatching();
        _sut.StopWatching();

        // Assert
        _sut.IsWatching.Should().BeFalse();
    }

    [Fact]
    public void StartWatching_StopsExistingWatcher_BeforeStartingNew()
    {
        // Arrange
        _sut.StartWatching(_testDirectory);
        var firstPath = _sut.WatchedPath;

        var secondDir = Path.Combine(Path.GetTempPath(), $"lexichord_test2_{Guid.NewGuid():N}");
        Directory.CreateDirectory(secondDir);

        try
        {
            // Act
            _sut.StartWatching(secondDir);

            // Assert
            _sut.WatchedPath.Should().NotBe(firstPath);
            _sut.IsWatching.Should().BeTrue();
        }
        finally
        {
            try { Directory.Delete(secondDir, true); } catch { }
        }
    }

    [Fact]
    public void Dispose_StopsWatching()
    {
        // Arrange
        _sut.StartWatching(_testDirectory);
        _sut.IsWatching.Should().BeTrue("precondition");

        // Act
        _sut.Dispose();

        // Assert
        _sut.IsWatching.Should().BeFalse();
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        // Act - calling dispose multiple times should not throw
        _sut.Dispose();
        _sut.Dispose();
        _sut.Dispose();

        // Assert - no exception thrown
    }

    [Fact]
    public void StartWatching_ThrowsObjectDisposedException_WhenDisposed()
    {
        // Arrange
        _sut.Dispose();

        // Act
        var act = () => _sut.StartWatching(_testDirectory);

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    #endregion

    #region Path Validation Tests

    [Fact]
    public void StartWatching_ThrowsOnNullPath()
    {
        // Act
        var act = () => _sut.StartWatching(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void StartWatching_ThrowsOnEmptyPath()
    {
        // Act
        var act = () => _sut.StartWatching(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void StartWatching_ThrowsOnWhitespacePath()
    {
        // Act
        var act = () => _sut.StartWatching("   ");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Debounce Configuration Tests

    [Fact]
    public void DebounceDelayMs_HasDefaultValue()
    {
        // Assert
        _sut.DebounceDelayMs.Should().Be(300, "default debounce should be 300ms");
    }

    [Fact]
    public void DebounceDelayMs_CanBeChanged()
    {
        // Act
        _sut.DebounceDelayMs = 500;

        // Assert
        _sut.DebounceDelayMs.Should().Be(500);
    }

    #endregion

    #region ForceReloadAsync Tests

    [Fact]
    public async Task ForceReloadAsync_DoesNothing_WhenNotWatching()
    {
        // Arrange
        _sut.IsWatching.Should().BeFalse("precondition");

        // Act
        await _sut.ForceReloadAsync();

        // Assert - should not throw, no loader calls
        _loaderMock.Verify(x => x.LoadFromFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ForceReloadAsync_LoadsFile_WhenFileExists()
    {
        // Arrange
        _sut.StartWatching(_testDirectory);
        var styleFilePath = Path.Combine(_testDirectory, ".lexichord", "style.yaml");
        await File.WriteAllTextAsync(styleFilePath, "name: Test\nrules: []");

        var sheet = new StyleSheet("Test", [], Version: "1.0");
        _loaderMock.Setup(x => x.LoadFromFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sheet);

        // Act
        await _sut.ForceReloadAsync();

        // Assert
        _loaderMock.Verify(x => x.LoadFromFileAsync(styleFilePath, It.IsAny<CancellationToken>()), Times.Once);
        _engineMock.Verify(x => x.SetActiveStyleSheet(sheet), Times.Once);
    }

    [Fact]
    public async Task ForceReloadAsync_DoesNotLoad_WhenFileDoesNotExist()
    {
        // Arrange
        _sut.StartWatching(_testDirectory);
        // Don't create style.yaml

        // Act
        await _sut.ForceReloadAsync();

        // Assert
        _loaderMock.Verify(x => x.LoadFromFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Event Tests

    [Fact]
    public async Task ForceReloadAsync_PublishesMediatREvent_OnSuccess()
    {
        // Arrange
        _sut.StartWatching(_testDirectory);
        var styleFilePath = Path.Combine(_testDirectory, ".lexichord", "style.yaml");
        await File.WriteAllTextAsync(styleFilePath, "name: Test\nrules: []");

        var sheet = new StyleSheet("Test", [], Version: "1.0");
        _loaderMock.Setup(x => x.LoadFromFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sheet);

        // Act
        await _sut.ForceReloadAsync();

        // Assert
        _mediatorMock.Verify(x => x.Publish(
            It.Is<StyleSheetReloadedEvent>(e => 
                e.NewStyleSheet == sheet && 
                e.ReloadSource == StyleReloadSource.ManualReload),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForceReloadAsync_RaisesLocalEvent_OnSuccess()
    {
        // Arrange
        _sut.StartWatching(_testDirectory);
        var styleFilePath = Path.Combine(_testDirectory, ".lexichord", "style.yaml");
        await File.WriteAllTextAsync(styleFilePath, "name: Test\nrules: []");

        var sheet = new StyleSheet("Test", [], Version: "1.0");
        _loaderMock.Setup(x => x.LoadFromFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sheet);

        StyleFileChangedEventArgs? eventArgs = null;
        _sut.FileChanged += (_, args) => eventArgs = args;

        // Act
        await _sut.ForceReloadAsync();

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.FilePath.Should().Be(styleFilePath);
    }

    [Fact]
    public async Task ForceReloadAsync_PublishesErrorEvent_OnFailure()
    {
        // Arrange
        _sut.StartWatching(_testDirectory);
        var styleFilePath = Path.Combine(_testDirectory, ".lexichord", "style.yaml");
        await File.WriteAllTextAsync(styleFilePath, "invalid yaml: {{{");

        _loaderMock.Setup(x => x.LoadFromFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("YAML parse error"));

        // Act
        await _sut.ForceReloadAsync();

        // Assert
        _mediatorMock.Verify(x => x.Publish(
            It.Is<StyleWatcherErrorEvent>(e => e.ErrorType == StyleWatcherErrorType.YamlParseError),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForceReloadAsync_RaisesWatcherError_OnFailure()
    {
        // Arrange
        _sut.StartWatching(_testDirectory);
        var styleFilePath = Path.Combine(_testDirectory, ".lexichord", "style.yaml");
        await File.WriteAllTextAsync(styleFilePath, "invalid yaml");

        _loaderMock.Setup(x => x.LoadFromFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Parse error"));

        StyleWatcherErrorEventArgs? errorArgs = null;
        _sut.WatcherError += (_, args) => errorArgs = args;

        // Act
        await _sut.ForceReloadAsync();

        // Assert
        errorArgs.Should().NotBeNull();
        errorArgs!.Exception.Should().BeOfType<InvalidOperationException>();
    }

    #endregion

    #region Graceful Fallback Tests

    [Fact]
    public async Task ForceReloadAsync_KeepsPreviousRules_OnParseError()
    {
        // Arrange
        var previousSheet = new StyleSheet("Previous", [], Version: "1.0");
        _engineMock.Setup(x => x.GetActiveStyleSheet()).Returns(previousSheet);

        _sut.StartWatching(_testDirectory);
        var styleFilePath = Path.Combine(_testDirectory, ".lexichord", "style.yaml");
        await File.WriteAllTextAsync(styleFilePath, "bad yaml");

        _loaderMock.Setup(x => x.LoadFromFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("YAML error"));

        // Act
        await _sut.ForceReloadAsync();

        // Assert - engine should NOT be updated with a new sheet on error
        _engineMock.Verify(x => x.SetActiveStyleSheet(It.IsAny<StyleSheet>()), Times.Never);
    }

    #endregion
}
