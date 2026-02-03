// =============================================================================
// File: SentenceBoundaryDetectorTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for SentenceBoundaryDetector (v0.5.6c).
// =============================================================================
// VERSION: v0.5.6c (Smart Truncation)
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="SentenceBoundaryDetector"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.6c")]
public class SentenceBoundaryDetectorTests
{
    private readonly SentenceBoundaryDetector _sut = new(NullLogger<SentenceBoundaryDetector>.Instance);

    #region FindSentenceStart Tests

    [Theory]
    [InlineData("First sentence. Second sentence.", 20, 16)]  // "Second" starts at 16
    [InlineData("Hello world.", 5, 0)]                        // In first sentence
    [InlineData("", 0, 0)]                                    // Empty text
    public void FindSentenceStart_ReturnsCorrectPosition(
        string text, int position, int expected)
    {
        var result = _sut.FindSentenceStart(text, position);
        result.Should().Be(expected);
    }

    [Fact]
    public void FindSentenceStart_AtDocumentStart_ReturnsZero()
    {
        var text = "First sentence. Second sentence.";
        var result = _sut.FindSentenceStart(text, 0);
        result.Should().Be(0);
    }

    [Fact]
    public void FindSentenceStart_AfterExclamationMark_ReturnsCorrectPosition()
    {
        var text = "Hello! How are you?";
        var result = _sut.FindSentenceStart(text, 10); // In "How"
        result.Should().Be(7); // Position after "! "
    }

    [Fact]
    public void FindSentenceStart_AfterQuestionMark_ReturnsCorrectPosition()
    {
        var text = "What? Really?";
        var result = _sut.FindSentenceStart(text, 8); // In "Really"
        result.Should().Be(6);
    }

    #endregion

    #region FindSentenceEnd Tests

    [Theory]
    [InlineData("First sentence. Second sentence.", 0, 15)]   // After first "."
    [InlineData("Hello world. More text.", 0, 12)]            // After "."
    [InlineData("No terminator", 0, 13)]                      // End of text
    public void FindSentenceEnd_ReturnsCorrectPosition(
        string text, int position, int expected)
    {
        var result = _sut.FindSentenceEnd(text, position);
        result.Should().Be(expected);
    }

    [Fact]
    public void FindSentenceEnd_WithExclamationMark_ReturnsAfterMark()
    {
        var text = "Hello world!";
        var result = _sut.FindSentenceEnd(text, 0);
        result.Should().Be(12);
    }

    [Fact]
    public void FindSentenceEnd_WithQuestionMark_ReturnsAfterMark()
    {
        var text = "How are you?";
        var result = _sut.FindSentenceEnd(text, 0);
        result.Should().Be(12);
    }

    [Fact]
    public void FindSentenceEnd_WithMultiplePunctuation_ReturnsAfterAll()
    {
        var text = "Really?! That's amazing!";
        var result = _sut.FindSentenceEnd(text, 0);
        // Should return after "?!"
        result.Should().Be(8);
    }

    #endregion

    #region Abbreviation Tests

    [Theory]
    [InlineData("Dr. Smith said hello. Next sentence.", 0, 21)]  // After "hello."
    [InlineData("Mr. Jones arrived. Welcome.", 0, 18)]            // After "arrived."
    public void FindSentenceEnd_SkipsCommonAbbreviations(
        string text, int position, int expected)
    {
        var result = _sut.FindSentenceEnd(text, position);
        result.Should().Be(expected);
    }

    [Fact]
    public void FindSentenceEnd_SkipsMultipleTitleAbbreviations()
    {
        var text = "Dr. Mrs. Williams spoke. The end.";
        var result = _sut.FindSentenceEnd(text, 0);
        result.Should().Be(24); // After "spoke."
    }

    [Fact]
    public void FindSentenceEnd_SkipsLocationAbbreviations()
    {
        var text = "Visit the U.S.A. for vacation. Return home.";
        var result = _sut.FindSentenceEnd(text, 0);
        result.Should().Be(30); // After "vacation."
    }

    [Theory]
    [InlineData("e.g. this is an example. Next.")]
    [InlineData("i.e. that means this. Next.")]
    [InlineData("See figure, cf. page 5. Next.")]
    public void FindSentenceEnd_SkipsLatinAbbreviations(string text)
    {
        var result = _sut.FindSentenceEnd(text, 0);
        // Should skip the abbreviation and find the real sentence end
        result.Should().BeGreaterThan(10);
    }

