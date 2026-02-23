// -----------------------------------------------------------------------
// <copyright file="ISyncWorkflowStep.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Extended workflow step interface for synchronization steps that
//   sync document state to/from the knowledge graph. Adds sync direction,
//   conflict strategy, skip-on-failure logic, and a sync-specific execution
//   method that returns detailed SyncStepResult.
//
// v0.7.7g: Sync Step Type (CKVS Phase 4d)
// Dependencies: IWorkflowStep (v0.7.7e), SyncDirection (v0.7.7g),
//               ConflictStrategy (v0.7.7g), SyncStepResult (v0.7.7g),
//               SyncWorkflowContext (v0.7.7g)
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.Knowledge.Sync;

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// Extended workflow step interface for document-graph synchronization steps.
/// </summary>
/// <remarks>
/// <para>
/// Extends <see cref="IWorkflowStep"/> with synchronization-specific capabilities:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="Direction"/>: Controls the flow of data (document → graph, graph → document, or both).</description></item>
///   <item><description><see cref="ConflictStrategy"/>: Determines how to handle divergent state.</description></item>
///   <item><description><see cref="SkipIfValidationFailed"/>: Prevents syncing invalid documents.</description></item>
///   <item><description><see cref="ExecuteSyncAsync"/>: Detailed sync execution with full result metrics.</description></item>
/// </list>
/// <para>
/// The workflow engine calls <see cref="IWorkflowStep.ExecuteAsync"/> which internally
/// checks enabled state and validation status, then delegates to <see cref="ExecuteSyncAsync"/>
/// and maps the result to a <see cref="WorkflowStepResult"/>.
/// </para>
/// <para>
/// <b>License Gating:</b>
/// <list type="bullet">
///   <item><description>WriterPro: <see cref="SyncDirection.DocumentToGraph"/> only, basic strategies.</description></item>
///   <item><description>Teams: All directions + conflict strategies.</description></item>
///   <item><description>Enterprise: Full + custom sync handlers + conflict resolution.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7g as part of Sync Step Type (CKVS Phase 4d).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var syncStep = new SyncWorkflowStep(
///     id: "post-validation-sync",
///     name: "Post-Validation Sync",
///     direction: SyncDirection.DocumentToGraph,
///     syncService: syncService,
///     logger: logger,
///     conflictStrategy: ConflictStrategy.PreferNewer,
///     skipIfValidationFailed: true);
///
/// var context = new SyncWorkflowContext
/// {
///     WorkspaceId = workspace.Id,
///     DocumentId = "doc-123",
///     DocumentContent = "# My Document"
/// };
///
/// var result = await syncStep.ExecuteSyncAsync(context);
/// if (result.Success)
/// {
///     Console.WriteLine($"Synced {result.ItemsSynced} items");
/// }
/// </code>
/// </example>
public interface ISyncWorkflowStep : IWorkflowStep
{
    /// <summary>
    /// Gets the synchronization direction.
    /// </summary>
    /// <remarks>
    /// Determines whether changes flow from document to graph, graph to
    /// document, or in both directions.
    /// </remarks>
    SyncDirection Direction { get; }

    /// <summary>
    /// Gets the conflict resolution strategy.
    /// </summary>
    /// <remarks>
    /// Applied when the sync operation detects divergent state between the
    /// document and graph. The strategy determines whether to prefer one
    /// source, merge, fail, or defer to manual resolution.
    /// </remarks>
    ConflictStrategy ConflictStrategy { get; }

    /// <summary>
    /// Gets whether to skip synchronization when prior validation steps have failed.
    /// </summary>
    /// <remarks>
    /// When <c>true</c> (default), the step inspects
    /// <see cref="ValidationWorkflowContext.PreviousResults"/> for any failures
    /// and skips the sync operation to prevent syncing invalid documents to
    /// the knowledge graph.
    /// </remarks>
    bool SkipIfValidationFailed { get; }

    /// <summary>
    /// Executes the synchronization operation.
    /// </summary>
    /// <param name="context">
    /// The sync workflow context containing document identity, workspace,
    /// and sync configuration.
    /// </param>
    /// <param name="ct">
    /// Cancellation token for timeout enforcement and user cancellation.
    /// Implementations must check this token periodically.
    /// </param>
    /// <returns>
    /// A <see cref="SyncStepResult"/> containing detailed sync metrics:
    /// items synced, conflicts, changes, and audit logs.
    /// </returns>
    Task<SyncStepResult> ExecuteSyncAsync(
        SyncWorkflowContext context,
        CancellationToken ct = default);
}
