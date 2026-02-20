// -----------------------------------------------------------------------
// <copyright file="IWorkflowDesignerViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Modules.Agents.Workflows;

namespace Lexichord.Modules.Agents.ViewModels;

/// <summary>
/// ViewModel for the workflow designer.
/// </summary>
/// <remarks>
/// <para>
/// Defines the contract for the workflow designer UI, providing properties
/// and commands for creating, editing, validating, and persisting workflow
/// definitions through the visual designer interface.
/// </para>
/// <para>
/// The ViewModel bridges the <see cref="IWorkflowDesignerService"/> (business logic)
/// with the Avalonia View layer (UI rendering and interaction).
/// </para>
/// <para>
/// <b>License Requirement:</b> Editing requires
/// <see cref="Lexichord.Abstractions.Contracts.LicenseTier.Teams"/> or higher.
/// The <see cref="CanEdit"/> property reflects the current license state.
/// </para>
/// </remarks>
public interface IWorkflowDesignerViewModel
{
    /// <summary>
    /// Current workflow being edited.
    /// </summary>
    WorkflowDefinition? CurrentWorkflow { get; }

    /// <summary>
    /// Workflow name (bindable).
    /// </summary>
    string WorkflowName { get; set; }

    /// <summary>
    /// Workflow description (bindable).
    /// </summary>
    string WorkflowDescription { get; set; }

    /// <summary>
    /// Steps in the current workflow.
    /// </summary>
    ObservableCollection<WorkflowStepViewModel> Steps { get; }

    /// <summary>
    /// Currently selected step (or null).
    /// </summary>
    WorkflowStepViewModel? SelectedStep { get; set; }

    /// <summary>
    /// Available agents for the palette.
    /// </summary>
    IReadOnlyList<AgentPaletteItemViewModel> AvailableAgents { get; }

    /// <summary>
    /// Current validation result.
    /// </summary>
    WorkflowValidationResult? ValidationResult { get; }

    /// <summary>
    /// Whether the workflow has unsaved changes.
    /// </summary>
    bool HasUnsavedChanges { get; }

    /// <summary>
    /// Whether the current user can edit (Teams license or higher).
    /// </summary>
    bool CanEdit { get; }

    // ── Commands ────────────────────────────────────────────────────────

    /// <summary>Creates a new empty workflow.</summary>
    IRelayCommand NewWorkflowCommand { get; }

    /// <summary>Saves the current workflow to storage.</summary>
    IRelayCommand SaveWorkflowCommand { get; }

    /// <summary>Loads a workflow from a summary selection.</summary>
    IRelayCommand<WorkflowSummary> LoadWorkflowCommand { get; }

    /// <summary>Validates the current workflow.</summary>
    IRelayCommand ValidateCommand { get; }

    /// <summary>Adds a step for the specified agent ID.</summary>
    IRelayCommand<string> AddStepCommand { get; }

    /// <summary>Removes the specified step from the workflow.</summary>
    IRelayCommand<WorkflowStepViewModel> RemoveStepCommand { get; }

    /// <summary>Reorders a step from one position to another.</summary>
    IRelayCommand<(int from, int to)> ReorderStepCommand { get; }

    /// <summary>Exports the current workflow to YAML.</summary>
    IRelayCommand ExportYamlCommand { get; }

    /// <summary>Imports a workflow from YAML.</summary>
    IRelayCommand ImportYamlCommand { get; }

    /// <summary>Runs the current workflow asynchronously.</summary>
    IAsyncRelayCommand RunWorkflowCommand { get; }
}
