// =============================================================================
// File: ILinterIntegration.cs
// Project: Lexichord.Abstractions
// Description: Main integration interface bridging validation and linting.
// =============================================================================
// LOGIC: Orchestrates parallel execution of both engines and returns a
//   unified result. Also provides a combined "fix all" workflow.
//
// SPEC ADAPTATION:
//   - Document parameter → (string documentId, string content) pair
//   - ApplyUnifiedFixAsync removed (single-fix application is handled
//     by the source-specific engine directly)
//   - FixAllOptions → IReadOnlyList<UnifiedFix> (simplified to a list)
//
// v0.6.5j: Linter Integration (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;

/// <summary>
/// Bridges the CKVS Validation Engine with Lexichord's style linter.
/// </summary>
/// <remarks>
/// <para>
/// This is the primary entry point for consumers who want a unified view
/// of all document findings. It runs validation and linting in parallel,
/// normalizes the results, and provides a combined fix workflow.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe.
/// </para>
/// <para>
/// <b>Dependencies:</b>
/// <list type="bullet">
///   <item><see cref="IValidationEngine"/> — CKVS schema/axiom/consistency checks</item>
///   <item><see cref="IStyleEngine"/> — Style linter (v0.3.x)</item>
///   <item><see cref="IUnifiedFindingAdapter"/> — Finding normalization</item>
///   <item><see cref="ICombinedFixWorkflow"/> — Fix ordering and conflict detection</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5j as part of the Linter Integration.
/// </para>
/// </remarks>
public interface ILinterIntegration
{
    /// <summary>
    /// Gets combined findings from both the validation engine and the linter.
    /// </summary>
    /// <param name="documentId">Unique identifier for the document being analyzed.</param>
    /// <param name="content">The document text content to analyze.</param>
    /// <param name="options">Options controlling which sources and severities to include.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="UnifiedFindingResult"/> containing normalized findings from both sources.
    /// </returns>
    /// <remarks>
    /// LOGIC: Runs validation and linting in parallel via Task.WhenAll.
    /// Failures in one source do not block the other; partial results are returned.
    /// </remarks>
    Task<UnifiedFindingResult> GetUnifiedFindingsAsync(
        string documentId,
        string content,
        UnifiedFindingOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Applies all auto-fixable issues from a list of fixes.
    /// </summary>
    /// <param name="content">The current document content.</param>
    /// <param name="fixes">The fixes to apply.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="FixApplicationResult"/> with applied/failed counts.
    /// </returns>
    /// <remarks>
    /// LOGIC: Delegates to <see cref="ICombinedFixWorkflow"/> for ordering
    /// and conflict detection before application.
    /// </remarks>
    Task<FixApplicationResult> ApplyAllFixesAsync(
        string content,
        IReadOnlyList<UnifiedFix> fixes,
        CancellationToken ct = default);
}
