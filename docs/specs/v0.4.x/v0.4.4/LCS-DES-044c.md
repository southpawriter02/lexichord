# LCS-DES-044c: Token Counting

## Document Control

| Field            | Value                                    |
| :--------------- | :--------------------------------------- |
| **Document ID**  | LCS-DES-044c                             |
| **Version**      | v0.4.4c                                  |
| **Title**        | Token Counting                           |
| **Status**       | Draft                                    |
| **Last Updated** | 2026-01-27                               |
| **Owner**        | Lead Architect                           |
| **Module**       | `Lexichord.Modules.RAG`                  |
| **License Tier** | Core                                     |

---

## 1. Overview

### 1.1 Purpose

This specification defines `ITokenCounter` and `TiktokenTokenCounter`, which validate text size and truncate oversized content before sending to the embedding API. Accurate token counting prevents API errors and ensures chunks fit within model limits.

### 1.2 Goals

- Define `ITokenCounter` interface for token operations
- Implement token counting using `Microsoft.ML.Tokenizers`
- Provide text truncation that preserves complete tokens
- Log warnings when truncation occurs
- Support the `cl100k_base` encoding used by OpenAI models

### 1.3 Non-Goals

- Supporting all tokenizer encodings
- Token-based billing/cost tracking (future)
- Streaming tokenization

---

## 2. Token Basics

### 2.1 What Are Tokens?

Tokens are the basic units that language models process. They can be:
- Whole words: "hello" → 1 token
- Word pieces: "embedding" → 2 tokens ("embed" + "ding")
- Punctuation: "!" → 1 token
- Whitespace: " " → often merged with adjacent tokens

### 2.2 OpenAI Tokenization

OpenAI's embedding models use the `cl100k_base` encoding:

| Text | Tokens | Count |
| :--- | :----- | :---- |
| "Hello" | ["Hello"] | 1 |
| "Hello world" | ["Hello", " world"] | 2 |
| "internationalization" | ["intern", "ational", "ization"] | 3 |
| "🎉" | ["\ud83c\udf89"] | 1 |

### 2.3 Model Limits

| Model | Max Tokens | Encoding |
| :---- | :--------- | :------- |
| text-embedding-3-small | 8191 | cl100k_base |
| text-embedding-3-large | 8191 | cl100k_base |
| text-embedding-ada-002 | 8191 | cl100k_base |
| gpt-4 | 128k (context) | cl100k_base |

---

## 3. Design

### 3.1 ITokenCounter Interface

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for counting and managing tokens in text.
/// </summary>
public interface ITokenCounter
{
    /// <summary>
    /// Gets the tokenizer model/encoding name.
    /// </summary>
    string Model { get; }

    /// <summary>
    /// Counts the number of tokens in the text.
    /// </summary>
    /// <param name="text">Text to count. Empty string returns 0.</param>
    /// <returns>Token count.</returns>
    int CountTokens(string text);

    /// <summary>
    /// Truncates text to fit within the specified token limit.
    /// </summary>
    /// <param name="text">Text to potentially truncate.</param>
    /// <param name="maxTokens">Maximum allowed tokens.</param>
    /// <returns>
    /// Tuple of (truncated text, whether truncation occurred).
    /// If text fits, returns original text unchanged.
    /// </returns>
    (string Text, bool WasTruncated) TruncateToTokenLimit(string text, int maxTokens);

    /// <summary>
    /// Encodes text to token IDs.
    /// </summary>
    /// <param name="text">Text to encode.</param>
    /// <returns>Token IDs.</returns>
    IReadOnlyList<int> Encode(string text);

    /// <summary>
    /// Decodes token IDs back to text.
    /// </summary>
    /// <param name="tokens">Token IDs to decode.</param>
    /// <returns>Decoded text.</returns>
    string Decode(IReadOnlyList<int> tokens);
}
```

### 3.2 TiktokenTokenCounter Implementation

```csharp
namespace Lexichord.Modules.RAG.Embedding;

