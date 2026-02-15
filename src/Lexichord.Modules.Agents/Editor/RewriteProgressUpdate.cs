// -----------------------------------------------------------------------
// <copyright file="RewriteProgressUpdate.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Provides streaming progress updates during a rewrite operation
//   (v0.7.3b). Yielded by IEditorAgent.RewriteStreamingAsync and
//   IRewriteCommandHandler.ExecuteStreamingAsync for UI progress display.
//
//   Progress stages:
//     Initializing (0%)      → Preparing the pipeline
//     GatheringContext (10%)  → Assembling document context
//     GeneratingRewrite (25-95%) → Receiving LLM tokens
//     Completed (100%)       → Final text ready
//     Failed (any%)          → Error occurred
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Editor;

/// <summary>
/// Progress update during a streaming rewrite operation.
/// </summary>
/// <remarks>
/// <para>
/// Yielded as an <see cref="IAsyncEnumerable{T}"/> by
/// <see cref="IEditorAgent.RewriteStreamingAsync"/> and
/// <see cref="IRewriteCommandHandler.ExecuteStreamingAsync"/>.
/// </para>
/// <para>
/// The <see cref="PartialText"/> property accumulates the LLM response
/// incrementally during the <see cref="RewriteProgressState.GeneratingRewrite"/>
/// phase, becoming the final rewritten text at <see cref="RewriteProgressState.Completed"/>.
/// </para>
/// <para><b>Introduced in:</b> v0.7.3b</para>
/// </remarks>
public record RewriteProgressUpdate
{
    /// <summary>
    /// Partial text received so far from the LLM.
    /// </summary>
    /// <remarks>
    /// LOGIC: Empty during Initializing and GatheringContext phases.
    /// Accumulates incrementally during GeneratingRewrite.
    /// Contains the final trimmed text at Completed.
    /// </remarks>
    public required string PartialText { get; init; }

    /// <summary>
    /// Progress percentage from 0 to 100.
    /// </summary>
    /// <remarks>
    /// LOGIC: Estimated based on pipeline stage and output length ratio:
    /// <list type="bullet">
    ///   <item><description>0% — Initializing</description></item>
    ///   <item><description>10% — GatheringContext</description></item>
    ///   <item><description>25-95% — GeneratingRewrite (proportional to output/input ratio)</description></item>
    ///   <item><description>100% — Completed</description></item>
    /// </list>
    /// </remarks>
    public required double ProgressPercentage { get; init; }

    /// <summary>
    /// Current state of the rewrite operation.
    /// </summary>
    public required RewriteProgressState State { get; init; }

    /// <summary>
    /// Optional human-readable status message for UI display.
    /// </summary>
    /// <remarks>
    /// LOGIC: Examples: "Preparing rewrite...", "Gathering document context...",
    /// "Generating rewrite...", "Rewrite complete", "Rewrite failed: timeout".
    /// </remarks>
    public string? StatusMessage { get; init; }
}

/// <summary>
/// States during a rewrite progress pipeline.
/// </summary>
/// <remarks>
/// <para><b>Introduced in:</b> v0.7.3b</para>
/// </remarks>
public enum RewriteProgressState
{
    /// <summary>
    /// Initializing the rewrite operation (validating request, preparing pipeline).
    /// </summary>
    Initializing,

    /// <summary>
    /// Gathering context from the document (surrounding text, style rules, terminology).
    /// </summary>
    GatheringContext,

    /// <summary>
    /// Waiting for and receiving the LLM response.
    /// </summary>
    GeneratingRewrite,

    /// <summary>
    /// Operation completed successfully. <see cref="RewriteProgressUpdate.PartialText"/>
    /// contains the final rewritten text.
    /// </summary>
    Completed,

    /// <summary>
    /// Operation failed. Check <see cref="RewriteProgressUpdate.StatusMessage"/> for details.
    /// </summary>
    Failed
}
