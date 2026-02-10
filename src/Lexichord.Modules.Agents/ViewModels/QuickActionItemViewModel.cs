// -----------------------------------------------------------------------
// <copyright file="QuickActionItemViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Modules.Agents.Models;

namespace Lexichord.Modules.Agents.ViewModels;

/// <summary>
/// ViewModel representing an individual quick action button in the panel.
/// </summary>
/// <remarks>
/// <para>
/// LOGIC: Wraps a <see cref="QuickAction"/> model with execution state and
/// a relay command. Each instance represents one clickable button in the
/// <see cref="QuickActionsPanelViewModel"/>. The execution callback is
/// provided by the parent panel ViewModel to coordinate the action flow.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7d as part of the Quick Actions Panel feature.
/// </para>
/// </remarks>
public partial class QuickActionItemViewModel : ObservableObject
{
    private readonly Func<QuickAction, Task> _executeCallback;

    /// <summary>
    /// Indicates whether this action is currently executing.
    /// </summary>
    /// <remarks>
    /// LOGIC: Bound to UI to show a loading indicator on the action button
    /// while the agent processes the request. Set to true before execution
    /// begins and false after completion (success or failure).
    /// </remarks>
    [ObservableProperty]
    private bool _isExecuting;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuickActionItemViewModel"/> class.
    /// </summary>
    /// <param name="action">The quick action model this ViewModel represents.</param>
    /// <param name="executeCallback">
    /// Callback invoked when the user triggers this action. Provided by the
    /// parent <see cref="QuickActionsPanelViewModel"/> to coordinate execution
    /// through <see cref="Services.IQuickActionsService"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="action"/> or <paramref name="executeCallback"/> is null.
    /// </exception>
    public QuickActionItemViewModel(
        QuickAction action,
        Func<QuickAction, Task> executeCallback)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
        _executeCallback = executeCallback ?? throw new ArgumentNullException(nameof(executeCallback));
    }

    /// <summary>
    /// Gets the underlying <see cref="QuickAction"/> model.
    /// </summary>
    /// <remarks>
    /// LOGIC: Exposes the action metadata (Name, Description, Icon,
    /// KeyboardShortcut) for data binding in the panel UI.
    /// </remarks>
    public QuickAction Action { get; }

    /// <summary>
    /// Command to execute this quick action.
    /// </summary>
    /// <remarks>
    /// <para>
    /// LOGIC: Delegates to the parent panel's execution callback, which
    /// handles selection retrieval, agent invocation, and result preview.
    /// The <see cref="IsExecuting"/> flag is managed around the callback
    /// to provide visual feedback.
    /// </para>
    /// <para>
    /// If the callback throws, <see cref="IsExecuting"/> is still reset
    /// to false to prevent the button from appearing permanently disabled.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private async Task ExecuteAsync()
    {
        IsExecuting = true;

        try
        {
            await _executeCallback(Action);
        }
        finally
        {
            IsExecuting = false;
        }
    }
}
