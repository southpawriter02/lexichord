using Lexichord.Abstractions.Contracts.Ingestion;

namespace Lexichord.Tests.Unit.Abstractions.Ingestion;

/// <summary>
/// Unit tests for the <see cref="FileWatcherOptions"/> record.
/// </summary>
public class FileWatcherOptionsTests
{
    #region Default Values Tests

    [Fact]
    public void Default_IsEnabled()
    {
        // Assert
        FileWatcherOptions.Default.Enabled.Should().BeTrue();
    }

    [Fact]
    public void Default_HasExpectedExtensions()
    {
        // Assert
        FileWatcherOptions.Default.SupportedExtensions
            .Should().BeEquivalentTo([".md", ".txt", ".json", ".yaml"]);
    }

    [Fact]
    public void Default_HasExpectedExcludedDirectories()
    {
        // Assert
        FileWatcherOptions.Default.ExcludedDirectories
            .Should().BeEquivalentTo([".git", "node_modules", "bin", "obj", ".vs", ".idea"]);
    }

    [Fact]
    public void Default_DebounceDelayMs_Is300()
    {
        // Assert
        FileWatcherOptions.Default.DebounceDelayMs.Should().Be(300);
    }

    [Fact]
    public void DefaultDebounceDelayMs_Constant_Is300()
    {
        // Assert
        FileWatcherOptions.DefaultDebounceDelayMs.Should().Be(300);
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void FileWatcherOptions_WithSameValues_AreEqual()
    {
        // Arrange
        var options1 = new FileWatcherOptions(true, [".md"], ["bin"], 300);
        var options2 = new FileWatcherOptions(true, [".md"], ["bin"], 300);

        // Assert
        options1.Should().BeEquivalentTo(options2, because: "records with identical values should be equivalent");
    }

    [Fact]
    public void FileWatcherOptions_SupportsWithExpression()
    {
        // Arrange
        var original = FileWatcherOptions.Default;

        // Act
        var updated = original with { Enabled = false };

        // Assert
        updated.Enabled.Should().BeFalse();
        updated.DebounceDelayMs.Should().Be(original.DebounceDelayMs);
    }

    #endregion

    #region IsExtensionSupported Tests

    [Theory]
    [InlineData(".md", true)]
    [InlineData(".MD", true)]
    [InlineData(".txt", true)]
    [InlineData(".json", true)]
    [InlineData(".yaml", true)]
    [InlineData(".pdf", false)]
    [InlineData(".exe", false)]
    [InlineData(".cs", false)]
    [InlineData("", false)]
    public void IsExtensionSupported_ReturnsExpectedResult(string extension, bool expected)
    {
        // Assert
        FileWatcherOptions.Default.IsExtensionSupported(extension).Should().Be(expected);
    }

    [Fact]
    public void IsExtensionSupported_IsCaseInsensitive()
    {
        // Arrange
        var options = new FileWatcherOptions(true, [".Markdown"], [], 100);

        // Assert
        options.IsExtensionSupported(".markdown").Should().BeTrue();
        options.IsExtensionSupported(".MARKDOWN").Should().BeTrue();
    }

    #endregion

    #region IsDirectoryExcluded Tests

    [Theory]
    [InlineData("bin", true)]
    [InlineData("BIN", true)]
    [InlineData("obj", true)]
    [InlineData(".git", true)]
    [InlineData("node_modules", true)]
    [InlineData(".vs", true)]
    [InlineData(".idea", true)]
    [InlineData("src", false)]
    [InlineData("docs", false)]
    public void IsDirectoryExcluded_ReturnsExpectedResult(string directoryName, bool expected)
    {
        // Assert
        FileWatcherOptions.Default.IsDirectoryExcluded(directoryName).Should().Be(expected);
    }

    [Fact]
    public void IsDirectoryExcluded_IsCaseInsensitive()
    {
        // Arrange
        var options = new FileWatcherOptions(true, [], ["Build"], 100);

        // Assert
        options.IsDirectoryExcluded("build").Should().BeTrue();
        options.IsDirectoryExcluded("BUILD").Should().BeTrue();
    }

    #endregion

    #region ShouldProcessFile Tests

    [Theory]
    [InlineData("/project/docs/readme.md", true)]
    [InlineData("/project/notes.txt", true)]
    [InlineData("/project/config.json", true)]
    [InlineData("/project/settings.yaml", true)]
    [InlineData("/project/code.cs", false)]        // unsupported extension
    [InlineData("/project/README.MD", true)]       // case insensitive extension
    public void ShouldProcessFile_WithSupportedExtension_ReturnsTrue(string path, bool expected)
    {
        // Assert
        FileWatcherOptions.Default.ShouldProcessFile(path).Should().Be(expected);
    }

    [Theory]
    [InlineData("/project/.git/config", false)]
    [InlineData("/project/node_modules/package.json", false)]
    [InlineData("/project/bin/debug/readme.md", false)]
    [InlineData("/project/obj/readme.txt", false)]
    [InlineData("/project/.vs/settings.json", false)]
    [InlineData("/project/.idea/workspace.yaml", false)]
    public void ShouldProcessFile_WithExcludedPath_ReturnsFalse(string path, bool expected)
    {
        // Assert
        FileWatcherOptions.Default.ShouldProcessFile(path).Should().Be(expected);
    }

    [Fact]
    public void ShouldProcessFile_WithWindowsPath_HandlesBackslashes()
    {
        // Arrange - Windows-style path
        var path = "C:\\project\\docs\\readme.md";

        // Assert
        FileWatcherOptions.Default.ShouldProcessFile(path).Should().BeTrue();
    }

    [Fact]
    public void ShouldProcessFile_WithWindowsExcludedPath_ReturnsFalse()
    {
        // Arrange - Windows-style path with excluded directory
        var path = "C:\\project\\.git\\config.json";

        // Assert
        FileWatcherOptions.Default.ShouldProcessFile(path).Should().BeFalse();
    }

    [Fact]
    public void ShouldProcessFile_UnsupportedExtension_TakesPrecedence()
    {
        // A file with unsupported extension should be skipped even if not in excluded dir
        var path = "/project/src/code.cs";

        // Assert
        FileWatcherOptions.Default.ShouldProcessFile(path).Should().BeFalse();
    }

    [Fact]
    public void ShouldProcessFile_ExcludedDirectory_ExcludesEvenWithSupportedExtension()
    {
        // A .md file in .git should be excluded
        var path = "/project/.git/hooks/readme.md";

        // Assert
        FileWatcherOptions.Default.ShouldProcessFile(path).Should().BeFalse();
    }

    #endregion

    #region Disabled State Tests

    [Fact]
    public void Enabled_WhenFalse_ShouldStillHaveValidMethods()
    {
        // Arrange
        var options = FileWatcherOptions.Default with { Enabled = false };

        // Assert - methods still work even when disabled
        options.IsExtensionSupported(".md").Should().BeTrue();
        options.IsDirectoryExcluded("bin").Should().BeTrue();
        options.ShouldProcessFile("/project/readme.md").Should().BeTrue();
    }

    #endregion
}
