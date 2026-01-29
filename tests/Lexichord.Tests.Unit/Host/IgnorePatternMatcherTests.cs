using Lexichord.Host.Services;

namespace Lexichord.Tests.Unit.Host;

/// <summary>
/// Unit tests for IgnorePatternMatcher.
/// </summary>
public class IgnorePatternMatcherTests
{
    #region Git Directory Tests

    [Theory]
    [InlineData(".git/config")]
    [InlineData(".git/HEAD")]
    [InlineData(".git/objects/pack/file")]
    [InlineData("project/.git/config")]
    public void IsIgnored_GitDirectory_ReturnsTrue(string path)
    {
        // Arrange
        var matcher = new IgnorePatternMatcher(new[] { ".git/**" });

        // Act & Assert
        matcher.IsIgnored(path).Should().BeTrue();
    }

    #endregion

    #region Node Modules Tests

    [Theory]
    [InlineData("node_modules/package/index.js")]
    [InlineData("node_modules/.bin/cmd")]
    [InlineData("frontend/node_modules/react/package.json")]
    public void IsIgnored_NodeModules_ReturnsTrue(string path)
    {
        // Arrange
        var matcher = new IgnorePatternMatcher(new[] { "node_modules/**" });

        // Act & Assert
        matcher.IsIgnored(path).Should().BeTrue();
    }

    #endregion

    #region Source File Tests

    [Theory]
    [InlineData("src/Program.cs")]
    [InlineData("src/Services/MyService.cs")]
    [InlineData("Models/User.cs")]
    [InlineData("README.md")]
    public void IsIgnored_SourceFiles_ReturnsFalse(string path)
    {
        // Arrange
        var matcher = new IgnorePatternMatcher(new[] { ".git/**", "node_modules/**", "bin/**" });

        // Act & Assert
        matcher.IsIgnored(path).Should().BeFalse();
    }

    #endregion

    #region Wildcard Pattern Tests

    [Theory]
    [InlineData("file.pyc")]
    [InlineData("module.pyc")]
    [InlineData("tests/test.pyc")]
    public void IsIgnored_WildcardExtension_ReturnsTrue(string path)
    {
        // Arrange
        var matcher = new IgnorePatternMatcher(new[] { "*.pyc" });

        // Act & Assert
        matcher.IsIgnored(path).Should().BeTrue();
    }

    [Fact]
    public void IsIgnored_DoubleStarPattern_MatchesNestedPaths()
    {
        // Arrange
        var matcher = new IgnorePatternMatcher(new[] { "**/__pycache__/**" });

        // Act & Assert
        matcher.IsIgnored("src/__pycache__/module.pyc").Should().BeTrue();
        matcher.IsIgnored("deep/nested/__pycache__/file.pyc").Should().BeTrue();
    }

    #endregion

    #region DS_Store Tests

    [Theory]
    [InlineData(".DS_Store")]
    [InlineData("folder/.DS_Store")]
    [InlineData("deep/nested/.DS_Store")]
    public void IsIgnored_DSStore_ReturnsTrue(string path)
    {
        // Arrange
        var matcher = new IgnorePatternMatcher(new[] { ".DS_Store" });

        // Act & Assert
        matcher.IsIgnored(path).Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void IsIgnored_EmptyPath_ReturnsFalse()
    {
        // Arrange
        var matcher = new IgnorePatternMatcher(new[] { ".git/**" });

        // Act & Assert
        matcher.IsIgnored(string.Empty).Should().BeFalse();
    }

    [Fact]
    public void IsIgnored_NullPattern_IgnoresNullPatterns()
    {
        // Arrange - patterns list with whitespace (should be filtered out)
        var matcher = new IgnorePatternMatcher(new[] { "", "   ", ".git/**" });

        // Act & Assert
        matcher.IsIgnored(".git/config").Should().BeTrue();
    }

    [Fact]
    public void IsIgnored_NoPatterns_ReturnsFalse()
    {
        // Arrange
        var matcher = new IgnorePatternMatcher(Array.Empty<string>());

        // Act & Assert
        matcher.IsIgnored("anything.txt").Should().BeFalse();
    }

    [Fact]
    public void IsIgnored_WindowsPathSeparators_NormalizesToForwardSlashes()
    {
        // Arrange
        var matcher = new IgnorePatternMatcher(new[] { ".git/**" });

        // Act & Assert
        matcher.IsIgnored(".git\\config").Should().BeTrue();
        matcher.IsIgnored("project\\.git\\objects").Should().BeTrue();
    }

    #endregion

    #region Multiple Pattern Tests

    [Fact]
    public void IsIgnored_MultiplePatterns_MatchesAny()
    {
        // Arrange
        var matcher = new IgnorePatternMatcher(new[]
        {
            ".git/**",
            "node_modules/**",
            "bin/**",
            "obj/**"
        });

        // Act & Assert
        matcher.IsIgnored(".git/config").Should().BeTrue();
        matcher.IsIgnored("node_modules/pkg/index.js").Should().BeTrue();
        matcher.IsIgnored("bin/Debug/app.exe").Should().BeTrue();
        matcher.IsIgnored("obj/Debug/net8.0/app.dll").Should().BeTrue();
        matcher.IsIgnored("src/Program.cs").Should().BeFalse();
    }

    #endregion
}
