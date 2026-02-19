// =============================================================================
// File: ExtractionLineageStore.cs
// Project: Lexichord.Modules.Knowledge
// Description: In-memory storage for extraction lineage records.
// =============================================================================
// LOGIC: ExtractionLineageStore maintains extraction records per document in
//   memory with thread-safe access. It enables lineage retrieval and rollback
//   target identification.
//
// v0.7.6f: Doc-to-Graph Sync (CKVS Phase 4c)
// Dependencies: ExtractionRecord (v0.7.6f)
// =============================================================================

using System.Collections.Concurrent;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Sync.DocToGraph;

/// <summary>
/// Service for storing and retrieving extraction lineage records.
/// </summary>
/// <remarks>
/// <para>
/// Maintains extraction history for documents to enable:
/// </para>
/// <list type="bullet">
///   <item>Lineage queries to see extraction history.</item>
///   <item>Rollback target identification.</item>
///   <item>Change detection via extraction hash comparison.</item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This service is thread-safe and can be used from
/// multiple concurrent sync operations.
/// </para>
/// <para>
/// <b>Persistence:</b> Currently in-memory only. Lineage is lost on application
/// restart. Future versions may add persistence via PostgreSQL.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6f as part of the Doc-to-Graph Sync module.
/// </para>
/// </remarks>
public sealed class ExtractionLineageStore
{
    // LOGIC: ConcurrentDictionary for thread-safe lineage storage.
    // Key is DocumentId, value is a list of ExtractionRecord (most recent first).
    private readonly ConcurrentDictionary<Guid, List<ExtractionRecord>> _lineageCache = new();
    private readonly ILogger<ExtractionLineageStore> _logger;

    // LOGIC: Lock object for thread-safe list modifications.
    private readonly object _listLock = new();

    /// <summary>
    /// Initializes a new instance of <see cref="ExtractionLineageStore"/>.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ExtractionLineageStore(ILogger<ExtractionLineageStore> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Records an extraction for lineage tracking.
    /// </summary>
    /// <param name="record">The extraction record to store.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// LOGIC: Adds the record to the document's lineage list, maintaining
    /// chronological order with most recent first.
    /// </remarks>
    public Task RecordExtractionAsync(ExtractionRecord record, CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Recording extraction {ExtractionId} for document {DocumentId}",
            record.ExtractionId, record.DocumentId);

        // LOGIC: Get or create the lineage list for this document.
        var lineage = _lineageCache.GetOrAdd(
            record.DocumentId,
            _ => new List<ExtractionRecord>());

        // LOGIC: Thread-safe addition to the list, maintaining order.
        lock (_listLock)
        {
            // Insert at beginning for most-recent-first ordering.
            lineage.Insert(0, record);

            // LOGIC: Limit lineage history to prevent unbounded growth.
            // Keep the most recent 100 records per document.
            const int maxLineageRecords = 100;
            if (lineage.Count > maxLineageRecords)
            {
                lineage.RemoveRange(maxLineageRecords, lineage.Count - maxLineageRecords);
                _logger.LogDebug(
                    "Trimmed lineage for document {DocumentId} to {Count} records",
                    record.DocumentId, maxLineageRecords);
            }
        }

        _logger.LogDebug(
            "Recorded extraction {ExtractionId}. Document {DocumentId} now has {Count} lineage records",
            record.ExtractionId, record.DocumentId, lineage.Count);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the extraction lineage for a document.
    /// </summary>
    /// <param name="documentId">The document ID to query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A chronologically ordered list of extraction records, most recent first.
    /// Empty if the document has no extraction history.
    /// </returns>
    public Task<IReadOnlyList<ExtractionRecord>> GetLineageAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        // LOGIC: Return a copy of the lineage list to prevent external modification.
        if (_lineageCache.TryGetValue(documentId, out var lineage))
        {
            lock (_listLock)
            {
                return Task.FromResult<IReadOnlyList<ExtractionRecord>>(
                    lineage.ToList());
            }
        }

        _logger.LogDebug(
            "No lineage records found for document {DocumentId}",
            documentId);

        return Task.FromResult<IReadOnlyList<ExtractionRecord>>([]);
    }

    /// <summary>
    /// Finds an extraction record at or before the specified timestamp.
    /// </summary>
    /// <param name="documentId">The document ID to search.</param>
    /// <param name="targetVersion">The target timestamp.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The extraction record at or before the target, or null if not found.
    /// </returns>
    /// <remarks>
    /// LOGIC: Used by rollback to find the target version to restore.
    /// </remarks>
    public Task<ExtractionRecord?> FindExtractionAtAsync(
        Guid documentId,
        DateTimeOffset targetVersion,
        CancellationToken ct = default)
    {
        if (!_lineageCache.TryGetValue(documentId, out var lineage))
        {
            return Task.FromResult<ExtractionRecord?>(null);
        }

        lock (_listLock)
        {
            // LOGIC: Find the first record at or before the target timestamp.
            // Since records are ordered most-recent-first, find the first one
            // where ExtractedAt <= targetVersion.
            var record = lineage.FirstOrDefault(r => r.ExtractedAt <= targetVersion);
            return Task.FromResult(record);
        }
    }

    /// <summary>
    /// Gets extractions newer than the specified record.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="afterRecord">The reference record.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Extraction records that occurred after the specified record.
    /// </returns>
    /// <remarks>
    /// LOGIC: Used by rollback to identify entities to remove.
    /// </remarks>
    public Task<IReadOnlyList<ExtractionRecord>> GetExtractionsAfterAsync(
        Guid documentId,
        ExtractionRecord afterRecord,
        CancellationToken ct = default)
    {
        if (!_lineageCache.TryGetValue(documentId, out var lineage))
        {
            return Task.FromResult<IReadOnlyList<ExtractionRecord>>([]);
        }

        lock (_listLock)
        {
            // LOGIC: Return all records newer than the reference.
            var newerRecords = lineage
                .Where(r => r.ExtractedAt > afterRecord.ExtractedAt)
                .ToList();
            return Task.FromResult<IReadOnlyList<ExtractionRecord>>(newerRecords);
        }
    }

    /// <summary>
    /// Gets the most recent extraction hash for change detection.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The extraction hash from the most recent extraction, or null if no history.
    /// </returns>
    public Task<string?> GetLatestExtractionHashAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        if (!_lineageCache.TryGetValue(documentId, out var lineage))
        {
            return Task.FromResult<string?>(null);
        }

        lock (_listLock)
        {
            var hash = lineage.FirstOrDefault()?.ExtractionHash;
            return Task.FromResult(hash);
        }
    }
}
