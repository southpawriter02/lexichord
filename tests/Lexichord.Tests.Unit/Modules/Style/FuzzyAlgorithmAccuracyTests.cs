// -----------------------------------------------------------------------
// <copyright file="FuzzyAlgorithmAccuracyTests.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.Style.Services;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Algorithm accuracy tests for FuzzyMatchService (v0.3.8a).
/// </summary>
/// <remarks>
/// <para>
/// LOGIC: v0.3.8a - Verifies the mathematical correctness of the Levenshtein
/// distance algorithm implementation by testing against known input/output pairs.
/// These tests ensure the fuzzy matching foundation is reliable for typo detection.
/// </para>
/// <para>
/// <b>Test Categories:</b>
/// <list type="bullet">
///   <item>Distance Symmetry - Verifies d(a,b) == d(b,a)</item>
///   <item>Edge Cases - Empty strings, single characters, identical strings</item>
///   <item>Known Distance Values - Pre-computed Levenshtein distances</item>
///   <item>Unicode Handling - Emoji, diacritics, CJK characters</item>
/// </list>
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "FuzzyMatching")]
[Trait("Version", "v0.3.8a")]
public class FuzzyAlgorithmAccuracyTests
{
    private readonly FuzzyMatchService _sut = new();

    #region Distance Symmetry Tests

    /// <summary>
    /// Verifies that fuzzy ratio is symmetric: ratio(a,b) == ratio(b,a).
    /// </summary>
    /// <remarks>
    /// LOGIC: Levenshtein distance is mathematically symmetric; the number of
    /// edits to transform A→B equals the number to transform B→A.
    /// </remarks>
    [Fact]
    public void CalculateRatio_Symmetric_ReturnsIdenticalRatios()
    {
        // Arrange
        const string stringA = "kitten";
        const string stringB = "sitting";

        // Act
        var ratioAB = _sut.CalculateRatio(stringA, stringB);
        var ratioBA = _sut.CalculateRatio(stringB, stringA);

        // Assert
        ratioAB.Should().Be(ratioBA, "Levenshtein distance is symmetric");
    }

    /// <summary>
    /// Verifies symmetry with strings of significantly different lengths.
    /// </summary>
    [Fact]
    public void CalculateRatio_Symmetric_DifferentLengths_ReturnsIdenticalRatios()
    {
        // Arrange
        const string shortString = "cat";
        const string longString = "catastrophe";

        // Act
        var ratioShortToLong = _sut.CalculateRatio(shortString, longString);
        var ratioLongToShort = _sut.CalculateRatio(longString, shortString);

        // Assert
        ratioShortToLong.Should().Be(ratioLongToShort, "symmetry holds regardless of length");
    }

    /// <summary>
    /// Verifies symmetry with strings containing mixed case after normalization.
    /// </summary>
    [Fact]
    public void CalculateRatio_Symmetric_MixedCase_ReturnsIdenticalRatios()
    {
        // Arrange
        const string stringA = "WhiteList";
        const string stringB = "blacklist";

        // Act
        var ratioAB = _sut.CalculateRatio(stringA, stringB);
        var ratioBA = _sut.CalculateRatio(stringB, stringA);

        // Assert
        ratioAB.Should().Be(ratioBA, "case-insensitive comparison maintains symmetry");
    }

    #endregion

    #region Edge Case Tests

    /// <summary>
    /// Verifies that two empty strings are considered 100% identical.
    /// </summary>
    /// <remarks>
    /// LOGIC: Edge case - both strings empty means no edits needed, thus identical.
    /// </remarks>
    [Fact]
    public void CalculateRatio_BothEmpty_Returns100()
    {
        // Act
        var result = _sut.CalculateRatio("", "");

        // Assert
        result.Should().Be(100, "empty strings are identical");
    }

    /// <summary>
    /// Verifies that comparing single identical characters returns 100%.
    /// </summary>
    [Fact]
    public void CalculateRatio_SingleIdenticalCharacter_Returns100()
    {
        // Act
        var result = _sut.CalculateRatio("a", "a");

        // Assert
        result.Should().Be(100, "identical single characters match perfectly");
    }

    /// <summary>
    /// Verifies that comparing single different characters returns 0%.
    /// </summary>
    /// <remarks>
    /// LOGIC: One substitution in a single-character string = 100% different.
    /// </remarks>
    [Fact]
    public void CalculateRatio_SingleDifferentCharacter_Returns0()
    {
        // Act
        var result = _sut.CalculateRatio("a", "b");

        // Assert
        result.Should().Be(0, "single character substitution = complete difference");
    }

    /// <summary>
    /// Verifies that one empty string against non-empty returns 0%.
    /// </summary>
    [Fact]
    public void CalculateRatio_OneEmptyOneNonEmpty_Returns0()
    {
        // Act
        var result = _sut.CalculateRatio("", "hello");

        // Assert
        result.Should().Be(0, "empty vs non-empty has no similarity");
    }

    /// <summary>
    /// Verifies that identical strings always return 100%.
    /// </summary>
    [Fact]
    public void CalculateRatio_IdenticalStrings_Returns100()
    {
        // Arrange
        const string input = "terminology";

        // Act
        var result = _sut.CalculateRatio(input, input);

        // Assert
        result.Should().Be(100, "identical strings are 100% similar");
    }

    #endregion

    #region Known Distance Value Tests

