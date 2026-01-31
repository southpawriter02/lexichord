// -----------------------------------------------------------------------
// <copyright file="FuzzyTypoDetectionTests.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.Style.Services;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Typo detection pattern tests for FuzzyMatchService (v0.3.8a).
/// </summary>
/// <remarks>
/// <para>
/// LOGIC: v0.3.8a - Verifies the fuzzy matching service correctly detects
/// common typo patterns that occur in real-world writing. These tests
/// ensure the algorithm catches the kinds of mistakes writers actually make.
/// </para>
/// <para>
/// <b>Test Categories:</b>
/// <list type="bullet">
///   <item>Single Substitution - One character replaced with another</item>
///   <item>Transposition - Adjacent characters swapped</item>
///   <item>Omission - Missing character</item>
///   <item>Insertion - Extra character added</item>
///   <item>Common Patterns - Double letters, common misspellings</item>
/// </list>
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "FuzzyMatching")]
[Trait("Version", "v0.3.8a")]
public class FuzzyTypoDetectionTests
{
    private readonly FuzzyMatchService _sut = new();

    /// <summary>
    /// Default threshold for typo detection tests (80%).
    /// </summary>
    private const double DefaultThreshold = 0.80;

    #region Single Substitution Tests

    /// <summary>
    /// Verifies detection of single character substitution at start.
    /// </summary>
    /// <remarks>
    /// LOGIC: Single substitution is a common typo where one key is
    /// hit instead of an adjacent key.
    /// </remarks>
    [Fact]
    public void IsMatch_SingleSubstitutionAtStart_DetectedAsTypo()
    {
        // Arrange - "blacklist" → "dlackist" (b→d)
        const string correct = "blacklist";
        const string typo = "dlackist";

        // Act
        var result = _sut.IsMatch(correct, typo, DefaultThreshold);

        // Assert
        result.Should().BeTrue("single substitution at start should match");
    }

    /// <summary>
    /// Verifies detection of single character substitution in middle.
    /// </summary>
    [Fact]
    public void IsMatch_SingleSubstitutionInMiddle_DetectedAsTypo()
    {
        // Arrange - "whitelist" → "whiTelist" → normalized same, use "whitdlist"
        const string correct = "whitelist";
        const string typo = "whitdlist"; // e→d

        // Act
        var result = _sut.IsMatch(correct, typo, DefaultThreshold);

        // Assert
        result.Should().BeTrue("single substitution in middle should match");
    }

    /// <summary>
    /// Verifies detection of single character substitution at end.
    /// </summary>
    [Fact]
    public void IsMatch_SingleSubstitutionAtEnd_DetectedAsTypo()
    {
        // Arrange - "whitelist" → "whitelisT" normalized, use "whitelisr"
        const string correct = "whitelist";
        const string typo = "whitelisr"; // t→r

        // Act
        var result = _sut.IsMatch(correct, typo, DefaultThreshold);

        // Assert
        result.Should().BeTrue("single substitution at end should match");
    }

    /// <summary>
    /// Verifies substitution with vowel confusion (common typo).
    /// </summary>
    [Fact]
    public void IsMatch_VowelSubstitution_DetectedAsTypo()
    {
        // Arrange - Common vowel confusion: "terminology" → "termonology" (i→o)
        const string correct = "terminology";
        const string typo = "termonology";

        // Act
        var result = _sut.IsMatch(correct, typo, DefaultThreshold);

        // Assert
        result.Should().BeTrue("vowel substitution should match");
    }

    #endregion

    #region Transposition Tests

    /// <summary>
    /// Verifies detection of adjacent character transposition.
    /// </summary>
    /// <remarks>
    /// LOGIC: Transposition errors occur when fingers hit keys in wrong order.
    /// </remarks>
    [Fact]
    public void IsMatch_AdjacentTransposition_DetectedAsTypo()
    {
        // Arrange - "the" → "teh" (common typo)
        const string correct = "their";
        const string typo = "thier"; // e and i swapped

        // Act
        var result = _sut.IsMatch(correct, typo, DefaultThreshold);

        // Assert
        result.Should().BeTrue("adjacent transposition should match");
    }

