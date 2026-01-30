using FluentAssertions;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style.Linting;

/// <summary>
/// Unit tests for IgnorePatternMatcher.
/// </summary>
/// <remarks>
/// LOGIC: Verifies gitignore-style pattern matching for .lexichordignore files.
///
/// Version: v0.2.6d
/// </remarks>
public class IgnorePatternMatcherTests
{
    [Fact]
    public void Constructor_WithNoPatterns_CreatesEmptyMatcher()
    {
        // Act
        var matcher = new Lexichord.Modules.Style.Services.Linting.IgnorePatternMatcher();

        // Assert
        matcher.PatternCount.Should().Be(0);
    }

    [Fact]
    public void IsIgnored_WithNoPatterns_ReturnsFalse()
    {
        // Arrange
        var matcher = new Lexichord.Modules.Style.Services.Linting.IgnorePatternMatcher();

        // Act & Assert
        matcher.IsIgnored("any/path.txt").Should().BeFalse();
    }

    [Theory]
    [InlineData("*.log", "app.log", true)]
    [InlineData("*.log", "debug.log", true)]
    [InlineData("*.log", "readme.md", false)]
    [InlineData("*.log", "logs/app.log", true)]
    public void IsIgnored_WildcardPattern_MatchesCorrectly(
        string pattern, string path, bool expected)
    {
        // Arrange
        var matcher = new Lexichord.Modules.Style.Services.Linting.IgnorePatternMatcher([pattern]);

        // Act & Assert
        matcher.IsIgnored(path).Should().Be(expected);
    }

    [Theory]
    [InlineData("drafts/", "drafts/doc.md", true)]
    [InlineData("drafts/", "drafts/nested/doc.md", true)]
    [InlineData("drafts/", "other/drafts/doc.md", false)]
    public void IsIgnored_DirectoryPattern_MatchesDirectoryContents(
        string pattern, string path, bool expected)
    {
        // Arrange
        var matcher = new Lexichord.Modules.Style.Services.Linting.IgnorePatternMatcher([pattern]);

        // Act & Assert
        matcher.IsIgnored(path).Should().Be(expected);
    }

    [Theory]
    [InlineData("**/temp/", "temp/file.txt", true)]
    [InlineData("**/temp/", "src/temp/file.txt", true)]
    [InlineData("**/temp/", "deep/nested/temp/file.txt", true)]
    public void IsIgnored_RecursiveWildcard_MatchesAtAnyDepth(
        string pattern, string path, bool expected)
    {
        // Arrange
        var matcher = new Lexichord.Modules.Style.Services.Linting.IgnorePatternMatcher([pattern]);

        // Act & Assert
        matcher.IsIgnored(path).Should().Be(expected);
    }

    [Fact]
    public void IsIgnored_NegationPattern_UnignoresFiles()
    {
        // Arrange
        var patterns = new[]
        {
            "*.log",          // Ignore all .log files
            "!important.log"  // But not important.log
        };
        var matcher = new Lexichord.Modules.Style.Services.Linting.IgnorePatternMatcher(patterns);

        // Act & Assert
        matcher.IsIgnored("app.log").Should().BeTrue();
        matcher.IsIgnored("important.log").Should().BeFalse();
    }

    [Fact]
    public void IsIgnored_MultiplePatterns_LastMatchWins()
    {
        // Arrange - patterns processed in order, last match determines result
        var patterns = new[]
        {
            "*.txt",   // Ignore all .txt
            "!keep.txt", // Unignore keep.txt
            "keep.txt"   // Re-ignore keep.txt
        };
        var matcher = new Lexichord.Modules.Style.Services.Linting.IgnorePatternMatcher(patterns);

        // Act & Assert
        matcher.IsIgnored("keep.txt").Should().BeTrue();
    }

    [Fact]
    public void Constructor_IgnoresEmptyLines()
    {
        // Arrange
        var patterns = new[]
        {
            "",
            "*.log",
            "",
            "*.tmp"
        };

        // Act
        var matcher = new Lexichord.Modules.Style.Services.Linting.IgnorePatternMatcher(patterns);

        // Assert
        matcher.PatternCount.Should().Be(2);
    }

    [Fact]
    public void Constructor_IgnoresCommentLines()
    {
        // Arrange
        var patterns = new[]
        {
            "# This is a comment",
            "*.log",
            "  # Indented comment",
            "*.tmp"
        };

        // Act
        var matcher = new Lexichord.Modules.Style.Services.Linting.IgnorePatternMatcher(patterns);

        // Assert
        matcher.PatternCount.Should().Be(2);
    }

    [Theory]
    [InlineData("file.txt", "file.txt", true)]
    [InlineData("src/file.txt", "src/file.txt", true)]
    [InlineData("src/file.txt", "other/file.txt", false)]
    public void IsIgnored_ExactPath_MatchesExactly(
        string pattern, string path, bool expected)
    {
        // Arrange
        var matcher = new Lexichord.Modules.Style.Services.Linting.IgnorePatternMatcher([pattern]);

        // Act & Assert
        matcher.IsIgnored(path).Should().Be(expected);
    }

    [Fact]
    public void IsIgnored_NormalizesBackslashes()
    {
        // Arrange
        var matcher = new Lexichord.Modules.Style.Services.Linting.IgnorePatternMatcher(["drafts/"]);

        // Act & Assert - Windows-style path should still match
        matcher.IsIgnored("drafts\\doc.md").Should().BeTrue();
        matcher.IsIgnored(@"drafts\nested\doc.md").Should().BeTrue();
    }

    [Fact]
    public void IsIgnored_EmptyPath_ReturnsFalse()
    {
        // Arrange
        var matcher = new Lexichord.Modules.Style.Services.Linting.IgnorePatternMatcher(["*.log"]);

        // Act & Assert
        matcher.IsIgnored("").Should().BeFalse();
        matcher.IsIgnored(null!).Should().BeFalse();
    }

    [Fact]
    public void IsIgnored_StripsLeadingSlash()
    {
        // Arrange
        var matcher = new Lexichord.Modules.Style.Services.Linting.IgnorePatternMatcher(["*.log"]);

        // Act & Assert
        matcher.IsIgnored("/app.log").Should().BeTrue();
        matcher.IsIgnored("/src/debug.log").Should().BeTrue();
    }
}
