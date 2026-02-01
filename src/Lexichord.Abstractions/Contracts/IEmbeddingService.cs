// <copyright file="IEmbeddingService.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for converting text into embedding vectors for semantic search and analysis.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IEmbeddingService"/> abstracts the embedding model and API interactions,
/// enabling semantic similarity searches, clustering, and other vector-based operations.
/// Implementations should handle model selection, tokenization, and result formatting.
/// </para>
/// <para>
/// <b>Features:</b>
/// <list type="bullet">
///   <item>Single and batch embedding operations.</item>
///   <item>Configurable embedding dimensions and model selection.</item>
///   <item>Token counting and overflow handling.</item>
///   <item>Automatic retry logic for transient failures.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.4a as part of the Embedding Abstractions layer.
/// </para>
/// </remarks>
public interface IEmbeddingService
{
    /// <summary>
    /// Gets the name of the embedding model in use.
    /// </summary>
    /// <remarks>
    /// LOGIC: Identifies which model is being used for embeddings (e.g., "text-embedding-3-small").
    /// This property should remain consistent throughout the service lifetime.
    /// </remarks>
    /// <value>
    /// The model identifier as a string (e.g., "text-embedding-3-small", "text-embedding-3-large").
    /// </value>
    string ModelName { get; }

    /// <summary>
    /// Gets the dimensionality of the embedding vectors produced.
    /// </summary>
    /// <remarks>
    /// LOGIC: Specifies the number of dimensions in the output vectors.
    /// Must be consistent with the configured model and match database schema expectations.
    /// </remarks>
    /// <value>
    /// The number of dimensions per embedding vector (e.g., 1536 for text-embedding-3-small).
    /// </value>
    int Dimensions { get; }

    /// <summary>
    /// Gets the maximum number of tokens that can be encoded in a single text input.
    /// </summary>
    /// <remarks>
    /// LOGIC: Represents the model's token limit. Text longer than this will be truncated
    /// during embedding. Use <see cref="EmbedAsync"/> to detect and handle truncation.
    /// </remarks>
    /// <value>
    /// The maximum token count supported by the model (e.g., 8191 for text-embedding-3-small).
    /// </value>
    int MaxTokens { get; }

    /// <summary>
    /// Converts a single text string into a semantic embedding vector.
    /// </summary>
    /// <param name="text">The text to embed. Cannot be null or empty.</param>
    /// <param name="ct">Cancellation token for operation control.</param>
    /// <returns>
    /// A task that completes with a float array representing the embedding vector.
    /// The array length will equal <see cref="Dimensions"/>.
    /// </returns>
    /// <remarks>
    /// <para>LOGIC: Single-text embedding operation suitable for interactive use.</para>
    /// <para>
    /// <b>Behavior:</b>
    /// <list type="bullet">
    ///   <item>Returns a normalized vector if configured.</item>
    ///   <item>Truncates input exceeding <see cref="MaxTokens"/> silently.</item>
    ///   <item>Retries on transient failures per <see cref="EmbeddingOptions.MaxRetries"/>.</item>
    ///   <item>Throws <see cref="EmbeddingException"/> on permanent failures.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Exception Behavior:</b> See <see cref="EmbeddingException"/> for details on
    /// transient vs. permanent failures.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="text"/> is null or empty.
    /// </exception>
    /// <exception cref="EmbeddingException">
    /// Thrown when the embedding operation fails after exhausting retries.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="ct"/> is cancelled.
    /// </exception>
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// Converts multiple text strings into semantic embedding vectors in a single batch operation.
    /// </summary>
    /// <param name="texts">Collection of texts to embed. Cannot be null or contain null elements.</param>
    /// <param name="ct">Cancellation token for operation control.</param>
    /// <returns>
    /// A task that completes with a read-only list of float arrays, one per input text.
    /// Each array has length equal to <see cref="Dimensions"/>.
    /// </returns>
    /// <remarks>
    /// <para>LOGIC: Batch embedding operation for efficient processing of multiple texts.</para>
    /// <para>
    /// <b>Behavior:</b>
    /// <list type="bullet">
    ///   <item>More efficient than sequential <see cref="EmbedAsync"/> calls.</item>
    ///   <item>Respects <see cref="EmbeddingOptions.MaxBatchSize"/> by auto-partitioning if needed.</item>
    ///   <item>Preserves order: output[i] corresponds to texts[i].</item>
    ///   <item>Truncates inputs exceeding <see cref="MaxTokens"/> silently.</item>
    ///   <item>Returns normalized vectors if configured.</item>
    ///   <item>Retries on transient failures per <see cref="EmbeddingOptions.MaxRetries"/>.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Partial Failures:</b> If a batch fails after retries, the entire operation throws.
    /// For partial failure handling, use <see cref="EmbedAsync"/> on individual texts.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="texts"/> is null or contains null elements.
    /// </exception>
    /// <exception cref="EmbeddingException">
    /// Thrown when the embedding operation fails after exhausting retries.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="ct"/> is cancelled.
    /// </exception>
    Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct = default);
}
