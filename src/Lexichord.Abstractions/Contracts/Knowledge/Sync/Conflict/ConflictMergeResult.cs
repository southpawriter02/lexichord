// =============================================================================
// File: ConflictMergeResult.cs
// Project: Lexichord.Abstractions
// Description: Extended merge result with original conflict values.
// =============================================================================
// LOGIC: ConflictMergeResult extends MergeResult to include the
//   original document and graph values, plus an explanation of the
//   merge decision. This provides full context for audit and review.
//
// v0.7.6h: Conflict Resolver (CKVS Phase 4c)
// Dependencies: MergeResult (v0.7.6h)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict;

/// <summary>
/// Extended merge result that includes original conflict values.
/// </summary>
/// <remarks>
/// <para>
/// Extends <see cref="MergeResult"/> with:
/// </para>
/// <list type="bullet">
///   <item><b>DocumentValue:</b> The original value from the document.</item>
///   <item><b>GraphValue:</b> The original value from the graph.</item>
///   <item><b>Explanation:</b> Human-readable explanation of merge decision.</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// var result = await resolver.MergeConflictAsync(conflict);
/// Console.WriteLine($"Document: {result.DocumentValue}");
/// Console.WriteLine($"Graph: {result.GraphValue}");
/// Console.WriteLine($"Result: {result.MergedValue}");
/// Console.WriteLine($"Reason: {result.Explanation}");
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6h as part of the Conflict Resolver module.
/// </para>
/// </remarks>
public record ConflictMergeResult : MergeResult
{
    /// <summary>
    /// The original document value.
    /// </summary>
    /// <value>
    /// The value from the document that was in conflict.
    /// </value>
    /// <remarks>
    /// LOGIC: Preserved for audit trail and UI display.
    /// Shows what the document proposed before merge.
    /// </remarks>
    public required object DocumentValue { get; init; }

    /// <summary>
    /// The original graph value.
    /// </summary>
    /// <value>
    /// The value from the graph that was in conflict.
    /// </value>
    /// <remarks>
    /// LOGIC: Preserved for audit trail and UI display.
    /// Shows what the graph contained before merge.
    /// </remarks>
    public required object GraphValue { get; init; }

    /// <summary>
    /// Human-readable explanation of the merge decision.
    /// </summary>
    /// <value>
    /// A description of why this merge strategy was chosen.
    /// Null if no explanation is available.
    /// </value>
    /// <remarks>
    /// LOGIC: Provides transparency about merge decisions.
    /// Useful for user understanding and audit review.
    /// Example: "Selected document value because it was more recently modified."
    /// </remarks>
    public string? Explanation { get; init; }

    /// <summary>
    /// Creates a ConflictMergeResult from a base MergeResult.
    /// </summary>
    /// <param name="baseResult">The base merge result.</param>
    /// <param name="documentValue">The original document value.</param>
    /// <param name="graphValue">The original graph value.</param>
    /// <param name="explanation">Optional explanation of the merge.</param>
    /// <returns>A new ConflictMergeResult with the provided context.</returns>
    public static ConflictMergeResult FromMergeResult(
        MergeResult baseResult,
        object documentValue,
        object graphValue,
        string? explanation = null)
    {
        return new ConflictMergeResult
        {
            Success = baseResult.Success,
            MergedValue = baseResult.MergedValue,
            UsedStrategy = baseResult.UsedStrategy,
            Confidence = baseResult.Confidence,
            ErrorMessage = baseResult.ErrorMessage,
            MergeType = baseResult.MergeType,
            DocumentValue = documentValue,
            GraphValue = graphValue,
            Explanation = explanation ?? $"Merged using {baseResult.UsedStrategy}"
        };
    }
}
