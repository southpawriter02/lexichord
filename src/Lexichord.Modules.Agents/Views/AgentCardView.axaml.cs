// Copyright (c) 2024-2026 Lexichord. All rights reserved.
// Licensed under the Lexichord License v1.0.
// See LICENSE file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Lexichord.Modules.Agents.ViewModels;

namespace Lexichord.Modules.Agents.Views;

/// <summary>
/// Code-behind for the Agent Card component (v0.7.1d).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Displays a single agent in the Agent Selector dropdown
/// with icon, name, description, tier badge, favorite toggle, and persona selection.
/// </para>
/// <para>
/// <strong>Related:</strong>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="ViewModels.AgentItemViewModel"/> — ViewModel for this view.</description></item>
///   <item><description><see cref="AgentSelectorView"/> — Parent dropdown component.</description></item>
/// </list>
/// <para>
/// <strong>Introduced in:</strong> v0.7.1d as part of the Agent Selector UI feature.
/// </para>
/// </remarks>
public partial class AgentCardView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentCardView"/> class.
    /// </summary>
    public AgentCardView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles the agent card tapped event to trigger selection.
    /// </summary>
    private void OnAgentCardTapped(object? sender, TappedEventArgs e)
    {
        // LOGIC: Find the AgentSelectorViewModel in the visual tree and invoke SelectAgentCommand
        if (DataContext is AgentItemViewModel agentItem)
        {
            // Find parent AgentSelectorViewModel from visual tree
            var parent = this.FindAncestorOfType<AgentSelectorView>();
            if (parent?.DataContext is AgentSelectorViewModel viewModel)
            {
                if (viewModel.SelectAgentCommand.CanExecute(agentItem))
                {
                    viewModel.SelectAgentCommand.Execute(agentItem);
                }
            }
        }
    }
}
