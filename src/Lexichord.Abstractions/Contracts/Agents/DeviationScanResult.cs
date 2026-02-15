// -----------------------------------------------------------------------
// <copyright file="DeviationScanResult.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.Agents;

/// <summary>
/// Result of a deviation scan operation, containing all detected deviations
/// with metadata for caching and grouping.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> DeviationScanResult is an immutable container returned by
/// <see cref="IStyleDeviationScanner"/>. It provides:
/// <list type="bullet">
///   <item><description>All detected deviations</description></item>
///   <item><description>Computed counts for UI display</description></item>
///   <item><description>Grouping helpers for organization</description></item>
///   <item><description>Cache validation metadata</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> This record is immutable and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5a as part of the Style Deviation Scanner feature.
/// </para>
/// </remarks>
public record DeviationScanResult
{
    /// <summary>
    /// Path to the scanned document.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Used to associate results with documents and as part of
    /// the cache key generation.
    /// </remarks>
    public required string DocumentPath { get; init; }

    /// <summary>
    /// All detected deviations in the scanned document or range.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Ordered by document position (start offset). Each deviation
    /// contains full context needed for AI fix generation.
    /// </remarks>
    public required IReadOnlyList<StyleDeviation> Deviations { get; init; }

    /// <summary>
    /// Gets the total number of deviations found.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Computed property for convenience. Used in UI badges and logging.
    /// </remarks>
    public int TotalCount => Deviations.Count;

    /// <summary>
    /// Gets the number of deviations that can be auto-fixed by AI.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Computed by filtering deviations where <see cref="StyleDeviation.IsAutoFixable"/>
    /// is <c>true</c>. Used to show the "Fix All" option availability.
    /// </remarks>
    public int AutoFixableCount => Deviations.Count(d => d.IsAutoFixable);

    /// <summary>
    /// Gets the number of deviations requiring manual review.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Computed by filtering deviations where <see cref="StyleDeviation.IsAutoFixable"/>
    /// is <c>false</c>. These require human judgment to resolve.
    /// </remarks>
    public int ManualOnlyCount => Deviations.Count(d => !d.IsAutoFixable);

    /// <summary>
    /// Timestamp when the scan was performed.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Set at scan completion time. Used for cache age tracking
    /// and display purposes.
    /// </remarks>
    public DateTimeOffset ScannedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Duration of the scan operation.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Measured from scan start to completion. Used for performance
    /// monitoring and logging.
    /// </remarks>
    public TimeSpan ScanDuration { get; init; }

    /// <summary>
    /// Whether the result was retrieved from cache.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Set to <c>true</c> when the result is returned from
    /// <see cref="IMemoryCache"/> rather than a fresh scan.
    /// </remarks>
    public bool IsCached { get; init; }

    /// <summary>
    /// Content hash of the document at scan time, used for cache validation.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Computed as SHA256 hash of document content (first 16 base64 chars).
    /// If document content changes, the hash changes, invalidating the cache.
    /// </remarks>
    public string? ContentHash { get; init; }

    /// <summary>
    /// Style rules version at scan time, used for cache validation.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Retrieved from style sheet metadata. If rules change,
    /// the version changes, invalidating all cached results.
    /// </remarks>
    public string? RulesVersion { get; init; }

    /// <summary>
    /// Groups deviations by category for UI display.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Creates a dictionary keyed by <see cref="StyleDeviation.Category"/>
    /// with deviations sorted by position within each group. Enables tabbed or
    /// collapsible category views in the UI.
    /// </remarks>
    public IReadOnlyDictionary<string, IReadOnlyList<StyleDeviation>> ByCategory =>
        Deviations
            .GroupBy(d => d.Category)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<StyleDeviation>)g.OrderBy(d => d.Location.Start).ToList());

    /// <summary>
    /// Groups deviations by priority for review ordering.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Creates a dictionary keyed by <see cref="StyleDeviation.Priority"/>
    /// with deviations sorted by position within each group. Enables "fix critical first"
    /// workflow in the UI.
    /// </remarks>
    public IReadOnlyDictionary<DeviationPriority, IReadOnlyList<StyleDeviation>> ByPriority =>
        Deviations
            .GroupBy(d => d.Priority)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<StyleDeviation>)g.OrderBy(d => d.Location.Start).ToList());

    /// <summary>
    /// Gets deviations sorted by document position (top to bottom).
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Returns deviations ordered by <see cref="StyleDeviation.Location"/>.Start.
    /// This is the default ordering for sequential review.
    /// </remarks>
    public IReadOnlyList<StyleDeviation> ByPosition =>
        Deviations.OrderBy(d => d.Location.Start).ToList();

    /// <summary>
    /// Creates an empty result for documents with no violations.
    /// </summary>
    /// <param name="documentPath">The document path.</param>
    /// <param name="duration">The scan duration.</param>
    /// <returns>An empty <see cref="DeviationScanResult"/>.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for creating results when no deviations are found.
    /// Sets <see cref="IsCached"/> to <c>false</c> as this is typically a fresh scan.
    /// </remarks>
    public static DeviationScanResult Empty(string documentPath, TimeSpan duration) => new()
    {
        DocumentPath = documentPath,
        Deviations = Array.Empty<StyleDeviation>(),
        ScanDuration = duration,
        IsCached = false
    };

    /// <summary>
    /// Creates an empty result for license-restricted users.
    /// </summary>
    /// <param name="documentPath">The document path.</param>
    /// <returns>An empty <see cref="DeviationScanResult"/> with zero duration.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for returning graceful results when the user
    /// doesn't have the required license tier. UI can detect this and show upgrade prompt.
    /// </remarks>
    public static DeviationScanResult LicenseRequired(string documentPath) => new()
    {
        DocumentPath = documentPath,
        Deviations = Array.Empty<StyleDeviation>(),
        ScanDuration = TimeSpan.Zero,
        IsCached = false
    };
}
