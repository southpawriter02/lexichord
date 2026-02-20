// -----------------------------------------------------------------------
// <copyright file="WorkflowStepViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using Lexichord.Modules.Agents.Workflows;

namespace Lexichord.Modules.Agents.ViewModels;

/// <summary>
/// ViewModel for a single workflow step in the designer canvas.
/// </summary>
/// <remarks>
/// <para>
/// Represents a step card on the workflow designer canvas. Each step
/// corresponds to a registered agent and can optionally specify a persona,
/// a prompt override, and conditional execution logic.
/// </para>
/// <para>
/// Observable properties support two-way data binding with the Avalonia UI
/// for the step configuration panel. Changes to step properties trigger
/// the <see cref="IWorkflowDesignerViewModel.HasUnsavedChanges"/> flag.
/// </para>
/// </remarks>
public partial class WorkflowStepViewModel : ObservableObject
{
    // ── Identity Properties ─────────────────────────────────────────────

    /// <summary>
    /// Unique identifier for this step within the workflow.
    /// </summary>
    public string StepId { get; }

    /// <summary>
    /// ID of the agent to invoke for this step.
    /// </summary>
    [ObservableProperty]
    private string _agentId;

    /// <summary>
    /// Display name of the agent.
    /// </summary>
    public string AgentName { get; }

    /// <summary>
    /// Lucide icon name for the agent.
    /// </summary>
    public string AgentIcon { get; }

    // ── Configuration Properties ────────────────────────────────────────

    /// <summary>
    /// Optional persona ID for the agent. Null uses the default persona.
    /// </summary>
    [ObservableProperty]
    private string? _personaId;

    /// <summary>
    /// Available personas for the agent's persona dropdown.
    /// </summary>
    public IReadOnlyList<PersonaOption> AvailablePersonas { get; }

    /// <summary>
    /// Optional custom prompt to use instead of the agent's default.
    /// </summary>
    [ObservableProperty]
    private string? _promptOverride;

    /// <summary>
    /// Execution order (1-based, ascending).
    /// </summary>
    [ObservableProperty]
    private int _order;

    // ── Condition Properties ────────────────────────────────────────────

    /// <summary>
    /// Type of condition for this step's execution.
    /// </summary>
    [ObservableProperty]
    private ConditionType _conditionType = ConditionType.Always;

    /// <summary>
    /// Expression string for <see cref="Workflows.ConditionType.Expression"/> conditions.
    /// </summary>
    [ObservableProperty]
    private string? _conditionExpression;

    // ── UI State Properties ─────────────────────────────────────────────

    /// <summary>
    /// Whether this step is currently selected in the designer.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Whether this step has a validation error.
    /// </summary>
    [ObservableProperty]
    private bool _hasValidationError;

    /// <summary>
    /// Validation error message, if any.
    /// </summary>
    [ObservableProperty]
    private string? _validationErrorMessage;

    // ── Constructor ─────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowStepViewModel"/> class.
    /// </summary>
    /// <param name="stepId">Unique step identifier.</param>
    /// <param name="agentId">Agent identifier.</param>
    /// <param name="agentName">Agent display name.</param>
    /// <param name="agentIcon">Agent icon name.</param>
    /// <param name="availablePersonas">Available personas for the agent.</param>
    /// <param name="order">Initial execution order.</param>
    public WorkflowStepViewModel(
        string stepId,
        string agentId,
        string agentName,
        string agentIcon,
        IReadOnlyList<PersonaOption> availablePersonas,
        int order)
    {
        StepId = stepId;
        _agentId = agentId;
        AgentName = agentName;
        AgentIcon = agentIcon;
        AvailablePersonas = availablePersonas;
        _order = order;
    }
}
