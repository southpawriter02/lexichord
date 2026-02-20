// -----------------------------------------------------------------------
// <copyright file="WorkflowDesignerViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Agents.Workflows;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.ViewModels;

/// <summary>
/// ViewModel for the workflow designer.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="IWorkflowDesignerViewModel"/> to manage the full lifecycle
/// of workflow editing in the designer UI. Creates, validates, and persists
/// workflow definitions through <see cref="IWorkflowDesignerService"/>.
/// </para>
/// <para>
/// Populates the agent palette from <see cref="IAgentRegistry"/> and enforces
/// license gating via <see cref="ILicenseContext"/>. Only users with
/// <see cref="LicenseTier.Teams"/> or higher can edit workflows.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7a as part of the Workflow Designer UI feature.
/// </para>
/// </remarks>
public partial class WorkflowDesignerViewModel : ObservableObject, IWorkflowDesignerViewModel
{
    // ── Dependencies ────────────────────────────────────────────────────

    private readonly IWorkflowDesignerService _designerService;
    private readonly IAgentRegistry _agentRegistry;
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<WorkflowDesignerViewModel> _logger;

    // ── Observable Properties ───────────────────────────────────────────

    /// <inheritdoc />
    [ObservableProperty]
    private WorkflowDefinition? _currentWorkflow;

    /// <inheritdoc />
    [ObservableProperty]
    private string _workflowName = string.Empty;

    /// <inheritdoc />
    [ObservableProperty]
    private string _workflowDescription = string.Empty;

    /// <inheritdoc />
    [ObservableProperty]
    private WorkflowStepViewModel? _selectedStep;

    /// <inheritdoc />
    [ObservableProperty]
    private WorkflowValidationResult? _validationResult;

    /// <inheritdoc />
    [ObservableProperty]
    private bool _hasUnsavedChanges;

    // ── Constructor ─────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowDesignerViewModel"/> class.
    /// </summary>
    /// <param name="designerService">Service for workflow CRUD and validation.</param>
    /// <param name="agentRegistry">Registry for discovering available agents.</param>
    /// <param name="licenseContext">License context for tier checking.</param>
    public WorkflowDesignerViewModel(
        IWorkflowDesignerService designerService,
        IAgentRegistry agentRegistry,
        ILicenseContext licenseContext)
    {
        _designerService = designerService ?? throw new ArgumentNullException(nameof(designerService));
        _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<WorkflowDesignerViewModel>.Instance;

        // Populate available agents from registry
        AvailableAgents = _agentRegistry.AvailableAgents
            .Select(a =>
            {
                var config = _agentRegistry.GetConfiguration(a.AgentId);
                return new AgentPaletteItemViewModel(
                    AgentId: a.AgentId,
                    Name: a.Name,
                    Description: a.Description,
                    Icon: config?.Icon ?? "box",
                    IsAvailable: true,
                    RequiredTier: LicenseTier.Teams);
            })
            .ToList()
            .AsReadOnly();

        // Initialize commands
        NewWorkflowCommand = new RelayCommand(ExecuteNewWorkflow);
        SaveWorkflowCommand = new RelayCommand(ExecuteSaveWorkflow);
        LoadWorkflowCommand = new RelayCommand<WorkflowSummary>(ExecuteLoadWorkflow);
        ValidateCommand = new RelayCommand(ExecuteValidate);
        AddStepCommand = new RelayCommand<string>(ExecuteAddStep);
        RemoveStepCommand = new RelayCommand<WorkflowStepViewModel>(ExecuteRemoveStep);
        ReorderStepCommand = new RelayCommand<(int from, int to)>(ExecuteReorderStep);
        ExportYamlCommand = new RelayCommand(ExecuteExportYaml);
        ImportYamlCommand = new RelayCommand(ExecuteImportYaml);
        RunWorkflowCommand = new AsyncRelayCommand(ExecuteRunWorkflowAsync);

        _logger.LogDebug("WorkflowDesignerViewModel initialized with {AgentCount} available agents",
            AvailableAgents.Count);
    }

    // ── Public Properties ───────────────────────────────────────────────

    /// <inheritdoc />
    public ObservableCollection<WorkflowStepViewModel> Steps { get; } = new();

