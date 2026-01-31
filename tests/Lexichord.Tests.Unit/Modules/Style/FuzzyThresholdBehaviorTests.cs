// -----------------------------------------------------------------------
// <copyright file="FuzzyThresholdBehaviorTests.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.Style.Services;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Threshold behavior tests for FuzzyMatchService (v0.3.8a).
/// </summary>
/// <remarks>
/// <para>
/// LOGIC: v0.3.8a - Verifies the threshold-based matching behavior with
/// precise boundary conditions. These tests ensure that the IsMatch method
/// correctly applies thresholds and that boundary cases are handled as expected.
/// </para>
/// <para>
/// <b>Test Categories:</b>
/// <list type="bullet">
///   <item>Boundary Precision - Tests exactly-at-threshold behavior</item>
///   <item>Threshold Sweep - Systematic tests at 0%, 50%, 80%, 100%</item>
///   <item>Partial Ratio Thresholds - Threshold tests for partial matching</item>
/// </list>
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "FuzzyMatching")]
[Trait("Version", "v0.3.8a")]
public class FuzzyThresholdBehaviorTests
{
    private readonly FuzzyMatchService _sut = new();

    #region Boundary Precision Tests

    /// <summary>
    /// Verifies that a ratio exactly at the threshold passes.
    /// </summary>
    /// <remarks>
    /// LOGIC: The IsMatch method uses >= comparison, so exactly at threshold should pass.
    /// </remarks>
    [Fact]
    public void IsMatch_ExactlyAtThreshold_ReturnsTrue()
    {
        // Arrange - Identical strings = 100% ratio
        const string source = "identical";
        const string target = "identical";

        // Act - Threshold of 1.0 (100%) should still pass
        var result = _sut.IsMatch(source, target, 1.0);

        // Assert
        result.Should().BeTrue("ratio exactly at threshold should match");
    }

    /// <summary>
    /// Verifies that a ratio just below threshold fails.
    /// </summary>
    [Fact]
    public void IsMatch_JustBelowThreshold_ReturnsFalse()
    {
        // Arrange - Find a pair with known ratio around 80%
        // "whitelist" vs "whitelst" = ~88%, so use higher threshold
        const string source = "whitelist";
        const string target = "whitelst";
        var actualRatio = _sut.CalculateRatio(source, target);

        // Act - Set threshold just above the actual ratio
        var thresholdJustAbove = (actualRatio + 1) / 100.0;
        var result = _sut.IsMatch(source, target, thresholdJustAbove);

        // Assert
        result.Should().BeFalse("ratio below threshold should not match");
    }

    /// <summary>
    /// Verifies that a ratio just above threshold passes.
    /// </summary>
    [Fact]
    public void IsMatch_JustAboveThreshold_ReturnsTrue()
    {
        // Arrange
        const string source = "whitelist";
        const string target = "whitelst";
        var actualRatio = _sut.CalculateRatio(source, target);

        // Act - Set threshold just below the actual ratio
        var thresholdJustBelow = (actualRatio - 1) / 100.0;
        var result = _sut.IsMatch(source, target, thresholdJustBelow);

        // Assert
        result.Should().BeTrue("ratio above threshold should match");
    }

    /// <summary>
    /// Verifies threshold boundary with 80% (default fuzzy threshold).
    /// </summary>
    [Fact]
    public void IsMatch_DefaultThreshold80Percent_BoundaryBehavior()
    {
        // Arrange - "whitelist" vs "whtelist" (missing 'i') - actual ratio is ~94%
        const string source = "whitelist";
        const string typo = "whtelist";

        // Act
        var passesAt80 = _sut.IsMatch(source, typo, 0.80);
        var passesAt95 = _sut.IsMatch(source, typo, 0.95);

        // Assert
        passesAt80.Should().BeTrue("94% passes 80% threshold");
        passesAt95.Should().BeFalse("94% fails 95% threshold");
    }

    #endregion

    #region Threshold Sweep Tests

    /// <summary>
    /// Verifies behavior at 0% threshold - everything matches.
    /// </summary>
    [Fact]
    public void IsMatch_ZeroThreshold_AlwaysReturnsTrue()
    {
        // Arrange - Completely different strings
        const string source = "apple";
        const string target = "zebra";

        // Act
        var result = _sut.IsMatch(source, target, 0.0);

        // Assert
        result.Should().BeTrue("0% threshold matches everything");
    }

    /// <summary>
    /// Verifies behavior at 50% threshold - moderate similarity required.
    /// </summary>
    [Fact]
    public void IsMatch_FiftyPercentThreshold_RequiresModerateMatch()
    {
        // Arrange
        const string similar = "whitelist";
        const string typo = "whtelist";      // High similarity (~88%)
        const string different = "blocklist"; // Low similarity

        // Act
        var similarResult = _sut.IsMatch(similar, typo, 0.50);
        var differentResult = _sut.IsMatch(similar, different, 0.50);

        // Assert
        similarResult.Should().BeTrue("high similarity passes 50% threshold");
        differentResult.Should().BeFalse("low similarity fails 50% threshold");
    }

