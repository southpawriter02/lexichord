// -----------------------------------------------------------------------
// <copyright file="IEditorAgent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Defines the Editor Agent contract extending IAgent with
//   rewrite-specific operations (v0.7.3b). The EditorAgent is specialized
//   for text transformation — it gathers document context, renders
//   intent-specific prompt templates, and invokes the LLM.
//
//   This interface is consumed by:
//     - RewriteCommandHandler (orchestrates the full pipeline)
//     - RewriteRequestedEventHandler (bridges v0.7.3a events)
//
//   The streaming variant (RewriteStreamingAsync) enables real-time
//   progress updates in the UI during longer rewrites.
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Editor;

namespace Lexichord.Modules.Agents.Editor;

/// <summary>
/// Editor Agent specialized for text rewriting operations.
/// </summary>
/// <remarks>
/// <para>
/// Extends the base <see cref="IAgent"/> interface with rewrite-specific methods
/// that accept <see cref="RewriteRequest"/> and return <see cref="RewriteResult"/>.
/// </para>
/// <para>
/// The agent supports four rewrite intents (Formal, Simplified, Expanded, Custom),
/// each backed by a distinct prompt template. Context is gathered via
/// <see cref="Abstractions.Agents.Context.IContextOrchestrator"/> to include
/// surrounding text, style rules, and terminology.
/// </para>
/// <para><b>Introduced in:</b> v0.7.3b</para>
/// </remarks>
public interface IEditorAgent : IAgent
{
    /// <summary>
    /// Performs a text rewrite according to the specified intent.
    /// </summary>
    /// <param name="request">The rewrite request containing selected text, intent, and options.</param>
    /// <param name="ct">Cancellation token for user-initiated cancellation.</param>
    /// <returns>
    /// A <see cref="RewriteResult"/> with the rewritten text on success,
    /// or the original text with error details on failure.
    /// </returns>
    /// <remarks>
    /// LOGIC: Pipeline steps:
    /// <list type="number">
    ///   <item><description>Validate request</description></item>
    ///   <item><description>Gather context via IContextOrchestrator</description></item>
    ///   <item><description>Select prompt template for intent</description></item>
    ///   <item><description>Build prompt variables from context</description></item>
    ///   <item><description>Render messages via IPromptRenderer</description></item>
    ///   <item><description>Invoke LLM via IChatCompletionService</description></item>
    ///   <item><description>Return RewriteResult with trimmed output</description></item>
    /// </list>
    /// </remarks>
    Task<RewriteResult> RewriteAsync(RewriteRequest request, CancellationToken ct = default);

    /// <summary>
    /// Performs a streaming rewrite with progress updates.
    /// </summary>
    /// <param name="request">The rewrite request containing selected text, intent, and options.</param>
    /// <param name="ct">Cancellation token for user-initiated cancellation.</param>
    /// <returns>
    /// An async enumerable of <see cref="RewriteProgressUpdate"/> instances,
    /// yielding incremental progress from initialization through completion.
    /// </returns>
    /// <remarks>
    /// LOGIC: Yields progress updates at each pipeline stage:
    /// Initializing → GatheringContext → GeneratingRewrite (per-token) → Completed.
    /// The final update contains the complete rewritten text.
    /// </remarks>
    IAsyncEnumerable<RewriteProgressUpdate> RewriteStreamingAsync(
        RewriteRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the prompt template ID for a given rewrite intent.
    /// </summary>
    /// <param name="intent">The rewrite intent.</param>
    /// <returns>The template ID string (e.g., "editor-rewrite-formal").</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for undefined intent values.</exception>
    /// <remarks>
    /// LOGIC: Mapping:
    /// <list type="bullet">
    ///   <item><description>Formal → "editor-rewrite-formal"</description></item>
    ///   <item><description>Simplified → "editor-rewrite-simplify"</description></item>
    ///   <item><description>Expanded → "editor-rewrite-expand"</description></item>
    ///   <item><description>Custom → "editor-rewrite-custom"</description></item>
    /// </list>
    /// </remarks>
    string GetTemplateId(RewriteIntent intent);
}