    /// <summary>
    /// Verifies detection of transposition in longer word.
    /// </summary>
    [Fact]
    public void IsMatch_TranspositionInLongWord_DetectedAsTypo()
    {
        // Arrange - "terminology" → "terminoloyg" (g and y swapped)
        const string correct = "terminology";
        const string typo = "terminoloyg";

        // Act
        var result = _sut.IsMatch(correct, typo, DefaultThreshold);

        // Assert
        result.Should().BeTrue("transposition in long word should match");
    }

    /// <summary>
    /// Verifies detection of double-letter transposition.
    /// </summary>
    [Fact]
    public void IsMatch_DoubleLetterTransposition_DetectedAsTypo()
    {
        // Arrange - "success" → "suceess" (c transposed)
        const string correct = "success";
        const string typo = "succses"; // e and s swapped

        // Act
        var result = _sut.IsMatch(correct, typo, DefaultThreshold);

        // Assert
        result.Should().BeTrue("double-letter transposition should match");
    }

    /// <summary>
    /// Verifies typical "ie" vs "ei" transposition.
    /// </summary>
    [Fact]
    public void IsMatch_IETransposition_DetectedAsTypo()
    {
        // Arrange - Classic "ie" vs "ei" confusion
        const string correct = "receive";
        const string typo = "recieve";

        // Act
        var result = _sut.IsMatch(correct, typo, DefaultThreshold);

        // Assert
        result.Should().BeTrue("ie/ei transposition should match");
    }

    #endregion

    #region Omission Tests

    /// <summary>
    /// Verifies detection of missing character at start.
    /// </summary>
    /// <remarks>
    /// LOGIC: Omission errors occur when a key is missed entirely.
    /// </remarks>
    [Fact]
    public void IsMatch_OmissionAtStart_DetectedAsTypo()
    {
        // Arrange - "whitelist" → "hitelist" (missing 'w')
        const string correct = "whitelist";
        const string typo = "hitelist";

        // Act
        var result = _sut.IsMatch(correct, typo, DefaultThreshold);

        // Assert
        result.Should().BeTrue("omission at start should match");
    }

    /// <summary>
    /// Verifies detection of missing character in middle.
    /// </summary>
    [Fact]
    public void IsMatch_OmissionInMiddle_DetectedAsTypo()
    {
        // Arrange - "whitelist" → "whitelst" (missing 'i')
        const string correct = "whitelist";
        const string typo = "whitelst";

        // Act
        var result = _sut.IsMatch(correct, typo, DefaultThreshold);

        // Assert
        result.Should().BeTrue("omission in middle should match");
    }

    /// <summary>
    /// Verifies detection of missing character at end.
    /// </summary>
    [Fact]
    public void IsMatch_OmissionAtEnd_DetectedAsTypo()
    {
        // Arrange - "whitelist" → "whitelis" (missing 't')
        const string correct = "whitelist";
        const string typo = "whitelis";

        // Act
        var result = _sut.IsMatch(correct, typo, DefaultThreshold);

        // Assert
        result.Should().BeTrue("omission at end should match");
    }

    /// <summary>
    /// Verifies detection of missing doubled letter.
    /// </summary>
    [Fact]
    public void IsMatch_MissingDoubledLetter_DetectedAsTypo()
    {
        // Arrange - "success" → "sucess" (missing one 'c')
        const string correct = "success";
        const string typo = "sucess";

        // Act
        var result = _sut.IsMatch(correct, typo, DefaultThreshold);

        // Assert
        result.Should().BeTrue("missing doubled letter should match");
    }

    #endregion

    #region Insertion Tests

    /// <summary>
    /// Verifies detection of extra character at start.
    /// </summary>
    /// <remarks>
    /// LOGIC: Insertion errors occur when an extra key is accidentally hit.
    /// </remarks>
    [Fact]
    public void IsMatch_InsertionAtStart_DetectedAsTypo()
    {
        // Arrange - "whitelist" → "awhitelist" (extra 'a')
        const string correct = "whitelist";
        const string typo = "awhitelist";

        // Act
        var result = _sut.IsMatch(correct, typo, DefaultThreshold);

        // Assert
        result.Should().BeTrue("insertion at start should match");
    }

    /// <summary>
    /// Verifies detection of extra character in middle.
    /// </summary>
    [Fact]
    public void IsMatch_InsertionInMiddle_DetectedAsTypo()
    {
        // Arrange - "whitelist" → "whiutelist" (extra 'u')
        const string correct = "whitelist";
        const string typo = "whiutelist";

        // Act
        var result = _sut.IsMatch(correct, typo, DefaultThreshold);

        // Assert
        result.Should().BeTrue("insertion in middle should match");
    }