using Microsoft.ML.Tokenizers;

/// <summary>
/// Token counter using tiktoken tokenizer (cl100k_base encoding).
/// Compatible with OpenAI GPT-4 and embedding models.
/// </summary>
public sealed class TiktokenTokenCounter : ITokenCounter
{
    private readonly Tokenizer _tokenizer;
    private readonly ILogger<TiktokenTokenCounter> _logger;

    public string Model { get; }

    /// <summary>
    /// Creates a token counter for the specified model.
    /// </summary>
    /// <param name="model">
    /// Model name for tokenizer selection.
    /// Default: "cl100k_base" (used by GPT-4 and embeddings).
    /// </param>
    /// <param name="logger">Logger instance.</param>
    public TiktokenTokenCounter(
        string model = "cl100k_base",
        ILogger<TiktokenTokenCounter>? logger = null)
    {
        Model = model;
        _logger = logger ?? NullLogger<TiktokenTokenCounter>.Instance;

        // Create tokenizer for the model encoding
        _tokenizer = TiktokenTokenizer.CreateForModel("gpt-4"); // Uses cl100k_base
    }

    public int CountTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        var encoded = _tokenizer.EncodeToIds(text);
        return encoded.Count;
    }

    public (string Text, bool WasTruncated) TruncateToTokenLimit(string text, int maxTokens)
    {
        if (string.IsNullOrEmpty(text))
            return (string.Empty, false);

        if (maxTokens <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxTokens), "Must be positive");

        var tokens = _tokenizer.EncodeToIds(text);

        if (tokens.Count <= maxTokens)
            return (text, false);

        // Take first maxTokens tokens
        var truncatedTokens = tokens.Take(maxTokens).ToArray();
        var truncatedText = _tokenizer.Decode(truncatedTokens);

        _logger.LogWarning(
            "Text truncated from {OriginalTokens} to {MaxTokens} tokens ({OriginalChars} → {TruncatedChars} chars)",
            tokens.Count, maxTokens, text.Length, truncatedText?.Length ?? 0);

        return (truncatedText ?? string.Empty, true);
    }

    public IReadOnlyList<int> Encode(string text)
    {
        if (string.IsNullOrEmpty(text))
            return Array.Empty<int>();

        return _tokenizer.EncodeToIds(text).ToArray();
    }

    public string Decode(IReadOnlyList<int> tokens)
    {
        if (tokens == null || tokens.Count == 0)
            return string.Empty;

        return _tokenizer.Decode(tokens.ToArray()) ?? string.Empty;
    }
}
```

### 3.3 DI Registration

```csharp
// In RAGModule.cs
services.AddSingleton<TiktokenTokenCounter>();
services.AddSingleton<ITokenCounter>(sp =>
    sp.GetRequiredService<TiktokenTokenCounter>());
```

---

## 4. Usage Examples

### 4.1 Basic Counting

```csharp
var counter = new TiktokenTokenCounter();

var count1 = counter.CountTokens("Hello world");
// count1 = 2

var count2 = counter.CountTokens("The quick brown fox jumps over the lazy dog.");
// count2 ≈ 10
```

### 4.2 Truncation Before Embedding

```csharp
var counter = serviceProvider.GetRequiredService<ITokenCounter>();
var embedder = serviceProvider.GetRequiredService<IEmbeddingService>();

var chunks = chunker.Split(document, options);