    /// <summary>
    /// Verifies behavior at 80% threshold - high similarity required.
    /// </summary>
    [Fact]
    public void IsMatch_EightyPercentThreshold_RequiresHighMatch()
    {
        // Arrange
        const string term = "terminology";
        const string closeTypo = "terminolgy";   // One char missing
        const string farTypo = "trmnlgy";        // Multiple chars missing (more severe)

        // Act
        var closeResult = _sut.IsMatch(term, closeTypo, 0.80);
        var farResult = _sut.IsMatch(term, farTypo, 0.80);

        // Assert
        closeResult.Should().BeTrue("close typo passes 80% threshold");
        farResult.Should().BeFalse("far typo fails 80% threshold");
    }

    /// <summary>
    /// Verifies behavior at 100% threshold - only identical matches.
    /// </summary>
    [Fact]
    public void IsMatch_HundredPercentThreshold_RequiresExactMatch()
    {
        // Arrange
        const string word = "exact";
        const string sameWord = "exact";
        const string slightDiff = "exat"; // One char missing

        // Act
        var exactResult = _sut.IsMatch(word, sameWord, 1.0);
        var slightResult = _sut.IsMatch(word, slightDiff, 1.0);

        // Assert
        exactResult.Should().BeTrue("identical strings pass 100% threshold");
        slightResult.Should().BeFalse("any difference fails 100% threshold");
    }

    /// <summary>
    /// Verifies threshold edge case with empty strings.
    /// </summary>
    [Fact]
    public void IsMatch_EmptyStrings_BehavesCorrectlyAtAllThresholds()
    {
        // Act - Both empty = 100% match
        var bothEmptyAt0 = _sut.IsMatch("", "", 0.0);
        var bothEmptyAt100 = _sut.IsMatch("", "", 1.0);

        // Assert
        bothEmptyAt0.Should().BeTrue("empty strings pass 0% threshold");
        bothEmptyAt100.Should().BeTrue("empty strings pass 100% threshold (identical)");
    }

    /// <summary>
    /// Verifies threshold sweep with incremental thresholds.
    /// </summary>
    // LOGIC: "whitelist" vs "whtelist" = ~94% per FuzzySharp
    [Theory]
    [InlineData(0.0, true)]
    [InlineData(0.50, true)]
    [InlineData(0.80, true)]
    [InlineData(0.90, true)]
    [InlineData(0.94, true)]
    [InlineData(0.95, false)]
    [InlineData(1.0, false)]
    public void IsMatch_ThresholdSweep_BehavesCorrectly(double threshold, bool expectedResult)
    {
        // Arrange - "whitelist" vs "whtelist" ≈ 88%
        const string source = "whitelist";
        const string typo = "whtelist";

        // Act
        var result = _sut.IsMatch(source, typo, threshold);

        // Assert
        result.Should().Be(expectedResult, $"88% ratio at {threshold * 100}% threshold");
    }

    #endregion

    #region Partial Ratio Threshold Tests

    /// <summary>
    /// Verifies partial ratio for substring matching.
    /// </summary>
    [Fact]
    public void CalculatePartialRatio_SubstringMatch_ReturnsHighRatio()
    {
        // Arrange - "list" is a perfect substring of "whitelist"
        const string pattern = "list";
        const string text = "whitelist";

        // Act
        var result = _sut.CalculatePartialRatio(pattern, text);

        // Assert
        result.Should().Be(100, "exact substring match yields 100%");
    }

    /// <summary>
    /// Verifies partial ratio with near-match substring.
    /// </summary>
    [Fact]
    public void CalculatePartialRatio_NearSubstringMatch_ReturnsModerateRatio()
    {
        // Arrange - "lst" is missing 'i' from "list" which is in "whitelist"
        const string typoPattern = "lst";
        const string text = "whitelist";

        // Act
        var result = _sut.CalculatePartialRatio(typoPattern, text);

        // Assert - FuzzySharp returns ~67% for this case
        result.Should().BeGreaterThanOrEqualTo(60, "near-substring has moderate partial ratio");
    }

    /// <summary>
    /// Verifies partial ratio comparison vs full ratio.
    /// </summary>
    [Fact]
    public void CalculatePartialRatio_VsFullRatio_PartialIsHigherForSubstring()
    {
        // Arrange - Short pattern in long text
        const string pattern = "white";
        const string longText = "whitelist managers";

        // Act
        var partialRatio = _sut.CalculatePartialRatio(pattern, longText);
        var fullRatio = _sut.CalculateRatio(pattern, longText);

        // Assert
        partialRatio.Should().BeGreaterThan(
            fullRatio, 
            "partial ratio finds best substring match");
    }

    /// <summary>
    /// Verifies partial ratio with no substring match.
    /// </summary>
    [Fact]
    public void CalculatePartialRatio_NoMatch_ReturnsLowRatio()
    {
        // Arrange
        const string pattern = "xyz";
        const string text = "whitelist";

        // Act
        var result = _sut.CalculatePartialRatio(pattern, text);

        // Assert
        result.Should().BeLessThan(50, "no substring match yields low ratio");
    }

    #endregion
}
