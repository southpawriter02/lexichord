// =============================================================================
// File: EmbeddingOptions.cs
// Project: Lexichord.Abstractions
// Description: Configuration options for embedding service behavior, including
//              model selection, token limits, normalization, batch sizing, and API settings.
// =============================================================================
// LOGIC: Non-positional record with init-only properties and sensible defaults.
//   - Default static property provides zero-configuration usage.
//   - Validate() enforces 6 internal consistency constraints:
//     1. Model name cannot be null or empty.
//     2. MaxTokens must be positive.
//     3. Dimensions must be positive.
//     4. MaxBatchSize must be positive.
//     5. TimeoutSeconds must be positive.
//     6. MaxRetries cannot be negative.
//   - Normalized vectors enable direct distance-based similarity without magnitude bias.
//   - ApiBaseUrl allows overriding default endpoints for proxies or alternative providers.
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Configuration options for embedding service behavior.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="EmbeddingOptions"/> controls how an <see cref="IEmbeddingService"/> implementation
/// operates, including which embedding model to use, dimensional output, batch processing limits,
/// API connectivity settings, and retry behavior.
/// </para>
/// <para>
/// <b>Validation:</b> Call <see cref="Validate"/> before passing options to an embedding service
/// to ensure internal consistency. Invalid configurations (e.g., zero dimensions, negative retries)
/// will throw <see cref="ArgumentException"/>.
/// </para>
/// <para>
/// <b>Defaults:</b> Use <see cref="Default"/> for a production-ready configuration suitable for
/// most use cases. Customize individual properties via the init accessor as needed.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.4a as part of the Embedding Abstractions layer.
/// </para>
/// </remarks>
public record EmbeddingOptions
{
    /// <summary>
    /// Default options with sensible defaults for production use.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Configuration:</b>
    /// <list type="bullet">
    ///   <item><see cref="Model"/> = "text-embedding-3-small"</item>
    ///   <item><see cref="MaxTokens"/> = 8191</item>
    ///   <item><see cref="Dimensions"/> = 1536</item>
    ///   <item><see cref="Normalize"/> = <c>true</c></item>
    ///   <item><see cref="MaxBatchSize"/> = 100</item>
    ///   <item><see cref="TimeoutSeconds"/> = 60</item>
    ///   <item><see cref="MaxRetries"/> = 3</item>
    ///   <item><see cref="ApiBaseUrl"/> = <c>null</c> (use default)</item>
    ///   <item><see cref="SecretKeyName"/> = "openai:api-key"</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <value>
    /// A <see cref="EmbeddingOptions"/> instance with production defaults.
    /// </value>
    public static EmbeddingOptions Default { get; } = new();

    /// <summary>
    /// The embedding model identifier.
    /// </summary>
    /// <remarks>
    /// LOGIC: Specifies which embedding model to use. Common values include
    /// "text-embedding-3-small" (1536 dimensions) and "text-embedding-3-large" (3072 dimensions).
    /// </remarks>
    /// <value>Default: "text-embedding-3-small".</value>
    public string Model { get; init; } = "text-embedding-3-small";

    /// <summary>
    /// The maximum number of tokens a single input can have.
    /// </summary>
    /// <remarks>
    /// LOGIC: Inputs exceeding this limit will be truncated. This prevents token limit
    /// errors from the embedding API. Set to model's documented max for safety.
    /// </remarks>
    /// <value>Default: 8191 tokens (text-embedding-3-* models).</value>
    public int MaxTokens { get; init; } = 8191;

    /// <summary>
    /// The dimensionality of the embedding vectors produced.
    /// </summary>
    /// <remarks>
    /// LOGIC: Must match the embedding model's output dimensions.
    /// text-embedding-3-small produces 1536-dimensional vectors.
    /// text-embedding-3-large produces 3072-dimensional vectors.
    /// </remarks>
    /// <value>Default: 1536 (for text-embedding-3-small).</value>
    public int Dimensions { get; init; } = 1536;

    /// <summary>
    /// Whether to normalize embedding vectors.
    /// </summary>
    /// <remarks>
    /// LOGIC: Normalized vectors (unit norm) are preferred for distance-based similarity metrics.
    /// Enables direct use of cosine similarity without magnitude considerations.
    /// Most semantic search implementations expect normalized embeddings.
    /// </remarks>
    /// <value>Default: <c>true</c>.</value>
    public bool Normalize { get; init; } = true;

    /// <summary>
    /// The maximum number of texts to embed in a single API call.
    /// </summary>
    /// <remarks>
    /// LOGIC: The embedding API imposes a limit on batch size. Batch requests
    /// exceeding this size are automatically partitioned into multiple calls.
    /// Larger batches are more efficient but consume more memory and bandwidth per call.
    /// </remarks>
    /// <value>Default: 100 texts per batch.</value>
    public int MaxBatchSize { get; init; } = 100;

    /// <summary>
    /// The timeout in seconds for a single embedding API request.
    /// </summary>
    /// <remarks>
    /// LOGIC: Prevents indefinite waiting on slow or unresponsive API endpoints.
    /// Timeouts are treated as transient failures and trigger retry logic.
    /// </remarks>
    /// <value>Default: 60 seconds.</value>
    public int TimeoutSeconds { get; init; } = 60;

    /// <summary>
    /// The maximum number of retry attempts for transient failures.
    /// </summary>
    /// <remarks>
    /// LOGIC: Transient failures (timeouts, rate limits, temporary errors) are retried
    /// with exponential backoff. Permanent errors (invalid API key, bad model name) are not retried.
    /// Set to 0 to disable retries.
    /// </remarks>
    /// <value>Default: 3 retry attempts.</value>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// Optional custom API base URL for the embedding service.
    /// </summary>
    /// <remarks>
    /// LOGIC: Allows overriding the default API endpoint for proxies, alternative providers,
    /// or private deployments. When null, the implementation uses its default endpoint.
    /// </remarks>
    /// <value>Default: <c>null</c> (use implementation default).</value>
    public string? ApiBaseUrl { get; init; } = null;

    /// <summary>
    /// The secret vault key name for the API authentication credential.
    /// </summary>
    /// <remarks>
    /// LOGIC: Specifies the key used to retrieve the API key from the secure vault.
    /// The SecureVault will be queried using this key when initializing the service.
    /// </remarks>
    /// <value>Default: "openai:api-key".</value>
    public string SecretKeyName { get; init; } = "openai:api-key";

    /// <summary>
    /// Validates that the options are internally consistent.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method enforces 6 constraints to ensure the options form a valid
    /// configuration for embedding services:
    /// </para>
    /// <list type="number">
    ///   <item><description><see cref="Model"/> cannot be null or empty.</description></item>
    ///   <item><description><see cref="MaxTokens"/> must be positive.</description></item>
    ///   <item><description><see cref="Dimensions"/> must be positive.</description></item>
    ///   <item><description><see cref="MaxBatchSize"/> must be positive.</description></item>
    ///   <item><description><see cref="TimeoutSeconds"/> must be positive.</description></item>
    ///   <item><description><see cref="MaxRetries"/> cannot be negative.</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when any constraint is violated.
    /// </exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Model))
            throw new ArgumentException("Model cannot be null or empty", nameof(Model));

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
