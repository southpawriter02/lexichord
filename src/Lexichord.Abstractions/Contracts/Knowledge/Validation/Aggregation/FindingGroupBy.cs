// =============================================================================
// File: FindingGroupBy.cs
// Project: Lexichord.Abstractions
// Description: Grouping criteria for validation findings.
// =============================================================================
// LOGIC: Defines how validation findings can be grouped for presentation.
//   Each value maps to a specific property on ValidationFinding.
//
// Spec Adaptations:
//   - Entity grouping omitted (no RelatedEntity property on ValidationFinding)
//   - Location grouping omitted (no Location property on ValidationFinding)
//
// v0.6.5i: Validation Result Aggregator (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation.Aggregation;

/// <summary>
/// Defines criteria by which <see cref="ValidationFinding"/> instances can be grouped.
/// </summary>
/// <remarks>
/// <para>
/// Used by <see cref="IResultAggregator.GroupFindings"/> to organize findings
/// into logical buckets for UI display or reporting.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5i as part of the Validation Result Aggregator.
/// </para>
/// </remarks>
public enum FindingGroupBy
{
    /// <summary>
    /// Group by the validator that produced the finding.
    /// </summary>
    /// <remarks>
    /// Groups on <see cref="ValidationFinding.ValidatorId"/>.
    /// </remarks>
    Validator,

    /// <summary>
    /// Group by the severity level of the finding.
    /// </summary>
    /// <remarks>
    /// Groups on <see cref="ValidationFinding.Severity"/> (converted to string key).
    /// </remarks>
    Severity,

    /// <summary>
    /// Group by the machine-readable finding code.
    /// </summary>
    /// <remarks>
    /// Groups on <see cref="ValidationFinding.Code"/>.
    /// </remarks>
    Code
}
