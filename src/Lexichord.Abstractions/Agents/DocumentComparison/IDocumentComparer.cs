// -----------------------------------------------------------------------
// <copyright file="IDocumentComparer.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Interface for semantic document comparison (v0.7.6d).
//   Provides methods for comparing document versions using a hybrid
//   DiffPlex + LLM approach for semantic analysis.
//
//   Methods:
//     - CompareAsync: Compare two documents by file path
//     - CompareContentAsync: Compare two content strings directly
//     - CompareWithGitVersionAsync: Compare document with git history version
//     - GenerateChangeSummaryAsync: Generate natural language summary
//     - GetTextDiff: Get raw text diff (no LLM)
//
//   NOTE: This interface does NOT extend IAgent. It is a standalone service
//   similar to ISummaryExporter (v0.7.6c).
//
//   Introduced in: v0.7.6d as part of Document Comparison
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.DocumentComparison;

/// <summary>
/// Interface for comparing document versions with semantic analysis.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="IDocumentComparer"/> interface provides a hybrid
/// comparison system that combines:
/// <list type="bullet">
/// <item><description><b>DiffPlex text diff</b>: Fast, deterministic line-by-line comparison</description></item>
/// <item><description><b>LLM semantic analysis</b>: Understanding meaning, categorizing changes, scoring significance</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Comparison Methods:</b>
/// <list type="table">
/// <listheader>
/// <term>Method</term>
/// <description>Use Case</description>
/// </listheader>
/// <item>
/// <term><see cref="CompareAsync"/></term>
/// <description>Compare two documents by file path</description>
/// </item>
/// <item>
/// <term><see cref="CompareContentAsync"/></term>
/// <description>Compare two content strings directly (no file I/O)</description>
/// </item>
/// <item>
/// <term><see cref="CompareWithGitVersionAsync"/></term>
/// <description>Compare document with a git history version</description>
/// </item>
/// </list>
/// </para>
/// <para>
/// <b>Lightweight Operations:</b>
/// <list type="bullet">
/// <item><description><see cref="GenerateChangeSummaryAsync"/>: Generate natural language summary from existing result</description></item>
/// <item><description><see cref="GetTextDiff"/>: Get raw text diff without LLM (fast, deterministic)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>License Gating:</b>
/// All comparison methods require WriterPro tier via <c>FeatureCodes.DocumentComparison</c>.
/// Lower tiers receive <see cref="ComparisonResult.Failed"/> with an upgrade message.
/// </para>
/// <para>
/// <b>Thread safety:</b> Implementations must be thread-safe as comparisons may be
/// invoked from multiple contexts (UI thread, background tasks).
/// </para>
/// <para>
/// <b>Design Note:</b> This interface does NOT extend <c>IAgent</c>. Like
/// <see cref="SummaryExport.ISummaryExporter"/> (v0.7.6c), it is a standalone
/// service that uses agents but is not an agent itself.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6d as part of the Document Comparison feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Compare two files
/// var result = await comparer.CompareAsync(
///     "/docs/spec-v1.md",
///     "/docs/spec-v2.md",
///     new ComparisonOptions { SignificanceThreshold = 0.3 });
///
/// if (result.Success)
/// {
///     Console.WriteLine($"Summary: {result.Summary}");
///     Console.WriteLine($"Changes: {result.Changes.Count}");
///     foreach (var change in result.Changes.Where(c => c.SignificanceLevel >= ChangeSignificance.High))
///     {
///         Console.WriteLine($"  [{change.Category}] {change.Description}");
///     }
/// }
///
/// // Compare with git version
/// var result = await comparer.CompareWithGitVersionAsync(
///     "/docs/spec.md",
///     "HEAD~1");
///
/// // Quick text diff (no LLM)
/// var diff = comparer.GetTextDiff(originalContent, newContent);
/// </code>
/// </example>
/// <seealso cref="ComparisonOptions"/>
/// <seealso cref="ComparisonResult"/>
/// <seealso cref="DocumentChange"/>
public interface IDocumentComparer
{
    /// <summary>
    /// Compares two document versions by file path.
    /// </summary>
    /// <param name="originalPath">Path to the original (older) document. Must exist.</param>
    /// <param name="newPath">Path to the new (current) document. Must exist.</param>
    /// <param name="options">Comparison configuration options. Uses <see cref="ComparisonOptions.Default"/> if <c>null</c>.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>
    /// A task that completes with the <see cref="ComparisonResult"/> containing
    /// the summary, changes, and metrics.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="originalPath"/> or <paramref name="newPath"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Loads both documents via <c>IFileService.LoadAsync</c>, then delegates
    /// to <see cref="CompareContentAsync"/>. If either file does not exist, returns a
    /// <see cref="ComparisonResult.Failed"/> result.
    /// </para>
    /// <para>
    /// <b>Events:</b> Publishes <c>DocumentComparisonStartedEvent</c> before analysis
    /// and <c>DocumentComparisonCompletedEvent</c> or <c>DocumentComparisonFailedEvent</c>
    /// after completion.
    /// </para>
    /// </remarks>
    Task<ComparisonResult> CompareAsync(
        string originalPath,
        string newPath,
        ComparisonOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Compares two content strings directly.
    /// </summary>
    /// <param name="originalContent">Original document content.</param>
    /// <param name="newContent">New document content.</param>
    /// <param name="options">Comparison configuration options. Uses <see cref="ComparisonOptions.Default"/> if <c>null</c>.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>
    /// A task that completes with the <see cref="ComparisonResult"/> containing
    /// the summary, changes, and metrics.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="originalContent"/> or <paramref name="newContent"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Core comparison method that performs the hybrid DiffPlex + LLM analysis:
    /// <list type="number">
    /// <item><description>Quick identical check (returns early if documents match)</description></item>
    /// <item><description>Generate text diff via DiffPlex for context</description></item>
    /// <item><description>Invoke LLM with comparison prompt template</description></item>
    /// <item><description>Parse JSON response into <see cref="DocumentChange"/> records</description></item>
    /// <item><description>Filter and order changes per options</description></item>
    /// <item><description>Identify related changes (if enabled)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Events:</b> Publishes lifecycle events for observability.
    /// </para>
    /// </remarks>
    Task<ComparisonResult> CompareContentAsync(
        string originalContent,
        string newContent,
        ComparisonOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Compares a document with a git history version.
    /// </summary>
    /// <param name="documentPath">Path to the current document.</param>
    /// <param name="gitRef">Git reference (e.g., "HEAD~1", "abc123", "v1.0", "main").</param>
    /// <param name="options">Comparison configuration options. Uses <see cref="ComparisonOptions.Default"/> if <c>null</c>.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>
    /// A task that completes with the <see cref="ComparisonResult"/> containing
    /// the summary, changes, and metrics.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="documentPath"/> or <paramref name="gitRef"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Retrieves the historical version using <c>git show {ref}:{path}</c>,
    /// then delegates to <see cref="CompareContentAsync"/>. Returns a failed result if:
    /// <list type="bullet">
    /// <item><description>Git is not available</description></item>
    /// <item><description>The document is not in a git repository</description></item>
    /// <item><description>The reference does not exist</description></item>
    /// <item><description>The file did not exist at the specified reference</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Version Labels:</b> Automatically sets <see cref="ComparisonOptions.OriginalVersionLabel"/>
    /// to the git ref and <see cref="ComparisonOptions.NewVersionLabel"/> to "Current".
    /// </para>
    /// </remarks>
    Task<ComparisonResult> CompareWithGitVersionAsync(
        string documentPath,
        string gitRef,
        ComparisonOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a natural language summary of changes.
    /// </summary>
    /// <param name="comparison">The comparison result to summarize.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>
    /// A task that completes with a natural language summary suitable for
    /// notifications or quick overviews.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="comparison"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Generates a more detailed or differently-formatted summary
    /// from an existing comparison result. Useful for:
    /// <list type="bullet">
    /// <item><description>Notification text</description></item>
    /// <item><description>Email summaries</description></item>
    /// <item><description>Changelog entries</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// For results with no changes, returns "No significant changes detected."
    /// </para>
    /// </remarks>
    Task<string> GenerateChangeSummaryAsync(
        ComparisonResult comparison,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a text diff between two documents.
    /// </summary>
    /// <param name="originalContent">Original content.</param>
    /// <param name="newContent">New content.</param>
    /// <returns>
    /// A unified diff format string, or an empty string if documents are identical.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Lightweight operation using DiffPlex only (no LLM).
    /// Returns a standard unified diff format suitable for display or storage.
    /// </para>
    /// <para>
    /// <b>Performance:</b> This method is synchronous and fast. Use it when
    /// you only need the raw diff without semantic analysis.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var diff = comparer.GetTextDiff(
    ///     "Line 1\nLine 2\nLine 3",
    ///     "Line 1\nLine 2 modified\nLine 3");
    ///
    /// // Output:
    /// // --- Original
    /// // +++ New
    /// // @@ @@
    /// //   Line 1
    /// // - Line 2
    /// // + Line 2 modified
    /// //   Line 3
    /// </code>
    /// </example>
    string GetTextDiff(string originalContent, string newContent);
}
