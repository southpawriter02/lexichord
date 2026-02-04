// =============================================================================
// File: SentenceParserTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for the Sentence Parser (v0.5.6f).
// =============================================================================
// LOGIC: Tests the SpacySentenceParser implementation including sentence
//   segmentation, tokenization, dependency parsing, semantic role labeling,
//   caching, and edge cases.
//
// v0.5.6f: Sentence Parser (Claim Extraction Pipeline)
// Dependencies: ISentenceParser, SpacySentenceParser, ParseOptions,
//               ParsedSentence, Token, DependencyNode, etc.
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts.Knowledge.Parsing;
using Lexichord.Modules.Knowledge.Extraction.Parsing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Knowledge;

/// <summary>
/// Unit tests for <see cref="SpacySentenceParser"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.6f")]
public class SentenceParserTests : IDisposable
{
    private readonly ISentenceParser _parser;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<SpacySentenceParser>> _loggerMock;

    public SentenceParserTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<SpacySentenceParser>>();
        _parser = new SpacySentenceParser(_cache, _loggerMock.Object);
    }

    public void Dispose()
    {
        _cache.Dispose();
        GC.SuppressFinalize(this);
    }

    #region ParseAsync Tests

    [Fact]
    public async Task ParseAsync_SimpleSentence_ReturnsTokens()
    {
        // Arrange
        var text = "The endpoint accepts parameters.";

        // Act
        var result = await _parser.ParseAsync(text);

        // Assert
        result.Sentences.Should().HaveCount(1);
        result.Sentences[0].Tokens.Should().HaveCountGreaterThan(3);
        result.Sentences[0].Tokens.Should().Contain(t => t.Text == "endpoint");
        result.Sentences[0].Tokens.Should().Contain(t => t.Text == "accepts");
        result.Sentences[0].Tokens.Should().Contain(t => t.Text == "parameters");
    }

    [Fact]
    public async Task ParseAsync_MultipleSentences_SegmentsCorrectly()
    {
        // Arrange
        var text = "First sentence. Second sentence. Third sentence.";

        // Act
        var result = await _parser.ParseAsync(text);

        // Assert
        result.Sentences.Should().HaveCount(3);
        result.Sentences[0].Text.Should().Contain("First");
        result.Sentences[1].Text.Should().Contain("Second");
        result.Sentences[2].Text.Should().Contain("Third");
    }

    [Fact]
    public async Task ParseAsync_WithDependencies_BuildsTree()
    {
        // Arrange
        var text = "The endpoint accepts a parameter.";
        var options = new ParseOptions { IncludeDependencies = true };

        // Act
        var result = await _parser.ParseAsync(text, options);

        // Assert
        var sentence = result.Sentences[0];
        sentence.Dependencies.Should().NotBeEmpty();
        sentence.DependencyRoot.Should().NotBeNull();
    }

    [Fact]
    public async Task ParseAsync_WithSRL_ExtractsFrames()
    {
        // Arrange
        var text = "The endpoint accepts parameters.";
        var options = new ParseOptions { IncludeSRL = true };

        // Act
        var result = await _parser.ParseAsync(text, options);

        // Assert
        var sentence = result.Sentences[0];
        sentence.SemanticFrames.Should().NotBeNullOrEmpty();
        sentence.SemanticFrames![0].Predicate.Should().NotBeNull();
    }

    [Fact]
    public async Task ParseAsync_EmptyText_ReturnsEmptyResult()
    {
        // Arrange
        var text = "";

        // Act
        var result = await _parser.ParseAsync(text);

        // Assert
        result.Sentences.Should().BeEmpty();
        result.Duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task ParseAsync_WhitespaceOnly_ReturnsEmptyResult()
    {
        // Arrange
        var text = "   \t\n  ";

        // Act
        var result = await _parser.ParseAsync(text);

        // Assert
        result.Sentences.Should().BeEmpty();
    }

    #endregion

    #region ParsedSentence Helper Methods Tests

    [Fact]
    public async Task ParsedSentence_GetRootVerb_ReturnsVerb()
    {
        // Arrange
        var text = "The endpoint accepts parameters.";
        var result = await _parser.ParseAsync(text);

        // Act
        var verb = result.Sentences[0].GetRootVerb();

        // Assert
        verb.Should().NotBeNull();
        verb!.Lemma.Should().Be("accept");
    }

    [Fact]
    public async Task ParsedSentence_GetSubject_ReturnsSubject()
    {
        // Arrange
        var text = "The endpoint accepts parameters.";
        var result = await _parser.ParseAsync(text);

        // Act
        var subject = result.Sentences[0].GetSubject();

        // Assert
        subject.Should().NotBeNull();
        subject!.Text.Should().Be("endpoint");
    }

    [Fact]
    public async Task ParsedSentence_GetDirectObject_ReturnsObject()
    {
        // Arrange
        var text = "The endpoint accepts parameters.";
        var result = await _parser.ParseAsync(text);

        // Act
        var dobj = result.Sentences[0].GetDirectObject();

        // Assert
        dobj.Should().NotBeNull();
        dobj!.Text.Should().Be("parameters");
    }

    [Fact]
    public async Task ParsedSentence_GetVerbs_ReturnsAllVerbs()
    {
        // Arrange
        var text = "The API accepts requests and returns responses.";
        var result = await _parser.ParseAsync(text);

        // Act
        var verbs = result.Sentences[0].GetVerbs();

        // Assert
        verbs.Should().HaveCountGreaterOrEqualTo(2);
        verbs.Should().Contain(v => v.Lemma == "accept");
        verbs.Should().Contain(v => v.Lemma == "return");
    }

    [Fact]
    public async Task ParsedSentence_GetNouns_ReturnsAllNouns()
    {
        // Arrange
        var text = "The endpoint accepts the parameter.";
        var result = await _parser.ParseAsync(text);

        // Act
        var nouns = result.Sentences[0].GetNouns();

        // Assert
        nouns.Should().HaveCountGreaterOrEqualTo(2);
        nouns.Should().Contain(n => n.Text == "endpoint");
        nouns.Should().Contain(n => n.Text == "parameter");
    }

    [Fact]
    public async Task ParsedSentence_GetContentWords_ExcludesStopWords()
    {
        // Arrange
        var text = "The endpoint accepts the parameter.";
        var result = await _parser.ParseAsync(text);

        // Act
        var contentWords = result.Sentences[0].GetContentWords();

        // Assert
        contentWords.Should().NotContain(w => w.Text.ToLower() == "the");
        contentWords.Should().Contain(w => w.Text == "endpoint");
        contentWords.Should().NotContain(w => w.IsPunct);
    }

    #endregion

    #region Caching Tests

    [Fact]
    public async Task ParseAsync_UseCache_ReturnsCached()
    {
        // Arrange
        var text = "Cached sentence.";
        var options = new ParseOptions { UseCache = true };

        // Act
        var result1 = await _parser.ParseAsync(text, options);
        var result2 = await _parser.ParseAsync(text, options);

        // Assert - same object from cache (same ID)
        result1.Sentences[0].Id.Should().Be(result2.Sentences[0].Id);
    }

    [Fact]
    public async Task ParseAsync_NoCaching_ReturnsDifferentIds()
    {
        // Arrange
        var text = "Non-cached sentence.";
        var options = new ParseOptions { UseCache = false };

        // Act
        var result1 = await _parser.ParseAsync(text, options);
        var result2 = await _parser.ParseAsync(text, options);

        // Assert - different objects (different IDs)
        result1.Sentences[0].Id.Should().NotBe(result2.Sentences[0].Id);
    }

    #endregion

    #region Long Sentence Handling Tests

    [Fact]
    public async Task ParseAsync_LongSentence_SkipsIfExceedsMax()
    {
        // Arrange
        var longSentence = new string('a', 600) + ".";
        var options = new ParseOptions { MaxSentenceLength = 500 };

        // Act
        var result = await _parser.ParseAsync(longSentence, options);

        // Assert
        result.Sentences.Should().BeEmpty();
        result.Stats.SkippedSentences.Should().Be(1);
    }

    [Fact]
    public async Task ParseAsync_SentenceAtMaxLength_IsProcessed()
    {
        // Arrange
        var sentence = new string('a', 50) + " word.";
        var options = new ParseOptions { MaxSentenceLength = 100 };

        // Act
        var result = await _parser.ParseAsync(sentence, options);

        // Assert
        result.Sentences.Should().NotBeEmpty();
    }

    #endregion

    #region Token Tests

    [Fact]
    public async Task ParseAsync_Tokens_HaveCorrectPOSTags()
    {
        // Arrange
        var text = "The fast endpoint accepts parameters.";
        var options = new ParseOptions { IncludePOS = true };

        // Act
        var result = await _parser.ParseAsync(text, options);
        var tokens = result.Sentences[0].Tokens;

        // Assert
        tokens.Should().Contain(t => t.Text == "The" && t.POS == "DET");
        tokens.Should().Contain(t => t.Text == "endpoint" && t.POS == "NOUN");
        tokens.Should().Contain(t => t.Text == "accepts" && t.POS == "VERB");
        tokens.Should().Contain(t =>
            (t.Text == "fast" && t.POS == "ADJ") ||
            (t.Text == "fast" && t.POS == "NOUN")); // May vary
    }

    [Fact]
    public async Task ParseAsync_Tokens_HaveLemmas()
    {
        // Arrange
        var text = "The endpoints accepting parameters.";
        var options = new ParseOptions { IncludePOS = true };

        // Act
        var result = await _parser.ParseAsync(text, options);
        var tokens = result.Sentences[0].Tokens;

        // Assert
        tokens.Should().Contain(t => t.Text == "accepting" && t.Lemma == "accept");
    }

    [Fact]
    public async Task ParseAsync_Tokens_HaveOffsets()
    {
        // Arrange
        var text = "Hello world.";

        // Act
        var result = await _parser.ParseAsync(text);
        var tokens = result.Sentences[0].Tokens;

        // Assert
        var hello = tokens.First(t => t.Text == "Hello");
        hello.StartChar.Should().Be(0);
        hello.EndChar.Should().Be(5);

        var world = tokens.First(t => t.Text == "world");
        world.StartChar.Should().Be(6);
        world.EndChar.Should().Be(11);
    }

    [Fact]
    public async Task ParseAsync_Tokens_IdentifyPunctuation()
    {
        // Arrange
        var text = "Hello, world!";

        // Act
        var result = await _parser.ParseAsync(text);
        var tokens = result.Sentences[0].Tokens;

        // Assert
        tokens.Should().Contain(t => t.Text == "," && t.IsPunct);
        tokens.Should().Contain(t => t.Text == "!" && t.IsPunct);
    }

    #endregion

    #region Dependency Tests

    [Fact]
    public async Task ParseAsync_Dependencies_ContainNSubj()
    {
        // Arrange
        var text = "The endpoint accepts parameters.";
        var options = new ParseOptions { IncludeDependencies = true };

        // Act
        var result = await _parser.ParseAsync(text, options);
        var deps = result.Sentences[0].Dependencies;

        // Assert
        deps.Should().Contain(d =>
            d.Relation == DependencyRelations.NSUBJ &&
            d.Dependent.Text == "endpoint");
    }

    [Fact]
    public async Task ParseAsync_Dependencies_ContainDObj()
    {
        // Arrange
        var text = "The endpoint accepts parameters.";
        var options = new ParseOptions { IncludeDependencies = true };

        // Act
        var result = await _parser.ParseAsync(text, options);
        var deps = result.Sentences[0].Dependencies;

        // Assert
        deps.Should().Contain(d =>
            d.Relation == DependencyRelations.DOBJ &&
            d.Dependent.Text == "parameters");
    }

    [Fact]
    public async Task DependencyNode_GetSubtreeText_ReturnsOrderedText()
    {
        // Arrange
        var text = "The fast endpoint accepts.";
        var options = new ParseOptions { IncludeDependencies = true };

        // Act
        var result = await _parser.ParseAsync(text, options);
        var root = result.Sentences[0].DependencyRoot;

        // Assert
        root.Should().NotBeNull();
        var subtreeText = root!.GetSubtreeText();
        subtreeText.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Semantic Frame Tests

    [Fact]
    public async Task ParseAsync_SemanticFrames_HavePredicate()
    {
        // Arrange
        var text = "The endpoint accepts parameters.";
        var options = new ParseOptions { IncludeSRL = true };

        // Act
        var result = await _parser.ParseAsync(text, options);
        var frames = result.Sentences[0].SemanticFrames;

        // Assert
        frames.Should().NotBeNullOrEmpty();
        frames![0].Predicate.Should().NotBeNull();
        frames[0].Predicate.Text.Should().Be("accepts");
    }

    [Fact]
    public async Task SemanticFrame_Agent_ReturnsARG0()
    {
        // Arrange
        var text = "The endpoint accepts parameters.";
        var options = new ParseOptions { IncludeSRL = true };

        // Act
        var result = await _parser.ParseAsync(text, options);
        var frame = result.Sentences[0].SemanticFrames?.FirstOrDefault();

        // Assert
        frame.Should().NotBeNull();
        frame!.Agent.Should().NotBeNull();
        frame.Agent!.Text.Should().Contain("endpoint");
    }

    [Fact]
    public async Task SemanticFrame_Patient_ReturnsARG1()
    {
        // Arrange
        var text = "The endpoint accepts parameters.";
        var options = new ParseOptions { IncludeSRL = true };

        // Act
        var result = await _parser.ParseAsync(text, options);
        var frame = result.Sentences[0].SemanticFrames?.FirstOrDefault();

        // Assert
        frame.Should().NotBeNull();
        frame!.Patient.Should().NotBeNull();
        frame.Patient!.Text.Should().Contain("parameters");
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public async Task ParseAsync_Stats_HasCorrectCounts()
    {
        // Arrange
        var text = "First sentence. Second sentence.";

        // Act
        var result = await _parser.ParseAsync(text);

        // Assert
        result.Stats.TotalSentences.Should().Be(2);
        result.Stats.TotalTokens.Should().BeGreaterThan(0);
        result.Stats.AverageTokensPerSentence.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ParseAsync_Duration_IsRecorded()
    {
        // Arrange
        var text = "A test sentence.";

        // Act
        var result = await _parser.ParseAsync(text);

        // Assert
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    #endregion

    #region Parser Metadata Tests

    [Fact]
    public void SupportedLanguages_ContainsEnglish()
    {
        // Assert
        _parser.SupportedLanguages.Should().Contain("en");
    }

    [Fact]
    public void SupportedLanguages_ContainsExpectedLanguages()
    {
        // Assert
        _parser.SupportedLanguages.Should().Contain("en");
        _parser.SupportedLanguages.Should().Contain("de");
        _parser.SupportedLanguages.Should().Contain("fr");
        _parser.SupportedLanguages.Should().Contain("es");
    }

    #endregion

    #region ParseSentenceAsync Tests

    [Fact]
    public async Task ParseSentenceAsync_ReturnsParsedSentence()
    {
        // Arrange
        var sentence = "The endpoint accepts parameters.";

        // Act
        var result = await _parser.ParseSentenceAsync(sentence);

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().Be(sentence);
        result.Tokens.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ParseSentenceAsync_EmptySentence_ReturnsEmptyTokens()
    {
        // Arrange
        var sentence = "";

        // Act
        var result = await _parser.ParseSentenceAsync(sentence);

        // Assert
        result.Tokens.Should().BeEmpty();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ParseAsync_SentenceWithAbbreviation_HandlesCorrectly()
    {
        // Arrange
        var text = "Dr. Smith works at Corp. Ltd.";

        // Act
        var result = await _parser.ParseAsync(text);

        // Assert - should handle abbreviations reasonably
        result.Sentences.Should().HaveCountGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task ParseAsync_QuestionSentence_Segmented()
    {
        // Arrange
        var text = "What is an endpoint? It accepts parameters.";

        // Act
        var result = await _parser.ParseAsync(text);

        // Assert
        result.Sentences.Should().HaveCount(2);
    }

    [Fact]
    public async Task ParseAsync_ExclamationSentence_Segmented()
    {
        // Arrange
        var text = "Hello world! This is a test.";

        // Act
        var result = await _parser.ParseAsync(text);

        // Assert
        result.Sentences.Should().HaveCount(2);
    }

    #endregion
}
