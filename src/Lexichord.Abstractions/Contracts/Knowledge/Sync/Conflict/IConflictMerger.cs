// =============================================================================
// File: IConflictMerger.cs
// Project: Lexichord.Abstractions
// Description: Interface for intelligently merging conflicted values.
// =============================================================================
// LOGIC: IConflictMerger provides intelligent value merging capabilities.
//   It selects appropriate merge strategies based on conflict type and
//   executes merges to produce unified values.
//
// v0.7.6h: Conflict Resolver (CKVS Phase 4c)
// Dependencies: MergeResult, MergeContext, MergeStrategy, ConflictType (v0.7.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict;

/// <summary>
/// Service for intelligently merging conflicted values.
/// </summary>
/// <remarks>
/// <para>
/// Provides intelligent value merging:
/// </para>
/// <list type="bullet">
///   <item>Strategy selection based on conflict type.</item>
///   <item>Value merging with multiple strategies.</item>
///   <item>Confidence-based result evaluation.</item>
/// </list>
/// <para>
/// <b>Implementation:</b> See <c>ConflictMerger</c> in Lexichord.Modules.Knowledge.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6h as part of the Conflict Resolver module.
/// </para>
/// </remarks>
public interface IConflictMerger
{
    /// <summary>
    /// Merges two conflicting values.
    /// </summary>
    /// <param name="documentValue">The value from the document.</param>
    /// <param name="graphValue">The value from the graph.</param>
    /// <param name="context">Context information for the merge.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="MergeResult"/> indicating success/failure and the merged value.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Merge process:
    /// </para>
    /// <list type="number">
    ///   <item>Determine merge strategy from context.</item>
    ///   <item>Execute strategy-specific merge logic.</item>
    ///   <item>Compute confidence in the result.</item>
    ///   <item>Return MergeResult with merged value.</item>
    /// </list>
    /// <para>
    /// If merge is not possible, returns a failed result with
    /// RequiresManualMerge strategy.
    /// </para>
    /// </remarks>
    Task<MergeResult> MergeAsync(
        object documentValue,
        object graphValue,
        MergeContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the recommended merge strategy for a conflict type.
    /// </summary>
    /// <param name="conflictType">The type of conflict.</param>
    /// <returns>The recommended <see cref="MergeStrategy"/>.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Strategy selection based on conflict type:
    /// </para>
    /// <list type="bullet">
    ///   <item>ValueMismatch: MostRecent or HighestConfidence.</item>
    ///   <item>MissingInGraph: DocumentFirst (create entity).</item>
    ///   <item>MissingInDocument: RequiresManualMerge (delete vs keep).</item>
    ///   <item>ConcurrentEdit: RequiresManualMerge (complex merge).</item>
    /// </list>
    /// </remarks>
    MergeStrategy GetMergeStrategy(ConflictType conflictType);
}
