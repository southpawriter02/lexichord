// =============================================================================
// File: FindingFilter.cs
// Project: Lexichord.Abstractions
// Description: Filter criteria for validation findings.
// =============================================================================
// LOGIC: Defines filter criteria that can be applied to a list of
//   ValidationFinding instances. Each property is optional — null means
//   "no constraint." Multiple non-null properties are AND-combined.
//
// Spec Adaptations:
//   - ValidatorNames → ValidatorIds (codebase uses ValidatorId, not ValidatorName)
//   - EntityTypes omitted (ValidationFinding has no RelatedEntity property)
//   - LocationRange omitted (ValidationFinding has no Location property)
//
// v0.6.5i: Validation Result Aggregator (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation.Aggregation;

/// <summary>
/// Filter criteria for narrowing a set of <see cref="ValidationFinding"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// All properties are optional. When a property is <c>null</c>, that criterion
/// is not applied. When multiple properties are non-null, they are AND-combined —
/// a finding must satisfy every specified criterion to be included.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5i as part of the Validation Result Aggregator.
/// </para>
/// </remarks>
public record FindingFilter
{
    /// <summary>
    /// Minimum severity level to include in results.
    /// </summary>
    /// <remarks>
    /// Findings with a severity value greater than or equal to this threshold
    /// are included. For example, <see cref="ValidationSeverity.Warning"/> includes
    /// both Warning and Error findings but excludes Info.
    /// </remarks>
    public ValidationSeverity? MinSeverity { get; init; }

    /// <summary>
    /// Set of validator IDs to include. Only findings produced by these
    /// validators are included.
    /// </summary>
    /// <remarks>
    /// Matches against <see cref="ValidationFinding.ValidatorId"/>.
    /// </remarks>
    public IReadOnlySet<string>? ValidatorIds { get; init; }

    /// <summary>
    /// Set of finding codes to include. Only findings with these machine-readable
    /// codes are included.
    /// </summary>
    /// <remarks>
    /// Matches against <see cref="ValidationFinding.Code"/>.
    /// </remarks>
    public IReadOnlySet<string>? Codes { get; init; }

    /// <summary>
    /// When <c>true</c>, only findings with a non-null
    /// <see cref="ValidationFinding.SuggestedFix"/> are included.
    /// </summary>
    public bool? FixableOnly { get; init; }
}
