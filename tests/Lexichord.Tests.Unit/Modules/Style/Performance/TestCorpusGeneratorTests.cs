using FluentAssertions;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style.Performance;

/// <summary>
/// Unit tests for <see cref="TestCorpusGenerator"/>.
/// </summary>
/// <remarks>
/// LOGIC: Tests verify document generation meets size targets
/// and includes expected content types.
///
/// Version: v0.2.7d
/// </remarks>
public sealed class TestCorpusGeneratorTests
{
    [Fact]
    public void GenerateDocument_ReachesTargetSize()
    {
        // Arrange
        var generator = new TestCorpusGenerator(seed: 42);
        var targetSize = 100_000; // 100KB

        // Act
        var document = generator.GenerateDocument(new DocumentGenerationOptions(
            TargetSizeBytes: targetSize
        ));

        // Assert
        document.Length.Should().BeGreaterOrEqualTo(targetSize);
    }

    [Fact]
    public void GenerateDocument_IncludesFrontmatter()
    {
        // Arrange
        var generator = new TestCorpusGenerator(seed: 42);

        // Act
        var document = generator.GenerateDocument(new DocumentGenerationOptions(
            TargetSizeBytes: 1000,
            IncludeFrontmatter: true
        ));

        // Assert
        document.Should().StartWith("---");
        document.Should().Contain("title:");
        document.Should().Contain("---\n\n");
    }

    [Fact]
    public void GenerateDocument_IncludesCodeBlocks()
    {
        // Arrange
        var generator = new TestCorpusGenerator(seed: 42);

        // Act
        var document = generator.GenerateDocument(new DocumentGenerationOptions(
            TargetSizeBytes: 50_000,
            IncludeCodeBlocks: true,
            CodeBlockDensity: 50 // High density to ensure code blocks
        ));

        // Assert
        document.Should().Contain("```csharp");
        document.Should().Contain("```");
    }

    [Fact]
    public void GenerateDocument_IncludesViolations()
    {
        // Arrange
        var generator = new TestCorpusGenerator(seed: 42);

        // Act
        var document = generator.GenerateDocument(new DocumentGenerationOptions(
            TargetSizeBytes: 50_000,
            ViolationDensity: 20 // High density
        ));

        // Assert - Should contain at least one violation term
        var containsViolation =
            document.Contains("utilise") ||
            document.Contains("colour") ||
            document.Contains("centre") ||
            document.Contains("behaviour") ||
            document.Contains("analyse");
        containsViolation.Should().BeTrue();
    }

    [Fact]
    public void GenerateDocument_Deterministic_WithSeed()
    {
        // Arrange
        var generator1 = new TestCorpusGenerator(seed: 42);
        var generator2 = new TestCorpusGenerator(seed: 42);

        // Act
        var document1 = generator1.GenerateDocument(new DocumentGenerationOptions(
            TargetSizeBytes: 10_000,
            Seed: 42
        ));
        var document2 = generator2.GenerateDocument(new DocumentGenerationOptions(
            TargetSizeBytes: 10_000,
            Seed: 42
        ));

        // Assert
        document1.Should().Be(document2);
    }

    [Fact]
    public void GenerateDocument_NoFrontmatter_WhenDisabled()
    {
        // Arrange
        var generator = new TestCorpusGenerator(seed: 42);

        // Act
        var document = generator.GenerateDocument(new DocumentGenerationOptions(
            TargetSizeBytes: 1000,
            IncludeFrontmatter: false
        ));

        // Assert
        document.Should().NotStartWith("---");
    }

    [Fact]
    public void GenerateDocument_NoCodeBlocks_WhenDisabled()
    {
        // Arrange
        var generator = new TestCorpusGenerator(seed: 42);

        // Act
        var document = generator.GenerateDocument(new DocumentGenerationOptions(
            TargetSizeBytes: 50_000,
            IncludeCodeBlocks: false
        ));

        // Assert
        document.Should().NotContain("```");
    }

    [Fact]
    public void GenerateDocument_ByMegabytes_ReachesTarget()
    {
        // Arrange
        var generator = new TestCorpusGenerator(seed: 42);

        // Act
        var document = generator.GenerateDocument(1); // 1MB

        // Assert
        document.Length.Should().BeGreaterOrEqualTo(1_000_000);
    }
}