foreach (var chunk in chunks)
{
    var (text, wasTruncated) = counter.TruncateToTokenLimit(
        chunk.Content,
        embedder.MaxTokens);

    if (wasTruncated)
    {
        logger.LogWarning("Chunk {Index} was truncated", chunk.Metadata.Index);
    }

    var embedding = await embedder.EmbedAsync(text);
}
```

### 4.3 Batch Validation

```csharp
public async Task<IReadOnlyList<float[]>> EmbedChunksAsync(
    IReadOnlyList<TextChunk> chunks,
    CancellationToken ct)
{
    var validatedTexts = new List<string>();
    var truncationCount = 0;

    foreach (var chunk in chunks)
    {
        var (text, truncated) = _tokenCounter.TruncateToTokenLimit(
            chunk.Content,
            _options.MaxTokens);

        validatedTexts.Add(text);
        if (truncated) truncationCount++;
    }

    if (truncationCount > 0)
    {
        _logger.LogWarning(
            "{Count} of {Total} chunks were truncated",
            truncationCount, chunks.Count);
    }

    return await _embedder.EmbedBatchAsync(validatedTexts, ct);
}
```

---

## 5. Token Estimation Rules

For quick estimates without full tokenization:

| Content Type | Estimate |
| :----------- | :------- |
| English prose | ~0.75 tokens per word |
| Code | ~1.5 tokens per word |
| Numbers | ~1 token per 3-4 digits |
| Special chars | ~1 token each |
| Whitespace | Often merged, ~0.5 tokens |

**Rule of thumb:** `tokens ≈ characters / 4` for English text.

---

## 6. Unit Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.4c")]
public class TiktokenTokenCounterTests
{
    private readonly TiktokenTokenCounter _sut = new();

    [Fact]
    public void CountTokens_EmptyString_ReturnsZero()
    {
        _sut.CountTokens("").Should().Be(0);
        _sut.CountTokens(null!).Should().Be(0);
    }

    [Fact]
    public void CountTokens_SimpleText_ReturnsCorrectCount()
    {
        // "Hello world" is typically 2 tokens
        var count = _sut.CountTokens("Hello world");
        count.Should().BeInRange(2, 3);
    }

    [Fact]
    public void CountTokens_LongText_CountsAccurately()
    {
        var text = string.Join(" ", Enumerable.Repeat("word", 1000));
        var count = _sut.CountTokens(text);

        // ~1000 words ≈ 1000-1500 tokens
        count.Should().BeInRange(900, 1500);
    }

    [Theory]
    [InlineData("Hello", 1)]
    [InlineData("Hello world", 2)]
    [InlineData("   ", 1)] // Whitespace
    public void CountTokens_KnownValues_MatchExpected(string text, int expected)
    {
        var count = _sut.CountTokens(text);
        count.Should().BeCloseTo(expected, 1); // Allow ±1 variance
    }

    [Fact]
    public void TruncateToTokenLimit_ShortText_ReturnsUnchanged()
    {
        var (text, truncated) = _sut.TruncateToTokenLimit("Hello", 100);

        text.Should().Be("Hello");
        truncated.Should().BeFalse();
    }

    [Fact]
    public void TruncateToTokenLimit_LongText_TruncatesCorrectly()
    {
        var longText = string.Join(" ", Enumerable.Repeat("word", 10000));
        var maxTokens = 100;

        var (text, truncated) = _sut.TruncateToTokenLimit(longText, maxTokens);

        var resultTokens = _sut.CountTokens(text);
        resultTokens.Should().BeLessOrEqualTo(maxTokens);
        truncated.Should().BeTrue();
    }

    [Fact]
    public void TruncateToTokenLimit_ExactLimit_ReturnsUnchanged()
    {
        // Build text that's exactly at the limit
        var text = "Hello world";
        var tokenCount = _sut.CountTokens(text);

        var (result, truncated) = _sut.TruncateToTokenLimit(text, tokenCount);

        result.Should().Be(text);
        truncated.Should().BeFalse();
    }

    [Fact]
    public void TruncateToTokenLimit_InvalidMaxTokens_Throws()
    {
        _sut.Invoking(s => s.TruncateToTokenLimit("test", 0))
            .Should().Throw<ArgumentOutOfRangeException>();

        _sut.Invoking(s => s.TruncateToTokenLimit("test", -1))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Encode_ReturnsTokenIds()
    {
        var tokens = _sut.Encode("Hello world");

        tokens.Should().NotBeEmpty();
        tokens.Should().HaveCount(_sut.CountTokens("Hello world"));
    }

    [Fact]
    public void Decode_ReturnsOriginalText()
    {
        var original = "Hello world";
        var tokens = _sut.Encode(original);
        var decoded = _sut.Decode(tokens);

        decoded.Should().Be(original);
    }

    [Fact]
    public void Encode_Decode_RoundTrip()
    {
        var texts = new[]
        {
            "Simple text",
            "Numbers: 12345",
            "Special: @#$%^&*()",
            "Unicode: 你好世界",
            "Emoji: 🎉🚀✨"
        };

        foreach (var text in texts)
        {
            var tokens = _sut.Encode(text);
            var decoded = _sut.Decode(tokens);
            decoded.Should().Be(text, because: $"'{text}' should round-trip");
        }
    }

    [Fact]
    public void CountTokens_Unicode_HandlesCorrectly()
    {
        var chineseText = "你好世界";
        var count = _sut.CountTokens(chineseText);

        // Each Chinese character is typically 1-2 tokens
        count.Should().BeInRange(2, 8);
    }

    [Fact]
    public void CountTokens_Emoji_HandlesCorrectly()
    {
        var emojiText = "🎉🚀✨";
        var count = _sut.CountTokens(emojiText);

        // Each emoji is typically 1-2 tokens
        count.Should().BeInRange(1, 6);
    }
}
```

