// =============================================================================
// File: SentenceBoundaryTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for SentenceBoundary record (v0.5.6c).
// =============================================================================
// VERSION: v0.5.6c (Smart Truncation)
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions;

/// <summary>
/// Unit tests for <see cref="SentenceBoundary"/> record.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.6c")]
public class SentenceBoundaryTests
{
    #region Length Tests

    [Fact]
    public void Length_ReturnsCorrectValue()
    {
        var boundary = new SentenceBoundary(10, 25);
        boundary.Length.Should().Be(15);
    }

    [Fact]
    public void Length_ZeroLength_ReturnsZero()
    {
        var boundary = new SentenceBoundary(5, 5);
        boundary.Length.Should().Be(0);
    }

    #endregion

    #region Contains Tests

    [Fact]
    public void Contains_PositionInRange_ReturnsTrue()
    {
        var boundary = new SentenceBoundary(10, 20);

        boundary.Contains(15).Should().BeTrue();
        boundary.Contains(10).Should().BeTrue(); // Start is inclusive
    }

    [Fact]
    public void Contains_PositionOutOfRange_ReturnsFalse()
    {
        var boundary = new SentenceBoundary(10, 20);

        boundary.Contains(5).Should().BeFalse();
        boundary.Contains(20).Should().BeFalse(); // End is exclusive
        boundary.Contains(25).Should().BeFalse();
    }

    [Fact]
    public void Contains_AtBoundaryStart_ReturnsTrue()
    {
        var boundary = new SentenceBoundary(10, 20);
        boundary.Contains(10).Should().BeTrue();
    }

    [Fact]
    public void Contains_JustBeforeEnd_ReturnsTrue()
    {
        var boundary = new SentenceBoundary(10, 20);
        boundary.Contains(19).Should().BeTrue();
    }

    #endregion

    #region OverlapsWith Tests

    [Fact]
    public void OverlapsWith_Overlapping_ReturnsTrue()
    {
        var boundary = new SentenceBoundary(10, 20);

        boundary.OverlapsWith(15, 25).Should().BeTrue();
        boundary.OverlapsWith(5, 15).Should().BeTrue();
        boundary.OverlapsWith(12, 18).Should().BeTrue();
    }

    [Fact]
    public void OverlapsWith_ContainedRange_ReturnsTrue()
    {
        var boundary = new SentenceBoundary(10, 20);
        boundary.OverlapsWith(12, 18).Should().BeTrue();
    }

    [Fact]
    public void OverlapsWith_ContainingRange_ReturnsTrue()
    {
        var boundary = new SentenceBoundary(10, 20);
        boundary.OverlapsWith(5, 25).Should().BeTrue();
    }

    [Fact]
    public void OverlapsWith_NonOverlapping_ReturnsFalse()
    {
        var boundary = new SentenceBoundary(10, 20);

        boundary.OverlapsWith(0, 10).Should().BeFalse(); // Touching at start
        boundary.OverlapsWith(20, 30).Should().BeFalse(); // Touching at end
    }

    [Fact]
    public void OverlapsWith_DisjointRanges_ReturnsFalse()
    {
        var boundary = new SentenceBoundary(10, 20);

        boundary.OverlapsWith(0, 5).Should().BeFalse();
        boundary.OverlapsWith(25, 30).Should().BeFalse();
    }

    [Fact]
    public void OverlapsWith_SinglePointOverlap_ReturnsTrue()
    {
        var boundary = new SentenceBoundary(10, 20);
        // Range [15, 16) overlaps with [10, 20)
        boundary.OverlapsWith(15, 16).Should().BeTrue();
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        var boundary1 = new SentenceBoundary(10, 20);
        var boundary2 = new SentenceBoundary(10, 20);

        boundary1.Should().Be(boundary2);
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        var boundary1 = new SentenceBoundary(10, 20);
        var boundary2 = new SentenceBoundary(10, 25);

        boundary1.Should().NotBe(boundary2);
    }

    [Fact]
    public void GetHashCode_SameValues_ReturnsSameHash()
    {
        var boundary1 = new SentenceBoundary(10, 20);
        var boundary2 = new SentenceBoundary(10, 20);

        boundary1.GetHashCode().Should().Be(boundary2.GetHashCode());
    }

    #endregion
}
