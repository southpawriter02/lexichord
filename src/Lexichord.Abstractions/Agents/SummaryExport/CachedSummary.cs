// -----------------------------------------------------------------------
// <copyright file="CachedSummary.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Cache entry record for persisted summaries (v0.7.6c).
//   Stores a generated summary with metadata for later retrieval:
//   document path, content hash for change detection, expiration time.
//
//   Cache Invalidation:
//     - Content hash changes (document modified)
//     - Cache entry expires (default: 7 days)
//     - User manually clears cache
//     - Document is deleted
//
//   Introduced in: v0.7.6c
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.MetadataExtraction;
using Lexichord.Abstractions.Agents.Summarizer;

namespace Lexichord.Abstractions.Agents.SummaryExport;

/// <summary>
/// A cached summary entry with metadata for persistence and invalidation.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="CachedSummary"/> record stores generated summaries
/// for later retrieval, avoiding redundant LLM calls for unchanged documents.
/// </para>
/// <para>
/// <b>Cache Storage:</b>
/// <list type="bullet">
/// <item><description>User-level: <c>%APPDATA%/Lexichord/cache/summaries/{hash}.json</c> (Windows)</description></item>
/// <item><description>User-level: <c>~/Library/Application Support/Lexichord/cache/summaries/{hash}.json</c> (macOS)</description></item>
/// <item><description>Workspace-level: <c>.lexichord/cache/summaries/{relative-path-hash}.json</c></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Cache Invalidation:</b>
/// Cache entries are invalidated when:
/// <list type="bullet">
/// <item><description><see cref="ContentHash"/> doesn't match current document hash</description></item>
/// <item><description><see cref="ExpiresAt"/> has passed (<see cref="IsExpired"/> returns <c>true</c>)</description></item>
/// <item><description>User manually clears cache via <see cref="ISummaryExporter.ClearCacheAsync"/></description></item>
/// <item><description>Source document is deleted</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread safety:</b> This is an immutable record and is inherently thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6c as part of the Summarizer Agent Export Formats.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Check if cached summary is still valid
/// var cached = await exporter.GetCachedSummaryAsync(documentPath, ct);
/// if (cached != null &amp;&amp; !cached.IsExpired)
/// {
///     Console.WriteLine($"Using cached summary from {cached.CachedAt}");
///     return cached.Summary;
/// }
///
/// // Generate new summary and cache it
/// var summary = await summarizer.SummarizeAsync(documentPath, options, ct);
/// await exporter.CacheSummaryAsync(documentPath, summary, metadata, ct);
/// </code>
/// </example>
/// <seealso cref="SummarizationResult"/>
/// <seealso cref="DocumentMetadata"/>
/// <seealso cref="ISummaryExporter"/>
public record CachedSummary
{
    /// <summary>
    /// Gets the source document path.
    /// </summary>
    /// <value>
    /// The full path to the document that was summarized.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Used as the primary key for cache lookup. Paths are
    /// normalized to use forward slashes for cross-platform compatibility.
    /// </remarks>
    public required string DocumentPath { get; init; }

    /// <summary>
    /// Gets the content hash of the document when the summary was generated.
    /// </summary>
    /// <value>
    /// A SHA256 hash of the document content, prefixed with "sha256:".
    /// Example: "sha256:abc123def456..."
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Used to detect if the document has changed since the summary
    /// was generated. When the current document hash doesn't match, the cache is
    /// considered stale and <see cref="ISummaryExporter.GetCachedSummaryAsync"/>
    /// returns <c>null</c>.
    /// </remarks>
    public required string ContentHash { get; init; }

    /// <summary>
    /// Gets the cached summarization result.
    /// </summary>
    /// <value>
    /// The complete <see cref="SummarizationResult"/> that was generated.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Contains the summary text, mode, compression metrics, and
    /// other summarization details. Serialized to JSON for persistence.
    /// </remarks>
    public required SummarizationResult Summary { get; init; }

    /// <summary>
    /// Gets the cached document metadata, if available.
    /// </summary>
    /// <value>
    /// The <see cref="DocumentMetadata"/> if it was extracted along with the summary;
    /// otherwise <c>null</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Metadata is optional and may be <c>null</c> if the summary was
    /// generated without metadata extraction. When present, cached metadata is used
    /// for panel display and frontmatter export.
    /// </remarks>
    public DocumentMetadata? Metadata { get; init; }

    /// <summary>
    /// Gets the timestamp when the cache entry was created.
    /// </summary>
    /// <value>
    /// UTC timestamp of when the summary was cached.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Used for display purposes ("Generated 2 min ago") and for
    /// determining cache age in analytics.
    /// </remarks>
    public DateTimeOffset CachedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the expiration time for this cache entry.
    /// </summary>
    /// <value>
    /// UTC timestamp after which the cache entry is considered stale.
    /// Default: 7 days from creation.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Even if the document hasn't changed, cached summaries expire
    /// after a period to ensure freshness. The default 7-day expiration balances
    /// performance with accuracy.
    /// </remarks>
    public DateTimeOffset ExpiresAt { get; init; } = DateTimeOffset.UtcNow.AddDays(7);

    /// <summary>
    /// Gets whether this cache entry has expired.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="ExpiresAt"/> is in the past; otherwise <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience property for checking expiration without comparing
    /// timestamps directly. Always compare against <see cref="DateTimeOffset.UtcNow"/>.
    /// </remarks>
    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;

    /// <summary>
    /// Gets the age of this cache entry.
    /// </summary>
    /// <value>
    /// The time elapsed since the entry was cached.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience property for displaying cache age in the UI
    /// (e.g., "Generated 2 min ago", "Generated 3 days ago").
    /// </remarks>
    public TimeSpan Age => DateTimeOffset.UtcNow - CachedAt;

    /// <summary>
    /// Creates a new cache entry for a summary.
    /// </summary>
    /// <param name="documentPath">The source document path.</param>
    /// <param name="contentHash">The SHA256 hash of the document content.</param>
    /// <param name="summary">The generated summary.</param>
    /// <param name="metadata">Optional metadata to cache.</param>
    /// <param name="expirationDays">Days until expiration. Default: 7.</param>
    /// <returns>A new <see cref="CachedSummary"/> instance.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="documentPath"/>, <paramref name="contentHash"/>,
    /// or <paramref name="summary"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="expirationDays"/> is less than or equal to 0.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for creating cache entries with validated
    /// parameters and consistent timestamp handling.
    /// </remarks>
    public static CachedSummary Create(
        string documentPath,
        string contentHash,
        SummarizationResult summary,
        DocumentMetadata? metadata = null,
        int expirationDays = 7)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(contentHash);
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expirationDays);

        var now = DateTimeOffset.UtcNow;
        return new CachedSummary
        {
            DocumentPath = documentPath,
            ContentHash = contentHash,
            Summary = summary,
            Metadata = metadata,
            CachedAt = now,
            ExpiresAt = now.AddDays(expirationDays)
        };
    }
}
