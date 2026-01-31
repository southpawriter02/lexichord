using FluentAssertions;
using Lexichord.Modules.Style.Services;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for SyllableCounter.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.3b - Verifies syllable counting behavior including
/// known words, suffix rules, edge cases, and complex word detection.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.3.3b")]
public class SyllableCounterTests
{
    private readonly SyllableCounter _sut = new();

    #region Known Words Tests

    [Theory]
    [InlineData("the", 1)]
    [InlineData("cat", 1)]
    [InlineData("dog", 1)]
    [InlineData("queue", 1)]
    [InlineData("fire", 1)]
    [InlineData("hour", 1)]
    [InlineData("table", 2)]
    [InlineData("water", 2)]
    [InlineData("happy", 2)]
    [InlineData("quiet", 2)]
    [InlineData("beautiful", 3)]
    [InlineData("animal", 3)]
    [InlineData("library", 3)]
    [InlineData("dictionary", 4)]
    [InlineData("understanding", 4)]
    [InlineData("documentation", 5)]
    [InlineData("vocabulary", 5)]
    public void CountSyllables_KnownWords_ReturnsExpectedCount(string word, int expected)
    {
        // LOGIC: v0.3.3b - Exception dictionary and heuristics should produce accurate counts

        // Act
        var result = _sut.CountSyllables(word);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region -ed Suffix Tests

    [Theory]
    [InlineData("jumped", 1)]    // -ed after consonant (not d/t) - silent
    [InlineData("walked", 1)]    // -ed after consonant (not d/t) - silent
    [InlineData("played", 1)]    // -ed after vowel - silent
    [InlineData("loaded", 2)]    // -ed after 'd' - pronounced
    [InlineData("wanted", 2)]    // -ed after 't' - pronounced
    [InlineData("needed", 2)]    // -ed after 'd' - pronounced
    public void CountSyllables_EdSuffix_HandlesCorrectly(string word, int expected)
    {
        // LOGIC: v0.3.3b - "-ed" adds syllable only after 'd' or 't'

        // Act
        var result = _sut.CountSyllables(word);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region -es Suffix Tests

    [Theory]
    [InlineData("makes", 1)]     // -es after consonant - silent
    [InlineData("takes", 1)]     // -es after consonant - silent
    [InlineData("boxes", 2)]     // -es after 'x' - pronounced
    [InlineData("dishes", 2)]    // -es after 'sh' - pronounced
    [InlineData("churches", 2)]  // -es after 'ch' - pronounced
    public void CountSyllables_EsSuffix_HandlesCorrectly(string word, int expected)
    {
        // LOGIC: v0.3.3b - "-es" adds syllable after s, x, z, ch, sh

        // Act
        var result = _sut.CountSyllables(word);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Silent 'e' Tests

    [Theory]
    [InlineData("make", 1)]      // Silent 'e'
    [InlineData("give", 1)]      // Silent 'e'
    [InlineData("like", 1)]      // Silent 'e'
    [InlineData("table", 2)]     // "-le" exception - 'e' is pronounced
    [InlineData("apple", 2)]     // "-le" exception - 'e' is pronounced
    [InlineData("simple", 2)]    // "-le" exception - 'e' is pronounced
    public void CountSyllables_SilentE_HandlesCorrectly(string word, int expected)
    {
        // LOGIC: v0.3.3b - Silent 'e' at end reduces count, unless "-le"

        // Act
        var result = _sut.CountSyllables(word);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Edge Cases Tests

    [Theory]
    [InlineData("", 1)]
    [InlineData(null, 1)]
    [InlineData("   ", 1)]
    [InlineData("a", 1)]
    [InlineData("I", 1)]
    public void CountSyllables_EdgeCases_ReturnsMinimumOne(string? word, int expected)
    {
        // LOGIC: v0.3.3b - Empty/null input returns minimum 1

        // Act
        var result = _sut.CountSyllables(word!);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region IsComplexWord Tests

    [Theory]
    [InlineData("beautiful", true)]      // 3 syllables - complex
    [InlineData("understanding", true)]  // 4 syllables - complex
    [InlineData("documentation", true)]  // 5 syllables - complex
    [InlineData("running", false)]       // 2 syllables - not complex
    [InlineData("simple", false)]        // 2 syllables - not complex
    [InlineData("cat", false)]           // 1 syllable - not complex
    [InlineData("table", false)]         // 2 syllables - not complex
    public void IsComplexWord_ReturnsExpected(string word, bool expected)
    {
        // LOGIC: v0.3.3b - Complex words have 3+ meaningful syllables

        // Act
        var result = _sut.IsComplexWord(word);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void IsComplexWord_EmptyInput_ReturnsFalse(string? word)
    {
        // LOGIC: v0.3.3b - Empty/null words are not complex

        // Act
        var result = _sut.IsComplexWord(word!);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Suffix Inflation Tests

    [Theory]
    [InlineData("quickly", false)]   // "quick" has 1 syllable, +ly = 2
    [InlineData("happily", false)]   // "happy" has 2 syllables, -y+ily = 3, but root < 3
    [InlineData("jumped", false)]    // 1 syllable
    [InlineData("walking", false)]   // 2 syllables, root "walk" has 1
    public void IsComplexWord_SuffixInflation_ExcludesFromComplex(string word, bool expected)
    {
        // LOGIC: v0.3.3b - Words reaching 3 syllables only due to suffixes are not complex

        // Act
        var result = _sut.IsComplexWord(word);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Case Insensitivity Tests

    [Fact]
    public void CountSyllables_CaseInsensitive()
    {
        // LOGIC: v0.3.3b - Case should not affect syllable count

        // Act
        var lower = _sut.CountSyllables("beautiful");
        var upper = _sut.CountSyllables("BEAUTIFUL");
        var mixed = _sut.CountSyllables("BeAuTiFuL");

        // Assert
        lower.Should().Be(upper).And.Be(mixed).And.Be(3);
    }

    [Fact]
    public void CountSyllables_ExceptionDictionary_CaseInsensitive()
    {
        // LOGIC: v0.3.3b - Exception dictionary is case-insensitive

        // Act & Assert
        _sut.CountSyllables("QUEUE").Should().Be(1);
        _sut.CountSyllables("Queue").Should().Be(1);
        _sut.CountSyllables("queue").Should().Be(1);
    }

    #endregion
}
