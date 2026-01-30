using FluentAssertions;
using Lexichord.Modules.Style.Services;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for DocumentTokenizer.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.1c - Verifies word tokenization behavior including
/// hyphenated words, case normalization, and position tracking.
/// </remarks>
public class DocumentTokenizerTests
{
    private readonly DocumentTokenizer _sut = new();

    #region Tokenize Tests

    [Fact]
    public void Tokenize_EmptyString_ReturnsEmptySet()
    {
        // Act
        var result = _sut.Tokenize("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Tokenize_WhitespaceOnly_ReturnsEmptySet()
    {
        // Act
        var result = _sut.Tokenize("   \t\n  ");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Tokenize_SingleWord_ReturnsSingleToken()
    {
        // Act
        var result = _sut.Tokenize("hello");

        // Assert
        result.Should().ContainSingle().Which.Should().Be("hello");
    }

    [Fact]
    public void Tokenize_MultipleWords_ReturnsAllUniqueTokens()
    {
        // Act
        var result = _sut.Tokenize("The quick brown fox");

        // Assert
        result.Should().HaveCount(4);
        result.Should().Contain(["the", "quick", "brown", "fox"]);
    }

    [Fact]
    public void Tokenize_DuplicateWords_ReturnsUniqueSet()
    {
        // Act
        var result = _sut.Tokenize("the the the");

        // Assert
        result.Should().ContainSingle().Which.Should().Be("the");
    }

    [Fact]
    public void Tokenize_MixedCase_NormalizesToLowercase()
    {
        // Act
        var result = _sut.Tokenize("Hello WORLD mixedCase");

        // Assert
        result.Should().Contain(["hello", "world", "mixedcase"]);
    }

    [Fact]
    public void Tokenize_HyphenatedWords_PreservesAsOneToken()
    {
        // LOGIC: v0.3.1c - Requirement: hyphenated words like "self-aware" stay intact
        // Act
        var result = _sut.Tokenize("This is a self-aware robot");

        // Assert
        result.Should().Contain("self-aware");
        result.Should().HaveCount(5);
    }

    [Fact]
    public void Tokenize_ComplexHyphens_PreservesAllHyphenatedParts()
    {
        // LOGIC: Multi-part hyphenations should remain together
        // Act
        var result = _sut.Tokenize("state-of-the-art technology");

        // Assert
        result.Should().Contain("state-of-the-art");
        result.Should().Contain("technology");
    }

    [Fact]
    public void Tokenize_Punctuation_ExcludesPunctuation()
    {
        // Act
        var result = _sut.Tokenize("Hello, world! How are you?");

        // Assert
        result.Should().Contain(["hello", "world", "how", "are", "you"]);
        result.Should().NotContain(",");
        result.Should().NotContain("!");
    }

    [Fact]
    public void Tokenize_Numbers_IncludesNumbers()
    {
        // Act
        var result = _sut.Tokenize("Testing 123 words 456");

        // Assert
        result.Should().Contain(["testing", "123", "words", "456"]);
    }

    [Fact]
    public void Tokenize_Underscores_TreatsAsPartOfWord()
    {
        // Act
        var result = _sut.Tokenize("snake_case variable_name");

        // Assert
        result.Should().Contain(["snake_case", "variable_name"]);
    }

    #endregion

    #region TokenizeWithPositions Tests

    [Fact]
    public void TokenizeWithPositions_EmptyString_ReturnsEmptyList()
    {
        // Act
        var result = _sut.TokenizeWithPositions("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void TokenizeWithPositions_SingleWord_ReturnsCorrectPositions()
    {
        // Act
        var result = _sut.TokenizeWithPositions("hello");

        // Assert
        result.Should().ContainSingle();
        var token = result[0];
        token.Token.Should().Be("hello");
        token.StartOffset.Should().Be(0);
        token.EndOffset.Should().Be(5);
    }

    [Fact]
    public void TokenizeWithPositions_MultipleWords_ReturnsCorrectPositions()
    {
        // Arrange
        const string text = "The quick brown fox";
        //                   0123456789...

        // Act
        var result = _sut.TokenizeWithPositions(text);

        // Assert
        result.Should().HaveCount(4);
        result[0].Token.Should().Be("the");
        result[0].StartOffset.Should().Be(0);
        result[0].EndOffset.Should().Be(3);

        result[1].Token.Should().Be("quick");
        result[1].StartOffset.Should().Be(4);
        result[1].EndOffset.Should().Be(9);
    }

    [Fact]
    public void TokenizeWithPositions_DuplicateWords_ReturnsAllOccurrences()
    {
        // LOGIC: Unlike Tokenize(), this returns ALL occurrences for violation reporting
        // Act
        var result = _sut.TokenizeWithPositions("the the the");

        // Assert
        result.Should().HaveCount(3);
        result.All(t => t.Token == "the").Should().BeTrue();
    }

    [Fact]
    public void TokenizeWithPositions_HyphenatedWord_CorrectSpan()
    {
        // Arrange
        const string text = "A self-aware robot";
        //                   0123456789012345678

        // Act
        var result = _sut.TokenizeWithPositions(text);

        // Assert
        var hyphenatedToken = result.FirstOrDefault(t => t.Token == "self-aware");
        hyphenatedToken.Should().NotBeNull();
        hyphenatedToken.StartOffset.Should().Be(2);
        hyphenatedToken.EndOffset.Should().Be(12);
    }

    [Fact]
    public void TokenizeWithPositions_Multiline_CorrectOffsets()
    {
        // Arrange
        const string text = "First line\nSecond line";

        // Act
        var result = _sut.TokenizeWithPositions(text);

        // Assert
        result.Should().HaveCount(4);
        var secondWord = result.FirstOrDefault(t => t.Token == "second");
        secondWord.Should().NotBeNull();
        secondWord.StartOffset.Should().Be(11);
    }

    #endregion
}
