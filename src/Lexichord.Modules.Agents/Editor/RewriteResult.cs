// -----------------------------------------------------------------------
// <copyright file="RewriteResult.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Encapsulates the outcome of a rewrite operation performed by the
//   Editor Agent (v0.7.3b). Contains the original text, rewritten text,
//   success status, usage metrics, and timing information.
//
//   On failure, RewrittenText equals OriginalText to ensure the document
//   is never corrupted by a partial or empty result.
//
//   The Failed() factory method provides a convenient way to create error
//   results with consistent defaults (Usage = Zero, RewrittenText = original).
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Editor;

namespace Lexichord.Modules.Agents.Editor;

/// <summary>
/// The result of a rewrite operation performed by the Editor Agent.
/// </summary>
/// <remarks>
/// <para>
/// Returned by <see cref="IEditorAgent.RewriteAsync"/> and consumed by
/// <see cref="IRewriteCommandHandler"/> for document application and event publishing.
/// </para>
/// <para>
/// On failure (<see cref="Success"/> = false), <see cref="RewrittenText"/> is set
/// to <see cref="OriginalText"/> to prevent document corruption.
/// </para>
/// <para><b>Introduced in:</b> v0.7.3b</para>
/// </remarks>
public record RewriteResult
{
    /// <summary>
    /// The original text that was selected for rewriting.
    /// </summary>
    public required string OriginalText { get; init; }

    /// <summary>
    /// The rewritten text from the LLM.
    /// </summary>
    /// <remarks>
    /// LOGIC: If <see cref="Success"/> is false, this equals <see cref="OriginalText"/>
    /// to ensure the document is never left in an inconsistent state.
    /// </remarks>
    public required string RewrittenText { get; init; }

    /// <summary>
    /// The intent that was applied.
    /// </summary>
    public required RewriteIntent Intent { get; init; }

    /// <summary>
    /// Whether the rewrite completed successfully.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Error message if <see cref="Success"/> is false.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Token usage metrics from the LLM invocation.
    /// </summary>
    /// <remarks>
    /// LOGIC: Uses the existing <see cref="UsageMetrics"/> record from v0.6.6a
    /// with PromptTokens, CompletionTokens, and EstimatedCost. Set to
    /// <see cref="UsageMetrics.Zero"/> on failure.
    /// </remarks>
    public required UsageMetrics Usage { get; init; }

    /// <summary>
    /// Total time taken for the rewrite operation.
    /// </summary>
    /// <remarks>
    /// LOGIC: Measured from request validation to LLM response completion.
    /// Includes context gathering, prompt rendering, and LLM invocation time.
    /// </remarks>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Creates a failed rewrite result.
    /// </summary>
    /// <param name="originalText">The original text that was selected.</param>
    /// <param name="intent">The rewrite intent that was attempted.</param>
    /// <param name="errorMessage">Description of the failure.</param>
    /// <param name="duration">Time elapsed before failure.</param>
    /// <returns>A <see cref="RewriteResult"/> with Success=false and RewrittenText=OriginalText.</returns>
    /// <remarks>
    /// LOGIC: Factory method ensures consistent failure semantics:
    /// <list type="bullet">
    ///   <item><description>RewrittenText is always set to OriginalText on failure</description></item>
    ///   <item><description>Usage is always <see cref="UsageMetrics.Zero"/></description></item>
    ///   <item><description>ErrorMessage is always populated</description></item>
    /// </list>
    /// </remarks>
    public static RewriteResult Failed(
        string originalText,
        RewriteIntent intent,
        string errorMessage,
        TimeSpan duration) => new()
    {
        OriginalText = originalText,
        RewrittenText = originalText,
        Intent = intent,
        Success = false,
        ErrorMessage = errorMessage,
        Usage = UsageMetrics.Zero,
        Duration = duration
    };
}
