using Lexichord.Abstractions.Contracts;

namespace Lexichord.Tests.Unit.Abstractions;

/// <summary>
/// Unit tests for FileIndexSettings record.
/// </summary>
public class FileIndexSettingsTests
{
    [Fact]
    public void Defaults_SectionName_IsFileIndex()
    {
        // Assert
        FileIndexSettings.SectionName.Should().Be("FileIndex");
    }

    [Fact]
    public void Defaults_IgnorePatterns_ContainsExpectedPatterns()
    {
        // Arrange
        var settings = new FileIndexSettings();

        // Assert
        settings.IgnorePatterns.Should().Contain(".git/**");
        settings.IgnorePatterns.Should().Contain("node_modules/**");
        settings.IgnorePatterns.Should().Contain("bin/**");
        settings.IgnorePatterns.Should().Contain("obj/**");
        settings.IgnorePatterns.Should().Contain(".DS_Store");
    }

    [Fact]
    public void Defaults_IncludeHiddenFiles_IsFalse()
    {
        // Arrange
        var settings = new FileIndexSettings();

        // Assert
        settings.IncludeHiddenFiles.Should().BeFalse();
    }

    [Fact]
    public void Defaults_MaxFileSizeBytes_Is50MB()
    {
        // Arrange
        var settings = new FileIndexSettings();
        var expected = 50 * 1024 * 1024;

        // Assert
        settings.MaxFileSizeBytes.Should().Be(expected);
    }

    [Fact]
    public void Defaults_BinaryExtensions_ContainsExpectedExtensions()
    {
        // Arrange
        var settings = new FileIndexSettings();

        // Assert
        settings.BinaryExtensions.Should().Contain(".exe");
        settings.BinaryExtensions.Should().Contain(".dll");
        settings.BinaryExtensions.Should().Contain(".zip");
        settings.BinaryExtensions.Should().Contain(".png");
        settings.BinaryExtensions.Should().Contain(".pdf");
    }

    [Fact]
    public void Defaults_MaxRecentFiles_Is50()
    {
        // Arrange
        var settings = new FileIndexSettings();

        // Assert
        settings.MaxRecentFiles.Should().Be(50);
    }

    [Fact]
    public void Defaults_Recursive_IsTrue()
    {
        // Arrange
        var settings = new FileIndexSettings();

        // Assert
        settings.Recursive.Should().BeTrue();
    }

    [Fact]
    public void Defaults_MaxDepth_IsZero()
    {
        // Arrange
        var settings = new FileIndexSettings();

        // Assert
        settings.MaxDepth.Should().Be(0);
    }

    [Fact]
    public void Defaults_FileWatcherDebounceMs_Is300()
    {
        // Arrange
        var settings = new FileIndexSettings();

        // Assert
        settings.FileWatcherDebounceMs.Should().Be(300);
    }

    [Fact]
    public void With_CanOverrideDefaults()
    {
        // Arrange
        var settings = new FileIndexSettings
        {
            IncludeHiddenFiles = true,
            MaxFileSizeBytes = 100 * 1024 * 1024,
            MaxRecentFiles = 100
        };

        // Assert
        settings.IncludeHiddenFiles.Should().BeTrue();
        settings.MaxFileSizeBytes.Should().Be(100 * 1024 * 1024);
        settings.MaxRecentFiles.Should().Be(100);
    }
}
