// -----------------------------------------------------------------------
// <copyright file="ISummaryExporter.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Interface for multi-destination summary export (v0.7.6c).
//   Provides methods for exporting summaries and metadata to:
//   Panel, Frontmatter, File, Clipboard, and InlineInsert destinations.
//
//   Also handles caching for generated summaries to avoid redundant LLM calls.
//
//   Methods:
//     - ExportAsync: Export summary to specified destination
//     - ExportMetadataAsync: Export metadata to specified destination
//     - UpdateFrontmatterAsync: Merge summary/metadata into frontmatter
//     - GetCachedSummaryAsync: Retrieve cached summary if valid
//     - CacheSummaryAsync: Store summary in cache
//     - ClearCacheAsync: Remove cached summary
//     - ShowInPanelAsync: Display summary in Summary Panel UI
//
//   Introduced in: v0.7.6c
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.MetadataExtraction;
using Lexichord.Abstractions.Agents.Summarizer;

namespace Lexichord.Abstractions.Agents.SummaryExport;

/// <summary>
/// Interface for exporting summaries and metadata to various destinations.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="ISummaryExporter"/> interface provides a unified API
/// for exporting generated summaries (<see cref="SummarizationResult"/>) and metadata
/// (<see cref="DocumentMetadata"/>) to multiple destinations including UI panels,
/// document frontmatter, standalone files, clipboard, and inline editor insertions.
/// </para>
/// <para>
/// <b>Supported Destinations:</b>
/// <list type="table">
/// <listheader>
/// <term>Destination</term>
/// <description>Export Method</description>
/// </listheader>
/// <item>
/// <term><see cref="ExportDestination.Panel"/></term>
/// <description><see cref="ShowInPanelAsync"/> or <see cref="ExportAsync"/></description>
/// </item>
/// <item>
/// <term><see cref="ExportDestination.Frontmatter"/></term>
/// <description><see cref="ExportAsync"/> or <see cref="UpdateFrontmatterAsync"/></description>
/// </item>
/// <item>
/// <term><see cref="ExportDestination.File"/></term>
/// <description><see cref="ExportAsync"/></description>
/// </item>
/// <item>
/// <term><see cref="ExportDestination.Clipboard"/></term>
/// <description><see cref="ExportAsync"/></description>
/// </item>
/// <item>
/// <term><see cref="ExportDestination.InlineInsert"/></term>
/// <description><see cref="ExportAsync"/></description>
/// </item>
/// </list>
/// </para>
/// <para>
/// <b>Caching:</b>
/// The exporter includes integrated caching to avoid redundant LLM calls:
/// <list type="bullet">
/// <item><description><see cref="GetCachedSummaryAsync"/>: Retrieves valid cached summary</description></item>
/// <item><description><see cref="CacheSummaryAsync"/>: Stores summary for later retrieval</description></item>
/// <item><description><see cref="ClearCacheAsync"/>: Removes cached summary</description></item>
/// </list>
/// Cache entries are invalidated when document content changes (via content hash comparison)
/// or when the cache expires (default: 7 days).
/// </para>
/// <para>
/// <b>License Gating:</b>
/// All export methods require WriterPro tier. Lower tiers receive
/// <see cref="SummaryExportResult.Failed"/> with an upgrade message.
/// </para>
/// <para>
/// <b>Thread safety:</b> Implementations must be thread-safe as exports may be
/// invoked from multiple contexts (UI thread, background tasks).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6c as part of the Summarizer Agent Export Formats.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Export summary to clipboard
/// var options = new SummaryExportOptions
/// {
///     Destination = ExportDestination.Clipboard,
///     ClipboardAsMarkdown = true
/// };
/// var result = await exporter.ExportAsync(summary, documentPath, options, ct);
///
/// if (result.Success)
/// {
///     await toastService.ShowAsync($"Copied {result.CharactersWritten} characters!");
/// }
///
/// // Update frontmatter with summary and metadata
/// await exporter.UpdateFrontmatterAsync(documentPath, summary, metadata, ct);
/// </code>
/// </example>
/// <seealso cref="SummaryExportOptions"/>
/// <seealso cref="SummaryExportResult"/>
/// <seealso cref="ExportDestination"/>
/// <seealso cref="CachedSummary"/>
public interface ISummaryExporter
{
    /// <summary>
    /// Exports a summarization result to the specified destination.
    /// </summary>
    /// <param name="summary">The summary to export. Must not be <c>null</c>.</param>
    /// <param name="sourceDocumentPath">Path to the source document. Must not be <c>null</c> or empty.</param>
    /// <param name="options">Export configuration options.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>
    /// A task that completes with the <see cref="SummaryExportResult"/> indicating success or failure.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="summary"/> or <paramref name="sourceDocumentPath"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Routes to the appropriate export handler based on <see cref="SummaryExportOptions.Destination"/>.
    /// Each destination handler formats the summary appropriately and writes to the target.
    /// Publishes <c>SummaryExportedEvent</c> on success, <c>SummaryExportFailedEvent</c> on failure.
    /// </remarks>
    Task<SummaryExportResult> ExportAsync(
        SummarizationResult summary,
        string sourceDocumentPath,
        SummaryExportOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Exports document metadata to the specified destination.
    /// </summary>
    /// <param name="metadata">The metadata to export. Must not be <c>null</c>.</param>
    /// <param name="sourceDocumentPath">Path to the source document. Must not be <c>null</c> or empty.</param>
    /// <param name="options">Export configuration options.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>
    /// A task that completes with the <see cref="SummaryExportResult"/> indicating success or failure.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="metadata"/> or <paramref name="sourceDocumentPath"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Similar to <see cref="ExportAsync"/> but exports metadata instead of summary.
    /// Useful when users want to export extracted metadata (key terms, tags, reading time)
    /// without the full summary.
    /// </remarks>
    Task<SummaryExportResult> ExportMetadataAsync(
        DocumentMetadata metadata,
        string sourceDocumentPath,
        SummaryExportOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Updates existing frontmatter with new summary and/or metadata.
    /// </summary>
    /// <param name="documentPath">Path to the document to update. Must not be <c>null</c> or empty.</param>
    /// <param name="summary">Summary to add. <c>null</c> to skip summary fields.</param>
    /// <param name="metadata">Metadata to add. <c>null</c> to skip metadata fields.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>
    /// A task that completes with the <see cref="SummaryExportResult"/> indicating success or failure.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="documentPath"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when both <paramref name="summary"/> and <paramref name="metadata"/> are <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Intelligent frontmatter merge:
    /// <list type="number">
    /// <item><description>Parses existing frontmatter (if any)</description></item>
    /// <item><description>Preserves user-defined fields not related to summary</description></item>
    /// <item><description>Adds/updates summary section with provided data</description></item>
    /// <item><description>Adds/updates metadata section with provided data</description></item>
    /// <item><description>Serializes back to YAML and writes to document</description></item>
    /// </list>
    /// </remarks>
    Task<SummaryExportResult> UpdateFrontmatterAsync(
        string documentPath,
        SummarizationResult? summary,
        DocumentMetadata? metadata,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves a previously cached summary for a document.
    /// </summary>
    /// <param name="documentPath">Path to the document. Must not be <c>null</c> or empty.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>
    /// A task that completes with the <see cref="CachedSummary"/> if a valid cache exists;
    /// otherwise <c>null</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="documentPath"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Returns <c>null</c> if:
    /// <list type="bullet">
    /// <item><description>No cache entry exists for the document</description></item>
    /// <item><description>Cache entry has expired (<see cref="CachedSummary.IsExpired"/>)</description></item>
    /// <item><description>Document content hash doesn't match (<see cref="CachedSummary.ContentHash"/>)</description></item>
    /// </list>
    /// </remarks>
    Task<CachedSummary?> GetCachedSummaryAsync(
        string documentPath,
        CancellationToken ct = default);

    /// <summary>
    /// Caches a summary for later retrieval.
    /// </summary>
    /// <param name="documentPath">Path to the source document. Must not be <c>null</c> or empty.</param>
    /// <param name="summary">Summary to cache. Must not be <c>null</c>.</param>
    /// <param name="metadata">Optional metadata to cache alongside the summary.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A task that completes when the summary has been cached.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="documentPath"/> or <paramref name="summary"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Computes a SHA256 hash of the document content and stores the cache entry
    /// in both memory (IMemoryCache) and persistent storage (JSON file). Cache entries expire
    /// after 7 days by default.
    /// </remarks>
    Task CacheSummaryAsync(
        string documentPath,
        SummarizationResult summary,
        DocumentMetadata? metadata,
        CancellationToken ct = default);

    /// <summary>
    /// Clears the cached summary for a document.
    /// </summary>
    /// <param name="documentPath">Path to the document. Must not be <c>null</c> or empty.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A task that completes when the cache has been cleared.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="documentPath"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Removes the cache entry from both memory and persistent storage.
    /// Subsequent calls to <see cref="GetCachedSummaryAsync"/> will return <c>null</c>
    /// until a new summary is cached.
    /// </remarks>
    Task ClearCacheAsync(string documentPath, CancellationToken ct = default);

    /// <summary>
    /// Shows the summary in the Summary Panel UI.
    /// </summary>
    /// <param name="summary">Summary to display. Must not be <c>null</c>.</param>
    /// <param name="metadata">Optional metadata to display alongside the summary.</param>
    /// <param name="sourceDocumentPath">Path to the source document. Must not be <c>null</c> or empty.</param>
    /// <returns>A task that completes when the panel has been opened and populated.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="summary"/> or <paramref name="sourceDocumentPath"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Opens the Summary Panel (if not already open) and populates it with
    /// the provided summary and metadata. Publishes <c>SummaryPanelOpenedEvent</c> for analytics.
    /// The panel provides quick actions for copying, exporting to frontmatter, and file export.
    /// </remarks>
    Task ShowInPanelAsync(
        SummarizationResult summary,
        DocumentMetadata? metadata,
        string sourceDocumentPath);
}
