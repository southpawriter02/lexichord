// =============================================================================
// File: EmbeddingResult.cs
// Project: Lexichord.Abstractions
// Description: Record encapsulating the outcome of an embedding operation,
//              including success status, vector data, token counts, and metadata.
// =============================================================================
// LOGIC: Immutable result type providing comprehensive embedding operation feedback.
//   - Factory methods enforce correct state combinations.
//   - WasTruncated indicates input text exceeded MaxTokens.
//   - LatencyMs enables performance monitoring and optimization.
//   - RetryCount provides insights into transient failures and recovery.
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Represents the result of a text embedding operation.
/// </summary>
/// <remarks>
/// <para>
/// This record encapsulates all relevant information about an embedding attempt,
/// including success status, the embedding vector, token consumption, timing metrics,
/// and diagnostic information about input truncation and retry attempts.
/// </para>
/// <para>
/// <b>Success vs. Failure:</b>
/// <list type="bullet">
///   <item>
///     <b>Success:</b> <see cref="Success"/> is <c>true</c>, <see cref="Embedding"/> contains
///     a valid float array of length matching <see cref="IEmbeddingService.Dimensions"/>,
///     and <see cref="ErrorMessage"/> is <c>null</c>.
///   </item>
///   <item>
///     <b>Failure:</b> <see cref="Success"/> is <c>false</c>, <see cref="Embedding"/> is <c>null</c>,
///     and <see cref="ErrorMessage"/> describes the failure reason.
///   </item>
/// </list>
/// </para>
/// <para>
/// <b>Diagnostics:</b>
/// <list type="bullet">
///   <item><see cref="WasTruncated"/> indicates the input exceeded the model's token limit.</item>
///   <item><see cref="LatencyMs"/> helps identify slow operations for optimization.</item>
///   <item><see cref="RetryCount"/> shows how many transient failures were recovered.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.4a as part of the Embedding Abstractions layer.
/// </para>
/// </remarks>
/// <param name="Success">
/// Indicates whether the embedding operation completed successfully.
/// </param>
/// <param name="Embedding">
/// The embedding vector as a float array. Null if <see cref="Success"/> is <c>false</c>.
/// </param>
/// <param name="TokenCount">
/// The number of tokens consumed during embedding (after truncation if applicable).
/// Zero if the operation failed.
/// </param>
/// <param name="ErrorMessage">
/// The error message describing the failure. Null if <see cref="Success"/> is <c>true</c>.
/// </param>
/// <param name="WasTruncated">
/// Indicates whether the input text was truncated to fit the token limit.
/// </param>
/// <param name="OriginalLength">
/// The number of characters in the original input text.
/// Used to detect truncation impact on semantics.
/// </param>
/// <param name="LatencyMs">
/// The elapsed time in milliseconds for the complete embedding operation.
/// </param>
/// <param name="RetryCount">
/// The number of retry attempts used to succeed. Zero if no retries were needed.
/// </param>
public record EmbeddingResult(
    bool Success,
    float[]? Embedding,
    int TokenCount,
    string? ErrorMessage,
    bool WasTruncated,
    int OriginalLength,
    long LatencyMs,
    int RetryCount)
{
    /// <summary>
    /// Creates a successful embedding result.
    /// </summary>
    /// <param name="embedding">The embedding vector. Cannot be null.</param>
    /// <param name="tokenCount">The number of tokens consumed.</param>
    /// <param name="originalLength">The original input text character count.</param>
    /// <param name="wasTruncated">Whether the input was truncated.</param>
    /// <param name="latencyMs">The operation latency in milliseconds.</param>
    /// <param name="retryCount">The number of retries used (default: 0).</param>
    /// <returns>A successful <see cref="EmbeddingResult"/>.</returns>
    /// <remarks>
    /// LOGIC: Factory method ensuring proper initialization of successful results.
    /// </remarks>
    public static EmbeddingResult Ok(
        float[] embedding,
        int tokenCount,
        int originalLength,
        bool wasTruncated,
        long latencyMs,
        int retryCount = 0)
    {
        if (embedding == null)
            throw new ArgumentNullException(nameof(embedding));

        if (tokenCount < 0)
            throw new ArgumentException("tokenCount cannot be negative", nameof(tokenCount));

        if (originalLength < 0)
            throw new ArgumentException("originalLength cannot be negative", nameof(originalLength));

        if (latencyMs < 0)
            throw new ArgumentException("latencyMs cannot be negative", nameof(latencyMs));

        if (retryCount < 0)
            throw new ArgumentException("retryCount cannot be negative", nameof(retryCount));

        return new EmbeddingResult(
            Success: true,
            Embedding: embedding,
            TokenCount: tokenCount,
            ErrorMessage: null,
            WasTruncated: wasTruncated,
            OriginalLength: originalLength,
            LatencyMs: latencyMs,
            RetryCount: retryCount);
    }

    /// <summary>
    /// Creates a failed embedding result.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <param name="originalLength">The original input text character count.</param>
    /// <param name="latencyMs">The operation latency in milliseconds.</param>
    /// <param name="retryCount">The number of retries attempted (default: 0).</param>
    /// <returns>A failed <see cref="EmbeddingResult"/>.</returns>
    /// <remarks>
    /// LOGIC: Factory method ensuring proper initialization of failed results.
    /// Failed results have null embedding and zero token count.
    /// </remarks>
    public static EmbeddingResult Fail(
        string errorMessage,
        int originalLength,
        long latencyMs,
        int retryCount = 0)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("errorMessage cannot be null or empty", nameof(errorMessage));

        if (originalLength < 0)
            throw new ArgumentException("originalLength cannot be negative", nameof(originalLength));

        if (latencyMs < 0)
            throw new ArgumentException("latencyMs cannot be negative", nameof(latencyMs));

        if (retryCount < 0)
            throw new ArgumentException("retryCount cannot be negative", nameof(retryCount));

        return new EmbeddingResult(
            Success: false,
            Embedding: null,
            TokenCount: 0,
            ErrorMessage: errorMessage,
            WasTruncated: false,
            OriginalLength: originalLength,
            LatencyMs: latencyMs,
            RetryCount: retryCount);
    }
}
