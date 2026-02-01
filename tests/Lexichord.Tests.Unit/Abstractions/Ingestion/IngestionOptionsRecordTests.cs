using Lexichord.Abstractions.Contracts.Ingestion;

namespace Lexichord.Tests.Unit.Abstractions.Ingestion;

/// <summary>
/// Unit tests for the <see cref="IngestionOptions"/> record.
/// </summary>
public class IngestionOptionsRecordTests
{
    #region Default Values Tests

    [Fact]
    public void Default_HasExpectedExtensions()
    {
        // Assert
        IngestionOptions.Default.SupportedExtensions
            .Should().BeEquivalentTo([".md", ".txt", ".rst"]);
    }

    [Fact]
    public void Default_HasExpectedExcludedDirectories()
    {
        // Assert
        IngestionOptions.Default.ExcludedDirectories
            .Should().BeEquivalentTo(["bin", "obj", ".git", "node_modules", ".vs", ".idea"]);
    }

    [Fact]
    public void Default_MaxFileSizeBytes_Is10MB()
    {
        // Assert
        IngestionOptions.Default.MaxFileSizeBytes.Should().Be(10 * 1024 * 1024);
    }

    [Fact]
    public void Default_MaxConcurrency_IsProcessorCount()
    {
        // Assert
        IngestionOptions.Default.MaxConcurrency.Should().Be(Environment.ProcessorCount);
    }

    [Fact]
    public void Default_ThrottleDelayMs_IsNull()
    {
        // Assert
        IngestionOptions.Default.ThrottleDelayMs.Should().BeNull();
    }

    [Fact]
    public void DefaultMaxFileSizeBytes_Constant_Is10MB()
    {
        // Assert
        IngestionOptions.DefaultMaxFileSizeBytes.Should().Be(10 * 1024 * 1024);
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void IngestionOptions_WithSameValues_AreEqual()
    {
        // Arrange
        var options1 = new IngestionOptions(
            [".md"], ["bin"], 1024, 4, 100);
        var options2 = new IngestionOptions(
            [".md"], ["bin"], 1024, 4, 100);

        // Assert
        options1.Should().BeEquivalentTo(options2, because: "records with identical values should be equivalent");
    }

    [Fact]
    public void IngestionOptions_SupportsWithExpression()
    {
        // Arrange
        var original = IngestionOptions.Default;

        // Act
        var updated = original with { MaxConcurrency = 2 };

        // Assert
        updated.MaxConcurrency.Should().Be(2);
        updated.SupportedExtensions.Should().BeEquivalentTo(original.SupportedExtensions);
    }

    #endregion

    #region IsExtensionSupported Tests

    [Theory]
    [InlineData(".md", true)]
    [InlineData(".MD", true)]
    [InlineData(".txt", true)]
    [InlineData(".rst", true)]
    [InlineData(".pdf", false)]
    [InlineData(".exe", false)]
    [InlineData("", false)]
    public void IsExtensionSupported_ReturnsExpectedResult(string extension, bool expected)
    {
        // Assert
        IngestionOptions.Default.IsExtensionSupported(extension).Should().Be(expected);
    }

    [Fact]
    public void IsExtensionSupported_IsCaseInsensitive()
    {
        // Arrange
        var options = new IngestionOptions([".Markdown"], [], 1024, 1, null);

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
    [InlineData("src", false)]
    [InlineData("docs", false)]
    public void IsDirectoryExcluded_ReturnsExpectedResult(string directoryName, bool expected)
    {
        // Assert
        IngestionOptions.Default.IsDirectoryExcluded(directoryName).Should().Be(expected);
    }

    [Fact]
    public void IsDirectoryExcluded_IsCaseInsensitive()
    {
        // Arrange
        var options = new IngestionOptions([], ["Build"], 1024, 1, null);

        // Assert
        options.IsDirectoryExcluded("build").Should().BeTrue();
        options.IsDirectoryExcluded("BUILD").Should().BeTrue();
    }

    #endregion
}
