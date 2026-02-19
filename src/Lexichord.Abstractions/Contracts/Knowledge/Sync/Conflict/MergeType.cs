// =============================================================================
// File: MergeType.cs
// Project: Lexichord.Abstractions
// Description: Defines how a merge operation was performed.
// =============================================================================
// LOGIC: When a merge is completed, the MergeType indicates how the
//   merged value was created. This provides transparency about the
//   merge process for auditing and debugging purposes.
//
// v0.7.6h: Conflict Resolver (CKVS Phase 4c)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict;

/// <summary>
/// Indicates how a merge operation was performed.
/// </summary>
/// <remarks>
/// <para>
/// Describes the mechanism used to create the merged result:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="Selection"/>: Direct selection of one value.</description></item>
///   <item><description><see cref="Intelligent"/>: Intelligent combination of values.</description></item>
///   <item><description><see cref="Weighted"/>: Weighted merge based on confidence.</description></item>
///   <item><description><see cref="Manual"/>: Manual user selection.</description></item>
///   <item><description><see cref="Temporal"/>: Use most recent version.</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6h as part of the Conflict Resolver module.
/// </para>
/// </remarks>
public enum MergeType
{
    /// <summary>
    /// Direct selection of one value.
    /// </summary>
    /// <remarks>
    /// LOGIC: One of the values was chosen directly without modification.
    /// Either DocumentFirst or GraphFirst strategy was applied.
    /// The selected value is used as-is for the merged result.
    /// </remarks>
    Selection = 0,

    /// <summary>
    /// Intelligent combination of values.
    /// </summary>
    /// <remarks>
    /// LOGIC: Both values were analyzed and combined intelligently.
    /// The merge algorithm understood the value types and created
    /// a unified result that incorporates information from both sources.
    /// </remarks>
    Intelligent = 1,

    /// <summary>
    /// Weighted merge based on confidence.
    /// </summary>
    /// <remarks>
    /// LOGIC: Values were combined with weighting based on confidence scores.
    /// Higher-confidence portions of each value contribute more to the result.
    /// Suitable for numeric values or values with associated confidence metrics.
    /// </remarks>
    Weighted = 2,

    /// <summary>
    /// Manual user selection.
    /// </summary>
    /// <remarks>
    /// LOGIC: The user manually chose or edited the merged value.
    /// Automatic merge was not possible or was overridden by the user.
    /// Provides full user control over the final result.
    /// </remarks>
    Manual = 3,

    /// <summary>
    /// Use most recent version.
    /// </summary>
    /// <remarks>
    /// LOGIC: The value with the more recent timestamp was selected.
    /// Temporal precedence determined the winner.
    /// Relies on accurate timestamp metadata from both sources.
    /// </remarks>
    Temporal = 4
}
