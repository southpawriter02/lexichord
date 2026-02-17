// =============================================================================
// File: IUnifiedValidationService.cs
// Project: Lexichord.Abstractions
// Description: Service interface for aggregating validation from multiple sources.
// =============================================================================
// LOGIC: Provides a unified entry point for validating documents against all
//   available validators (Style Linter, Grammar Linter, CKVS Validation Engine).
//   Results are normalized to UnifiedIssue format, deduplicated, and filtered
//   based on license tier and user preferences.
//
// v0.7.5f: Issue Aggregator (Unified Validation Feature)
// =============================================================================

using Lexichord.Abstractions.Contracts.Editor;

namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Service that aggregates validation results from multiple independent validators
/// into a unified set of issues with consistent severity and fix information.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This service orchestrates validation from three sources:
/// <list type="bullet">
///   <item><description>Style Linter — Style guide violations via <c>IStyleDeviationScanner</c></description></item>
///   <item><description>Grammar Linter — Grammar/spelling issues (future)</description></item>
///   <item><description>CKVS Validation Engine — Knowledge/axiom validation via <c>IValidationEngine</c></description></item>
/// </list>
/// </para>
/// <para>
/// <b>License Gating:</b>
/// <list type="bullet">
///   <item><description>Core: Style Linter only</description></item>
///   <item><description>WriterPro: Style + Grammar Linter</description></item>
///   <item><description>Teams/Enterprise: All validators including CKVS</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Deduplication:</b> When the same issue is detected by multiple validators
/// (e.g., passive voice found by both Style and Grammar linters), the service
/// keeps the highest-severity instance and marks others as duplicates.
/// </para>
/// <para>
/// <b>Caching:</b> Results are cached by document path with configurable TTL
/// to avoid redundant validation on unchanged documents.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe for concurrent
/// validation requests.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5f as part of the Unified Validation feature.
/// </para>
/// </remarks>
public interface IUnifiedValidationService
{
    /// <summary>
    /// Validates a document using all available validators based on license tier.
    /// </summary>
    /// <param name="documentPath">Path to the document being validated.</param>
    /// <param name="content">The document content to validate.</param>
    /// <param name="options">Validation options controlling which validators run and how results are filtered.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Combined validation result from all validators.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="documentPath"/>, <paramref name="content"/>,
    /// or <paramref name="options"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> The validation pipeline:
    /// <list type="number">
    ///   <item><description>Check cache for existing result</description></item>
    ///   <item><description>Determine available validators by license tier</description></item>
    ///   <item><description>Execute validators (parallel or sequential per options)</description></item>
    ///   <item><description>Convert results to <see cref="UnifiedIssue"/> via factory</description></item>
    ///   <item><description>Deduplicate overlapping issues if enabled</description></item>
    ///   <item><description>Apply severity/category filters</description></item>
    ///   <item><description>Cache result and raise <see cref="ValidationCompleted"/> event</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    Task<UnifiedValidationResult> ValidateAsync(
        string documentPath,
        string content,
        UnifiedValidationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Validates a specific range within a document.
    /// </summary>
    /// <param name="documentPath">Path to the document being validated.</param>
    /// <param name="content">The full document content.</param>
    /// <param name="range">The text range to validate.</param>
    /// <param name="options">Validation options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validation result filtered to issues within the specified range.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="documentPath"/>, <paramref name="content"/>,
    /// or <paramref name="options"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Performs full document validation, then filters results
    /// to only include issues whose location falls within the specified range.
    /// This is useful for incremental validation during editing.
    /// </para>
    /// </remarks>
    Task<UnifiedValidationResult> ValidateRangeAsync(
        string documentPath,
        string content,
        TextSpan range,
        UnifiedValidationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the cached validation result for a document if available.
    /// </summary>
    /// <param name="documentPath">Path to the document.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Cached result or null if not cached or expired.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="documentPath"/> is null or whitespace.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Returns the cached result without re-running validation.
    /// Useful for UI refresh when document content hasn't changed.
    /// </remarks>
    Task<UnifiedValidationResult?> GetCachedResultAsync(
        string documentPath,
        CancellationToken ct = default);

    /// <summary>
    /// Invalidates the cached validation result for a document.
    /// </summary>
    /// <param name="documentPath">Path to the document.</param>
    /// <remarks>
    /// <b>LOGIC:</b> Called when document content changes to ensure
    /// the next validation uses fresh data.
    /// </remarks>
    void InvalidateCache(string documentPath);

    /// <summary>
    /// Invalidates all cached validation results.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Called when global configuration changes (e.g., style rules)
    /// that affect all documents.
    /// </remarks>
    void InvalidateAllCaches();

    /// <summary>
    /// Event raised when validation completes for a document.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Published after each successful validation, enabling
    /// UI components to update their displays with fresh results.
    /// </remarks>
    event EventHandler<ValidationCompletedEventArgs>? ValidationCompleted;
}
