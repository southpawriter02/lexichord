using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Workspace.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Workspace;

/// <summary>
/// Unit tests for RobustFileSystemWatcher.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the RobustFileSystemWatcher implementation:
/// - Initial state and property defaults
/// - StartWatching/StopWatching lifecycle
/// - Ignore pattern matching
/// - Debouncing and batching behavior
/// - Disposal cleanup
///
/// Note: Some aspects of file system watching are inherently integration-level
/// (actual file events from OS). These tests focus on the configurable logic
/// and state management that can be unit tested.
/// </remarks>
public class RobustFileSystemWatcherTests : IDisposable
{
    private readonly ILogger<RobustFileSystemWatcher> _mockLogger;
    private readonly RobustFileSystemWatcher _sut;
    private readonly string _testDirectory;

    public RobustFileSystemWatcherTests()
    {
        _mockLogger = Substitute.For<ILogger<RobustFileSystemWatcher>>();
        _sut = new RobustFileSystemWatcher(_mockLogger);

        // Create a temporary directory for testing
        _testDirectory = Path.Combine(Path.GetTempPath(), $"fsw_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        _sut?.Dispose();

        // Clean up test directory
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #region Initial State Tests

    [Fact]
    public void Constructor_SetsDefaultDebounceDelay()
    {
        // Assert
        _sut.DebounceDelayMs.Should().Be(100);
    }

    [Fact]
    public void Constructor_SetsDefaultIgnorePatterns()
    {
        // Assert
        _sut.IgnorePatterns.Should().Contain(".git");
        _sut.IgnorePatterns.Should().Contain("node_modules");
        _sut.IgnorePatterns.Should().Contain(".DS_Store");
        _sut.IgnorePatterns.Should().Contain("*.tmp");
        _sut.IgnorePatterns.Should().Contain("~$*");
    }

    [Fact]
    public void Initially_IsNotWatching()
    {
        // Assert
        _sut.IsWatching.Should().BeFalse();
    }

    [Fact]
    public void Initially_WatchPathIsNull()
    {
        // Assert
        _sut.WatchPath.Should().BeNull();
    }

    #endregion

    #region StartWatching Tests

    [Fact]
    public void StartWatching_WithValidPath_SetsIsWatchingToTrue()
    {
        // Act
        _sut.StartWatching(_testDirectory);

        // Assert
        _sut.IsWatching.Should().BeTrue();
    }

    [Fact]
    public void StartWatching_WithValidPath_SetsWatchPath()
    {
        // Act
        _sut.StartWatching(_testDirectory);

        // Assert
        _sut.WatchPath.Should().NotBeNull();
        _sut.WatchPath.Should().Contain(Path.GetFileName(_testDirectory));
    }

    [Fact]
    public void StartWatching_WithNonExistentPath_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}");

        // Act & Assert
        var action = () => _sut.StartWatching(nonExistentPath);
        action.Should().Throw<DirectoryNotFoundException>();
    }

    [Fact]
    public void StartWatching_WithNullPath_ThrowsArgumentException()
    {
        // Act & Assert
        var action = () => _sut.StartWatching(null!);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void StartWatching_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        var action = () => _sut.StartWatching(string.Empty);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void StartWatching_WhenAlreadyWatching_StopsOldAndStartsNew()
    {
        // Arrange
        var secondTestDir = Path.Combine(Path.GetTempPath(), $"fsw_test2_{Guid.NewGuid():N}");
        Directory.CreateDirectory(secondTestDir);

        try
        {
            _sut.StartWatching(_testDirectory);
            var originalPath = _sut.WatchPath;

            // Act
            _sut.StartWatching(secondTestDir);

            // Assert
            _sut.IsWatching.Should().BeTrue();
            _sut.WatchPath.Should().NotBe(originalPath);
            _sut.WatchPath.Should().Contain(Path.GetFileName(secondTestDir));
        }
        finally
        {
            if (Directory.Exists(secondTestDir))
            {
                Directory.Delete(secondTestDir, recursive: true);
            }
        }
    }

    [Fact]
    public void StartWatching_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _sut.Dispose();

        // Act & Assert
        var action = () => _sut.StartWatching(_testDirectory);
        action.Should().Throw<ObjectDisposedException>();
    }

