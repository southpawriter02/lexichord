// Copyright (c) 2024-2026 Lexichord. All rights reserved.
// Licensed under the Lexichord License v1.0.
// See LICENSE file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Lexichord.Modules.Agents.ViewModels;

namespace Lexichord.Modules.Agents.Views;

/// <summary>
/// Code-behind for the Agent Selector UI component (v0.7.1d).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Provides the view for agent discovery, selection,
/// and persona switching in the Co-pilot panel.
/// </para>
/// <para>
/// <strong>Related:</strong>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="ViewModels.AgentSelectorViewModel"/> — ViewModel for this view.</description></item>
///   <item><description><see cref="AgentCardView"/> — Card component for individual agents.</description></item>
/// </list>
/// <para>
/// <strong>Introduced in:</strong> v0.7.1d as part of the Agent Selector UI feature.
/// </para>
/// </remarks>
public partial class AgentSelectorView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentSelectorView"/> class.
    /// </summary>
    public AgentSelectorView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles the trigger tapped event to toggle the dropdown.
    /// </summary>
    private void OnTriggerTapped(object? sender, TappedEventArgs e)
    {
        // LOGIC: Invoke ToggleDropdownCommand from ViewModel
        if (DataContext is AgentSelectorViewModel viewModel)
        {
            if (viewModel.ToggleDropdownCommand.CanExecute(null))
            {
                viewModel.ToggleDropdownCommand.Execute(null);
            }
        }
    }
}
