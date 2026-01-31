# LCS-DES-044a: Embedding Abstractions

## Document Control

| Field            | Value                                    |
| :--------------- | :--------------------------------------- |
| **Document ID**  | LCS-DES-044a                             |
| **Version**      | v0.4.4a                                  |
| **Title**        | Embedding Abstractions                   |
| **Status**       | Draft                                    |
| **Last Updated** | 2026-01-27                               |
| **Owner**        | Lead Architect                           |
| **Module**       | `Lexichord.Abstractions`                 |
| **License Tier** | Core (interface only)                    |

---

## 1. Overview

### 1.1 Purpose

This specification defines the core interfaces and types for the embedding system. These abstractions enable pluggable embedding providers (OpenAI, local models, etc.) and provide a consistent contract for vector generation.

### 1.2 Goals

- Define `IEmbeddingService` interface for embedding operations
- Create `EmbeddingOptions` for service configuration
- Design `EmbeddingResult` for operation outcomes
- Enable extensibility for alternative embedding providers
- Support both single and batch embedding operations

### 1.3 Non-Goals

- Implementing specific embedding providers (v0.4.4b)
- Token counting (v0.4.4c)
- Pipeline orchestration (v0.4.4d)

---

## 2. Design

### 2.1 IEmbeddingService Interface

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for generating text embeddings (vector representations).
/// Implementations connect to embedding APIs or local models.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Gets the identifier of the model used for embedding.
    /// Example: "text-embedding-3-small"
    /// </summary>
    string ModelName { get; }

    /// <summary>
    /// Gets the number of dimensions in the output vectors.
    /// Must match the pgvector column definition.
    /// </summary>
    int Dimensions { get; }

    /// <summary>
    /// Gets the maximum number of tokens the model accepts.
    /// Texts exceeding this limit must be truncated.
    /// </summary>
    int MaxTokens { get; }

    /// <summary>
    /// Embeds a single text string into a vector.
    /// </summary>
    /// <param name="text">
    /// Text to embed. Should not exceed MaxTokens.
    /// Empty or null text throws ArgumentException.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Float array of length Dimensions representing the semantic embedding.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when text is null, empty, or whitespace only.
    /// </exception>
    /// <exception cref="EmbeddingException">
    /// Thrown when the embedding API fails after retries.
    /// </exception>
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// Embeds multiple texts in a single batch request.
    /// More efficient than multiple single calls due to reduced API overhead.
    /// </summary>
    /// <param name="texts">
    /// Texts to embed. Maximum count is implementation-dependent
    /// (typically 100 for OpenAI). Order is preserved in results.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// List of embeddings in the same order as input texts.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when texts is null, empty, or exceeds maximum batch size.
    /// </exception>
    /// <exception cref="EmbeddingException">
    /// Thrown when the embedding API fails after retries.
    /// </exception>
    Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default);
}
```

### 2.2 EmbeddingOptions Record

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Configuration options for embedding services.
/// </summary>
public record EmbeddingOptions
{
    /// <summary>
    /// Default options for OpenAI text-embedding-3-small.
    /// </summary>
    public static EmbeddingOptions Default { get; } = new();

    /// <summary>
    /// Model identifier for the embedding service.
    /// Default: "text-embedding-3-small" (OpenAI).
    /// </summary>
    public string Model { get; init; } = "text-embedding-3-small";

    /// <summary>
    /// Maximum tokens allowed per text input.
    /// Texts exceeding this will be truncated.
    /// Default: 8191 (OpenAI embedding models).
    /// </summary>
    public int MaxTokens { get; init; } = 8191;

    /// <summary>
    /// Number of dimensions in output vectors.
    /// Must match pgvector column definition.
    /// Default: 1536 (OpenAI text-embedding-3-small).
    /// </summary>
    public int Dimensions { get; init; } = 1536;

    /// <summary>
    /// Whether to L2-normalize output vectors.
    /// Normalized vectors enable cosine similarity via dot product.
    /// Default: true.
    /// </summary>
    public bool Normalize { get; init; } = true;

    /// <summary>
    /// Maximum texts per batch request.
    /// Default: 100 (OpenAI limit).
    /// </summary>
    public int MaxBatchSize { get; init; } = 100;

    /// <summary>
    /// HTTP request timeout in seconds.
    /// Default: 60.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 60;

    /// <summary>
    /// Maximum retry attempts for transient failures.
    /// Default: 3.
    /// </summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// Base URL for the embedding API (if customizable).
    /// Default: null (use provider default).
    /// </summary>
    public string? ApiBaseUrl { get; init; }

    /// <summary>
    /// Secret key name in ISecureVault for API key retrieval.
    /// Default: "openai:api-key".
    /// </summary>
    public string SecretKeyName { get; init; } = "openai:api-key";

    /// <summary>
    /// Validates the options are internally consistent.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Model))
            throw new ArgumentException("Model is required", nameof(Model));

        if (MaxTokens <= 0)
            throw new ArgumentException("MaxTokens must be positive", nameof(MaxTokens));

        if (Dimensions <= 0)
            throw new ArgumentException("Dimensions must be positive", nameof(Dimensions));

        if (MaxBatchSize <= 0)
            throw new ArgumentException("MaxBatchSize must be positive", nameof(MaxBatchSize));

        if (TimeoutSeconds <= 0)
            throw new ArgumentException("TimeoutSeconds must be positive", nameof(TimeoutSeconds));

        if (MaxRetries < 0)
            throw new ArgumentException("MaxRetries cannot be negative", nameof(MaxRetries));
    }
}
```

