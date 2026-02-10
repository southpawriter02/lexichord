// =============================================================================
// File: ContextIssue.cs
// Project: Lexichord.Abstractions
// Description: Represents an issue found during pre-generation validation.
// =============================================================================
// LOGIC: Each issue captures a specific problem found in the knowledge context
//   or user request, with a severity level, optional entity/axiom references,
//   and a suggested resolution. Issues are aggregated into a PreValidationResult
//   which determines whether generation can proceed.
//
// v0.6.6f: Pre-Generation Validator (CKVS Phase 3b)
// Dependencies: KnowledgeEntity (v0.4.5e), Axiom (v0.4.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// An issue found during pre-generation validation of knowledge context.
/// </summary>
/// <remarks>
/// <para>
/// Issues are produced by <see cref="IContextConsistencyChecker"/> and the
/// axiom compliance check in <see cref="IPreGenerationValidator"/>. Each
/// issue has a severity that determines its impact:
/// </para>
/// <list type="bullet">
///   <item><see cref="ContextIssueSeverity.Error"/>: Blocks generation entirely.</item>
///   <item><see cref="ContextIssueSeverity.Warning"/>: Warns but allows generation.</item>
///   <item><see cref="ContextIssueSeverity.Info"/>: Informational, no impact.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.6.6f as part of the Pre-Generation Validator.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var issue = new ContextIssue
/// {
///     Code = ContextIssueCodes.ConflictingEntities,
///     Message = "Multiple entities with name 'UserService' in context",
///     Severity = ContextIssueSeverity.Warning,
///     Resolution = "Consider which entity is most relevant"
/// };
/// </code>
/// </example>
public record ContextIssue
{
    /// <summary>
    /// Machine-readable issue code from <see cref="ContextIssueCodes"/>.
    /// </summary>
    /// <value>
    /// A <c>PREVAL_*</c> prefixed string identifying the issue type.
    /// </value>
    /// <remarks>
    /// LOGIC: Enables programmatic matching and filtering of issues.
    /// Use <see cref="ContextIssueCodes"/> constants for comparison.
    /// </remarks>
    public required string Code { get; init; }

    /// <summary>
    /// Human-readable description of the issue.
    /// </summary>
    /// <value>
    /// A sentence describing what was found and which entities are
    /// involved, suitable for display to the user.
    /// </value>
    public required string Message { get; init; }

    /// <summary>
    /// Severity level of this issue.
    /// </summary>
    /// <value>
    /// Defaults to <see cref="ContextIssueSeverity.Warning"/>.
    /// <see cref="ContextIssueSeverity.Error"/> issues block generation.
    /// </value>
    public ContextIssueSeverity Severity { get; init; } = ContextIssueSeverity.Warning;

    /// <summary>
    /// The entity related to this issue, if applicable.
    /// </summary>
    /// <value>
    /// The <see cref="KnowledgeEntity"/> that caused or is affected by
    /// this issue, or <c>null</c> if the issue is not entity-specific.
    /// </value>
    public KnowledgeEntity? RelatedEntity { get; init; }

    /// <summary>
    /// The axiom related to this issue, if applicable.
    /// </summary>
    /// <value>
    /// The <see cref="Axiom"/> that was violated, or <c>null</c> if the
    /// issue is not axiom-related.
    /// </value>
    public Axiom? RelatedAxiom { get; init; }

    /// <summary>
    /// Suggested resolution for this issue.
    /// </summary>
    /// <value>
    /// A human-readable suggestion for how to fix the issue, or <c>null</c>
    /// if no specific resolution is available.
    /// </value>
    public string? Resolution { get; init; }
}
