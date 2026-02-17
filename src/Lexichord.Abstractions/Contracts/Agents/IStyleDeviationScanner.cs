// -----------------------------------------------------------------------
// <copyright file="IStyleDeviationScanner.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.Editor;

namespace Lexichord.Abstractions.Contracts.Agents;

/// <summary>
/// Scans documents for style deviations that can be addressed by the Tuning Agent.
/// Bridges the linting infrastructure with AI-powered fix generation by providing
/// enriched context for each violation.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The Style Deviation Scanner wraps <see cref="ILintingOrchestrator"/> (v0.2.3a)
/// to transform raw lint violations into enriched <see cref="StyleDeviation"/> objects with:
/// <list type="bullet">
///   <item><description>Surrounding text context for AI understanding</description></item>
///   <item><description>Auto-fixability classification</description></item>
///   <item><description>Priority mapping from severity</description></item>
///   <item><description>Complete rule details</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Caching:</b> Results are cached using content hash and rules version for validation.
/// Cache is automatically invalidated when document content or style rules change.
/// </para>
/// <para>
/// <b>Real-time Updates:</b> Subscribes to linting events for incremental deviation detection.
/// UI components can subscribe to <see cref="DeviationsDetected"/> for live updates.
/// </para>
/// <para>
/// <b>License Requirement:</b> Requires WriterPro tier or higher. Returns empty results
/// for unlicensed users.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe and support concurrent scans.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5a as part of the Tuning Agent feature set.
/// </para>
/// </remarks>
public interface IStyleDeviationScanner
{
    /// <summary>
    /// Scans the entire document for style deviations.
    /// </summary>
    /// <param name="documentPath">Absolute path to the document to scan.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>
    /// A <see cref="DeviationScanResult"/> containing all detected deviations,
    /// or an empty result if unlicensed or no violations found.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> The scan workflow is:
    /// <list type="number">
    ///   <item><description>Check license tier (WriterPro required)</description></item>
    ///   <item><description>Generate cache key from document path, content hash, and rules version</description></item>
    ///   <item><description>Return cached result if available and valid</description></item>
    ///   <item><description>Get raw violations from linting orchestrator</description></item>
    ///   <item><description>Enrich each violation with context and rule details</description></item>
    ///   <item><description>Cache and return the result</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Performance:</b> Cold scans of 10,000 word documents should complete in &lt;500ms.
    /// Cached results should return in &lt;50ms.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown if <paramref name="documentPath"/> is null or whitespace.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the scanner has been disposed.</exception>
    Task<DeviationScanResult> ScanDocumentAsync(
        string documentPath,
        CancellationToken ct = default);

    /// <summary>
    /// Scans a specific range within the document.
    /// </summary>
    /// <param name="documentPath">Absolute path to the document.</param>
    /// <param name="range">The text span to scan (start offset and length).</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>
    /// A <see cref="DeviationScanResult"/> containing deviations within the specified range,
    /// or an empty result if unlicensed or no violations found.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Range scans are not cached as they are typically for small, focused areas.
    /// Only violations whose locations overlap with the specified range are returned.
    /// </para>
    /// <para>
    /// <b>Use Cases:</b>
    /// <list type="bullet">
    ///   <item><description>Scanning user-selected text</description></item>
    ///   <item><description>Scanning a single paragraph</description></item>
    ///   <item><description>Pre-fix validation</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown if <paramref name="documentPath"/> is null or whitespace.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the scanner has been disposed.</exception>
    Task<DeviationScanResult> ScanRangeAsync(
        string documentPath,
        TextSpan range,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the cached scan result for a document if available.
    /// </summary>
    /// <param name="documentPath">Absolute path to the document.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>
    /// The cached <see cref="DeviationScanResult"/> if available and still valid,
    /// or <c>null</c> if no valid cache entry exists.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Checks the memory cache for a valid result. Cache entries are keyed by
    /// document path, content hash, and rules version. If content or rules have changed,
    /// <c>null</c> is returned.
    /// </para>
    /// <para>
    /// <b>Use Case:</b> UI components can check for cached results before deciding whether
    /// to show stale data or trigger a fresh scan.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the scanner has been disposed.</exception>
    Task<DeviationScanResult?> GetCachedResultAsync(
        string documentPath,
        CancellationToken ct = default);

    /// <summary>
    /// Invalidates the cache for a specific document.
    /// </summary>
    /// <param name="documentPath">Absolute path to the document.</param>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Called when document content changes. The next scan will perform
    /// a fresh analysis. Typically triggered by <see cref="IEditorService.DocumentChanged"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown if <paramref name="documentPath"/> is null or whitespace.</exception>
    void InvalidateCache(string documentPath);

    /// <summary>
    /// Invalidates all cached results.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Called when global style rules change. All documents will require
    /// fresh scans. Typically triggered by StyleRulesChangedEvent.
    /// </para>
    /// </remarks>
    void InvalidateAllCaches();

    /// <summary>
    /// Event raised when new deviations are detected from real-time linting.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Fired when the scanner receives <see cref="LintingCompletedEvent"/>
    /// from the linting orchestrator. UI components subscribe to receive live updates
    /// without polling.
    /// </para>
    /// <para>
    /// <b>Threading:</b> Event handlers may be invoked on background threads. UI code
    /// should dispatch to the main thread as needed.
    /// </para>
    /// </remarks>
    event EventHandler<DeviationsDetectedEventArgs>? DeviationsDetected;
}
