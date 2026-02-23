// -----------------------------------------------------------------------
// <copyright file="ConflictStrategy.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Enum defining strategies for resolving synchronization conflicts
//   between document content and knowledge graph entities. Each strategy
//   maps to the existing ConflictResolutionStrategy enum from the sync
//   infrastructure for ISyncService delegation.
//
// v0.7.7g: Sync Step Type (CKVS Phase 4d)
// Dependencies: ConflictResolutionStrategy (v0.7.6e)
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.Knowledge.Sync;

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// Strategy for resolving synchronization conflicts between a document
/// and the knowledge graph within a sync workflow step.
/// </summary>
/// <remarks>
/// <para>
/// When a sync step detects that document and graph state diverge, the
/// conflict strategy determines how to proceed. This enum provides a
/// workflow-specific abstraction over the infrastructure-level
/// <see cref="ConflictResolutionStrategy"/>.
/// </para>
/// <para>
/// <b>License Gating:</b>
/// <list type="bullet">
///   <item><description>WriterPro: <see cref="PreferDocument"/> and <see cref="PreferNewer"/> only.</description></item>
///   <item><description>Teams: All strategies.</description></item>
///   <item><description>Enterprise: All strategies + custom conflict resolution.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7g as part of Sync Step Type (CKVS Phase 4d).
/// </para>
/// </remarks>
public enum ConflictStrategy
{
    /// <summary>
    /// Keep the document version as authoritative.
    /// </summary>
    /// <remarks>
    /// LOGIC: Document content overwrites graph state. Graph values are
    /// replaced with extracted document data. Use when the document
    /// represents the latest intended state.
    /// </remarks>
    PreferDocument = 0,

    /// <summary>
    /// Keep the graph version as authoritative.
    /// </summary>
    /// <remarks>
    /// LOGIC: Graph state is preserved. Document extractions are discarded
    /// where conflicts exist. Use when the graph has been curated or verified.
    /// </remarks>
    PreferGraph = 1,

    /// <summary>
    /// Keep the most recently modified version.
    /// </summary>
    /// <remarks>
    /// LOGIC: Compares modification timestamps to determine the authoritative
    /// version. Default strategy for most sync operations.
    /// </remarks>
    PreferNewer = 2,

    /// <summary>
    /// Attempt to merge both versions intelligently.
    /// </summary>
    /// <remarks>
    /// LOGIC: Tries to combine document and graph values. For collections,
    /// performs union. For text, attempts diff-based merge. Falls back to
    /// <see cref="Manual"/> if merge is not possible.
    /// </remarks>
    Merge = 3,

    /// <summary>
    /// Fail the sync operation when a conflict is detected.
    /// </summary>
    /// <remarks>
    /// LOGIC: No automatic resolution is attempted. The sync step reports
    /// the conflict and returns a failure result. Safest option for critical
    /// data integrity requirements.
    /// </remarks>
    FailOnConflict = 4,

    /// <summary>
    /// Require manual resolution by the user.
    /// </summary>
    /// <remarks>
    /// LOGIC: Conflicts are surfaced to the user for explicit decision-making.
    /// Each conflict must be reviewed and resolved individually. Requires
    /// user attention but provides maximum control.
    /// </remarks>
    Manual = 5
}

/// <summary>
/// Extension methods for <see cref="ConflictStrategy"/> to map to infrastructure types.
/// </summary>
/// <remarks>
/// <para>
/// Provides conversion between the workflow-level <see cref="ConflictStrategy"/>
/// and the infrastructure-level <see cref="ConflictResolutionStrategy"/> used by
/// <see cref="ISyncService"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7g as part of Sync Step Type (CKVS Phase 4d).
/// </para>
/// </remarks>
internal static class ConflictStrategyExtensions
{
    /// <summary>
    /// Maps a <see cref="ConflictStrategy"/> to the corresponding
    /// <see cref="ConflictResolutionStrategy"/> for <see cref="ISyncService"/> calls.
    /// </summary>
    /// <param name="strategy">The workflow-level conflict strategy.</param>
    /// <returns>
    /// The infrastructure-level <see cref="ConflictResolutionStrategy"/>.
    /// </returns>
    /// <remarks>
    /// LOGIC: Direct mapping for most strategies. <see cref="ConflictStrategy.PreferNewer"/>
    /// maps to <see cref="ConflictResolutionStrategy.Merge"/> as the closest equivalent
    /// (timestamp-based selection is handled within the merge logic).
    /// <see cref="ConflictStrategy.FailOnConflict"/> maps to
    /// <see cref="ConflictResolutionStrategy.Manual"/> since the infrastructure does not
    /// have a fail-fast strategy — the step itself handles failure on conflict detection.
    /// </remarks>
    public static ConflictResolutionStrategy ToResolutionStrategy(this ConflictStrategy strategy) =>
        strategy switch
        {
            ConflictStrategy.PreferDocument => ConflictResolutionStrategy.UseDocument,
            ConflictStrategy.PreferGraph => ConflictResolutionStrategy.UseGraph,
            ConflictStrategy.PreferNewer => ConflictResolutionStrategy.Merge,
            ConflictStrategy.Merge => ConflictResolutionStrategy.Merge,
            ConflictStrategy.FailOnConflict => ConflictResolutionStrategy.Manual,
            ConflictStrategy.Manual => ConflictResolutionStrategy.Manual,
            _ => ConflictResolutionStrategy.Merge
        };
}