    #endregion

    #region StopWatching Tests

    [Fact]
    public void StopWatching_WhenWatching_SetsIsWatchingToFalse()
    {
        // Arrange
        _sut.StartWatching(_testDirectory);
        _sut.IsWatching.Should().BeTrue();

        // Act
        _sut.StopWatching();

        // Assert
        _sut.IsWatching.Should().BeFalse();
    }

    [Fact]
    public void StopWatching_WhenWatching_ClearsWatchPath()
    {
        // Arrange
        _sut.StartWatching(_testDirectory);

        // Act
        _sut.StopWatching();

        // Assert
        _sut.WatchPath.Should().BeNull();
    }

    [Fact]
    public void StopWatching_WhenNotWatching_DoesNotThrow()
    {
        // Assert initial state
        _sut.IsWatching.Should().BeFalse();

        // Act & Assert - should not throw
        var action = () => _sut.StopWatching();
        action.Should().NotThrow();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_WhenWatching_StopsWatching()
    {
        // Arrange
        _sut.StartWatching(_testDirectory);

        // Act
        _sut.Dispose();

        // Assert
        _sut.IsWatching.Should().BeFalse();
        _sut.WatchPath.Should().BeNull();
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_DoesNotThrow()
    {
        // Act & Assert - should not throw
        var action = () =>
        {
            _sut.Dispose();
            _sut.Dispose();
            _sut.Dispose();
        };
        action.Should().NotThrow();
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void DebounceDelayMs_CanBeChanged()
    {
        // Act
        _sut.DebounceDelayMs = 500;

        // Assert
        _sut.DebounceDelayMs.Should().Be(500);
    }

    [Fact]
    public void IgnorePatterns_CanBeModified()
    {
        // Arrange
        var originalCount = _sut.IgnorePatterns.Count;

        // Act
        _sut.IgnorePatterns.Add("*.custom");

        // Assert
        _sut.IgnorePatterns.Count.Should().Be(originalCount + 1);
        _sut.IgnorePatterns.Should().Contain("*.custom");
    }

    [Fact]
    public void IgnorePatterns_CanBeCleared()
    {
        // Act
        _sut.IgnorePatterns.Clear();

        // Assert
        _sut.IgnorePatterns.Should().BeEmpty();
    }

    #endregion
}

/// <summary>
/// Unit tests for ignore pattern matching logic.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the ShouldIgnore method logic:
/// - Extension patterns (*.tmp, *.temp)
/// - Prefix patterns (~$*)
/// - Directory patterns (.git, node_modules)
/// - Case insensitivity
/// </remarks>
public class IgnorePatternTests : IDisposable
{
    private readonly ILogger<RobustFileSystemWatcher> _mockLogger;
    private readonly RobustFileSystemWatcher _sut;

    public IgnorePatternTests()
    {
        _mockLogger = Substitute.For<ILogger<RobustFileSystemWatcher>>();
        _sut = new RobustFileSystemWatcher(_mockLogger);
    }

    public void Dispose()
    {
        _sut?.Dispose();
    }

    #region Extension Pattern Tests

    [Theory]
    [InlineData("/path/to/file.tmp")]
    [InlineData("/path/to/file.TMP")]
    [InlineData("C:\\path\\to\\file.tmp")]
    public void ShouldIgnore_TmpExtension_ReturnsTrue(string path)
    {
        // Assert
        _sut.ShouldIgnore(path).Should().BeTrue();
    }

    [Theory]
    [InlineData("/path/to/file.temp")]
    [InlineData("/path/to/file.TEMP")]
    public void ShouldIgnore_TempExtension_ReturnsTrue(string path)
    {
        // Assert
        _sut.ShouldIgnore(path).Should().BeTrue();
    }

    [Theory]
    [InlineData("/path/to/file.txt")]
    [InlineData("/path/to/file.cs")]
    public void ShouldIgnore_NormalExtension_ReturnsFalse(string path)
    {
        // Assert
        _sut.ShouldIgnore(path).Should().BeFalse();
    }

    #endregion

    #region Prefix Pattern Tests

    [Theory]
    [InlineData("/path/to/~$document.docx")]
    [InlineData("/path/to/~$spreadsheet.xlsx")]
    public void ShouldIgnore_TildePrefix_ReturnsTrue(string path)
    {
        // Assert
        _sut.ShouldIgnore(path).Should().BeTrue();
    }

    [Theory]
    [InlineData("/path/to/document.docx")]
    [InlineData("/path/to/$document.docx")]
    public void ShouldIgnore_NoTildePrefix_ReturnsFalse(string path)
    {
        // Assert
        _sut.ShouldIgnore(path).Should().BeFalse();
    }

    #endregion

    #region Directory Pattern Tests

    [Theory]
    [InlineData("/project/.git/config")]
    [InlineData("/project/.git/HEAD")]
    [InlineData("C:\\project\\.git\\config")]
    public void ShouldIgnore_GitDirectory_ReturnsTrue(string path)
    {
        // Assert
        _sut.ShouldIgnore(path).Should().BeTrue();
    }

    [Theory]
    [InlineData("/project/node_modules/package/index.js")]
    [InlineData("C:\\project\\node_modules\\lodash\\index.js")]
    public void ShouldIgnore_NodeModulesDirectory_ReturnsTrue(string path)
    {
        // Assert
        _sut.ShouldIgnore(path).Should().BeTrue();
    }

    [Fact]
    public void ShouldIgnore_PycacheDirectory_ReturnsTrue()
    {
        // Assert
        _sut.ShouldIgnore("/project/__pycache__/module.pyc").Should().BeTrue();
    }

    [Theory]
    [InlineData("/project/.svn/entries")]
    [InlineData("/project/.hg/store")]
    public void ShouldIgnore_VersionControlDirectories_ReturnsTrue(string path)
    {
        // Assert
        _sut.ShouldIgnore(path).Should().BeTrue();
    }

    #endregion

    #region File Name Pattern Tests

    [Theory]
    [InlineData("/path/to/.DS_Store")]
    [InlineData("C:\\path\\to\\.DS_Store")]
    public void ShouldIgnore_DSStore_ReturnsTrue(string path)
    {
        // Assert
        _sut.ShouldIgnore(path).Should().BeTrue();
    }

    [Theory]
    [InlineData("/path/to/Thumbs.db")]
    [InlineData("/path/to/thumbs.db")]
    public void ShouldIgnore_ThumbsDb_ReturnsTrue(string path)
    {
        // Assert
        _sut.ShouldIgnore(path).Should().BeTrue();
    }

    #endregion

    #region Custom Pattern Tests

    [Fact]
    public void ShouldIgnore_CustomPattern_WhenAdded_ReturnsTrue()
    {
        // Arrange
        _sut.IgnorePatterns.Add("*.log");

        // Assert
        _sut.ShouldIgnore("/path/to/app.log").Should().BeTrue();
    }

    [Fact]
    public void ShouldIgnore_RemovedPattern_ReturnsFalse()
    {
        // Arrange - remove the .git pattern
        _sut.IgnorePatterns.Remove(".git");

        // Assert
        _sut.ShouldIgnore("/project/.git/config").Should().BeFalse();
    }

    [Fact]
    public void ShouldIgnore_WithEmptyPatterns_ReturnsFalse()
    {
        // Arrange
        _sut.IgnorePatterns.Clear();

        // Assert
        _sut.ShouldIgnore("/project/.git/config").Should().BeFalse();
        _sut.ShouldIgnore("/path/to/file.tmp").Should().BeFalse();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ShouldIgnore_EmptyPath_ReturnsFalse()
    {
        // Assert
        _sut.ShouldIgnore(string.Empty).Should().BeFalse();
    }

    [Fact]
    public void ShouldIgnore_NullPath_ReturnsFalse()
    {
        // Assert
        _sut.ShouldIgnore(null!).Should().BeFalse();
    }

    [Theory]
    [InlineData("/normal/path/to/file.cs")]
    [InlineData("/project/src/Program.cs")]
    public void ShouldIgnore_NormalPath_ReturnsFalse(string path)
    {
        // Assert
        _sut.ShouldIgnore(path).Should().BeFalse();
    }

    #endregion
}
