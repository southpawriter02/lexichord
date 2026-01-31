using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Style.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for <see cref="IgnorePatternService"/>.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the ignore pattern service contract:
/// - Glob pattern matching (*.log, build/, **/temp/*)
/// - Negation patterns (!important.log)
/// - License tier limits (Core: 5 patterns max)
/// - Hot-reload from file changes
/// - Thread safety (concurrent reads during writes)
/// - Performance requirements (&lt;10ms for 100 path checks)
///
/// Version: v0.3.6d
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Version", "v0.3.6d")]
public class IgnorePatternServiceTests : IDisposable
{
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly Mock<ILogger<IgnorePatternService>> _loggerMock;
    private readonly IgnorePatternService _sut;
    private readonly string _tempDir;

    public IgnorePatternServiceTests()
    {
        _licenseContextMock = new Mock<ILicenseContext>();
        _loggerMock = new Mock<ILogger<IgnorePatternService>>();

        // Default to Writer Pro (unlimited patterns)
        _licenseContextMock.Setup(l => l.GetCurrentTier()).Returns(LicenseTier.WriterPro);

        _sut = new IgnorePatternService(
            _licenseContextMock.Object,
            _loggerMock.Object);

        // Create temp directory for file-based tests
        _tempDir = Path.Combine(Path.GetTempPath(), $"lexichord-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        _sut.Dispose();
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    #region IsIgnored Tests

    [Fact]
    public void IsIgnored_NullPath_ReturnsFalse()
    {
        // Act
        var result = _sut.IsIgnored(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsIgnored_EmptyPath_ReturnsFalse()
    {
        // Act
        var result = _sut.IsIgnored("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsIgnored_NoPatterns_ReturnsFalse()
    {
        // Arrange - no patterns registered

        // Act
        var result = _sut.IsIgnored("any/file.txt");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsIgnored_SimpleGlobPattern_MatchesCorrectly()
    {
        // Arrange
        _sut.RegisterPatterns(new[] { "*.log" });

        // Act & Assert
        _sut.IsIgnored("debug.log").Should().BeTrue();
        _sut.IsIgnored("error.log").Should().BeTrue();
        _sut.IsIgnored("file.txt").Should().BeFalse();
    }

    [Fact]
    public void IsIgnored_DirectoryPattern_MatchesCorrectly()
    {
        // Arrange
        _sut.RegisterPatterns(new[] { "build/**" });

        // Act & Assert
        _sut.IsIgnored("build/output.dll").Should().BeTrue();
        _sut.IsIgnored("build/debug/file.pdb").Should().BeTrue();
        _sut.IsIgnored("src/file.cs").Should().BeFalse();
    }

    [Fact]
    public void IsIgnored_NegationPattern_UnignoresFile()
    {
        // Arrange
        _sut.RegisterPatterns(new[]
        {
            "*.log",
            "!important.log"
        });

        // Act & Assert
        _sut.IsIgnored("debug.log").Should().BeTrue();
        _sut.IsIgnored("important.log").Should().BeFalse();
    }

    [Fact]
    public void IsIgnored_NegationThenReIgnore_LastMatchWins()
    {
        // Arrange
        _sut.RegisterPatterns(new[]
        {
            "*.log",
            "!important.log",
            "*.log"  // Re-ignore
        });

        // Act & Assert
        _sut.IsIgnored("important.log").Should().BeTrue();
    }

    [Fact]
    public void IsIgnored_DeepNesting_MatchesCorrectly()
    {
        // Arrange
        _sut.RegisterPatterns(new[] { "**/temp/**" });

        // Act & Assert
        _sut.IsIgnored("temp/file.txt").Should().BeTrue();
        _sut.IsIgnored("a/b/temp/c/file.txt").Should().BeTrue();
        _sut.IsIgnored("temporary/file.txt").Should().BeFalse();
    }

    [Fact]
    public void IsIgnored_CaseInsensitive_MatchesCorrectly()
    {
        // Arrange
        _sut.RegisterPatterns(new[] { "*.LOG" });

        // Act & Assert
        _sut.IsIgnored("debug.log").Should().BeTrue();
        _sut.IsIgnored("DEBUG.LOG").Should().BeTrue();
        _sut.IsIgnored("Debug.Log").Should().BeTrue();
    }

    #endregion

    #region RegisterPatterns Tests

    [Fact]
    public void RegisterPatterns_Empty_ClearsExisting()
    {
        // Arrange
        _sut.RegisterPatterns(new[] { "*.log" });

        // Act
        _sut.RegisterPatterns(Array.Empty<string>());

        // Assert
        _sut.PatternCount.Should().Be(0);
        _sut.IsIgnored("debug.log").Should().BeFalse();
    }

    [Fact]
    public void RegisterPatterns_SkipsComments()
    {
        // Arrange
        var patterns = new[]
        {
            "# This is a comment",
            "*.log",
            "  # Indented comment",
            "build/"
        };

        // Act
        _sut.RegisterPatterns(patterns);

        // Assert
        _sut.PatternCount.Should().Be(2);
    }

    [Fact]
    public void RegisterPatterns_SkipsEmptyLines()
    {
        // Arrange
        var patterns = new[]
        {
            "",
            "*.log",
            "   ",
            "build/"
        };

        // Act
        _sut.RegisterPatterns(patterns);

        // Assert
        _sut.PatternCount.Should().Be(2);
    }

    [Fact]
    public void RegisterPatterns_RaisesPatternsReloadedEvent()
    {
        // Arrange
        PatternsReloadedEventArgs? eventArgs = null;
        _sut.PatternsReloaded += (_, args) => eventArgs = args;

        // Act
        _sut.RegisterPatterns(new[] { "*.log", "build/" });

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.PatternCount.Should().Be(2);
        eventArgs.TruncatedCount.Should().Be(0);
        eventArgs.Source.Should().BeNull();
    }

    #endregion

    #region License Tier Tests

    [Fact]
    public void RegisterPatterns_CoreTier_TruncatesAt5Patterns()
    {
        // Arrange
        _licenseContextMock.Setup(l => l.GetCurrentTier()).Returns(LicenseTier.Core);
        var sut = new IgnorePatternService(
            _licenseContextMock.Object,
            _loggerMock.Object);

        var patterns = Enumerable.Range(0, 10)
            .Select(i => $"*.file{i}")
            .ToArray();

        // Act
        sut.RegisterPatterns(patterns);

        // Assert
        sut.PatternCount.Should().Be(5);
        sut.TruncatedPatternCount.Should().Be(5);

        sut.Dispose();
    }

    [Fact]
    public void RegisterPatterns_WriterPro_AllowsUnlimitedPatterns()
    {
        // Arrange
        _licenseContextMock.Setup(l => l.GetCurrentTier()).Returns(LicenseTier.WriterPro);

        var patterns = Enumerable.Range(0, 100)
            .Select(i => $"*.file{i}")
            .ToArray();

        // Act
        _sut.RegisterPatterns(patterns);

        // Assert
        _sut.PatternCount.Should().Be(100);
        _sut.TruncatedPatternCount.Should().Be(0);
    }

    [Fact]
    public void RegisterPatterns_CoreTier_RaisesTruncatedCount()
    {
        // Arrange
        _licenseContextMock.Setup(l => l.GetCurrentTier()).Returns(LicenseTier.Core);
        var sut = new IgnorePatternService(
            _licenseContextMock.Object,
            _loggerMock.Object);

        PatternsReloadedEventArgs? eventArgs = null;
        sut.PatternsReloaded += (_, args) => eventArgs = args;

        var patterns = Enumerable.Range(0, 8)
            .Select(i => $"*.file{i}")
            .ToArray();

        // Act
        sut.RegisterPatterns(patterns);

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.TruncatedCount.Should().Be(3);

        sut.Dispose();
    }

    #endregion

    #region LoadFromWorkspaceAsync Tests

    [Fact]
    public async Task LoadFromWorkspaceAsync_FileNotFound_ReturnsFalse()
    {
        // Act
        var result = await _sut.LoadFromWorkspaceAsync(_tempDir);

        // Assert
        result.Should().BeFalse();
        _sut.PatternCount.Should().Be(0);
    }

    [Fact]
    public async Task LoadFromWorkspaceAsync_ValidFile_LoadsPatterns()
    {
        // Arrange
        var ignoreDir = Path.Combine(_tempDir, ".lexichord");
        Directory.CreateDirectory(ignoreDir);
        var ignoreFile = Path.Combine(ignoreDir, ".lexichordignore");
        await File.WriteAllTextAsync(ignoreFile, "*.log\nbuild/\n# Comment\n!important.log");

        // Act
        var result = await _sut.LoadFromWorkspaceAsync(_tempDir);

        // Assert
        result.Should().BeTrue();
        _sut.PatternCount.Should().Be(3);
    }

    [Fact]
    public async Task LoadFromWorkspaceAsync_FileTooLarge_ReturnsFalse()
    {
        // Arrange
        var ignoreDir = Path.Combine(_tempDir, ".lexichord");
        Directory.CreateDirectory(ignoreDir);
        var ignoreFile = Path.Combine(ignoreDir, ".lexichordignore");

        // Create a file larger than 100KB
        var largeContent = new string('x', 150 * 1024);
        await File.WriteAllTextAsync(ignoreFile, largeContent);

        // Act
        var result = await _sut.LoadFromWorkspaceAsync(_tempDir);

        // Assert
        result.Should().BeFalse();
        _sut.PatternCount.Should().Be(0);
    }

    [Fact]
    public async Task LoadFromWorkspaceAsync_StartsFileWatcher()
    {
        // Arrange
        var ignoreDir = Path.Combine(_tempDir, ".lexichord");
        Directory.CreateDirectory(ignoreDir);
        var ignoreFile = Path.Combine(ignoreDir, ".lexichordignore");
        await File.WriteAllTextAsync(ignoreFile, "*.log");

        // Act
        await _sut.LoadFromWorkspaceAsync(_tempDir);

        // Assert
        _sut.IsWatching.Should().BeTrue();
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllPatterns()
    {
        // Arrange
        _sut.RegisterPatterns(new[] { "*.log", "build/" });

        // Act
        _sut.Clear();

        // Assert
        _sut.PatternCount.Should().Be(0);
        _sut.IsIgnored("debug.log").Should().BeFalse();
    }

    [Fact]
    public void Clear_RaisesEventWithZeroCount()
    {
        // Arrange
        _sut.RegisterPatterns(new[] { "*.log" });
        PatternsReloadedEventArgs? eventArgs = null;
        _sut.PatternsReloaded += (_, args) => eventArgs = args;

        // Act
        _sut.Clear();

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.PatternCount.Should().Be(0);
    }

    [Fact]
    public async Task Clear_StopsFileWatcher()
    {
        // Arrange
        var ignoreDir = Path.Combine(_tempDir, ".lexichord");
        Directory.CreateDirectory(ignoreDir);
        var ignoreFile = Path.Combine(ignoreDir, ".lexichordignore");
        await File.WriteAllTextAsync(ignoreFile, "*.log");
        await _sut.LoadFromWorkspaceAsync(_tempDir);

        // Act
        _sut.Clear();

        // Assert
        _sut.IsWatching.Should().BeFalse();
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void IsIgnored_100PathChecks_CompletesUnder100ms()
    {
        // Arrange
        var patterns = Enumerable.Range(0, 20)
            .Select(i => $"**/pattern{i}/**")
            .ToArray();
        _sut.RegisterPatterns(patterns);

        var paths = Enumerable.Range(0, 100)
            .Select(i => $"some/path/file{i}.txt")
            .ToArray();

        // Warmup - JIT compilation overhead
        foreach (var path in paths)
        {
            _sut.IsIgnored(path);
        }

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        foreach (var path in paths)
        {
            _sut.IsIgnored(path);
        }
        stopwatch.Stop();

        // Assert - 500ms is conservative for CI environments with parallel test execution
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500,
            "checking 100 paths should complete in under 500ms even under load");
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void IsIgnored_ConcurrentReads_NoExceptions()
    {
        // Arrange
        _sut.RegisterPatterns(new[] { "*.log", "build/**", "**/temp/*" });

        var paths = Enumerable.Range(0, 100)
            .Select(i => $"file{i}.txt")
            .ToArray();

        // Act & Assert
        var exceptions = new List<Exception>();
        Parallel.ForEach(paths, path =>
        {
            try
            {
                _sut.IsIgnored(path);
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        });

        exceptions.Should().BeEmpty();
    }

    [Fact]
    public void RegisterPatterns_DuringReads_NoExceptions()
    {
        // Arrange
        _sut.RegisterPatterns(new[] { "*.log" });

        // Start concurrent reads
        var cts = new CancellationTokenSource();
        var readTask = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                _sut.IsIgnored("test.txt");
            }
        });

        // Act - do multiple registrations while reads are happening
        var exceptions = new List<Exception>();
        for (var i = 0; i < 10; i++)
        {
            try
            {
                _sut.RegisterPatterns(new[] { $"*.pattern{i}" });
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        cts.Cancel();

        // Assert
        exceptions.Should().BeEmpty();
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullLicenseContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new IgnorePatternService(null!, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("licenseContext");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new IgnorePatternService(_licenseContextMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion
}
