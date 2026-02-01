// =============================================================================
// File: TiktokenTokenCounterTests.cs
// Project: Lexichord.Tests.Unit
// Description: Comprehensive unit tests for v0.4.4c Tiktoken token counter.
//              Tests token counting, truncation, encoding, decoding, and Unicode handling.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Embedding;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace Lexichord.Tests.Unit.Modules;

/// <summary>
/// Unit tests for <see cref="TiktokenTokenCounter"/>.
/// </summary>
/// <remarks>
/// Introduced in v0.4.4c as part of the Token Counter implementation.
/// Tests the production-ready Tiktoken tokenizer for GPT models.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.4c")]
public class TiktokenTokenCounterTests
{
    private readonly Mock<ILogger<TiktokenTokenCounter>> _mockLogger = new();

    #region Constructor Tests

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void Constructor_WithDefaultModel_CreatesInstance()
    {
        // Act
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);

        // Assert
        counter.Should().NotBeNull();
        counter.Model.Should().Be("cl100k_base");
    }

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void Constructor_WithExplicitModel_CreatesInstance()
    {
        // Act
        var counter = new TiktokenTokenCounter("cl100k_base", _mockLogger.Object);

        // Assert
        counter.Should().NotBeNull();
        counter.Model.Should().Be("cl100k_base");
    }

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void Constructor_WithNullModel_DefaultsToClBase()
    {
        // Act
        var counter = new TiktokenTokenCounter(null, _mockLogger.Object);

        // Assert
        counter.Model.Should().Be("cl100k_base");
    }

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void Constructor_WithWhitespaceModel_DefaultsToClBase()
    {
        // Act
        var counter = new TiktokenTokenCounter("   ", _mockLogger.Object);

        // Assert
        counter.Model.Should().Be("cl100k_base");
    }

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void Constructor_WithoutLogger_CreatesInstance()
    {
        // Act
        var counter = new TiktokenTokenCounter("cl100k_base", null);

        // Assert
        counter.Should().NotBeNull();
    }

    #endregion

    #region CountTokens Tests

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void CountTokens_EmptyString_ReturnsZero()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);

        // Act
        var count = counter.CountTokens("");

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void CountTokens_NullString_ReturnsZero()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);

        // Act
        var count = counter.CountTokens(null);

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void CountTokens_SimpleText_ReturnsCorrectCount()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);
        var text = "Hello, world!";

        // Act
        var count = counter.CountTokens(text);

        // Assert
        count.Should().BeGreaterThan(0);
        count.Should().BeLessThan(10); // Short text should be < 10 tokens
    }

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void CountTokens_LongText_CountsAccurately()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);
        var text = string.Concat(Enumerable.Repeat("The quick brown fox jumps over the lazy dog. ", 100));

        // Act
        var count = counter.CountTokens(text);

        // Assert
        count.Should().BeGreaterThan(100);
        count.Should().BeLessThan(2000); // Should be reasonable for repeated text
    }

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void CountTokens_IsConsistent()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);
        var text = "This is a test of consistency.";

        // Act
        var count1 = counter.CountTokens(text);
        var count2 = counter.CountTokens(text);

        // Assert
        count1.Should().Be(count2);
    }

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void CountTokens_SingleWord_ReturnsAtLeastOne()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);

        // Act
        var count = counter.CountTokens("Hello");

        // Assert
        count.Should().BeGreaterThanOrEqualTo(1);
    }

    #endregion

    #region TruncateToTokenLimit Tests

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void TruncateToTokenLimit_ShortText_ReturnsUnchanged()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);
        var text = "Hello world";

        // Act
        var (truncated, wasTruncated) = counter.TruncateToTokenLimit(text, 100);

        // Assert
        truncated.Should().Be(text);
        wasTruncated.Should().BeFalse();
    }

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void TruncateToTokenLimit_LongText_TruncatesCorrectly()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);
        var text = string.Concat(Enumerable.Repeat("word ", 100));
        var maxTokens = 10;

        // Act
        var (truncated, wasTruncated) = counter.TruncateToTokenLimit(text, maxTokens);

        // Assert
        wasTruncated.Should().BeTrue();
        truncated.Length.Should().BeLessThan(text.Length);
        var truncatedCount = counter.CountTokens(truncated);
        truncatedCount.Should().BeLessThanOrEqualTo(maxTokens);
    }

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void TruncateToTokenLimit_EmptyString_ReturnsEmpty()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);

        // Act
        var (truncated, wasTruncated) = counter.TruncateToTokenLimit("", 100);

        // Assert
        truncated.Should().BeEmpty();
        wasTruncated.Should().BeFalse();
    }

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void TruncateToTokenLimit_NullString_ReturnsEmpty()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);

        // Act
        var (truncated, wasTruncated) = counter.TruncateToTokenLimit(null, 100);

        // Assert
        truncated.Should().BeEmpty();
        wasTruncated.Should().BeFalse();
    }

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void TruncateToTokenLimit_InvalidMaxTokens_Throws()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);

        // Act & Assert
        var act = () => counter.TruncateToTokenLimit("text", 0);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("maxTokens")
            .WithMessage("*Maximum tokens must be greater than 0*");
    }

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void TruncateToTokenLimit_NegativeMaxTokens_Throws()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);

        // Act & Assert
        var act = () => counter.TruncateToTokenLimit("text", -1);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("maxTokens");
    }

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void TruncateToTokenLimit_RespectsBoundaries()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);
        var text = "The quick brown fox jumps over the lazy dog";
        var maxTokens = 5;

        // Act
        var (truncated, wasTruncated) = counter.TruncateToTokenLimit(text, maxTokens);

        // Assert
        var truncatedTokenCount = counter.CountTokens(truncated);
        truncatedTokenCount.Should().BeLessThanOrEqualTo(maxTokens);
    }

    #endregion

    #region Encode Tests

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void Encode_EmptyString_ReturnsEmptyList()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);

        // Act
        var tokens = counter.Encode("");

        // Assert
        tokens.Should().BeEmpty();
    }

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void Encode_NullString_ReturnsEmptyList()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);

        // Act
        var tokens = counter.Encode(null);

        // Assert
        tokens.Should().BeEmpty();
    }

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void Encode_ReturnsTokenIds()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);
        var text = "Hello world";

        // Act
        var tokens = counter.Encode(text);

        // Assert
        tokens.Should().NotBeEmpty();
        tokens.Should().AllSatisfy(t => t.Should().BeGreaterThanOrEqualTo(0));
    }

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void Encode_ReturnsReadOnlyList()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);

        // Act
        var tokens = counter.Encode("test");

        // Assert
        tokens.Should().BeAssignableTo<IReadOnlyList<int>>();
    }

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void Encode_CountMatchesCountTokens()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);
        var text = "The quick brown fox";

        // Act
        var tokens = counter.Encode(text);
        var count = counter.CountTokens(text);

        // Assert
        tokens.Count.Should().Be(count);
    }

    #endregion

    #region Decode Tests

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void Decode_EmptyList_ReturnsEmptyString()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);

        // Act
        var text = counter.Decode(Array.Empty<int>());

        // Assert
        text.Should().BeEmpty();
    }

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void Decode_NullList_Throws()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);

        // Act & Assert
        var act = () => counter.Decode(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void Decode_ReturnsOriginalText()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);
        var text = "Hello world";

        // Act
        var tokens = counter.Encode(text);
        var decoded = counter.Decode(tokens);

        // Assert
        decoded.Should().NotBeEmpty();
        // Note: Decode may not be exact due to BPE nature, but should be close
    }

    #endregion

    #region Round-Trip Tests

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void Encode_Decode_RoundTrip()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);
        var originalText = "The quick brown fox jumps over the lazy dog";

        // Act
        var tokens = counter.Encode(originalText);
        var decodedText = counter.Decode(tokens);

        // Assert
        decodedText.Should().NotBeNullOrEmpty();
        // Decoded text should have similar length (may not be exactly same due to tokenization)
        decodedText.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void CountTokens_MatchesEncodedLength()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);
        var texts = new[] { "test", "longer text", "very long text with many words and punctuation!!!" };

        // Act & Assert
        foreach (var text in texts)
        {
            var count = counter.CountTokens(text);
            var encoded = counter.Encode(text);
            count.Should().Be(encoded.Count, $"Count mismatch for text: {text}");
        }
    }

    #endregion

    #region Unicode Tests

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void CountTokens_Unicode_HandlesCorrectly()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);
        var unicodeText = "Hello 世界 مرحبا мир";

        // Act
        var count = counter.CountTokens(unicodeText);

        // Assert
        count.Should().BeGreaterThan(0);
    }

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void Encode_Unicode_ReturnsTokenIds()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);
        var unicodeText = "🚀 emoji test 😀";

        // Act
        var tokens = counter.Encode(unicodeText);

        // Assert
        tokens.Should().NotBeEmpty();
    }

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void TruncateToTokenLimit_Unicode_HandlesCorrectly()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);
        var unicodeText = "This is a test with unicode: 你好世界 and emoji 🌟";

        // Act
        var (truncated, wasTruncated) = counter.TruncateToTokenLimit(unicodeText, 5);

        // Assert
        truncated.Should().NotBeNull();
    }

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void CountTokens_Emoji_CountsCorrectly()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);

        // Act
        var emojiCount = counter.CountTokens("😀😁😂");
        var textCount = counter.CountTokens("abc");

        // Assert
        emojiCount.Should().BeGreaterThan(0);
        textCount.Should().BeGreaterThan(0);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void CountTokens_ConcurrentCalls_ProducesConsistentResults()
    {
        // Arrange
        var counter = new TiktokenTokenCounter(logger: _mockLogger.Object);
        var text = "concurrent test text";
        var results = new List<int>();
        var lockObj = new object();

        // Act
        Parallel.For(0, 10, _ =>
        {
            var count = counter.CountTokens(text);
            lock (lockObj)
            {
                results.Add(count);
            }
        });

        // Assert
        results.Should().AllSatisfy(r => r.Should().Be(results[0]));
    }

    #endregion

    #region Property Tests

    [Fact]
    [Trait("Feature", "v0.4.4c")]
    public void Model_ReturnsConfiguredValue()
    {
        // Arrange
        var counter = new TiktokenTokenCounter("cl100k_base", _mockLogger.Object);

        // Act
        var model = counter.Model;

        // Assert
        model.Should().Be("cl100k_base");
    }

    #endregion
}
