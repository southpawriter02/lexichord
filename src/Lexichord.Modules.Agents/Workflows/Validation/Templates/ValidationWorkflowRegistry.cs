// -----------------------------------------------------------------------
// <copyright file="ValidationWorkflowRegistry.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Workflows.Validation.Templates;

/// <summary>
/// Registry managing pre-built and custom validation workflow definitions.
/// </summary>
/// <remarks>
/// <para>
/// The registry implements a three-tier lookup strategy:
/// <list type="number">
///   <item><description>Pre-built cache (in-memory dictionary)</description></item>
///   <item><description>Custom storage (<see cref="IValidationWorkflowStorage"/>)</description></item>
///   <item><description>Embedded resource loader (<see cref="IValidationWorkflowLoader"/>)</description></item>
/// </list>
/// </para>
/// <para>
/// Pre-built workflows are immutable — they cannot be updated or deleted through
/// this registry. Custom workflows are delegated to the storage implementation.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-h §3–§4
/// </para>
/// </remarks>
internal sealed class ValidationWorkflowRegistry : IValidationWorkflowRegistry
{
    // ── Constants ────────────────────────────────────────────────────────

    /// <summary>IDs of all pre-built validation workflows shipped with v0.7.7h.</summary>
    private static readonly string[] PrebuiltIds =
    {
        "on-save-validation",
        "pre-publish-gate",
        "nightly-health-check"
    };

    // ── Fields ───────────────────────────────────────────────────────────

    /// <summary>Storage for custom (user-defined) workflows.</summary>
    private readonly IValidationWorkflowStorage _storage;

    /// <summary>Loader for pre-built workflows from embedded resources.</summary>
    private readonly IValidationWorkflowLoader _loader;

    /// <summary>Logger for diagnostic output.</summary>
    private readonly ILogger<ValidationWorkflowRegistry> _logger;

    /// <summary>
    /// Cache of pre-built workflow definitions. Populated lazily on first access
    /// and never evicted during the application lifetime.
    /// </summary>
    private readonly Dictionary<string, ValidationWorkflowDefinition> _prebuiltCache = new();

    // ── Constructor ─────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationWorkflowRegistry"/>.
    /// </summary>
    /// <param name="storage">Storage for custom workflow persistence.</param>
    /// <param name="loader">Loader for pre-built workflow resources.</param>
    /// <param name="logger">Logger for recording registry operations.</param>
    public ValidationWorkflowRegistry(
        IValidationWorkflowStorage storage,
        IValidationWorkflowLoader loader,
        ILogger<ValidationWorkflowRegistry> logger)
    {
        _storage = storage;
        _loader = loader;
        _logger = logger;

        _logger.LogDebug("ValidationWorkflowRegistry initialized");
    }

    // ── IValidationWorkflowRegistry Implementation ──────────────────────

    /// <inheritdoc />
    /// <remarks>
    /// Lookup order: pre-built cache → custom storage → embedded resource loader.
    /// Results from the loader are cached for subsequent lookups.
    /// </remarks>
    public async Task<ValidationWorkflowDefinition> GetWorkflowAsync(
        string workflowId,
        CancellationToken ct = default)
    {
        _logger.LogDebug("GetWorkflowAsync: looking up {WorkflowId}", workflowId);

        // LOGIC: Try the pre-built cache first (fastest path).
        if (_prebuiltCache.TryGetValue(workflowId, out var cached))
        {
            _logger.LogDebug(
                "GetWorkflowAsync: {WorkflowId} found in pre-built cache",
                workflowId);
            return cached;
        }

        // LOGIC: Try custom storage (user-defined workflows).
        var fromStorage = await _storage.GetWorkflowAsync(workflowId, ct);
        if (fromStorage != null)
        {
            _logger.LogDebug(
                "GetWorkflowAsync: {WorkflowId} found in custom storage",
                workflowId);
            return fromStorage;
        }

        // LOGIC: Try loading from embedded resources (pre-built workflows).
        var prebuilt = await _loader.LoadWorkflowAsync(workflowId, ct);
        if (prebuilt != null)
        {
            _prebuiltCache[workflowId] = prebuilt;
            _logger.LogDebug(
                "GetWorkflowAsync: {WorkflowId} loaded from embedded resources and cached",
                workflowId);
            return prebuilt;
        }

        // LOGIC: Workflow not found in any tier — throw.
        _logger.LogWarning("GetWorkflowAsync: workflow not found: {WorkflowId}", workflowId);
        throw new InvalidOperationException($"Workflow not found: {workflowId}");
    }

