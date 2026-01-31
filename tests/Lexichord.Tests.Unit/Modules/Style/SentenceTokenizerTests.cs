using FluentAssertions;
using Lexichord.Modules.Style.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for SentenceTokenizer.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.3a - Verifies sentence tokenization behavior including
/// abbreviation handling, edge cases, and position tracking.
/// </remarks>
public class SentenceTokenizerTests
{
    private readonly SentenceTokenizer _sut;

    public SentenceTokenizerTests()
    {
        var loggerMock = new Mock<ILogger<SentenceTokenizer>>();
        _sut = new SentenceTokenizer(loggerMock.Object);
    }

    #region Empty/Null/Whitespace Input Tests

    [Fact]
    public void Tokenize_NullInput_ReturnsEmptyList()
    {
        // Act
        var result = _sut.Tokenize(null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Tokenize_EmptyString_ReturnsEmptyList()
    {
        // Act
        var result = _sut.Tokenize("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Tokenize_WhitespaceOnly_ReturnsEmptyList()
    {
        // Act
        var result = _sut.Tokenize("   \t\n  ");

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Simple Sentence Tests

    [Fact]
    public void Tokenize_SingleSentence_ReturnsSingleSentence()
    {
        // Arrange
        const string text = "This is a simple sentence.";

        // Act
        var result = _sut.Tokenize(text);

        // Assert
        result.Should().ContainSingle();
        result[0].Text.Should().Be("This is a simple sentence.");
        result[0].WordCount.Should().Be(5);
    }

    [Fact]
    public void Tokenize_TwoSentences_ReturnsTwoSentences()
    {
        // Arrange
        const string text = "First sentence. Second sentence.";

        // Act
        var result = _sut.Tokenize(text);

        // Assert
        result.Should().HaveCount(2);
        result[0].Text.Should().Be("First sentence.");
        result[1].Text.Should().Be("Second sentence.");
    }

    [Fact]
    public void Tokenize_QuestionMark_TreatedAsSentenceEnd()
    {
        // Arrange
        const string text = "Is this a question? Yes it is.";

        // Act
        var result = _sut.Tokenize(text);

        // Assert
        result.Should().HaveCount(2);
        result[0].Text.Should().Be("Is this a question?");
        result[1].Text.Should().Be("Yes it is.");
    }

    [Fact]
    public void Tokenize_ExclamationMark_TreatedAsSentenceEnd()
    {
        // Arrange
        const string text = "Wow! That is amazing.";

        // Act
        var result = _sut.Tokenize(text);

        // Assert
        result.Should().HaveCount(2);
        result[0].Text.Should().Be("Wow!");
        result[1].Text.Should().Be("That is amazing.");
    }

    #endregion

    #region Title Abbreviation Tests

    [Theory]
    [InlineData("Mr. Smith went to the store.", 1)]
    [InlineData("Mrs. Jones is here.", 1)]
    [InlineData("Dr. Brown diagnosed the patient.", 1)]
    [InlineData("Prof. Wilson gave a lecture.", 1)]
    [InlineData("Rev. Johnson led the service.", 1)]
    public void Tokenize_TitleAbbreviation_DoesNotSplitSentence(string text, int expectedCount)
    {
        // LOGIC: v0.3.3a - Title abbreviations like Mr., Mrs., Dr. should not end sentences

        // Act
        var result = _sut.Tokenize(text);

        // Assert
        result.Should().HaveCount(expectedCount);
    }

    [Fact]
    public void Tokenize_MixedTitlesAndSentences_CorrectlySplits()
    {
        // Arrange
        const string text = "Dr. Smith arrived. He met Mrs. Jones at noon.";

        // Act
        var result = _sut.Tokenize(text);

        // Assert
        result.Should().HaveCount(2);
        result[0].Text.Should().Be("Dr. Smith arrived.");
        result[1].Text.Should().Be("He met Mrs. Jones at noon.");
    }

    #endregion

    #region Business Abbreviation Tests

    [Theory]
    [InlineData("Acme Inc. is a great company.", 1)]
    [InlineData("Apple Corp. makes iPhones.", 1)]
    [InlineData("Honda Ltd. sells cars.", 1)]
    [InlineData("Smith Bros. Bakery is closed.", 1)]
    public void Tokenize_BusinessAbbreviation_DoesNotSplitSentence(string text, int expectedCount)
    {
        // LOGIC: v0.3.3a - Business abbreviations like Inc., Corp., Ltd. should not end sentences

        // Act
        var result = _sut.Tokenize(text);

        // Assert
        result.Should().HaveCount(expectedCount);
    }

    #endregion

    #region Geographic/Address Abbreviation Tests

    [Theory]
    [InlineData("I live on Main St. in the city.", 1)]
    [InlineData("The hotel is on Park Ave. downtown.", 1)]
    [InlineData("Mt. Everest is very tall.", 1)]
    public void Tokenize_GeographicAbbreviation_DoesNotSplitSentence(string text, int expectedCount)
    {
        // Act
        var result = _sut.Tokenize(text);

        // Assert
        result.Should().HaveCount(expectedCount);
    }

    #endregion

    #region Latin Abbreviation Tests

    [Theory]
    [InlineData("I have many hobbies, e.g. reading and writing.", 1)]
    [InlineData("This is important, i.e. it matters a lot.", 1)]
    [InlineData("There were many items, etc. in the box.", 1)]
    [InlineData("Team A vs. Team B was a great game.", 1)]
    public void Tokenize_LatinAbbreviation_DoesNotSplitSentence(string text, int expectedCount)
    {
        // LOGIC: v0.3.3a - Latin abbreviations like e.g., i.e., etc. should not end sentences

        // Act
        var result = _sut.Tokenize(text);

        // Assert
        result.Should().HaveCount(expectedCount);
    }

    #endregion

    #region Period-Embedded Abbreviation Tests

    [Fact]
    public void Tokenize_USA_DoesNotSplitWhenFollowedByLowercase()
    {
        // LOGIC: v0.3.3a - "U.S.A." followed by lowercase continues the sentence
        // Arrange
        const string text = "The U.S.A. is a large country.";

        // Act
        var result = _sut.Tokenize(text);

        // Assert
        result.Should().ContainSingle();
        result[0].Text.Should().Contain("U.S.A.");
    }

    [Fact]
    public void Tokenize_USA_SplitsWhenFollowedByUppercase()
    {
        // LOGIC: v0.3.3a - "U.S.A." followed by uppercase starts a new sentence
        // Arrange
        const string text = "I visited the U.S.A. It was wonderful.";

        // Act
        var result = _sut.Tokenize(text);

        // Assert
        result.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("She has a Ph.D. in chemistry.")]
    [InlineData("The meeting is at 3 p.m. today.")]
    [InlineData("He earned his M.D. last year.")]
    public void Tokenize_PeriodEmbeddedAbbreviation_SingleSentence(string text)
    {
        // Act
        var result = _sut.Tokenize(text);

        // Assert
        result.Should().ContainSingle();
    }

    #endregion

    #region Ellipsis Tests

    [Fact]
    public void Tokenize_Ellipsis_DoesNotSplitSentence()
    {
        // LOGIC: v0.3.3a - Ellipsis (...) should not be treated as sentence end
        // Arrange
        const string text = "Wait... what happened next?";

        // Act
        var result = _sut.Tokenize(text);

        // Assert
        result.Should().ContainSingle();
        result[0].Text.Should().Be("Wait... what happened next?");
    }

    [Fact]
    public void Tokenize_EllipsisFollowedByUppercase_StaysAsSingleSentence()
    {
        // LOGIC: Ellipsis followed by uppercase is NOT treated as a break
        // because the ellipsis check returns false for IsRealSentenceBreak
        // Arrange
        const string text = "And then... The next day was different.";

        // Act
        var result = _sut.Tokenize(text);

        // Assert
        result.Should().ContainSingle();
        result[0].Text.Should().Be("And then... The next day was different.");
    }

    #endregion

    #region Initials Tests

    [Fact]
    public void Tokenize_Initials_DoesNotSplitSentence()
    {
        // LOGIC: v0.3.3a - Initials like J.F.K. should stay together
        // Arrange
        const string text = "J.F.K. was the 35th president.";

        // Act
        var result = _sut.Tokenize(text);

        // Assert
        result.Should().ContainSingle();
        result[0].Text.Should().Contain("J.F.K.");
    }

    [Fact]
    public void Tokenize_InitialsFollowedByLowercase_StaysTogether()
    {
        // LOGIC: Initials followed by lowercase indicate continuation
        // Arrange
        const string text = "J.F.K. was assassinated in Dallas.";

        // Act
        var result = _sut.Tokenize(text);

        // Assert
        result.Should().ContainSingle();
        result[0].Text.Should().Contain("J.F.K.");
    }

    #endregion

    #region Position Tracking Tests

    [Fact]
    public void Tokenize_SingleSentence_CorrectPositions()
    {
        // Arrange
        const string text = "Hello world.";
        //                   012345678901

        // Act
        var result = _sut.Tokenize(text);

        // Assert
        result.Should().ContainSingle();
        result[0].StartIndex.Should().Be(0);
        result[0].EndIndex.Should().Be(12);
    }

    [Fact]
    public void Tokenize_MultipleSentences_CorrectPositions()
    {
        // Arrange
        const string text = "First. Second.";
        //                   01234567890123

        // Act
        var result = _sut.Tokenize(text);

        // Assert
        result.Should().HaveCount(2);
        result[0].StartIndex.Should().Be(0);
        result[0].EndIndex.Should().Be(6);
        result[1].StartIndex.Should().Be(7);
        result[1].EndIndex.Should().Be(14);
    }

    [Fact]
    public void Tokenize_LeadingWhitespace_CorrectStartIndex()
    {
        // Arrange
        const string text = "   Hello world.";

        // Act
        var result = _sut.Tokenize(text);

        // Assert
        result.Should().ContainSingle();
        result[0].StartIndex.Should().Be(3); // Skips leading whitespace
    }

    #endregion

    #region Word Count Tests

    [Theory]
    [InlineData("One.", 1)]
    [InlineData("One two.", 2)]
    [InlineData("One two three.", 3)]
    [InlineData("The quick brown fox.", 4)]
    [InlineData("A self-aware robot.", 3)] // Hyphenated counts as one word
    public void Tokenize_VariousLengths_CorrectWordCount(string text, int expectedWords)
    {
        // Act
        var result = _sut.Tokenize(text);

        // Assert
        result.Should().ContainSingle();
        result[0].WordCount.Should().Be(expectedWords);
    }

    #endregion

    #region Sentence Without Terminal Punctuation

    [Fact]
    public void Tokenize_NoTerminalPunctuation_StillReturnsSentence()
    {
        // LOGIC: v0.3.3a - Text without terminal punctuation is included as a sentence
        // Arrange
        const string text = "This sentence has no period";

        // Act
        var result = _sut.Tokenize(text);

        // Assert
        result.Should().ContainSingle();
        result[0].Text.Should().Be("This sentence has no period");
    }

    [Fact]
    public void Tokenize_MixedWithAndWithoutPunctuation_ReturnsAll()
    {
        // Arrange
        const string text = "First sentence. Second without punctuation";

        // Act
        var result = _sut.Tokenize(text);

        // Assert
        result.Should().HaveCount(2);
        result[0].Text.Should().Be("First sentence.");
        result[1].Text.Should().Be("Second without punctuation");
    }

    #endregion

    #region Reference Text Tests

    [Fact]
    public void Tokenize_GettysburgAddressOpening_CorrectSentenceCount()
    {
        // LOGIC: v0.3.3a - Reference text for validation
        // Arrange
        const string text = "Four score and seven years ago our fathers brought forth on this continent, " +
                            "a new nation, conceived in Liberty, and dedicated to the proposition that all men are created equal.";

        // Act
        var result = _sut.Tokenize(text);

        // Assert
        result.Should().ContainSingle();
        result[0].WordCount.Should().BeGreaterThan(25);
    }

    [Fact]
    public void Tokenize_ComplexText_HandlesAllCases()
    {
        // Arrange - includes abbreviations, multiple sentences
        // Note: Quoted punctuation (? and !) inside quotes IS treated as sentence breaks
        // This is acceptable behavior for readability metrics
        const string text = "Dr. Smith arrived at 3 p.m. He met Mrs. Jones. They discussed the U.S.A. economy.";

        // Act
        var result = _sut.Tokenize(text);

        // Assert
        result.Should().HaveCount(3);
    }

    #endregion

    #region Month Abbreviation Tests

    [Theory]
    [InlineData("The event is on Jan. 15th.", 1)]
    [InlineData("We met in Feb. last year.", 1)]
    [InlineData("The deadline is Dec. 31st.", 1)]
    public void Tokenize_MonthAbbreviation_DoesNotSplitSentence(string text, int expectedCount)
    {
        // Act
        var result = _sut.Tokenize(text);

        // Assert
        result.Should().HaveCount(expectedCount);
    }

    #endregion
}
