// -----------------------------------------------------------------------
// <copyright file="ICachedContextAssembler.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.LLM;

namespace Lexichord.Modules.Agents.Performance;

/// <summary>
/// Caches assembled context to avoid redundant RAG searches and style rule lookups.
/// </summary>
/// <remarks>
/// <para>
/// The cached context assembler wraps an <see cref="IContextInjector"/> and
/// caches the results of context assembly operations. Cache entries are keyed
/// by document path and context request configuration, with configurable
/// expiration via <see cref="PerformanceOptions.ContextCacheDuration"/>.
/// </para>
/// <para>
/// Cache hit ratio is tracked via <see cref="CacheHitRatio"/> for observability.
/// Individual document caches can be invalidated via <see cref="Invalidate"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.8c as part of Performance Optimization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Get or create cached context for a document
/// var context = await cachedAssembler.GetOrCreateAsync(
///     "test.md",
///     new ContextRequest("test.md", null, null, true, true, 3),
///     cancellationToken);
///
/// // Invalidate cache when document changes
/// cachedAssembler.Invalidate("test.md");
///
/// // Monitor cache performance
/// Console.WriteLine($"Cache hit ratio: {cachedAssembler.CacheHitRatio:P1}");
/// </code>
/// </example>
/// <seealso cref="CachedContextAssembler"/>
/// <seealso cref="PerformanceOptions"/>
public interface ICachedContextAssembler
{
    /// <summary>
    /// Gets or creates cached context for a document.
    /// </summary>
    /// <param name="documentPath">The document path to retrieve context for.</param>
    /// <param name="request">The context request specifying which sources to include.</param>
    /// <param name="ct">Cancellation token for async operations.</param>
    /// <returns>
    /// A task that resolves to the assembled context dictionary, either from
    /// cache or freshly assembled.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="documentPath"/> or <paramref name="request"/> is null.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via <paramref name="ct"/>.
    /// </exception>
    /// <remarks>
    /// Cache keys are derived from the document path and request configuration.
    /// Cache entries expire after <see cref="PerformanceOptions.ContextCacheDuration"/>.
    /// </remarks>
    Task<IDictionary<string, object>> GetOrCreateAsync(
        string documentPath,
        ContextRequest request,
        CancellationToken ct);

    /// <summary>
    /// Invalidates all cache entries for the specified document.
    /// </summary>
    /// <param name="documentPath">The document path whose cache entries should be invalidated.</param>
    /// <remarks>
    /// Call this when a document's content changes to ensure the next context
    /// assembly reflects the updated content.
    /// </remarks>
    void Invalidate(string documentPath);

    /// <summary>
    /// Gets the cache hit ratio (0.0 to 1.0).
    /// </summary>
    /// <value>
    /// The ratio of cache hits to total requests, or 0.0 if no requests have been made.
    /// A value above 0.7 (70%) is considered good for steady-state operation.
    /// </value>
    double CacheHitRatio { get; }
}