    /// <inheritdoc />
    /// <remarks>
    /// Merges the results of <see cref="ListPrebuiltAsync"/> with custom
    /// workflows from storage. Pre-built workflows appear first in the list.
    /// </remarks>
    public async Task<IReadOnlyList<ValidationWorkflowDefinition>> ListWorkflowsAsync(
        CancellationToken ct = default)
    {
        _logger.LogDebug("ListWorkflowsAsync: listing all workflows");

        // LOGIC: Load both pre-built and custom workflows, then merge.
        var prebuilt = await ListPrebuiltAsync(ct);
        var custom = await _storage.ListWorkflowsAsync(ct);

        var all = new List<ValidationWorkflowDefinition>(prebuilt.Count + custom.Count);
        all.AddRange(prebuilt);
        all.AddRange(custom);

        _logger.LogDebug(
            "ListWorkflowsAsync: returning {Total} workflows ({PrebuiltCount} pre-built, {CustomCount} custom)",
            all.Count,
            prebuilt.Count,
            custom.Count);

        return all;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Loads each known pre-built workflow ID via the loader, caching results.
    /// Individual load failures are logged and skipped.
    /// </remarks>
    public async Task<IReadOnlyList<ValidationWorkflowDefinition>> ListPrebuiltAsync(
        CancellationToken ct = default)
    {
        _logger.LogDebug("ListPrebuiltAsync: loading pre-built validation workflows");

        var workflows = new List<ValidationWorkflowDefinition>();

        foreach (var id in PrebuiltIds)
        {
            try
            {
                // LOGIC: Check cache first to avoid redundant loading.
                if (_prebuiltCache.TryGetValue(id, out var cached))
                {
                    workflows.Add(cached);
                    continue;
                }

                // LOGIC: Load from embedded resources and cache.
                var workflow = await _loader.LoadWorkflowAsync(id, ct);
                if (workflow != null)
                {
                    workflows.Add(workflow);
                    _prebuiltCache[id] = workflow;

                    _logger.LogDebug(
                        "ListPrebuiltAsync: loaded and cached {WorkflowId}",
                        id);
                }
                else
                {
                    _logger.LogWarning(
                        "ListPrebuiltAsync: pre-built workflow not found: {WorkflowId}",
                        id);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // LOGIC: Log the error but continue loading remaining presets.
                // A single malformed YAML file should not prevent other presets from loading.
                _logger.LogError(
                    ex,
                    "ListPrebuiltAsync: failed to load pre-built workflow: {WorkflowId} — {Error}",
                    id,
                    ex.Message);
            }
        }

        _logger.LogDebug(
            "ListPrebuiltAsync: returning {Count} pre-built workflows",
            workflows.Count);

        return workflows;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Delegates to <see cref="IValidationWorkflowStorage.SaveWorkflowAsync"/>.
    /// The workflow's <see cref="ValidationWorkflowDefinition.IsPrebuilt"/> flag
    /// is forced to <c>false</c> for custom registrations.
    /// </remarks>
    public async Task<string> RegisterWorkflowAsync(
        ValidationWorkflowDefinition workflow,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "RegisterWorkflowAsync: registering custom workflow {WorkflowId} ({WorkflowName})",
            workflow.Id,
            workflow.Name);

        // LOGIC: Ensure custom workflows are never marked as pre-built.
        var safeWorkflow = workflow with { IsPrebuilt = false };

        var id = await _storage.SaveWorkflowAsync(safeWorkflow, ct);

        _logger.LogInformation(
            "RegisterWorkflowAsync: custom workflow registered: {WorkflowId}",
            id);

        return id;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Prevents updates to pre-built workflows. Delegates to
    /// <see cref="IValidationWorkflowStorage.UpdateWorkflowAsync"/> for custom workflows.
    /// </remarks>
    public async Task UpdateWorkflowAsync(
        string workflowId,
        ValidationWorkflowDefinition workflow,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "UpdateWorkflowAsync: attempting to update {WorkflowId}",
            workflowId);

        // LOGIC: Check if it's a pre-built workflow (cannot be modified).
        if (_prebuiltCache.ContainsKey(workflowId) ||
            Array.Exists(PrebuiltIds, id => id == workflowId))
        {
            _logger.LogWarning(
                "UpdateWorkflowAsync: cannot update pre-built workflow: {WorkflowId}",
                workflowId);
            throw new InvalidOperationException(
                $"Cannot update pre-built workflow: {workflowId}");
        }

        await _storage.UpdateWorkflowAsync(workflowId, workflow, ct);

        _logger.LogInformation(
            "UpdateWorkflowAsync: custom workflow updated: {WorkflowId}",
            workflowId);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Prevents deletion of pre-built workflows. Delegates to
    /// <see cref="IValidationWorkflowStorage.DeleteWorkflowAsync"/> for custom workflows.
    /// </remarks>
    public async Task DeleteWorkflowAsync(
        string workflowId,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "DeleteWorkflowAsync: attempting to delete {WorkflowId}",
            workflowId);

        // LOGIC: Check if it's a pre-built workflow (cannot be deleted).
        if (_prebuiltCache.ContainsKey(workflowId) ||
            Array.Exists(PrebuiltIds, id => id == workflowId))
        {
            _logger.LogWarning(
                "DeleteWorkflowAsync: cannot delete pre-built workflow: {WorkflowId}",
                workflowId);
            throw new InvalidOperationException(
                $"Cannot delete pre-built workflow: {workflowId}");
        }

        await _storage.DeleteWorkflowAsync(workflowId, ct);

        _logger.LogInformation(
            "DeleteWorkflowAsync: custom workflow deleted: {WorkflowId}",
            workflowId);
    }
}
