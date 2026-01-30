using FluentAssertions;
using Lexichord.Abstractions.Contracts.Linting;

namespace Lexichord.Tests.Unit.Abstractions.Contracts.Linting;

/// <summary>
/// Unit tests for <see cref="ExcludedRegion"/>.
/// </summary>
/// <remarks>
/// Version: v0.2.7b
/// </remarks>
public sealed class ExcludedRegionTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        // Act
        var region = new ExcludedRegion(10, 50, ExclusionReason.FencedCodeBlock, "python");

        // Assert
        region.StartOffset.Should().Be(10);
        region.EndOffset.Should().Be(50);
        region.Reason.Should().Be(ExclusionReason.FencedCodeBlock);
        region.Metadata.Should().Be("python");
    }

    [Fact]
    public void Constructor_MetadataDefaultsToNull()
    {
        // Act
        var region = new ExcludedRegion(0, 10, ExclusionReason.InlineCode);

        // Assert
        region.Metadata.Should().BeNull();
    }

    [Fact]
    public void Length_CalculatesCorrectly()
    {
        // Arrange
        var region = new ExcludedRegion(10, 50, ExclusionReason.FencedCodeBlock);

        // Assert
        region.Length.Should().Be(40);
    }

    [Fact]
    public void Length_ZeroForEmptyRegion()
    {
        // Arrange
        var region = new ExcludedRegion(10, 10, ExclusionReason.FencedCodeBlock);

        // Assert
        region.Length.Should().Be(0);
    }

    [Fact]
    public void Contains_OffsetInside_ReturnsTrue()
    {
        // Arrange
        var region = new ExcludedRegion(10, 50, ExclusionReason.FencedCodeBlock);

        // Assert
        region.Contains(10).Should().BeTrue();  // Start (inclusive)
        region.Contains(25).Should().BeTrue();  // Middle
        region.Contains(49).Should().BeTrue();  // Just before end
    }

    [Fact]
    public void Contains_OffsetOutside_ReturnsFalse()
    {
        // Arrange
        var region = new ExcludedRegion(10, 50, ExclusionReason.FencedCodeBlock);

        // Assert
        region.Contains(9).Should().BeFalse();   // Just before start
        region.Contains(50).Should().BeFalse();  // End (exclusive)
        region.Contains(100).Should().BeFalse(); // Well after
    }

    [Fact]
    public void Overlaps_OverlappingRegions_ReturnsTrue()
    {
        // Arrange
        var region1 = new ExcludedRegion(10, 50, ExclusionReason.FencedCodeBlock);
        var region2 = new ExcludedRegion(40, 80, ExclusionReason.InlineCode);

        // Assert
        region1.Overlaps(region2).Should().BeTrue();
        region2.Overlaps(region1).Should().BeTrue();
    }

    [Fact]
    public void Overlaps_ContainedRegion_ReturnsTrue()
    {
        // Arrange
        var outer = new ExcludedRegion(10, 100, ExclusionReason.FencedCodeBlock);
        var inner = new ExcludedRegion(30, 50, ExclusionReason.InlineCode);

        // Assert
        outer.Overlaps(inner).Should().BeTrue();
        inner.Overlaps(outer).Should().BeTrue();
    }

    [Fact]
    public void Overlaps_AdjacentRegions_ReturnsFalse()
    {
        // Arrange - Adjacent but not overlapping
        var region1 = new ExcludedRegion(10, 50, ExclusionReason.FencedCodeBlock);
        var region2 = new ExcludedRegion(50, 100, ExclusionReason.InlineCode);

        // Assert
        region1.Overlaps(region2).Should().BeFalse();
        region2.Overlaps(region1).Should().BeFalse();
    }

    [Fact]
    public void Overlaps_DisjointRegions_ReturnsFalse()
    {
        // Arrange
        var region1 = new ExcludedRegion(10, 30, ExclusionReason.FencedCodeBlock);
        var region2 = new ExcludedRegion(50, 80, ExclusionReason.InlineCode);

        // Assert
        region1.Overlaps(region2).Should().BeFalse();
        region2.Overlaps(region1).Should().BeFalse();
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        var region1 = new ExcludedRegion(10, 50, ExclusionReason.FencedCodeBlock, "python");
        var region2 = new ExcludedRegion(10, 50, ExclusionReason.FencedCodeBlock, "python");

        // Assert
        region1.Should().Be(region2);
    }

    [Fact]
    public void RecordEquality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var region1 = new ExcludedRegion(10, 50, ExclusionReason.FencedCodeBlock);
        var region2 = new ExcludedRegion(10, 50, ExclusionReason.InlineCode);

        // Assert
        region1.Should().NotBe(region2);
    }
}
