using FluentAssertions;
using Lexichord.Abstractions.Contracts.Linting;

namespace Lexichord.Tests.Unit.Abstractions.Contracts.Linting;

/// <summary>
/// Unit tests for <see cref="FilteredContent"/>.
/// </summary>
/// <remarks>
/// Version: v0.2.7b
/// </remarks>
public sealed class FilteredContentTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        // Arrange
        var exclusions = new[]
        {
            new ExcludedRegion(10, 50, ExclusionReason.FencedCodeBlock)
        };

        // Act
        var filtered = new FilteredContent("processed", exclusions, "original");

        // Assert
        filtered.ProcessedContent.Should().Be("processed");
        filtered.ExcludedRegions.Should().Equal(exclusions);
        filtered.OriginalContent.Should().Be("original");
    }

    [Fact]
    public void HasExclusions_WithExclusions_ReturnsTrue()
    {
        // Arrange
        var exclusions = new[]
        {
            new ExcludedRegion(10, 50, ExclusionReason.FencedCodeBlock)
        };
        var filtered = new FilteredContent("content", exclusions, "content");

        // Assert
        filtered.HasExclusions.Should().BeTrue();
    }

    [Fact]
    public void HasExclusions_NoExclusions_ReturnsFalse()
    {
        // Arrange
        var filtered = new FilteredContent(
            "content",
            Array.Empty<ExcludedRegion>(),
            "content");

        // Assert
        filtered.HasExclusions.Should().BeFalse();
    }

    [Fact]
    public void TotalExcludedLength_CalculatesCorrectly()
    {
        // Arrange
        var exclusions = new[]
        {
            new ExcludedRegion(10, 30, ExclusionReason.FencedCodeBlock),  // 20
            new ExcludedRegion(50, 60, ExclusionReason.InlineCode)         // 10
        };
        var filtered = new FilteredContent("content", exclusions, "content");

        // Assert
        filtered.TotalExcludedLength.Should().Be(30);
    }

    [Fact]
    public void TotalExcludedLength_NoExclusions_ReturnsZero()
    {
        // Arrange
        var filtered = FilteredContent.None("content");

        // Assert
        filtered.TotalExcludedLength.Should().Be(0);
    }

    [Fact]
    public void None_CreatesEmptyExclusions()
    {
        // Act
        var filtered = FilteredContent.None("my content");

        // Assert
        filtered.ProcessedContent.Should().Be("my content");
        filtered.OriginalContent.Should().Be("my content");
        filtered.ExcludedRegions.Should().BeEmpty();
        filtered.HasExclusions.Should().BeFalse();
    }

    [Fact]
    public void None_WithEmptyString_Works()
    {
        // Act
        var filtered = FilteredContent.None("");

        // Assert
        filtered.ProcessedContent.Should().BeEmpty();
        filtered.HasExclusions.Should().BeFalse();
    }

    [Fact]
    public void IsExcluded_OffsetInExclusion_ReturnsTrue()
    {
        // Arrange
        var exclusions = new[]
        {
            new ExcludedRegion(10, 50, ExclusionReason.FencedCodeBlock),
            new ExcludedRegion(100, 150, ExclusionReason.InlineCode)
        };
        var filtered = new FilteredContent("content", exclusions, "content");

        // Assert
        filtered.IsExcluded(25).Should().BeTrue();
        filtered.IsExcluded(125).Should().BeTrue();
    }

    [Fact]
    public void IsExcluded_OffsetOutsideExclusion_ReturnsFalse()
    {
        // Arrange
        var exclusions = new[]
        {
            new ExcludedRegion(10, 50, ExclusionReason.FencedCodeBlock)
        };
        var filtered = new FilteredContent("content", exclusions, "content");

        // Assert
        filtered.IsExcluded(5).Should().BeFalse();
        filtered.IsExcluded(50).Should().BeFalse();  // End is exclusive
        filtered.IsExcluded(75).Should().BeFalse();
    }

    [Fact]
    public void IsExcluded_NoExclusions_ReturnsFalse()
    {
        // Arrange
        var filtered = FilteredContent.None("content");

        // Assert
        filtered.IsExcluded(0).Should().BeFalse();
        filtered.IsExcluded(100).Should().BeFalse();
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        var exclusions = new[]
        {
            new ExcludedRegion(10, 50, ExclusionReason.FencedCodeBlock)
        };
        var filtered1 = new FilteredContent("content", exclusions, "content");
        var filtered2 = new FilteredContent("content", exclusions, "content");

        // Assert
        filtered1.Should().Be(filtered2);
    }
}
