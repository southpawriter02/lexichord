// -----------------------------------------------------------------------
// <copyright file="UsageMetricsTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Agents;

/// <summary>
/// Unit tests for <see cref="UsageMetrics"/> record.
/// </summary>
/// <remarks>
/// Tests cover construction, computed properties, Add method, ToDisplayString,
/// Calculate factory, and record equality.
/// Introduced in v0.6.6a.
/// </remarks>
public class UsageMetricsTests
{
    // -----------------------------------------------------------------------
    // Constructor / TotalTokens Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that TotalTokens returns the sum of prompt and completion tokens.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void TotalTokens_ReturnsSumOfPromptAndCompletion()
    {
        // Arrange
        var metrics = new UsageMetrics(100, 50, 0.003m);

        // Assert
        metrics.TotalTokens.Should().Be(150);
    }

    /// <summary>
    /// Verifies that constructor sets all properties correctly.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void Constructor_SetsAllProperties()
    {
        // Arrange & Act
        var metrics = new UsageMetrics(1500, 500, 0.045m);

        // Assert
        metrics.PromptTokens.Should().Be(1500);
        metrics.CompletionTokens.Should().Be(500);
        metrics.EstimatedCost.Should().Be(0.045m);
        metrics.TotalTokens.Should().Be(2000);
    }

    // -----------------------------------------------------------------------
    // Zero Sentinel Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that Zero returns a metrics instance with all zero values.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void Zero_ReturnsZeroMetrics()
    {
        // Act
        var zero = UsageMetrics.Zero;

        // Assert
        zero.PromptTokens.Should().Be(0);
        zero.CompletionTokens.Should().Be(0);
        zero.EstimatedCost.Should().Be(0m);
        zero.TotalTokens.Should().Be(0);
    }

    /// <summary>
    /// Verifies that Zero is a shared singleton instance.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void Zero_ReturnsSameInstance()
    {
        // Act
        var zero1 = UsageMetrics.Zero;
        var zero2 = UsageMetrics.Zero;

        // Assert
        ReferenceEquals(zero1, zero2).Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // Add Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that Add combines two metrics instances correctly.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void Add_CombinesMetricsCorrectly()
    {
        // Arrange
        var m1 = new UsageMetrics(100, 50, 0.003m);
        var m2 = new UsageMetrics(200, 100, 0.006m);

        // Act
        var combined = m1.Add(m2);

        // Assert
        combined.PromptTokens.Should().Be(300);
        combined.CompletionTokens.Should().Be(150);
        combined.EstimatedCost.Should().Be(0.009m);
        combined.TotalTokens.Should().Be(450);
    }

    /// <summary>
    /// Verifies that Add with Zero returns an equivalent copy.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void Add_WithZero_ReturnsEquivalent()
    {
        // Arrange
        var metrics = new UsageMetrics(100, 50, 0.003m);

        // Act
        var result = metrics.Add(UsageMetrics.Zero);

        // Assert
        result.Should().Be(metrics);
    }

    // -----------------------------------------------------------------------
    // ToDisplayString Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that ToDisplayString formats the metrics correctly.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void ToDisplayString_FormatsCorrectly()
    {
        // Arrange
        var metrics = new UsageMetrics(1500, 500, 0.045m);

        // Act
        var display = metrics.ToDisplayString();

        // Assert
        display.Should().Contain("2,000");
        display.Should().Contain("$0.0450");
        display.Should().Contain("tokens");
    }

    /// <summary>
    /// Verifies that ToDisplayString handles zero metrics.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void ToDisplayString_ZeroMetrics_FormatsCorrectly()
    {
        // Act
        var display = UsageMetrics.Zero.ToDisplayString();

        // Assert
        display.Should().Contain("0");
        display.Should().Contain("$0.0000");
    }

    // -----------------------------------------------------------------------
    // Calculate Factory Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that Calculate computes estimated cost correctly.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void Calculate_ComputesCostCorrectly()
    {
        // Act
        var metrics = UsageMetrics.Calculate(
            promptTokens: 1000,
            completionTokens: 500,
            promptCostPer1K: 0.01m,
            completionCostPer1K: 0.03m);

        // Assert
        // (1000/1000 * 0.01) + (500/1000 * 0.03) = 0.01 + 0.015 = 0.025
        metrics.PromptTokens.Should().Be(1000);
        metrics.CompletionTokens.Should().Be(500);
        metrics.EstimatedCost.Should().Be(0.025m);
        metrics.TotalTokens.Should().Be(1500);
    }

    /// <summary>
    /// Verifies that Calculate handles zero tokens.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void Calculate_ZeroTokens_ReturnsZeroCost()
    {
        // Act
        var metrics = UsageMetrics.Calculate(
            promptTokens: 0,
            completionTokens: 0,
            promptCostPer1K: 0.01m,
            completionCostPer1K: 0.03m);

        // Assert
        metrics.EstimatedCost.Should().Be(0m);
    }

    /// <summary>
    /// Verifies that Calculate handles fractional token counts correctly.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void Calculate_FractionalTokenCounts_CalculatesPrecisely()
    {
        // Act
        var metrics = UsageMetrics.Calculate(
            promptTokens: 750,
            completionTokens: 250,
            promptCostPer1K: 0.01m,
            completionCostPer1K: 0.03m);

        // Assert
        // (750/1000 * 0.01) + (250/1000 * 0.03) = 0.0075 + 0.0075 = 0.015
        metrics.EstimatedCost.Should().Be(0.0150m);
    }

    // -----------------------------------------------------------------------
    // Record Equality Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that record equality works for UsageMetrics.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        var metrics1 = new UsageMetrics(100, 50, 0.003m);
        var metrics2 = new UsageMetrics(100, 50, 0.003m);

        // Assert
        metrics1.Should().Be(metrics2);
    }

    /// <summary>
    /// Verifies that record inequality works for different values.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void RecordEquality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var metrics1 = new UsageMetrics(100, 50, 0.003m);
        var metrics2 = new UsageMetrics(200, 50, 0.003m);

        // Assert
        metrics1.Should().NotBe(metrics2);
    }
}
