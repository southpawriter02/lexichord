// =============================================================================
// File: MergeResult.cs
// Project: Lexichord.Abstractions
// Description: Result of a merge operation.
// =============================================================================
// LOGIC: MergeResult captures the outcome of a merge operation,
//   including whether it succeeded, the merged value, the strategy
//   used, and confidence in the result.
//
// v0.7.6h: Conflict Resolver (CKVS Phase 4c)
// Dependencies: MergeStrategy, MergeType (v0.7.6h)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict;

/// <summary>
/// Result of a merge operation.
/// </summary>
/// <remarks>
/// <para>
/// Captures merge operation outcome:
/// </para>
/// <list type="bullet">
///   <item><b>Success:</b> Whether the merge completed successfully.</item>
///   <item><b>MergedValue:</b> The resulting merged value.</item>
///   <item><b>UsedStrategy:</b> The merge strategy that was applied.</item>
///   <item><b>Confidence:</b> Confidence in the merged result.</item>
///   <item><b>MergeType:</b> How the merge was performed.</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// var result = await merger.MergeAsync(docValue, graphValue, context);
/// if (result.Success)
/// {
///     Console.WriteLine($"Merged using {result.UsedStrategy}: {result.MergedValue}");
/// }
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6h as part of the Conflict Resolver module.
/// </para>
/// </remarks>
public record MergeResult
{
    /// <summary>
    /// Indicates whether the merge succeeded.
    /// </summary>
    /// <value>True if merge completed successfully; otherwise, false.</value>
    /// <remarks>
    /// LOGIC: Primary success indicator.
    /// When false, see ErrorMessage for failure reason.
    /// </remarks>
    public required bool Success { get; init; }

    /// <summary>
    /// The merged value if successful.
    /// </summary>
    /// <value>
    /// The result of the merge operation.
    /// Null if merge failed or no value was produced.
    /// </value>
    /// <remarks>
    /// LOGIC: The value to be used after merge resolution.
    /// May be from document, graph, or a combination.
    /// </remarks>
    public object? MergedValue { get; init; }

    /// <summary>
    /// The merge strategy that was used.
    /// </summary>
    /// <value>The <see cref="MergeStrategy"/> applied.</value>
    /// <remarks>
    /// LOGIC: Indicates which strategy produced the merged result.
    /// Useful for auditing and understanding the merge decision.
    /// </remarks>
    public MergeStrategy UsedStrategy { get; init; }

    /// <summary>
    /// Confidence in the merge result.
    /// </summary>
    /// <value>
    /// A value between 0.0 and 1.0.
    /// Higher values indicate higher confidence in the merged result.
    /// </value>
    /// <remarks>
    /// LOGIC: Helps determine if the merge result should be reviewed.
    /// Low confidence (< 0.7) results may require manual verification.
    /// </remarks>
    public float Confidence { get; init; }

    /// <summary>
    /// Error message if merge failed.
    /// </summary>
    /// <value>
    /// A description of why the merge failed.
    /// Null if merge succeeded.
    /// </value>
    /// <remarks>
    /// LOGIC: Provides diagnostic information for failed merges.
    /// Should be user-readable for display in UI.
    /// </remarks>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// How the merge was performed.
    /// </summary>
    /// <value>The <see cref="MergeType"/> indicating the merge mechanism.</value>
    /// <remarks>
    /// LOGIC: Describes the method used to create the merged value.
    /// Selection indicates direct choice; Intelligent indicates algorithm.
    /// </remarks>
    public MergeType MergeType { get; init; }

    /// <summary>
    /// Creates a successful merge result with document value.
    /// </summary>
    /// <param name="documentValue">The document value to use.</param>
    /// <returns>A successful MergeResult using DocumentFirst strategy.</returns>
    public static MergeResult DocumentWins(object documentValue) => new()
    {
        Success = true,
        MergedValue = documentValue,
        UsedStrategy = MergeStrategy.DocumentFirst,
        Confidence = 1.0f,
        MergeType = MergeType.Selection
    };

    /// <summary>
    /// Creates a successful merge result with graph value.
    /// </summary>
    /// <param name="graphValue">The graph value to use.</param>
    /// <returns>A successful MergeResult using GraphFirst strategy.</returns>
    public static MergeResult GraphWins(object graphValue) => new()
    {
        Success = true,
        MergedValue = graphValue,
        UsedStrategy = MergeStrategy.GraphFirst,
        Confidence = 1.0f,
        MergeType = MergeType.Selection
    };

    /// <summary>
    /// Creates a failed merge result requiring manual intervention.
    /// </summary>
    /// <param name="reason">The reason for requiring manual merge.</param>
    /// <returns>A failed MergeResult with RequiresManualMerge strategy.</returns>
    public static MergeResult RequiresManual(string reason) => new()
    {
        Success = false,
        UsedStrategy = MergeStrategy.RequiresManualMerge,
        Confidence = 0.0f,
        ErrorMessage = reason,
        MergeType = MergeType.Manual
    };

    /// <summary>
    /// Creates a failed merge result with error.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failed MergeResult.</returns>
    public static MergeResult Failed(string errorMessage) => new()
    {
        Success = false,
        Confidence = 0.0f,
        ErrorMessage = errorMessage,
        MergeType = MergeType.Selection
    };
}
