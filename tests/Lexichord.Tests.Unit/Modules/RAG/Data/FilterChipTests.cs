// =============================================================================
// File: FilterChipTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for the FilterChip record and factory methods.
// =============================================================================
// VERSION: v0.5.7a (Panel Redesign)
// =============================================================================

using FluentAssertions;
using Lexichord.Modules.RAG.Data;
using Lexichord.Modules.RAG.Enums;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Data;

/// <summary>
/// Unit tests for <see cref="FilterChip"/> record factory methods.
/// </summary>
/// <remarks>
/// Verifies factory method behavior, label generation, and validation.
/// Introduced in v0.5.7a.
/// </remarks>
public class FilterChipTests
{
    // =========================================================================
    // ForPath Factory Tests
    // =========================================================================

    /// <summary>
    /// Verifies that ForPath creates a chip with the correct type and label.
    /// </summary>
    [Theory]
    [InlineData("docs/**", "docs/**")]
    [InlineData("src/Models/*", "src/Models/*")]
    [InlineData(".git", ".git")]
    public void ForPath_ValidPattern_CreatesCorrectChip(string pattern, string expectedLabel)
    {
        // Act
        var chip = FilterChip.ForPath(pattern);

        // Assert
        chip.Type.Should().Be(FilterChipType.Path);
        chip.Label.Should().Be(expectedLabel);
        chip.Value.Should().Be(pattern);
    }

    /// <summary>
    /// Verifies that ForPath throws for null input.
    /// </summary>
    [Fact]
    public void ForPath_NullPattern_ThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() => FilterChip.ForPath(null!))
            .Should().Throw<ArgumentException>()
            .WithParameterName("pathPattern");
    }

    /// <summary>
    /// Verifies that ForPath throws for empty input.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ForPath_EmptyOrWhitespace_ThrowsArgumentException(string pattern)
    {
        // Act & Assert
        FluentActions.Invoking(() => FilterChip.ForPath(pattern))
            .Should().Throw<ArgumentException>()
            .WithParameterName("pathPattern");
    }

    // =========================================================================
    // ForExtension Factory Tests
    // =========================================================================

    /// <summary>
    /// Verifies that ForExtension creates a chip with dotted label.
    /// </summary>
    [Theory]
    [InlineData("md", ".md", "md")]
    [InlineData("txt", ".txt", "txt")]
    [InlineData("cs", ".cs", "cs")]
    public void ForExtension_ValidExtension_CreatesCorrectChip(
        string input, 
        string expectedLabel, 
        string expectedValue)
    {
        // Act
        var chip = FilterChip.ForExtension(input);

        // Assert
        chip.Type.Should().Be(FilterChipType.Extension);
        chip.Label.Should().Be(expectedLabel);
        chip.Value.Should().Be(expectedValue);
    }

    /// <summary>
    /// Verifies that ForExtension strips leading dot from input.
    /// </summary>
    [Fact]
    public void ForExtension_InputWithDot_NormalizesValue()
    {
        // Act
        var chip = FilterChip.ForExtension(".md");

        // Assert
        chip.Label.Should().Be(".md");
        chip.Value.Should().Be("md");
    }

    /// <summary>
    /// Verifies that ForExtension throws for null input.
    /// </summary>
    [Fact]
    public void ForExtension_NullExtension_ThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() => FilterChip.ForExtension(null!))
            .Should().Throw<ArgumentException>()
            .WithParameterName("extension");
    }

    /// <summary>
    /// Verifies that ForExtension throws for empty input.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ForExtension_EmptyOrWhitespace_ThrowsArgumentException(string extension)
    {
        // Act & Assert
        FluentActions.Invoking(() => FilterChip.ForExtension(extension))
            .Should().Throw<ArgumentException>()
            .WithParameterName("extension");
    }

    // =========================================================================
    // ForDateRange Factory Tests
    // =========================================================================

    /// <summary>
    /// Verifies that ForDateRange creates correct labels for each option.
    /// </summary>
    [Theory]
    [InlineData(DateRangeOption.AnyTime, "Any time")]
    [InlineData(DateRangeOption.LastDay, "Last day")]
    [InlineData(DateRangeOption.Last7Days, "Last 7 days")]
    [InlineData(DateRangeOption.Last30Days, "Last 30 days")]
    [InlineData(DateRangeOption.Custom, "Custom range")]
    public void ForDateRange_ValidOption_CreatesCorrectChip(
        DateRangeOption option, 
        string expectedLabel)
    {
        // Act
        var chip = FilterChip.ForDateRange(option);

        // Assert
        chip.Type.Should().Be(FilterChipType.DateRange);
        chip.Label.Should().Be(expectedLabel);
        chip.Value.Should().Be(option);
    }

    // =========================================================================
    // ForTag Factory Tests
    // =========================================================================

    /// <summary>
    /// Verifies that ForTag creates a chip with the correct type and label.
    /// </summary>
    [Theory]
    [InlineData("important")]
    [InlineData("draft")]
    [InlineData("review-needed")]
    public void ForTag_ValidTag_CreatesCorrectChip(string tag)
    {
        // Act
        var chip = FilterChip.ForTag(tag);

        // Assert
        chip.Type.Should().Be(FilterChipType.Tag);
        chip.Label.Should().Be(tag);
        chip.Value.Should().Be(tag);
    }

    /// <summary>
    /// Verifies that ForTag throws for null input.
    /// </summary>
    [Fact]
    public void ForTag_NullTag_ThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() => FilterChip.ForTag(null!))
            .Should().Throw<ArgumentException>()
            .WithParameterName("tag");
    }

    /// <summary>
    /// Verifies that ForTag throws for empty input.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ForTag_EmptyOrWhitespace_ThrowsArgumentException(string tag)
    {
        // Act & Assert
        FluentActions.Invoking(() => FilterChip.ForTag(tag))
            .Should().Throw<ArgumentException>()
            .WithParameterName("tag");
    }

    // =========================================================================
    // Record Equality Tests
    // =========================================================================

    /// <summary>
    /// Verifies that FilterChip records with same values are equal.
    /// </summary>
    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        // Arrange
        var chip1 = FilterChip.ForPath("docs/**");
        var chip2 = FilterChip.ForPath("docs/**");

        // Assert
        chip1.Should().Be(chip2);
        (chip1 == chip2).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that FilterChip records with different values are not equal.
    /// </summary>
    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var chip1 = FilterChip.ForPath("docs/**");
        var chip2 = FilterChip.ForPath("src/**");

        // Assert
        chip1.Should().NotBe(chip2);
        (chip1 == chip2).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that FilterChip records with different types are not equal.
    /// </summary>
    [Fact]
    public void Equality_DifferentTypes_AreNotEqual()
    {
        // Arrange
        var pathChip = FilterChip.ForPath("test");
        var tagChip = FilterChip.ForTag("test");

        // Assert
        pathChip.Should().NotBe(tagChip);
    }
}
