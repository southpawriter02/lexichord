namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Transforms raw scan matches into user-facing style violations.
/// </summary>
/// <remarks>
/// LOGIC: The aggregator is the final stage of the linting pipeline:
/// 1. Scanner produces PatternMatchSpan positions
/// 2. Orchestrator wraps them as ScanMatch with rule context
/// 3. Aggregator transforms to AggregatedStyleViolation with:
///    - Line/column positions calculated from offsets
///    - Overlapping violations deduplicated (higher severity wins)
///    - Results sorted by position and cached
///
/// Caching: Per-document cache enables fast lookups without re-aggregating.
/// Call ClearViolations when a document is closed or significantly edited.
///
/// Threading: All methods are thread-safe. Uses ConcurrentDictionary internally.
///
/// Version: v0.2.3d
/// </remarks>
public interface IViolationAggregator
{
    /// <summary>
    /// Aggregates scan matches into violations for a document.
    /// </summary>
    /// <param name="matches">Raw matches from the scanner.</param>
    /// <param name="documentId">Unique identifier for the document.</param>
    /// <param name="content">The document content for position calculation.</param>
    /// <returns>Sorted, deduplicated list of violations.</returns>
    /// <remarks>
    /// LOGIC: Main aggregation pipeline:
    /// 1. Transform each ScanMatch to AggregatedStyleViolation
    /// 2. Calculate line/column from offsets using PositionIndex
    /// 3. Deduplicate overlapping violations (keep highest severity)
    /// 4. Sort by line, then column
    /// 5. Apply MaxViolationsPerDocument limit
    /// 6. Cache results for quick retrieval
    ///
    /// Empty input returns empty list (not null).
    /// </remarks>
    IReadOnlyList<AggregatedStyleViolation> Aggregate(
        IEnumerable<ScanMatch> matches,
        string documentId,
        string content);

    /// <summary>
    /// Clears cached violations for a document.
    /// </summary>
    /// <param name="documentId">The document to clear.</param>
    /// <remarks>
    /// LOGIC: Call when:
    /// - Document is closed
    /// - Document content changes significantly (re-scan will repopulate)
    /// - Memory pressure detected
    ///
    /// Safe to call for non-existent documents (no-op).
    /// </remarks>
    void ClearViolations(string documentId);

    /// <summary>
    /// Gets all cached violations for a document.
    /// </summary>
    /// <param name="documentId">The document to query.</param>
    /// <returns>Cached violations, or empty if not aggregated.</returns>
    /// <remarks>
    /// LOGIC: Returns last aggregation result without re-scanning.
    /// Call Aggregate first to populate the cache.
    /// </remarks>
    IReadOnlyList<AggregatedStyleViolation> GetViolations(string documentId);

    /// <summary>
    /// Gets a specific violation by its ID.
    /// </summary>
    /// <param name="documentId">The document containing the violation.</param>
    /// <param name="violationId">The unique violation ID.</param>
    /// <returns>The violation if found, null otherwise.</returns>
    /// <remarks>
    /// LOGIC: O(1) lookup via internal dictionary.
    /// Used by quick-fix commands that reference violations by ID.
    /// </remarks>
    AggregatedStyleViolation? GetViolation(string documentId, string violationId);

    /// <summary>
    /// Gets the violation at a specific character offset.
    /// </summary>
    /// <param name="documentId">The document to query.</param>
    /// <param name="offset">Character offset (0-indexed).</param>
    /// <returns>First violation containing the offset, or null.</returns>
    /// <remarks>
    /// LOGIC: Used for hover tooltips. Returns first match if
    /// multiple violations overlap at the offset.
    /// </remarks>
    AggregatedStyleViolation? GetViolationAt(string documentId, int offset);

    /// <summary>
    /// Gets all violations overlapping a character range.
    /// </summary>
    /// <param name="documentId">The document to query.</param>
    /// <param name="startOffset">Start of range (inclusive).</param>
    /// <param name="endOffset">End of range (exclusive).</param>
    /// <returns>All violations that overlap the range.</returns>
    /// <remarks>
    /// LOGIC: Used for visible-range rendering. A violation overlaps
    /// if any part of it is within [startOffset, endOffset).
    /// </remarks>
    IReadOnlyList<AggregatedStyleViolation> GetViolationsInRange(
        string documentId,
        int startOffset,
        int endOffset);

    /// <summary>
    /// Gets violation counts grouped by severity.
    /// </summary>
    /// <param name="documentId">The document to query.</param>
    /// <returns>Dictionary mapping severity to count.</returns>
    /// <remarks>
    /// LOGIC: Used for status bar badges (e.g., "3 errors, 5 warnings").
    /// Returns zeros for all severities if document has no violations.
    /// </remarks>
    IReadOnlyDictionary<ViolationSeverity, int> GetViolationCounts(string documentId);
}
