// <copyright file="ResonanceAxisDefinitionTests.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for <see cref="ResonanceAxisDefinition"/> normalization logic.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.5a - Tests the axis normalization algorithm including clamping and inversion.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.3.5a")]
public class ResonanceAxisDefinitionTests
{
    #region Normalize - Standard Scale

    [Theory]
    [InlineData(0, 0)]
    [InlineData(50, 50)]
    [InlineData(100, 100)]
    public void Normalize_StandardScale_ReturnsExpected(double rawValue, double expected)
    {
        // Arrange
        var axis = new ResonanceAxisDefinition(
            Name: "Test",
            MetricKey: "test",
            MinValue: 0,
            MaxValue: 100,
            InvertScale: false);

        // Act
        var result = axis.Normalize(rawValue);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(-10, 0)]  // Below min - clamps to 0
    [InlineData(110, 100)] // Above max - clamps to 100
    public void Normalize_ClampsToRange(double rawValue, double expected)
    {
        // Arrange
        var axis = new ResonanceAxisDefinition(
            Name: "Test",
            MetricKey: "test",
            MinValue: 0,
            MaxValue: 100,
            InvertScale: false);

        // Act
        var result = axis.Normalize(rawValue);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Normalize - Inverted Scale

    [Theory]
    [InlineData(0, 100)]   // 0 -> 100 (inverted)
    [InlineData(50, 50)]   // 50 -> 50 (midpoint same)
    [InlineData(100, 0)]   // 100 -> 0 (inverted)
    public void Normalize_InvertedScale_ReturnsExpected(double rawValue, double expected)
    {
        // Arrange
        var axis = new ResonanceAxisDefinition(
            Name: "Test",
            MetricKey: "test",
            MinValue: 0,
            MaxValue: 100,
            InvertScale: true);

        // Act
        var result = axis.Normalize(rawValue);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Normalize - Custom Range

    [Theory]
    [InlineData(0, 0)]
    [InlineData(9, 50)]   // Midpoint of 0-18 range
    [InlineData(18, 100)]
    public void Normalize_GradeLevelScale_ReturnsExpected(double rawValue, double expected)
    {
        // Arrange - FK Grade Level scale (0-18)
        var axis = new ResonanceAxisDefinition(
            Name: "Accessibility",
            MetricKey: "FleschKincaidGrade",
            MinValue: 0,
            MaxValue: 18,
            InvertScale: false);

        // Act
        var result = axis.Normalize(rawValue);

        // Assert
        result.Should().BeApproximately(expected, 0.01);
    }

    [Theory]
    [InlineData(5, 0)]    // Min value
    [InlineData(22.5, 50)] // Midpoint
    [InlineData(40, 100)]  // Max value
    public void Normalize_DensityScale_ReturnsExpected(double rawValue, double expected)
    {
        // Arrange - Words per sentence (5-40 range)
        var axis = new ResonanceAxisDefinition(
            Name: "Density",
            MetricKey: "AverageWordsPerSentence",
            MinValue: 5,
            MaxValue: 40,
            InvertScale: false);

        // Act
        var result = axis.Normalize(rawValue);

        // Assert
        result.Should().BeApproximately(expected, 0.01);
    }

    #endregion

    #region Normalize - Edge Cases

    [Fact]
    public void Normalize_ZeroRange_ReturnsZero()
    {
        // Arrange - Invalid range where min == max
        var axis = new ResonanceAxisDefinition(
            Name: "Test",
            MetricKey: "test",
            MinValue: 50,
            MaxValue: 50,
            InvertScale: false);

        // Act
        var result = axis.Normalize(50);

        // Assert
        result.Should().Be(0, "zero range should return 0 to avoid division by zero");
    }

    [Fact]
    public void Normalize_NegativeRange_ReturnsZero()
    {
        // Arrange - Invalid range where min > max
        var axis = new ResonanceAxisDefinition(
            Name: "Test",
            MetricKey: "test",
            MinValue: 100,
            MaxValue: 0,
            InvertScale: false);

        // Act
        var result = axis.Normalize(50);

        // Assert
        result.Should().Be(0, "negative range should return 0");
    }

    #endregion
}
