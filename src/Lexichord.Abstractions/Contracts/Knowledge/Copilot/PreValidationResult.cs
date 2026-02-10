// =============================================================================
// File: PreValidationResult.cs
// Project: Lexichord.Abstractions
// Description: Result of pre-generation validation of knowledge context.
// =============================================================================
// LOGIC: Aggregates all issues found during pre-generation validation into
//   a single result. The CanProceed flag is the primary decision point—if
//   false, the Co-pilot must not invoke the LLM. Blocking issues and warnings
//   are computed from the Issues list using LINQ projections.
//
// v0.6.6f: Pre-Generation Validator (CKVS Phase 3b)
// Dependencies: ContextIssue (v0.6.6f), ContextModification (v0.6.6f)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Result of pre-generation validation.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="PreValidationResult"/> is the output of
/// <see cref="IPreGenerationValidator.ValidateAsync"/>. It aggregates all
/// issues found during validation and provides a clear proceed/block decision
/// via <see cref="CanProceed"/>.
/// </para>
/// <para>
/// <b>Decision Logic:</b>
/// <list type="bullet">
///   <item><see cref="CanProceed"/> is <c>true</c> when no
///     <see cref="ContextIssueSeverity.Error"/> issues exist.</item>
///   <item><see cref="BlockingIssues"/> contains only error-level issues.</item>
///   <item><see cref="Warnings"/> contains warning-level issues.</item>
///   <item><see cref="SuggestedModifications"/> provides optional fix suggestions.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6f as part of the Pre-Generation Validator.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await validator.ValidateAsync(request, context, ct);
/// if (!result.CanProceed)
/// {
///     // Show blocking message to user
///     ShowError(result.UserMessage);
///     return;
/// }
/// if (result.Warnings.Count > 0)
/// {
///     ShowWarnings(result.Warnings);
/// }
/// // Proceed with LLM generation
/// </code>
/// </example>
public record PreValidationResult
{
    /// <summary>
    /// Whether LLM generation can proceed.
    /// </summary>
    /// <value>
    /// <c>true</c> if no <see cref="ContextIssueSeverity.Error"/> issues
    /// were found; otherwise <c>false</c>.
    /// </value>
    /// <remarks>
    /// LOGIC: This is the primary decision flag. When false, the Co-pilot
    /// must not invoke the LLM and should display <see cref="UserMessage"/>
    /// to the user.
    /// </remarks>
    public bool CanProceed { get; init; }

    /// <summary>
    /// All issues found during validation.
    /// </summary>
    /// <value>
    /// A read-only list of all issues across all severity levels.
    /// Never null; empty list if validation passed cleanly.
    /// </value>
    public required IReadOnlyList<ContextIssue> Issues { get; init; }

    /// <summary>
    /// Issues that block generation (error-level only).
    /// </summary>
    /// <value>
    /// A filtered view of <see cref="Issues"/> containing only
    /// <see cref="ContextIssueSeverity.Error"/> entries.
    /// </value>
    /// <remarks>
    /// LOGIC: Computed property — filters Issues on each access.
    /// Typically called once after validation to build the user message.
    /// </remarks>
    public IReadOnlyList<ContextIssue> BlockingIssues =>
        Issues.Where(i => i.Severity == ContextIssueSeverity.Error).ToList();

    /// <summary>
    /// Non-blocking warnings found during validation.
    /// </summary>
    /// <value>
    /// A filtered view of <see cref="Issues"/> containing only
    /// <see cref="ContextIssueSeverity.Warning"/> entries.
    /// </value>
    /// <remarks>
    /// LOGIC: Computed property — filters Issues on each access.
    /// Warnings are shown to the user but do not prevent generation.
    /// </remarks>
    public IReadOnlyList<ContextIssue> Warnings =>
        Issues.Where(i => i.Severity == ContextIssueSeverity.Warning).ToList();

    /// <summary>
    /// Optional suggested modifications to resolve found issues.
    /// </summary>
    /// <value>
    /// A list of <see cref="ContextModification"/> suggestions, or
    /// <c>null</c> if no specific modifications are suggested.
    /// </value>
    /// <remarks>
    /// LOGIC: Advisory only — modifications are never applied automatically.
    /// The caller may use these to offer "fix" actions to the user.
    /// </remarks>
    public IReadOnlyList<ContextModification>? SuggestedModifications { get; init; }

    /// <summary>
    /// User-facing message summarizing the validation outcome.
    /// </summary>
    /// <value>
    /// A human-readable message when <see cref="CanProceed"/> is <c>false</c>,
    /// or <c>null</c> when validation passes.
    /// </value>
    public string? UserMessage { get; init; }

    /// <summary>
    /// Creates a passing result with no issues.
    /// </summary>
    /// <returns>A <see cref="PreValidationResult"/> with <see cref="CanProceed"/> set to <c>true</c>.</returns>
    /// <remarks>
    /// LOGIC: Convenience factory for the common case where validation
    /// finds no problems. Used when context is clean or empty (with
    /// warning handled separately).
    /// </remarks>
    public static PreValidationResult Pass() => new()
    {
        CanProceed = true,
        Issues = []
    };

    /// <summary>
    /// Creates a blocking result with the given issues and message.
    /// </summary>
    /// <param name="issues">The issues that caused the block.</param>
    /// <param name="message">User-facing message explaining why generation is blocked.</param>
    /// <returns>A <see cref="PreValidationResult"/> with <see cref="CanProceed"/> set to <c>false</c>.</returns>
    /// <remarks>
    /// LOGIC: Convenience factory for creating blocking results. The
    /// message should summarize the blocking issues for display to the user.
    /// </remarks>
    public static PreValidationResult Block(
        IReadOnlyList<ContextIssue> issues,
        string message) => new()
    {
        CanProceed = false,
        Issues = issues,
        UserMessage = message
    };
}