    /// <summary>
    /// Verifies the classic "kitten" to "sitting" example.
    /// </summary>
    /// <remarks>
    /// LOGIC: Classic Levenshtein example.
    /// Edits: k→s (substitution), e→i (substitution), +g (insertion) = 3 edits.
    /// Ratio depends on FuzzySharp's algorithm (not raw distance).
    /// </remarks>
    [Fact]
    public void CalculateRatio_KittenToSitting_ReturnsExpectedRatio()
    {
        // Act
        var result = _sut.CalculateRatio("kitten", "sitting");

        // Assert - FuzzySharp calculates ~57% for this pair
        // LOGIC: FuzzySharp uses a different ratio calculation than raw Levenshtein
        result.Should().BeInRange(60, 65, "kitten→sitting has ~62% similarity per FuzzySharp");
    }

    /// <summary>
    /// Verifies the "saturday" to "sunday" example.
    /// </summary>
    /// <remarks>
    /// LOGIC: Edits: sat→sun = 2 substitutions, urday→day = delete 2 chars.
    /// </remarks>
    [Fact]
    public void CalculateRatio_SaturdayToSunday_ReturnsExpectedRatio()
    {
        // Act
        var result = _sut.CalculateRatio("saturday", "sunday");

        // Assert - FuzzySharp calculates ~71% for this pair
        result.Should().BeInRange(69, 75, "saturday→sunday has ~71% similarity per FuzzySharp");
    }

    /// <summary>
    /// Verifies high similarity for single-character difference.
    /// </summary>
    [Fact]
    public void CalculateRatio_SingleCharacterDifference_ReturnsHighRatio()
    {
        // Arrange - "whitelist" vs "whitelst" (missing 'i')
        const string correct = "whitelist";
        const string typo = "whitelst";

        // Act
        var result = _sut.CalculateRatio(correct, typo);

        // Assert - One character missing in 9-char word = high similarity
        result.Should().BeGreaterThanOrEqualTo(88, "single missing char yields high similarity");
    }

    /// <summary>
    /// Verifies low similarity for completely different strings.
    /// </summary>
    [Fact]
    public void CalculateRatio_CompletelyDifferent_ReturnsLowRatio()
    {
        // Act
        var result = _sut.CalculateRatio("apple", "zebra");

        // Assert
        result.Should().BeLessThan(50, "completely different strings have low similarity");
    }

    /// <summary>
    /// Verifies ratio for string with prefix match.
    /// </summary>
    [Fact]
    public void CalculateRatio_PrefixMatch_ReturnsModerateRatio()
    {
        // Arrange
        const string shortWord = "allow";
        const string longWord = "allowlist";

        // Act
        var result = _sut.CalculateRatio(shortWord, longWord);

        // Assert - "allow" is prefix of "allowlist"
        result.Should().BeInRange(60, 75, "prefix match yields moderate similarity");
    }

    /// <summary>
    /// Verifies ratio for strings with same length but multiple differences.
    /// </summary>
    [Fact]
    public void CalculateRatio_SameLengthMultipleDifferences_ReturnsLowRatio()
    {
        // Arrange - "abcdef" vs "ghijkl" - same length, completely different
        const string stringA = "abcdef";
        const string stringB = "ghijkl";

        // Act
        var result = _sut.CalculateRatio(stringA, stringB);

        // Assert
        result.Should().Be(0, "same-length strings with no common characters");
    }

    /// <summary>
    /// Verifies ratio for anagram strings (same characters, different order).
    /// </summary>
    [Fact]
    public void CalculateRatio_Anagram_ReturnsModerateRatio()
    {
        // Arrange
        const string word = "listen";
        const string anagram = "silent";

        // Act
        var result = _sut.CalculateRatio(word, anagram);

        // Assert - Many edits needed despite same characters
        result.Should().BeInRange(30, 60, "anagrams require significant edits");
    }

    #endregion

    #region Unicode Handling Tests

    /// <summary>
    /// Verifies correct handling of strings with emoji characters.
    /// </summary>
    /// <remarks>
    /// LOGIC: Emoji are multi-byte Unicode characters that must be handled
    /// correctly by the Levenshtein algorithm.
    /// </remarks>
    [Fact]
    public void CalculateRatio_IdenticalEmoji_Returns100()
    {
        // Arrange
        const string emojiString = "hello 👋 world";

        // Act
        var result = _sut.CalculateRatio(emojiString, emojiString);

        // Assert
        result.Should().Be(100, "identical strings with emoji match perfectly");
    }

    /// <summary>
    /// Verifies correct handling of strings with diacritical marks.
    /// </summary>
    [Fact]
    public void CalculateRatio_Diacritics_HandlesDifferencesCorrectly()
    {
        // Arrange
        const string withDiacritics = "café";
        const string withoutDiacritics = "cafe";

        // Act
        var result = _sut.CalculateRatio(withDiacritics, withoutDiacritics);

        // Assert - Single character difference (é vs e)
        result.Should().BeGreaterThanOrEqualTo(75, "diacritic difference is one character");
    }

    /// <summary>
    /// Verifies correct handling of CJK (Chinese/Japanese/Korean) characters.
    /// </summary>
    [Fact]
    public void CalculateRatio_CJKCharacters_HandlesCorrectly()
    {
        // Arrange
        const string japanese1 = "こんにちは";
        const string japanese2 = "こんにちわ"; // Last char different

        // Act
        var result = _sut.CalculateRatio(japanese1, japanese2);

        // Assert - One character difference in 5-character string
        result.Should().BeGreaterThanOrEqualTo(75, "single CJK character difference");
    }

    /// <summary>
    /// Verifies correct handling of mixed ASCII and Unicode.
    /// </summary>
    [Fact]
    public void CalculateRatio_MixedAsciiUnicode_HandlesCorrectly()
    {
        // Arrange
        const string mixed1 = "test-テスト";
        const string mixed2 = "test-テスト";

        // Act
        var result = _sut.CalculateRatio(mixed1, mixed2);

        // Assert
        result.Should().Be(100, "identical mixed strings match perfectly");
    }

    #endregion
}
