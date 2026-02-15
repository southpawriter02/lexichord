// -----------------------------------------------------------------------
// <copyright file="IRewriteCommandHandler.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Defines the rewrite command pipeline contract (v0.7.3b).
//   Orchestrates the complete flow from user action to document update:
//     1. License verification
//     2. Publishing RewriteStartedEvent
//     3. Delegating to IEditorAgent for LLM invocation
//     4. Delegating to IRewriteApplicator for document update (v0.7.3d)
//     5. Publishing RewriteCompletedEvent
//
//   Supports both synchronous (ExecuteAsync) and streaming
//   (ExecuteStreamingAsync) execution modes.
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Editor;

/// <summary>
/// Handles rewrite command execution from user action to document update.
/// </summary>
/// <remarks>
/// <para>
/// The command handler is the central orchestrator of the Editor Agent pipeline.
/// It enforces license requirements, manages execution state, publishes MediatR
/// events for observability, and coordinates between the <see cref="IEditorAgent"/>
/// and <see cref="IRewriteApplicator"/>.
/// </para>
/// <para>
/// Consumed by <see cref="RewriteRequestedEventHandler"/> which bridges
/// the v0.7.3a context menu events to this pipeline.
/// </para>
/// <para><b>Introduced in:</b> v0.7.3b</para>
/// </remarks>
public interface IRewriteCommandHandler
{
    /// <summary>
    /// Executes a rewrite command synchronously (waits for completion).
    /// </summary>
    /// <param name="request">The rewrite request.</param>
    /// <param name="ct">Cancellation token for user-initiated cancellation.</param>
    /// <returns>The complete rewrite result.</returns>
    /// <remarks>
    /// LOGIC: Full pipeline: license check → start event → agent rewrite →
    /// applicator apply → completion event → return result.
    /// Sets <see cref="IsExecuting"/> during the operation.
    /// </remarks>
    Task<RewriteResult> ExecuteAsync(RewriteRequest request, CancellationToken ct = default);

    /// <summary>
    /// Executes a rewrite with streaming progress updates.
    /// </summary>
    /// <param name="request">The rewrite request.</param>
    /// <param name="ct">Cancellation token for user-initiated cancellation.</param>
    /// <returns>Async enumerable of progress updates.</returns>
    /// <remarks>
    /// LOGIC: Delegates to <see cref="IEditorAgent.RewriteStreamingAsync"/> and
    /// applies the result to the document on completion.
    /// </remarks>
    IAsyncEnumerable<RewriteProgressUpdate> ExecuteStreamingAsync(
        RewriteRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Cancels any in-progress rewrite operation.
    /// </summary>
    /// <remarks>
    /// LOGIC: Cancels the linked <see cref="CancellationTokenSource"/> created
    /// during <see cref="ExecuteAsync"/> or <see cref="ExecuteStreamingAsync"/>.
    /// The cancelled operation returns a failed <see cref="RewriteResult"/>.
    /// </remarks>
    void Cancel();

    /// <summary>
    /// Gets whether a rewrite is currently in progress.
    /// </summary>
    /// <remarks>
    /// LOGIC: Set to true at the start of Execute/ExecuteStreaming and
    /// reset to false in the finally block. Used by the UI to disable
    /// rewrite commands during execution.
    /// </remarks>
    bool IsExecuting { get; }
}