### 2.3 EmbeddingResult Record

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Result of an embedding operation with diagnostics.
/// </summary>
public record EmbeddingResult
{
    /// <summary>
    /// Whether the embedding was generated successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The generated embedding vector.
    /// Null if Success is false.
    /// </summary>
    public float[]? Embedding { get; init; }

    /// <summary>
    /// Number of tokens in the input text.
    /// Useful for monitoring and cost tracking.
    /// </summary>
    public int TokenCount { get; init; }

    /// <summary>
    /// Error message if the operation failed.
    /// Null if Success is true.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Whether the input text was truncated to fit token limit.
    /// </summary>
    public bool WasTruncated { get; init; }

    /// <summary>
    /// Original text length before any truncation.
    /// </summary>
    public int OriginalLength { get; init; }

    /// <summary>
    /// API latency in milliseconds.
    /// </summary>
    public long LatencyMs { get; init; }

    /// <summary>
    /// Number of retry attempts made.
    /// </summary>
    public int RetryCount { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static EmbeddingResult Ok(
        float[] embedding,
        int tokenCount,
        long latencyMs,
        bool wasTruncated = false,
        int originalLength = 0) => new()
    {
        Success = true,
        Embedding = embedding,
        TokenCount = tokenCount,
        LatencyMs = latencyMs,
        WasTruncated = wasTruncated,
        OriginalLength = originalLength
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static EmbeddingResult Fail(string errorMessage, int retryCount = 0) => new()
    {
        Success = false,
        ErrorMessage = errorMessage,
        RetryCount = retryCount
    };
}
```

### 2.4 EmbeddingException

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Exception thrown when embedding generation fails.
/// </summary>
public class EmbeddingException : Exception
{
    /// <summary>
    /// HTTP status code if the failure was API-related.
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Number of retry attempts made before failure.
    /// </summary>
    public int RetryCount { get; }

    /// <summary>
    /// Whether the failure is transient and may succeed on retry.
    /// </summary>
    public bool IsTransient { get; }

    public EmbeddingException(string message)
        : base(message) { }

    public EmbeddingException(string message, Exception innerException)
        : base(message, innerException) { }

    public EmbeddingException(
        string message,
        int? statusCode = null,
        int retryCount = 0,
        bool isTransient = false,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        RetryCount = retryCount;
        IsTransient = isTransient;
    }
}
```

---

## 3. Supported Models

| Provider | Model | Dimensions | Max Tokens | Cost/1M tokens |
| :------- | :---- | :--------- | :--------- | :------------- |
| OpenAI | text-embedding-3-small | 1536 | 8191 | $0.02 |
| OpenAI | text-embedding-3-large | 3072 | 8191 | $0.13 |
| OpenAI | text-embedding-ada-002 | 1536 | 8191 | $0.10 |
| (Future) | Local (ONNX) | Variable | Variable | Free |

---

## 4. Usage Examples

### 4.1 Single Embedding

```csharp
// Inject service
var embedder = serviceProvider.GetRequiredService<IEmbeddingService>();

// Embed single text
var embedding = await embedder.EmbedAsync("Hello world");

// Use in search
var similarity = CosineSimilarity(embedding, storedEmbedding);
```

### 4.2 Batch Embedding

```csharp
// Embed multiple texts efficiently
var texts = chunks.Select(c => c.Content).ToList();
var embeddings = await embedder.EmbedBatchAsync(texts);

// Pair with chunks
for (int i = 0; i < chunks.Count; i++)
{
    chunks[i].Embedding = embeddings[i];
}
```

### 4.3 Error Handling

```csharp
try
{
    var embedding = await embedder.EmbedAsync(text);
}
catch (EmbeddingException ex) when (ex.StatusCode == 429)
{
    logger.LogWarning("Rate limited after {Retries} retries", ex.RetryCount);
    // Wait and retry or queue for later
}
catch (EmbeddingException ex) when (ex.StatusCode == 401)
{
    logger.LogError("API key invalid or expired");
    throw;
}
```

---

## 5. Extension Points

### 5.1 Custom Embedding Provider

```csharp
// Implement for alternative providers (Cohere, HuggingFace, local models)
public class LocalEmbeddingService : IEmbeddingService
{
    private readonly OnnxModel _model;

    public string ModelName => "all-MiniLM-L6-v2";
    public int Dimensions => 384;
    public int MaxTokens => 512;

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        // Run ONNX inference locally
        return await _model.InferAsync(text, ct);
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts, CancellationToken ct)
    {
        // Batch inference
        return await _model.BatchInferAsync(texts, ct);
    }
}
```

### 5.2 Provider Selection

```csharp
// Configuration-driven provider selection
services.AddSingleton<IEmbeddingService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var provider = config["Embedding:Provider"];

    return provider switch
    {
        "openai" => sp.GetRequiredService<OpenAIEmbeddingService>(),
        "local" => sp.GetRequiredService<LocalEmbeddingService>(),
        _ => throw new InvalidOperationException($"Unknown provider: {provider}")
    };
});
```

---

## 6. Unit Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.4a")]
public class EmbeddingAbstractionsTests
{
    [Fact]
    public void EmbeddingOptions_Default_HasCorrectValues()
    {
        var options = EmbeddingOptions.Default;

        options.Model.Should().Be("text-embedding-3-small");
        options.Dimensions.Should().Be(1536);
        options.MaxTokens.Should().Be(8191);
        options.MaxBatchSize.Should().Be(100);
    }

    [Fact]
    public void EmbeddingOptions_Validate_ThrowsForInvalidModel()
    {
        var options = new EmbeddingOptions { Model = "" };

        options.Invoking(o => o.Validate())
            .Should().Throw<ArgumentException>()
            .WithMessage("*Model*");
    }

    [Fact]
    public void EmbeddingOptions_Validate_ThrowsForInvalidDimensions()
    {
        var options = new EmbeddingOptions { Dimensions = 0 };

        options.Invoking(o => o.Validate())
            .Should().Throw<ArgumentException>()
            .WithMessage("*Dimensions*");
    }

    [Fact]
    public void EmbeddingResult_Ok_CreatesSuccessfulResult()
    {
        var embedding = new float[1536];
        var result = EmbeddingResult.Ok(embedding, 10, 100);

        result.Success.Should().BeTrue();
        result.Embedding.Should().BeSameAs(embedding);
        result.TokenCount.Should().Be(10);
        result.LatencyMs.Should().Be(100);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void EmbeddingResult_Fail_CreatesFailedResult()
    {
        var result = EmbeddingResult.Fail("API error", retryCount: 3);

        result.Success.Should().BeFalse();
        result.Embedding.Should().BeNull();
        result.ErrorMessage.Should().Be("API error");
        result.RetryCount.Should().Be(3);
    }

    [Fact]
    public void EmbeddingException_PreservesStatusCode()
    {
        var ex = new EmbeddingException("Rate limited", statusCode: 429, retryCount: 3, isTransient: true);

        ex.StatusCode.Should().Be(429);
        ex.RetryCount.Should().Be(3);
        ex.IsTransient.Should().BeTrue();
    }
}
```

---

## 7. File Locations

| File | Path |
| :--- | :--- |
| IEmbeddingService | `src/Lexichord.Abstractions/Contracts/IEmbeddingService.cs` |
| EmbeddingOptions | `src/Lexichord.Abstractions/Contracts/EmbeddingOptions.cs` |
| EmbeddingResult | `src/Lexichord.Abstractions/Contracts/EmbeddingResult.cs` |
| EmbeddingException | `src/Lexichord.Abstractions/Contracts/EmbeddingException.cs` |
| Unit tests | `tests/Lexichord.Abstractions.Tests/EmbeddingAbstractionsTests.cs` |

---

## 8. Dependencies

| Dependency | Version | Purpose |
| :--------- | :------ | :------ |
| None | — | Pure abstractions with no external dependencies |

---

## 9. Acceptance Criteria

| # | Criterion | Status |
| :- | :-------- | :----- |
| 1 | IEmbeddingService defines ModelName, Dimensions, MaxTokens | [ ] |
| 2 | IEmbeddingService defines EmbedAsync and EmbedBatchAsync | [ ] |
| 3 | EmbeddingOptions has all configuration properties | [ ] |
| 4 | EmbeddingOptions.Validate() enforces constraints | [ ] |
| 5 | EmbeddingResult provides Success, Embedding, diagnostics | [ ] |
| 6 | EmbeddingResult.Ok and Fail factory methods work | [ ] |
| 7 | EmbeddingException preserves error context | [ ] |
| 8 | All unit tests pass | [ ] |

---

## 10. Revision History

| Version | Date       | Author         | Changes                    |
| :------ | :--------- | :------------- | :------------------------- |
| 0.1     | 2026-01-27 | Lead Architect | Initial draft              |

---
