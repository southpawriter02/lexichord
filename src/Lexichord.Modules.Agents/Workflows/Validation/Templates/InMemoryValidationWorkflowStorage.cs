// -----------------------------------------------------------------------
// <copyright file="InMemoryValidationWorkflowStorage.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Workflows.Validation.Templates;

/// <summary>
/// In-memory stub implementation of <see cref="IValidationWorkflowStorage"/>
/// for custom validation workflows.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses a <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// for thread-safe storage. Data is not persisted across application restarts.
/// A future version will add persistent storage (e.g., SQLite or file-based).
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-h §4
/// </para>
/// </remarks>
internal sealed class InMemoryValidationWorkflowStorage : IValidationWorkflowStorage
{
    // ── Fields ───────────────────────────────────────────────────────────

    /// <summary>Thread-safe dictionary storing custom workflow definitions by ID.</summary>
    private readonly ConcurrentDictionary<string, ValidationWorkflowDefinition> _workflows = new();

    /// <summary>Logger for diagnostic output during storage operations.</summary>
    private readonly ILogger<InMemoryValidationWorkflowStorage> _logger;

    // ── Constructor ─────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new instance of <see cref="InMemoryValidationWorkflowStorage"/>.
    /// </summary>
    /// <param name="logger">Logger for recording storage operations.</param>
    public InMemoryValidationWorkflowStorage(
        ILogger<InMemoryValidationWorkflowStorage> logger)
    {
        _logger = logger;

        _logger.LogDebug("InMemoryValidationWorkflowStorage initialized (non-persistent stub)");
    }

    // ── IValidationWorkflowStorage Implementation ───────────────────────

    /// <inheritdoc />
    public Task<ValidationWorkflowDefinition?> GetWorkflowAsync(
        string workflowId,
        CancellationToken ct = default)
    {
        // LOGIC: Attempt to retrieve the workflow from the in-memory store.
        // Returns null if the workflow ID is not found.
        _workflows.TryGetValue(workflowId, out var workflow);

        _logger.LogDebug(
            "GetWorkflowAsync: {WorkflowId} -> {Found}",
            workflowId,
            workflow != null ? "found" : "not found");

        return Task.FromResult(workflow);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ValidationWorkflowDefinition>> ListWorkflowsAsync(
        CancellationToken ct = default)
    {
        // LOGIC: Return all stored custom workflows as an immutable snapshot.
        var workflows = _workflows.Values.ToList();

        _logger.LogDebug(
            "ListWorkflowsAsync: returning {Count} custom workflows",
            workflows.Count);

        return Task.FromResult<IReadOnlyList<ValidationWorkflowDefinition>>(workflows);
    }

    /// <inheritdoc />
    public Task<string> SaveWorkflowAsync(
        ValidationWorkflowDefinition workflow,
        CancellationToken ct = default)
    {
        // LOGIC: Add or replace the workflow in the in-memory store.
        _workflows[workflow.Id] = workflow;

        _logger.LogInformation(
            "SaveWorkflowAsync: stored custom workflow {WorkflowId} ({WorkflowName})",
            workflow.Id,
            workflow.Name);

        return Task.FromResult(workflow.Id);
    }

    /// <inheritdoc />
    public Task UpdateWorkflowAsync(
        string workflowId,
        ValidationWorkflowDefinition workflow,
        CancellationToken ct = default)
    {
        // LOGIC: Replace the workflow at the given key. If the key does not
        // exist, the update still stores the value (upsert behavior).
        _workflows[workflowId] = workflow;

        _logger.LogInformation(
            "UpdateWorkflowAsync: updated custom workflow {WorkflowId}",
            workflowId);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteWorkflowAsync(
        string workflowId,
        CancellationToken ct = default)
    {
        // LOGIC: Remove the workflow from the in-memory store.
        // TryRemove is used for thread safety — removal is idempotent.
        var removed = _workflows.TryRemove(workflowId, out _);

        _logger.LogInformation(
            "DeleteWorkflowAsync: {WorkflowId} -> {Result}",
            workflowId,
            removed ? "removed" : "not found");

        return Task.CompletedTask;
    }
}
