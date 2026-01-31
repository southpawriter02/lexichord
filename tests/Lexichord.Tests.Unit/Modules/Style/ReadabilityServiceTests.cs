using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Style.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for <see cref="ReadabilityService"/>.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.3c - Verifies formula accuracy, edge case handling,
/// and async behavior with cancellation support.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.3.3c")]
public class ReadabilityServiceTests
{
    private readonly Mock<ILogger<ReadabilityService>> _loggerMock = new();
    
    /// <summary>
    /// Creates a ReadabilityService with real implementations of dependencies.
    /// </summary>
    private ReadabilityService CreateService()
    {
        var sentenceLogger = new Mock<ILogger<SentenceTokenizer>>();
        
        var sentenceTokenizer = new SentenceTokenizer(sentenceLogger.Object);
        var syllableCounter = new SyllableCounter();
        
        return new ReadabilityService(sentenceTokenizer, syllableCounter, _loggerMock.Object);
    }

    #region Empty/Null Input Tests

    [Fact]
    public void Analyze_WithNullInput_ReturnsEmptyMetrics()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.Analyze(null!);

        // Assert
        result.Should().Be(ReadabilityMetrics.Empty);
    }

    [Fact]
    public void Analyze_WithEmptyString_ReturnsEmptyMetrics()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.Analyze(string.Empty);

        // Assert
        result.Should().Be(ReadabilityMetrics.Empty);
    }

    [Fact]
    public void Analyze_WithWhitespaceOnly_ReturnsEmptyMetrics()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.Analyze("   \t\n   ");

        // Assert
        result.Should().Be(ReadabilityMetrics.Empty);
    }

    #endregion

    #region Word and Sentence Count Tests

    [Fact]
    public void Analyze_SimpleSentence_CountsCorrectly()
    {
        // Arrange
        var service = CreateService();
        var text = "The cat sat on the mat.";

        // Act
        var result = service.Analyze(text);

        // Assert
        result.WordCount.Should().Be(6);
        result.SentenceCount.Should().Be(1);
    }

    [Fact]
    public void Analyze_MultipleSentences_CountsCorrectly()
    {
        // Arrange
        var service = CreateService();
        var text = "The cat sat on the mat. The dog ran in the park. The bird flew over the tree.";

        // Act
        var result = service.Analyze(text);

        // Assert
        // LOGIC: 18 words: The cat sat on the mat (6) + The dog ran in the park (6) + The bird flew over the tree (6)
        result.WordCount.Should().Be(18);
        result.SentenceCount.Should().Be(3);
        result.AverageWordsPerSentence.Should().BeApproximately(6.0, 0.01);
    }

    #endregion

    #region Formula Accuracy Tests

    [Fact]
    public void Analyze_SimpleText_FleschKincaidIsLow()
    {
        // Arrange
        var service = CreateService();
        // Simple text with short words and sentences
        var text = "I am the man. You are the cat. We are the dog.";

        // Act
        var result = service.Analyze(text);

        // Assert
        // LOGIC: Simple text should have a low grade level (< 5)
        result.FleschKincaidGradeLevel.Should().BeLessThan(5);
    }

    [Fact]
    public void Analyze_ComplexText_FleschKincaidIsHigh()
    {
        // Arrange
        var service = CreateService();
        // Complex text with multi-syllable words
        var text = "The extraordinary circumstances surrounding the unprecedented " +
                   "technological implementation necessitated comprehensive documentation " +
                   "and meticulous architectural considerations.";

        // Act
        var result = service.Analyze(text);

        // Assert
        // LOGIC: Complex text should have a high grade level (> 12)
        result.FleschKincaidGradeLevel.Should().BeGreaterThan(12);
    }

    [Fact]
    public void Analyze_SimpleText_FleschReadingEaseIsHigh()
    {
        // Arrange
        var service = CreateService();
        var text = "I am the man. You are the cat. We are the dog.";

        // Act
        var result = service.Analyze(text);

        // Assert
        // LOGIC: Simple text should have high reading ease (> 70 = Fairly Easy)
        result.FleschReadingEase.Should().BeGreaterThan(70);
        result.ReadingEaseInterpretation.Should().BeOneOf("Very Easy", "Easy", "Fairly Easy");
    }

    [Fact]
    public void Analyze_ComplexText_FleschReadingEaseIsLow()
    {
        // Arrange
        var service = CreateService();
        var text = "The extraordinary circumstances surrounding the unprecedented " +
                   "technological implementation necessitated comprehensive documentation " +
                   "and meticulous architectural considerations.";

        // Act
        var result = service.Analyze(text);

        // Assert
        // LOGIC: Complex text should have low reading ease (< 50 = Fairly Difficult)
        result.FleschReadingEase.Should().BeLessThan(50);
    }

    [Fact]
    public void Analyze_ComplexWords_IncludesGunningFog()
    {
        // Arrange
        var service = CreateService();
        var text = "The extraordinary circumstances surrounding the unprecedented " +
                   "technological implementation necessitated comprehensive documentation.";

        // Act
        var result = service.Analyze(text);

        // Assert
        // LOGIC: Gunning Fog should be positive and reasonably high for complex text
        result.GunningFogIndex.Should().BeGreaterThan(10);
        result.ComplexWordCount.Should().BeGreaterThan(0);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void Analyze_SingleWord_ReturnsValidMetrics()
    {
        // Arrange
        var service = CreateService();
        var text = "Hello";

        // Act
        var result = service.Analyze(text);

        // Assert
        result.WordCount.Should().Be(1);
        result.SentenceCount.Should().Be(1); // Implied sentence
        result.SyllableCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Analyze_SentenceWithAbbreviations_HandlesCorrectly()
    {
        // Arrange
        var service = CreateService();
        var text = "Dr. Smith and Mr. Jones went to the U.S.A. for a conference.";

        // Act
        var result = service.Analyze(text);

        // Assert
        // LOGIC: Abbreviations should not create false sentence breaks
        result.SentenceCount.Should().Be(1);
        result.WordCount.Should().BeGreaterThan(5);
    }

    [Fact]
    public void Analyze_MultiplePunctuation_HandlesCorrectly()
    {
        // Arrange
        var service = CreateService();
        var text = "What do you mean?! That is incredible! Amazing...";

        // Act
        var result = service.Analyze(text);

        // Assert
        result.SentenceCount.Should().BeGreaterThanOrEqualTo(2);
    }

    #endregion

    #region Async Tests

    [Fact]
    public async Task AnalyzeAsync_ReturnsValidMetrics()
    {
        // Arrange
        var service = CreateService();
        var text = "The cat sat on the mat.";

        // Act
        var result = await service.AnalyzeAsync(text);

        // Assert
        result.WordCount.Should().Be(6);
        result.SentenceCount.Should().Be(1);
    }

    [Fact]
    public async Task AnalyzeAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var service = CreateService();
        var text = "Some text to analyze.";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.AnalyzeAsync(text, cts.Token));
    }

    [Fact]
    public async Task AnalyzeAsync_WithEmptyInput_ReturnsEmptyMetrics()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.AnalyzeAsync(string.Empty);

        // Assert
        result.Should().Be(ReadabilityMetrics.Empty);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task Analyze_ConcurrentCalls_ProducesConsistentResults()
    {
        // Arrange
        var service = CreateService();
        var text = "The cat sat on the mat. The dog ran in the park.";
        const int concurrentCalls = 10;

        // Act
        var tasks = Enumerable.Range(0, concurrentCalls)
            .Select(_ => Task.Run(() => service.Analyze(text)))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        // All results should be identical
        var firstResult = results[0];
        foreach (var result in results)
        {
            result.Should().Be(firstResult);
        }
    }

    #endregion

    #region Formula Clamping Tests

    [Fact]
    public void Analyze_ExtremelySimpleText_FleschKincaidClampedToZero()
    {
        // Arrange
        var service = CreateService();
        // Very simple single-syllable words
        var text = "I go. He is. We do.";

        // Act
        var result = service.Analyze(text);

        // Assert
        // LOGIC: Even extremely simple text should not produce negative grade level
        result.FleschKincaidGradeLevel.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void Analyze_FleschReadingEase_ClampedTo0To100()
    {
        // Arrange
        var service = CreateService();
        var text = "I am. We go. It is. He can. She may.";

        // Act
        var result = service.Analyze(text);

        // Assert
        result.FleschReadingEase.Should().BeInRange(0, 100);
    }

    #endregion
}