---

## 7. Performance

### 7.1 Benchmarks

| Operation | Input Size | Time |
| :-------- | :--------- | :--- |
| CountTokens | 100 chars | < 1ms |
| CountTokens | 1,000 chars | < 5ms |
| CountTokens | 10,000 chars | < 50ms |
| TruncateToTokenLimit | 100,000 chars → 1000 tokens | < 100ms |

### 7.2 Memory Considerations

- Tokenizer is thread-safe and can be reused
- Token arrays are allocated per call
- For very large texts, consider streaming approaches (future)

---

## 8. Logging

| Level | Message | Context |
| :---- | :------ | :------ |
| Warning | "Text truncated from {OriginalTokens} to {MaxTokens} tokens" | On truncation |
| Debug | "Counted {TokenCount} tokens in {CharCount} chars" | After counting (verbose) |
| Trace | "Encoding text of {Length} characters" | Before encode (verbose) |

---

## 9. File Locations

| File | Path |
| :--- | :--- |
| Interface | `src/Lexichord.Abstractions/Contracts/ITokenCounter.cs` |
| Implementation | `src/Lexichord.Modules.RAG/Embedding/TiktokenTokenCounter.cs` |
| Unit tests | `tests/Lexichord.Modules.RAG.Tests/Embedding/TiktokenTokenCounterTests.cs` |

---

## 10. Dependencies

| Dependency | Version | Purpose |
| :--------- | :------ | :------ |
| `Microsoft.ML.Tokenizers` | 0.22.x | Tiktoken tokenizer implementation |

---

## 11. Acceptance Criteria

| # | Criterion | Status |
| :- | :-------- | :----- |
| 1 | CountTokens returns accurate count for English text | [ ] |
| 2 | CountTokens returns 0 for empty/null text | [ ] |
| 3 | TruncateToTokenLimit returns unchanged text if under limit | [ ] |
| 4 | TruncateToTokenLimit truncates to exactly maxTokens | [ ] |
| 5 | TruncateToTokenLimit logs warning on truncation | [ ] |
| 6 | Encode and Decode are inverses (round-trip) | [ ] |
| 7 | Unicode and emoji handled correctly | [ ] |
| 8 | CountTokens < 50ms for 10,000 chars | [ ] |
| 9 | All unit tests pass | [ ] |

---

## 12. Revision History

| Version | Date       | Author         | Changes                    |
| :------ | :--------- | :------------- | :------------------------- |
| 0.1     | 2026-01-27 | Lead Architect | Initial draft              |

---
