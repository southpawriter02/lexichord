// -----------------------------------------------------------------------
// <copyright file="WorkflowDesignerService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Workflows;

/// <summary>
/// Service for managing workflow definitions in the designer.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="IWorkflowDesignerService"/> providing complete CRUD operations
/// for workflow definitions, validation against the agent registry, YAML export/import,
/// and workflow duplication.
/// </para>
/// <para>
/// Storage is currently in-memory via a <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// Future versions may persist to disk or a database.
/// </para>
/// <para>
/// <b>License Requirement:</b> <see cref="LicenseTier.Teams"/> or higher for workflow creation.
/// </para>
/// </remarks>
public class WorkflowDesignerService : IWorkflowDesignerService
{
    // ── Dependencies ────────────────────────────────────────────────────

    private readonly IAgentRegistry _agentRegistry;
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<WorkflowDesignerService> _logger;

    /// <summary>
    /// In-memory storage for workflow definitions.
    /// Keyed by <see cref="WorkflowDefinition.WorkflowId"/>.
    /// </summary>
    private readonly ConcurrentDictionary<string, WorkflowDefinition> _workflows = new();

    // ── Constructor ─────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowDesignerService"/> class.
    /// </summary>
    /// <param name="agentRegistry">Agent registry for validating agent IDs and personas.</param>
    /// <param name="configurationService">Configuration service for reading workflow settings.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public WorkflowDesignerService(
        IAgentRegistry agentRegistry,
        IConfigurationService configurationService,
        ILogger<WorkflowDesignerService> logger)
    {
        _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("WorkflowDesignerService initialized");
    }

    // ── IWorkflowDesignerService Implementation ─────────────────────────

    /// <inheritdoc />
    /// <remarks>
    /// Generates a unique workflow ID prefixed with "wf-" using a GUID.
    /// Initializes an empty step list and default metadata with the current timestamp.
    /// </remarks>
    public WorkflowDefinition CreateNew(string name)
    {
        var workflowId = $"wf-{Guid.NewGuid():N}"[..12];

        var metadata = new WorkflowMetadata(
            Author: "user",
            CreatedAt: DateTime.UtcNow,
            ModifiedAt: DateTime.UtcNow,
            Version: "1.0",
            Tags: Array.Empty<string>(),
            Category: WorkflowCategory.General,
            IsBuiltIn: false,
            RequiredTier: LicenseTier.Teams);

        var workflow = new WorkflowDefinition(
            WorkflowId: workflowId,
            Name: name,
            Description: string.Empty,
            IconName: null,
            Steps: Array.Empty<WorkflowStepDefinition>(),
            Metadata: metadata);

        _logger.LogDebug("Created new workflow {WorkflowId} with name '{Name}'", workflowId, name);

        return workflow;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>Applies the following validation rules, in order:</para>
    /// <list type="number">
    ///   <item><description>Check workflow has a name → Error: MISSING_NAME</description></item>
    ///   <item><description>Check workflow has steps → Error: EMPTY_WORKFLOW</description></item>
    ///   <item><description>For each step: check agent exists → Error: UNKNOWN_AGENT</description></item>
    ///   <item><description>For each step: check persona exists (if specified) → Error: UNKNOWN_PERSONA</description></item>
    ///   <item><description>Check for duplicate step IDs → Error: DUPLICATE_STEP_ID</description></item>
    ///   <item><description>Single step workflow → Warning: SINGLE_STEP</description></item>
    ///   <item><description>All steps have same agent → Warning: SAME_AGENT</description></item>
    ///   <item><description>No conditions used → Warning: NO_CONDITIONS</description></item>
    /// </list>
    /// </remarks>
    public WorkflowValidationResult Validate(WorkflowDefinition workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        var errors = new List<WorkflowValidationError>();
        var warnings = new List<WorkflowValidationWarning>();

        // 1. Check workflow has a name
        if (string.IsNullOrWhiteSpace(workflow.Name))
        {
            errors.Add(new WorkflowValidationError(
                StepId: null,
                Code: "MISSING_NAME",
                Message: "Workflow name is required."));
        }

        // 2. Check workflow has steps
        if (workflow.Steps.Count == 0)
        {
            errors.Add(new WorkflowValidationError(
                StepId: null,
                Code: "EMPTY_WORKFLOW",
                Message: "Workflow must have at least one step."));

            // Return early — no point validating steps if there are none
            _logger.LogInformation("Workflow {WorkflowId} validation: {IsValid}", workflow.WorkflowId, false);
            return new WorkflowValidationResult(false, errors.AsReadOnly(), warnings.AsReadOnly());
        }

        // 3. Validate each step's agent and persona
        var availableAgents = _agentRegistry.AvailableAgents;
        foreach (var step in workflow.Steps)
        {
            // Check agent exists in registry
            var agent = availableAgents.FirstOrDefault(a => a.AgentId == step.AgentId);
            if (agent == null)
            {
                errors.Add(new WorkflowValidationError(
                    StepId: step.StepId,
                    Code: "UNKNOWN_AGENT",
                    Message: $"Agent '{step.AgentId}' is not registered."));
            }
            else if (step.PersonaId != null)
            {
                // Check persona exists for this agent (if specified)
                var config = _agentRegistry.GetConfiguration(step.AgentId);
                if (config?.GetPersona(step.PersonaId) == null)
                {
                    errors.Add(new WorkflowValidationError(
                        StepId: step.StepId,
                        Code: "UNKNOWN_PERSONA",
                        Message: $"Persona '{step.PersonaId}' not found for agent '{step.AgentId}'."));
                }
            }
        }

        // 4. Check for duplicate step IDs
        var duplicateIds = workflow.Steps
            .GroupBy(s => s.StepId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        foreach (var duplicateId in duplicateIds)
        {
            errors.Add(new WorkflowValidationError(
                StepId: duplicateId,
                Code: "DUPLICATE_STEP_ID",
                Message: $"Duplicate step ID: '{duplicateId}'."));
        }

        // 5. Generate warnings

        // Single step workflow
        if (workflow.Steps.Count == 1)
        {
            warnings.Add(new WorkflowValidationWarning(
                StepId: null,
                Code: "SINGLE_STEP",
                Message: "Workflow has only one step. Consider adding more steps for a multi-agent pipeline."));
        }

        // All steps have the same agent
        if (workflow.Steps.Count > 1 &&
            workflow.Steps.Select(s => s.AgentId).Distinct().Count() == 1)
        {
            warnings.Add(new WorkflowValidationWarning(
                StepId: null,
                Code: "SAME_AGENT",
                Message: "All steps use the same agent. Consider using different agents for diverse processing."));
        }

        // No conditions used
        if (workflow.Steps.All(s => s.Condition == null || s.Condition.Type == ConditionType.Always))
        {
            warnings.Add(new WorkflowValidationWarning(
                StepId: null,
                Code: "NO_CONDITIONS",
                Message: "No conditional logic used. All steps will execute sequentially."));
        }

        var isValid = errors.Count == 0;

        _logger.LogInformation("Workflow {WorkflowId} validation: {IsValid}", workflow.WorkflowId, isValid);

        if (!isValid)
        {
            _logger.LogError("Workflow validation failed: {ErrorCount} errors", errors.Count);
        }

        foreach (var warning in warnings)
        {
            _logger.LogWarning("Workflow validation warning: {Code} - {Message}", warning.Code, warning.Message);
        }

        return new WorkflowValidationResult(isValid, errors.AsReadOnly(), warnings.AsReadOnly());
    }

    /// <inheritdoc />
    public Task SaveAsync(WorkflowDefinition workflow, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ct.ThrowIfCancellationRequested();

        // Update the modification timestamp
        var updated = workflow with
        {
            Metadata = workflow.Metadata with { ModifiedAt = DateTime.UtcNow }
        };

        _workflows[updated.WorkflowId] = updated;
        _logger.LogInformation("Workflow {WorkflowId} saved by user", updated.WorkflowId);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<WorkflowDefinition?> LoadAsync(string workflowId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _workflows.TryGetValue(workflowId, out var workflow);

        if (workflow != null)
        {
            _logger.LogDebug("Workflow designer opened for {WorkflowId}", workflowId);
        }

        return Task.FromResult(workflow);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<WorkflowSummary>> ListAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var summaries = _workflows.Values
            .Select(w => new WorkflowSummary(
                WorkflowId: w.WorkflowId,
                Name: w.Name,
                Description: string.IsNullOrEmpty(w.Description) ? null : w.Description,
                StepCount: w.Steps.Count,
                ModifiedAt: w.Metadata.ModifiedAt,
                Category: w.Metadata.Category,
                IsBuiltIn: w.Metadata.IsBuiltIn))
            .OrderByDescending(s => s.ModifiedAt)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<WorkflowSummary>>(summaries);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string workflowId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_workflows.TryRemove(workflowId, out _))
        {
            _logger.LogInformation("Workflow {WorkflowId} deleted", workflowId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Delegates to <see cref="WorkflowYamlSerializer.Serialize"/> for YAML generation.
    /// </remarks>
    public string ExportToYaml(WorkflowDefinition workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        var yaml = WorkflowYamlSerializer.Serialize(workflow);
        _logger.LogInformation("Workflow exported to YAML: {WorkflowId}", workflow.WorkflowId);

        return yaml;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Delegates to <see cref="WorkflowYamlSerializer.Deserialize"/> for YAML parsing.
    /// Throws <see cref="WorkflowImportException"/> if the YAML is malformed.
    /// </remarks>
    public WorkflowDefinition ImportFromYaml(string yaml)
    {
        var workflow = WorkflowYamlSerializer.Deserialize(yaml);
        _logger.LogInformation("Workflow imported from YAML: {WorkflowId}", workflow.WorkflowId);

        return workflow;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Loads the source workflow, creates a deep copy with a new ID, name, and timestamps.
    /// Throws <see cref="KeyNotFoundException"/> if the source workflow is not found.
    /// </remarks>
    public async Task<WorkflowDefinition> DuplicateAsync(
        string workflowId,
        string newName,
        CancellationToken ct = default)
    {
        var source = await LoadAsync(workflowId, ct);
        if (source == null)
        {
            throw new KeyNotFoundException($"Workflow '{workflowId}' not found for duplication.");
        }

        var now = DateTime.UtcNow;
        var duplicate = source with
        {
            WorkflowId = $"wf-{Guid.NewGuid():N}"[..12],
            Name = newName,
            Metadata = source.Metadata with
            {
                CreatedAt = now,
                ModifiedAt = now,
                IsBuiltIn = false
            }
        };

        await SaveAsync(duplicate, ct);
        _logger.LogInformation(
            "Workflow {SourceId} duplicated as {NewId} with name '{NewName}'",
            workflowId, duplicate.WorkflowId, newName);

        return duplicate;
    }
}
