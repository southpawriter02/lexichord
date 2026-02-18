// -----------------------------------------------------------------------
// <copyright file="LineRangeTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.DocumentComparison;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.DocumentComparison;

/// <summary>
/// Unit tests for <see cref="LineRange"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6d")]
public class LineRangeTests
{
    // ── Construction ─────────────────────────────────────────────────────

    [Fact]
    public void Constructor_ValidRange_SetsProperties()
    {
        // Arrange & Act
        var range = new LineRange(5, 10);

        // Assert
        range.Start.Should().Be(5);
        range.End.Should().Be(10);
    }

    [Fact]
    public void Constructor_SingleLine_SetsStartEqualToEnd()
    {
        // Arrange & Act
        var range = new LineRange(7, 7);

        // Assert
        range.Start.Should().Be(7);
        range.End.Should().Be(7);
    }

    // ── LineCount Computed Property ──────────────────────────────────────

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(1, 5, 5)]
    [InlineData(10, 15, 6)]
    [InlineData(100, 200, 101)]
    public void LineCount_CalculatesInclusiveCount(int start, int end, int expectedCount)
    {
        // Arrange
        var range = new LineRange(start, end);

        // Assert
        range.LineCount.Should().Be(expectedCount);
    }

    // ── IsSingleLine Computed Property ───────────────────────────────────

    [Fact]
    public void IsSingleLine_WhenStartEqualsEnd_ReturnsTrue()
    {
        // Arrange
        var range = new LineRange(5, 5);

        // Assert
        range.IsSingleLine.Should().BeTrue();
    }

    [Fact]
    public void IsSingleLine_WhenStartDiffersFromEnd_ReturnsFalse()
    {
        // Arrange
        var range = new LineRange(5, 10);

        // Assert
        range.IsSingleLine.Should().BeFalse();
    }

    // ── IsValid Computed Property ────────────────────────────────────────

    [Theory]
    [InlineData(1, 5, true)]
    [InlineData(5, 5, true)]
    [InlineData(1, 1, true)]
    public void IsValid_ValidRange_ReturnsTrue(int start, int end, bool expected)
    {
        // Arrange
        var range = new LineRange(start, end);

        // Assert
        range.IsValid.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 5, false)]   // Start is 0 (1-based lines)
    [InlineData(-1, 5, false)]  // Negative start
    [InlineData(5, 3, false)]   // End before start
    public void IsValid_InvalidRange_ReturnsFalse(int start, int end, bool expected)
    {
        // Arrange
        var range = new LineRange(start, end);

        // Assert
        range.IsValid.Should().Be(expected);
    }

    // ── Validate Method ──────────────────────────────────────────────────

    [Fact]
    public void Validate_ValidRange_DoesNotThrow()
    {
        // Arrange
        var range = new LineRange(1, 10);

        // Act
        var act = () => range.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ZeroStart_ThrowsArgumentException()
    {
        // Arrange
        var range = new LineRange(0, 5);

        // Act
        var act = () => range.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("Start");
    }

    [Fact]
    public void Validate_NegativeStart_ThrowsArgumentException()
    {
        // Arrange
        var range = new LineRange(-1, 5);

        // Act
        var act = () => range.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("Start");
    }

    [Fact]
    public void Validate_EndBeforeStart_ThrowsArgumentException()
    {
        // Arrange
        var range = new LineRange(10, 5);

        // Act
        var act = () => range.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("End");
    }

    // ── Contains Method ──────────────────────────────────────────────────

    [Theory]
    [InlineData(5, 10, 5, true)]   // Start boundary
    [InlineData(5, 10, 7, true)]   // Middle
    [InlineData(5, 10, 10, true)]  // End boundary
    [InlineData(5, 10, 4, false)]  // Before range
    [InlineData(5, 10, 11, false)] // After range
    public void Contains_Line_ReturnsCorrectResult(int start, int end, int line, bool expected)
    {
        // Arrange
        var range = new LineRange(start, end);

        // Assert
        range.Contains(line).Should().Be(expected);
    }

    [Fact]
    public void Contains_SingleLineRange_ContainsOnlyThatLine()
    {
        // Arrange
        var range = new LineRange(5, 5);

        // Assert
        range.Contains(4).Should().BeFalse();
        range.Contains(5).Should().BeTrue();
        range.Contains(6).Should().BeFalse();
    }

    // ── Overlaps Method ──────────────────────────────────────────────────

    [Fact]
    public void Overlaps_IdenticalRanges_ReturnsTrue()
    {
        // Arrange
        var range1 = new LineRange(5, 10);
        var range2 = new LineRange(5, 10);

        // Assert
        range1.Overlaps(range2).Should().BeTrue();
    }

    [Fact]
    public void Overlaps_PartialOverlap_ReturnsTrue()
    {
        // Arrange
        var range1 = new LineRange(5, 10);
        var range2 = new LineRange(8, 15);

        // Assert
        range1.Overlaps(range2).Should().BeTrue();
    }

    [Fact]
    public void Overlaps_OneContainsOther_ReturnsTrue()
    {
        // Arrange
        var outer = new LineRange(1, 20);
        var inner = new LineRange(5, 10);

        // Assert
        outer.Overlaps(inner).Should().BeTrue();
        inner.Overlaps(outer).Should().BeTrue();
    }

    [Fact]
    public void Overlaps_TouchingRanges_ReturnsTrue()
    {
        // Arrange
        var range1 = new LineRange(5, 10);
        var range2 = new LineRange(10, 15);

        // Assert
        range1.Overlaps(range2).Should().BeTrue();
    }

    [Fact]
    public void Overlaps_DisjointRanges_ReturnsFalse()
    {
        // Arrange
        var range1 = new LineRange(1, 5);
        var range2 = new LineRange(10, 15);

        // Assert
        range1.Overlaps(range2).Should().BeFalse();
    }

    [Fact]
    public void Overlaps_AdjacentRanges_ReturnsFalse()
    {
        // Arrange
        var range1 = new LineRange(1, 5);
        var range2 = new LineRange(6, 10);

        // Assert
        range1.Overlaps(range2).Should().BeFalse();
    }

    // ── ToString Method ──────────────────────────────────────────────────

    [Fact]
    public void ToString_SingleLine_ReturnsLineNumber()
    {
        // Arrange
        var range = new LineRange(5, 5);

        // Assert
        range.ToString().Should().Be("line 5");
    }

    [Fact]
    public void ToString_MultiLine_ReturnsRange()
    {
        // Arrange
        var range = new LineRange(5, 10);

        // Assert
        range.ToString().Should().Be("lines 5-10");
    }

    // ── Equality ─────────────────────────────────────────────────────────

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var range1 = new LineRange(5, 10);
        var range2 = new LineRange(5, 10);

        // Assert
        range1.Should().Be(range2);
        (range1 == range2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var range1 = new LineRange(5, 10);
        var range2 = new LineRange(5, 15);

        // Assert
        range1.Should().NotBe(range2);
        (range1 != range2).Should().BeTrue();
    }

    // ── Edge Cases ───────────────────────────────────────────────────────

    [Fact]
    public void LineRange_LargeValues_WorksCorrectly()
    {
        // Arrange
        var range = new LineRange(1_000_000, 1_000_100);

        // Assert
        range.LineCount.Should().Be(101);
        range.Contains(1_000_050).Should().BeTrue();
        range.IsValid.Should().BeTrue();
    }
}
