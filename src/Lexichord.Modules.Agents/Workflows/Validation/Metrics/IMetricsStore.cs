// -----------------------------------------------------------------------
// <copyright file="IMetricsStore.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation.Metrics;

/// <summary>
/// Persistence layer for workflow metrics data.
/// </summary>
/// <remarks>
/// <para>
/// Abstracts the storage mechanism for metrics records. The v0.7.7i implementation
/// provides an in-memory stub (<see cref="InMemoryMetricsStore"/>); future versions
/// will add persistent storage (e.g., SQLite time-series).
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-i §4.1 (inferred from collector usage)
/// </para>
/// </remarks>
public interface IMetricsStore
{
    /// <summary>Stores a workflow execution metrics record.</summary>
    /// <param name="metrics">The metrics to store.</param>
    /// <param name="ct">Cancellation token.</param>
    Task StoreWorkflowExecutionAsync(
        WorkflowExecutionMetrics metrics,
        CancellationToken ct = default);

    /// <summary>Stores a validation step metrics record.</summary>
    /// <param name="metrics">The metrics to store.</param>
    /// <param name="ct">Cancellation token.</param>
    Task StoreValidationStepAsync(
        ValidationStepMetrics metrics,
        CancellationToken ct = default);

    /// <summary>Stores a gating decision metrics record.</summary>
    /// <param name="metrics">The metrics to store.</param>
    /// <param name="ct">Cancellation token.</param>
    Task StoreGatingDecisionAsync(
        GatingDecisionMetrics metrics,
        CancellationToken ct = default);

    /// <summary>Stores a sync operation metrics record.</summary>
    /// <param name="metrics">The metrics to store.</param>
    /// <param name="ct">Cancellation token.</param>
    Task StoreSyncOperationAsync(
        SyncOperationMetrics metrics,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves workflow execution metrics matching the query.
    /// </summary>
    /// <param name="query">Filter and pagination parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Matching execution records.</returns>
    Task<IReadOnlyList<WorkflowExecutionMetrics>> GetWorkflowExecutionsAsync(
        MetricsQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves validation step metrics matching the query.
    /// </summary>
    /// <param name="query">Filter and pagination parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Matching step records.</returns>
    Task<IReadOnlyList<ValidationStepMetrics>> GetValidationStepsAsync(
        MetricsQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves gating decision metrics matching the query.
    /// </summary>
    /// <param name="query">Filter and pagination parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Matching gating decision records.</returns>
    Task<IReadOnlyList<GatingDecisionMetrics>> GetGatingDecisionsAsync(
        MetricsQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves sync operation metrics matching the query.
    /// </summary>
    /// <param name="query">Filter and pagination parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Matching sync operation records.</returns>
    Task<IReadOnlyList<SyncOperationMetrics>> GetSyncOperationsAsync(
        MetricsQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves all workflow execution metrics for a specific document.
    /// </summary>
    /// <param name="documentId">The document to query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All execution records for the document.</returns>
    Task<IReadOnlyList<WorkflowExecutionMetrics>> GetDocumentMetricsAsync(
        Guid documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Stores pre-calculated health scores for caching.
    /// </summary>
    /// <param name="workspaceHealth">Workspace health score to cache.</param>
    /// <param name="documentHealth">Document health score to cache.</param>
    /// <param name="ct">Cancellation token.</param>
    Task StoreHealthScoresAsync(
        WorkspaceHealthScore workspaceHealth,
        DocumentHealthScore documentHealth,
        CancellationToken ct = default);
}