    #endregion

    #region Decimal Number Tests

    [Fact]
    public void FindSentenceEnd_SkipsDecimalNumbers()
    {
        var text = "The value is 3.14 approximately. Next sentence.";
        var result = _sut.FindSentenceEnd(text, 0);
        result.Should().Be(32); // After "approximately."
    }

    [Fact]
    public void FindSentenceEnd_SkipsMultipleDecimalNumbers()
    {
        var text = "Compare 3.14 to 2.71 and also 1.41. Done.";
        var result = _sut.FindSentenceEnd(text, 0);
        result.Should().Be(35); // After "1.41."
    }

    [Fact]
    public void FindSentenceEnd_HandlesPriceFormats()
    {
        var text = "It costs $9.99 on sale. Buy now.";
        var result = _sut.FindSentenceEnd(text, 0);
        result.Should().Be(23); // After "sale."
    }

    #endregion

    #region GetBoundaries Tests

    [Fact]
    public void GetBoundaries_DetectsAllSentences()
    {
        var text = "First sentence. Second sentence. Third sentence.";
        var boundaries = _sut.GetBoundaries(text);

        boundaries.Should().HaveCount(3);
        boundaries[0].Start.Should().Be(0);
        boundaries[0].End.Should().Be(15);
        boundaries[1].Start.Should().Be(16);
        boundaries[2].Start.Should().Be(33);
    }

    [Fact]
    public void GetBoundaries_HandlesMixedTerminators()
    {
        var text = "Question? Exclamation! Statement.";
        var boundaries = _sut.GetBoundaries(text);
        boundaries.Should().HaveCount(3);
    }

    [Fact]
    public void GetBoundaries_HandlesEmptyText()
    {
        var boundaries = _sut.GetBoundaries("");
        boundaries.Should().BeEmpty();
    }

    [Fact]
    public void GetBoundaries_HandlesWhitespaceOnlyText()
    {
        var boundaries = _sut.GetBoundaries("   ");
        boundaries.Should().BeEmpty();
    }

    [Fact]
    public void GetBoundaries_HandlesNoTerminators()
    {
        var text = "Text without any sentence terminators";
        var boundaries = _sut.GetBoundaries(text);

        boundaries.Should().ContainSingle()
            .Which.Should().Be(new SentenceBoundary(0, text.Length));
    }

    #endregion

    #region Word Boundary Tests

    [Theory]
    [InlineData("Hello world test", 8, 6)]   // "world" starts at 6
    [InlineData("Hello world test", 0, 0)]   // Already at start
    [InlineData("   word", 3, 3)]            // "word" starts at 3
    [InlineData("one two three", 5, 4)]      // "two" starts at 4
    public void FindWordStart_ReturnsCorrectPosition(
        string text, int position, int expected)
    {
        var result = _sut.FindWordStart(text, position);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Hello world test", 6, 11)]  // "world" ends at 11
    [InlineData("Hello world test", 12, 16)] // "test" ends at 16
    [InlineData("word", 0, 4)]               // Single word
    public void FindWordEnd_ReturnsCorrectPosition(
        string text, int position, int expected)
    {
        var result = _sut.FindWordEnd(text, position);
        result.Should().Be(expected);
    }

    [Fact]
    public void FindWordStart_EmptyText_ReturnsZero()
    {
        var result = _sut.FindWordStart("", 5);
        result.Should().Be(0);
    }

    [Fact]
    public void FindWordEnd_EmptyText_ReturnsZero()
    {
        var result = _sut.FindWordEnd("", 5);
        result.Should().Be(0);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void FindSentenceEnd_PositionBeyondText_ReturnsTextLength()
    {
        var text = "Short.";
        var result = _sut.FindSentenceEnd(text, 100);
        result.Should().Be(6);
    }

    [Fact]
    public void FindSentenceStart_NegativePosition_ReturnsZero()
    {
        var text = "Hello world.";
        var result = _sut.FindSentenceStart(text, -5);
        result.Should().Be(0);
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        var act = () => new SentenceBoundaryDetector(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion
}