    /// <summary>
    /// Verifies detection of extra character at end.
    /// </summary>
    [Fact]
    public void IsMatch_InsertionAtEnd_DetectedAsTypo()
    {
        // Arrange - "whitelist" → "whitelists" (extra 's')
        const string correct = "whitelist";
        const string typo = "whitelists";

        // Act
        var result = _sut.IsMatch(correct, typo, DefaultThreshold);

        // Assert
        result.Should().BeTrue("insertion at end should match");
    }

    /// <summary>
    /// Verifies detection of doubled letter insertion.
    /// </summary>
    [Fact]
    public void IsMatch_DoubledLetterInsertion_DetectedAsTypo()
    {
        // Arrange - "whitelist" → "wwhitelist" (doubled 'w')
        const string correct = "whitelist";
        const string typo = "wwhitelist";

        // Act
        var result = _sut.IsMatch(correct, typo, DefaultThreshold);

        // Assert
        result.Should().BeTrue("doubled letter insertion should match");
    }

    #endregion

    #region Common Pattern Tests

    /// <summary>
    /// Verifies detection of common "teh" typo for "the".
    /// </summary>
    [Fact]
    public void IsMatch_CommonTehTypo_DetectedAsTypo()
    {
        // Arrange
        const string correct = "therefore";
        const string typo = "therfore"; // missing 'e'

        // Act
        var result = _sut.IsMatch(correct, typo, DefaultThreshold);

        // Assert
        result.Should().BeTrue("common 'therefore' typo should match");
    }

    /// <summary>
    /// Verifies detection of "taht" for "that" pattern.
    /// </summary>
    [Fact]
    public void IsMatch_CommonTahtPattern_DetectedAsTypo()
    {
        // Arrange - Applied to longer word
        const string correct = "whatsoever";
        const string typo = "whtasoever"; // transposition

        // Act
        var result = _sut.IsMatch(correct, typo, DefaultThreshold);

        // Assert
        result.Should().BeTrue("common transposition pattern should match");
    }

    /// <summary>
    /// Verifies detection of dropping silent letters.
    /// </summary>
    [Fact]
    public void IsMatch_DroppedSilentLetter_DetectedAsTypo()
    {
        // Arrange
        const string correct = "knight";
        const string typo = "kight"; // missing silent 'n'

        // Act
        var result = _sut.IsMatch(correct, typo, DefaultThreshold);

        // Assert
        result.Should().BeTrue("dropped silent letter should match");
    }

    /// <summary>
    /// Verifies detection of incorrect double letters.
    /// </summary>
    [Fact]
    public void IsMatch_WrongDoubleLetter_DetectedAsTypo()
    {
        // Arrange - "accommodate" often misspelled
        const string correct = "accommodate";
        const string typo = "accomodate"; // missing one 'm'

        // Act
        var result = _sut.IsMatch(correct, typo, DefaultThreshold);

        // Assert
        result.Should().BeTrue("wrong double letter should match");
    }

    /// <summary>
    /// Verifies detection with British vs American spelling.
    /// </summary>
    [Fact]
    public void IsMatch_BritishAmericanSpelling_DetectedAsSimilar()
    {
        // Arrange
        const string american = "color";
        const string british = "colour";

        // Act
        var result = _sut.IsMatch(american, british, DefaultThreshold);

        // Assert
        result.Should().BeTrue("British/American variants should match at 80%");
    }

    /// <summary>
    /// Verifies combined typo patterns still match.
    /// </summary>
    [Fact]
    public void IsMatch_CombinedTypoPatterns_MatchesAtLowerThreshold()
    {
        // Arrange - Multiple typos
        const string correct = "terminology";
        const string multipleTypos = "termonlogy"; // substitution + omission

        // Act
        var ratioResult = _sut.CalculateRatio(correct, multipleTypos);
        var matchAt70 = _sut.IsMatch(correct, multipleTypos, 0.70);
        var matchAt80 = _sut.IsMatch(correct, multipleTypos, 0.80);

        // Assert
        ratioResult.Should().BeGreaterThan(70, "multiple typos still have decent similarity");
        matchAt70.Should().BeTrue("multiple typos match at 70% threshold");
        // Note: May or may not match at 80% depending on exact similarity
    }

    #endregion
}
