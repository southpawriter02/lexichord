// -----------------------------------------------------------------------
// <copyright file="ChangeSignificanceTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.DocumentComparison;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.DocumentComparison;

/// <summary>
/// Unit tests for <see cref="ChangeSignificance"/> enum and <see cref="ChangeSignificanceExtensions"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6d")]
public class ChangeSignificanceTests
{
    // ── FromScore Tests ──────────────────────────────────────────────────

    [Theory]
    [InlineData(0.0, ChangeSignificance.Low)]
    [InlineData(0.1, ChangeSignificance.Low)]
    [InlineData(0.29, ChangeSignificance.Low)]
    public void FromScore_BelowThreshold_ReturnsLow(double score, ChangeSignificance expected)
    {
        // Act
        var result = ChangeSignificanceExtensions.FromScore(score);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0.3, ChangeSignificance.Medium)]
    [InlineData(0.45, ChangeSignificance.Medium)]
    [InlineData(0.59, ChangeSignificance.Medium)]
    public void FromScore_MediumRange_ReturnsMedium(double score, ChangeSignificance expected)
    {
        // Act
        var result = ChangeSignificanceExtensions.FromScore(score);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0.6, ChangeSignificance.High)]
    [InlineData(0.7, ChangeSignificance.High)]
    [InlineData(0.79, ChangeSignificance.High)]
    public void FromScore_HighRange_ReturnsHigh(double score, ChangeSignificance expected)
    {
        // Act
        var result = ChangeSignificanceExtensions.FromScore(score);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0.8, ChangeSignificance.Critical)]
    [InlineData(0.9, ChangeSignificance.Critical)]
    [InlineData(1.0, ChangeSignificance.Critical)]
    public void FromScore_CriticalRange_ReturnsCritical(double score, ChangeSignificance expected)
    {
        // Act
        var result = ChangeSignificanceExtensions.FromScore(score);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(-0.5, ChangeSignificance.Low)]
    [InlineData(-1.0, ChangeSignificance.Low)]
    public void FromScore_NegativeScore_ClampsToLow(double score, ChangeSignificance expected)
    {
        // Act
        var result = ChangeSignificanceExtensions.FromScore(score);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1.5, ChangeSignificance.Critical)]
    [InlineData(2.0, ChangeSignificance.Critical)]
    public void FromScore_ScoreAboveOne_ClampsToCritical(double score, ChangeSignificance expected)
    {
        // Act
        var result = ChangeSignificanceExtensions.FromScore(score);

        // Assert
        result.Should().Be(expected);
    }

    // ── GetMinimumScore Tests ────────────────────────────────────────────

    [Theory]
    [InlineData(ChangeSignificance.Low, 0.0)]
    [InlineData(ChangeSignificance.Medium, 0.3)]
    [InlineData(ChangeSignificance.High, 0.6)]
    [InlineData(ChangeSignificance.Critical, 0.8)]
    public void GetMinimumScore_ReturnsCorrectThreshold(ChangeSignificance significance, double expectedMin)
    {
        // Act
        var result = significance.GetMinimumScore();

        // Assert
        result.Should().Be(expectedMin);
    }

    // ── GetDisplayLabel Tests ────────────────────────────────────────────

    [Theory]
    [InlineData(ChangeSignificance.Low, "LOW")]
    [InlineData(ChangeSignificance.Medium, "MEDIUM")]
    [InlineData(ChangeSignificance.High, "HIGH")]
    [InlineData(ChangeSignificance.Critical, "CRITICAL")]
    public void GetDisplayLabel_ReturnsUppercaseLabel(ChangeSignificance significance, string expectedLabel)
    {
        // Act
        var result = significance.GetDisplayLabel();

        // Assert
        result.Should().Be(expectedLabel);
    }

    // ── Enum Values ──────────────────────────────────────────────────────

    [Fact]
    public void ChangeSignificance_HasCorrectIntegerValues()
    {
        // Assert
        ((int)ChangeSignificance.Low).Should().Be(0);
        ((int)ChangeSignificance.Medium).Should().Be(1);
        ((int)ChangeSignificance.High).Should().Be(2);
        ((int)ChangeSignificance.Critical).Should().Be(3);
    }

    [Fact]
    public void ChangeSignificance_HasFourValues()
    {
        // Act
        var values = Enum.GetValues<ChangeSignificance>();

        // Assert
        values.Should().HaveCount(4);
    }

    // ── Edge Cases ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(0.299999, ChangeSignificance.Low)]
    [InlineData(0.300001, ChangeSignificance.Medium)]
    [InlineData(0.599999, ChangeSignificance.Medium)]
    [InlineData(0.600001, ChangeSignificance.High)]
    [InlineData(0.799999, ChangeSignificance.High)]
    [InlineData(0.800001, ChangeSignificance.Critical)]
    public void FromScore_BoundaryValues_ReturnsCorrectLevel(double score, ChangeSignificance expected)
    {
        // Act
        var result = ChangeSignificanceExtensions.FromScore(score);

        // Assert
        result.Should().Be(expected);
    }
}