    /// <inheritdoc />
    public IReadOnlyList<AgentPaletteItemViewModel> AvailableAgents { get; }

    /// <inheritdoc />
    public bool CanEdit => _licenseContext.Tier >= LicenseTier.Teams;

    // ── Commands ────────────────────────────────────────────────────────

    /// <inheritdoc />
    public IRelayCommand NewWorkflowCommand { get; }

    /// <inheritdoc />
    public IRelayCommand SaveWorkflowCommand { get; }

    /// <inheritdoc />
    public IRelayCommand<WorkflowSummary> LoadWorkflowCommand { get; }

    /// <inheritdoc />
    public IRelayCommand ValidateCommand { get; }

    /// <inheritdoc />
    public IRelayCommand<string> AddStepCommand { get; }

    /// <inheritdoc />
    public IRelayCommand<WorkflowStepViewModel> RemoveStepCommand { get; }

    /// <inheritdoc />
    public IRelayCommand<(int from, int to)> ReorderStepCommand { get; }

    /// <inheritdoc />
    public IRelayCommand ExportYamlCommand { get; }

    /// <inheritdoc />
    public IRelayCommand ImportYamlCommand { get; }

    /// <inheritdoc />
    public IAsyncRelayCommand RunWorkflowCommand { get; }

    // ── Command Implementations ─────────────────────────────────────────

    /// <summary>
    /// Creates a new empty workflow and resets the designer state.
    /// </summary>
    private void ExecuteNewWorkflow()
    {
        CurrentWorkflow = _designerService.CreateNew("New Workflow");
        WorkflowName = CurrentWorkflow.Name;
        WorkflowDescription = CurrentWorkflow.Description;
        Steps.Clear();
        SelectedStep = null;
        ValidationResult = null;
        HasUnsavedChanges = false;

        _logger.LogDebug("New workflow created: {WorkflowId}", CurrentWorkflow.WorkflowId);
    }

    /// <summary>
    /// Saves the current workflow to storage.
    /// </summary>
    private async void ExecuteSaveWorkflow()
    {
        if (CurrentWorkflow == null) return;

        var updated = BuildCurrentDefinition();
        await _designerService.SaveAsync(updated);
        CurrentWorkflow = updated;
        HasUnsavedChanges = false;

        _logger.LogInformation("Workflow {WorkflowId} saved by user", updated.WorkflowId);
    }

    /// <summary>
    /// Loads a workflow from a summary selection.
    /// </summary>
    /// <param name="summary">The workflow summary to load.</param>
    private async void ExecuteLoadWorkflow(WorkflowSummary? summary)
    {
        if (summary == null) return;

        var loaded = await _designerService.LoadAsync(summary.WorkflowId);
        if (loaded == null) return;

        CurrentWorkflow = loaded;
        WorkflowName = loaded.Name;
        WorkflowDescription = loaded.Description;
        Steps.Clear();

        foreach (var step in loaded.Steps)
        {
            var agent = _agentRegistry.AvailableAgents.FirstOrDefault(a => a.AgentId == step.AgentId);
            var config = _agentRegistry.GetConfiguration(step.AgentId);
            Steps.Add(new WorkflowStepViewModel(
                stepId: step.StepId,
                agentId: step.AgentId,
                agentName: agent?.Name ?? step.AgentId,
                agentIcon: config?.Icon ?? "box",
                availablePersonas: Array.Empty<PersonaOption>(),
                order: step.Order));
        }

        SelectedStep = null;
        ValidationResult = null;
        HasUnsavedChanges = false;

        _logger.LogDebug("Workflow designer opened for {WorkflowId}", loaded.WorkflowId);
    }

    /// <summary>
    /// Validates the current workflow and updates the result.
    /// </summary>
    private void ExecuteValidate()
    {
        if (CurrentWorkflow == null) return;

        var definition = BuildCurrentDefinition();
        ValidationResult = _designerService.Validate(definition);

        _logger.LogDebug("Workflow validated: IsValid={IsValid}", ValidationResult.IsValid);
    }

