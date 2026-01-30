// -----------------------------------------------------------------------
// <copyright file="FuzzyMatchServiceTests.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.Style.Services;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for FuzzyMatchService (v0.3.1a).
/// </summary>
/// <remarks>
/// LOGIC: Verifies the FuzzyMatchService correctly calculates fuzzy
/// similarity ratios and handles edge cases like null/empty inputs.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "FuzzyMatching")]
public class FuzzyMatchServiceTests
{
    private readonly FuzzyMatchService _sut = new();

    #region CalculateRatio Tests

    [Fact]
    public void CalculateRatio_IdenticalStrings_Returns100()
    {
        // Arrange & Act
        var result = _sut.CalculateRatio("whitelist", "whitelist");

        // Assert
        result.Should().Be(100);
    }

    [Fact]
    public void CalculateRatio_IdenticalStrings_CaseInsensitive_Returns100()
    {
        // Arrange & Act
        var result = _sut.CalculateRatio("WhiteList", "whitelist");

        // Assert
        result.Should().Be(100);
    }

    [Fact]
    public void CalculateRatio_IdenticalStrings_WithWhitespace_Returns100()
    {
        // Arrange & Act
        var result = _sut.CalculateRatio("  whitelist  ", "whitelist");

        // Assert
        result.Should().Be(100);
    }

    [Fact]
    public void CalculateRatio_SimilarStrings_ReturnsHighRatio()
    {
        // Arrange & Act - "whtelist" is missing an 'i'
        var result = _sut.CalculateRatio("whtelist", "whitelist");

        // Assert - Should be high (typo detection)
        result.Should().BeGreaterThanOrEqualTo(80);
    }

    [Fact]
    public void CalculateRatio_DifferentStrings_ReturnsLowRatio()
    {
        // Arrange & Act
        var result = _sut.CalculateRatio("apple", "orange");

        // Assert
        result.Should().BeLessThan(50);
    }

    [Fact]
    public void CalculateRatio_BothEmpty_Returns100()
    {
        // Arrange & Act
        var result = _sut.CalculateRatio("", "");

        // Assert
        result.Should().Be(100);
    }

    [Fact]
    public void CalculateRatio_BothWhitespaceOnly_Returns100()
    {
        // Arrange & Act
        var result = _sut.CalculateRatio("   ", "  ");

        // Assert
        result.Should().Be(100);
    }

    [Fact]
    public void CalculateRatio_OneEmpty_Returns0()
    {
        // Arrange & Act
        var result = _sut.CalculateRatio("hello", "");

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void CalculateRatio_OneWhitespaceOnly_Returns0()
    {
        // Arrange & Act
        var result = _sut.CalculateRatio("hello", "   ");

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void CalculateRatio_NullSource_ThrowsArgumentNullException()
    {
        // Act
        var action = () => _sut.CalculateRatio(null!, "target");

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("source");
    }

    [Fact]
    public void CalculateRatio_NullTarget_ThrowsArgumentNullException()
    {
        // Act
        var action = () => _sut.CalculateRatio("source", null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("target");
    }

    #endregion

    #region CalculatePartialRatio Tests

    [Fact]
    public void CalculatePartialRatio_SubstringMatch_ReturnsHighRatio()
    {
        // Arrange & Act - "list" appears in "whitelist"
        var result = _sut.CalculatePartialRatio("list", "whitelist");

        // Assert
        result.Should().Be(100);
    }

    [Fact]
    public void CalculatePartialRatio_IdenticalStrings_Returns100()
    {
        // Arrange & Act
        var result = _sut.CalculatePartialRatio("hello", "hello");

        // Assert
        result.Should().Be(100);
    }

    [Fact]
    public void CalculatePartialRatio_BothEmpty_Returns100()
    {
        // Arrange & Act
        var result = _sut.CalculatePartialRatio("", "");

        // Assert
        result.Should().Be(100);
    }

    [Fact]
    public void CalculatePartialRatio_OneEmpty_Returns0()
    {
        // Arrange & Act
        var result = _sut.CalculatePartialRatio("hello", "");

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void CalculatePartialRatio_NullSource_ThrowsArgumentNullException()
    {
        // Act
        var action = () => _sut.CalculatePartialRatio(null!, "target");

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("source");
    }

    [Fact]
    public void CalculatePartialRatio_NullTarget_ThrowsArgumentNullException()
    {
        // Act
        var action = () => _sut.CalculatePartialRatio("source", null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("target");
    }

    #endregion

    #region IsMatch Tests

    [Fact]
    public void IsMatch_AboveThreshold_ReturnsTrue()
    {
        // Arrange & Act - Identical strings should pass any threshold
        var result = _sut.IsMatch("hello", "hello", 0.80);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsMatch_BelowThreshold_ReturnsFalse()
    {
        // Arrange & Act - Completely different strings
        var result = _sut.IsMatch("apple", "zebra", 0.80);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsMatch_ExactlyAtThreshold_ReturnsTrue()
    {
        // Arrange - Identical strings = 100% match
        // Act - Threshold of 1.0 (100%) should still pass
        var result = _sut.IsMatch("test", "test", 1.0);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsMatch_ZeroThreshold_AlwaysReturnsTrue()
    {
        // Arrange & Act - Any strings should pass 0% threshold
        var result = _sut.IsMatch("completely", "different", 0.0);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsMatch_ThresholdBelowZero_ThrowsArgumentOutOfRangeException()
    {
        // Act
        var action = () => _sut.IsMatch("source", "target", -0.1);

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("threshold");
    }

    [Fact]
    public void IsMatch_ThresholdAboveOne_ThrowsArgumentOutOfRangeException()
    {
        // Act
        var action = () => _sut.IsMatch("source", "target", 1.1);

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("threshold");
    }

    [Fact]
    public void IsMatch_NullSource_ThrowsArgumentNullException()
    {
        // Act
        var action = () => _sut.IsMatch(null!, "target", 0.80);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("source");
    }

    [Fact]
    public void IsMatch_NullTarget_ThrowsArgumentNullException()
    {
        // Act
        var action = () => _sut.IsMatch("source", null!, 0.80);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("target");
    }

    #endregion

    #region Normalization Tests

    [Fact]
    public void Normalization_MixedCase_TreatedAsIdentical()
    {
        // Arrange & Act
        var result = _sut.CalculateRatio("HeLLo WoRLd", "hello world");

        // Assert
        result.Should().Be(100);
    }

    [Fact]
    public void Normalization_LeadingTrailingWhitespace_Trimmed()
    {
        // Arrange & Act
        var result = _sut.CalculateRatio("\t  hello  \n", "hello");

        // Assert
        result.Should().Be(100);
    }

    #endregion
}
