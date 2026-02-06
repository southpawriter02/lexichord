// -----------------------------------------------------------------------
// <copyright file="CoPilotView.axaml.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Lexichord.Modules.Agents.Chat.ViewModels;

namespace Lexichord.Modules.Agents.Chat.Views;

/// <summary>
/// Code-behind for the Co-Pilot chat view, handling auto-scroll behavior.
/// </summary>
/// <remarks>
/// <para>
/// This code-behind wires the <see cref="CoPilotViewModel.ScrollToBottomRequested"/>
/// event to the <see cref="ScrollViewer"/> control to ensure the user always sees
/// the latest streaming content.
/// </para>
/// <para>
/// The scroll-to-bottom operation is dispatched at <see cref="DispatcherPriority.Render"/>
/// to ensure the layout has been completed before scrolling, preventing stale
/// scroll positions.
/// </para>
/// <para>
/// <strong>Event Lifecycle:</strong>
/// <list type="bullet">
///   <item>Subscribe to <c>ScrollToBottomRequested</c> when DataContext is set</item>
///   <item>Unsubscribe when the control is unloaded to prevent memory leaks</item>
/// </list>
/// </para>
/// </remarks>
public partial class CoPilotView : UserControl
{
    /// <summary>
    /// Initializes a new instance of <see cref="CoPilotView"/>.
    /// </summary>
    public CoPilotView()
    {
        InitializeComponent();

        // Subscribe to scroll requests when DataContext is set
        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>
    /// Handles DataContext changes to wire up the scroll event.
    /// </summary>
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is CoPilotViewModel viewModel)
        {
            viewModel.ScrollToBottomRequested += OnScrollToBottomRequested;
        }
    }

    /// <summary>
    /// Handles scroll-to-bottom requests from the ViewModel.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="DispatcherPriority.Render"/> to ensure the layout
    /// has been updated before attempting to scroll, which prevents the
    /// scroll position from being calculated against stale layout data.
    /// </remarks>
    private void OnScrollToBottomRequested(object? sender, EventArgs e)
    {
        // Use dispatcher to ensure layout is complete
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var scrollViewer = this.FindControl<ScrollViewer>("MessageScrollViewer");
            if (scrollViewer is not null)
            {
                // Smooth scroll to bottom
                scrollViewer.ScrollToEnd();
            }
        }, DispatcherPriority.Render);
    }

    /// <summary>
    /// Cleanup: unsubscribe from ViewModel events when unloaded.
    /// </summary>
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        if (DataContext is CoPilotViewModel viewModel)
        {
            viewModel.ScrollToBottomRequested -= OnScrollToBottomRequested;
        }
    }
}