    /// <summary>
    /// Adds a step for the specified agent ID to the workflow.
    /// </summary>
    /// <param name="agentId">The agent ID to add as a step.</param>
    private void ExecuteAddStep(string? agentId)
    {
        if (agentId == null || CurrentWorkflow == null) return;

        var agent = _agentRegistry.AvailableAgents.FirstOrDefault(a => a.AgentId == agentId);
        if (agent == null) return;

        var config = _agentRegistry.GetConfiguration(agentId);
        var stepId = $"step-{Guid.NewGuid():N}"[..12];
        var order = Steps.Count + 1;

        var stepVm = new WorkflowStepViewModel(
            stepId: stepId,
            agentId: agentId,
            agentName: agent.Name,
            agentIcon: config?.Icon ?? "box",
            availablePersonas: Array.Empty<PersonaOption>(),
            order: order);

        Steps.Add(stepVm);
        SelectedStep = stepVm;
        HasUnsavedChanges = true;

        _logger.LogDebug("Step {StepId} added to workflow: agent={AgentId}", stepId, agentId);
    }

    /// <summary>
    /// Removes the specified step from the workflow.
    /// </summary>
    /// <param name="step">The step to remove.</param>
    private void ExecuteRemoveStep(WorkflowStepViewModel? step)
    {
        if (step == null) return;

        Steps.Remove(step);

        // Re-number remaining steps
        for (var i = 0; i < Steps.Count; i++)
        {
            Steps[i].Order = i + 1;
        }

        if (SelectedStep == step)
        {
            SelectedStep = null;
        }

        HasUnsavedChanges = true;
        _logger.LogDebug("Step {StepId} removed from workflow", step.StepId);
    }

    /// <summary>
    /// Reorders a step from one position to another.
    /// </summary>
    /// <param name="positions">Tuple of (fromIndex, toIndex).</param>
    private void ExecuteReorderStep((int from, int to) positions)
    {
        var (from, to) = positions;

        if (from < 0 || from >= Steps.Count || to < 0 || to >= Steps.Count || from == to)
            return;

        Steps.Move(from, to);

        // Update order properties
        for (var i = 0; i < Steps.Count; i++)
        {
            Steps[i].Order = i + 1;
        }

        HasUnsavedChanges = true;
        _logger.LogDebug("Steps reordered: {FromIndex} -> {ToIndex}", from, to);
    }

    /// <summary>
    /// Exports the current workflow to YAML.
    /// </summary>
    private void ExecuteExportYaml()
    {
        if (CurrentWorkflow == null) return;

        var definition = BuildCurrentDefinition();
        var yaml = _designerService.ExportToYaml(definition);

        _logger.LogInformation("Workflow exported to YAML: {WorkflowId}", definition.WorkflowId);
    }

    /// <summary>
    /// Imports a workflow from YAML. Currently a no-op stub
    /// (YAML content would be provided by a file dialog in the View layer).
    /// </summary>
    private void ExecuteImportYaml()
    {
        _logger.LogDebug("Import YAML requested — awaiting View layer file selection");
    }

    /// <summary>
    /// Runs the current workflow asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private Task ExecuteRunWorkflowAsync()
    {
        _logger.LogDebug("Run workflow requested — execution engine not yet implemented (v0.7.7b)");
        return Task.CompletedTask;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="WorkflowDefinition"/> from the current ViewModel state.
    /// </summary>
    /// <returns>A snapshot of the current workflow definition.</returns>
    private WorkflowDefinition BuildCurrentDefinition()
    {
        var steps = Steps.Select(s =>
        {
            WorkflowStepCondition? condition = null;
            if (s.ConditionType != ConditionType.Always)
            {
                condition = new WorkflowStepCondition(
                    s.ConditionExpression ?? string.Empty,
                    s.ConditionType);
            }

            return new WorkflowStepDefinition(
                StepId: s.StepId,
                AgentId: s.AgentId,
                PersonaId: s.PersonaId,
                PromptOverride: s.PromptOverride,
                Order: s.Order,
                Condition: condition,
                InputMappings: null,
                OutputMappings: null);
        }).ToList().AsReadOnly();

        return CurrentWorkflow! with
        {
            Name = WorkflowName,
            Description = WorkflowDescription,
            Steps = steps
        };
    }
}
