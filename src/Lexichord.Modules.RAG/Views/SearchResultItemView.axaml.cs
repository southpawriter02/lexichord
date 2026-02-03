// =============================================================================
// File: SearchResultItemView.axaml.cs
// Project: Lexichord.Modules.RAG
// Description: Code-behind for the SearchResultItemView UserControl.
// =============================================================================
// LOGIC: Handles user interaction events for search result items.
//   - DoubleTapped: Navigates to the source document at the chunk's location.
//   - ShowUpgradePromptRequested: Raises routed event for upgrade prompt (v0.5.3d).
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.4.6b: SearchResultItemViewModel with NavigateCommand
//   - v0.5.3d: ContextPreviewViewModel with ShowUpgradePromptRequested
// =============================================================================

using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Lexichord.Modules.RAG.ViewModels;

namespace Lexichord.Modules.RAG.Views;

/// <summary>
/// Code-behind for the SearchResultItemView UserControl.
/// </summary>
/// <remarks>
/// <para>
/// Handles user interaction events for search result items, including:
/// <list type="bullet">
///   <item><description>Double-tap gesture to navigate to the source document.</description></item>
///   <item><description>Upgrade prompt display for context expansion when unlicensed (v0.5.3d).</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Upgrade Prompt Pattern (v0.5.3d):</b> When an unlicensed user attempts to
/// expand context, the <see cref="ContextPreviewViewModel"/> sets
/// <see cref="ContextPreviewViewModel.ShowUpgradePromptRequested"/> to <c>true</c>.
/// This control observes that property and raises the <see cref="UpgradePromptRequestedEvent"/>
/// routed event, which bubbles up to be handled by a parent control or window
/// that has access to the upgrade dialog.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.6b as part of the Search Result Item View,
/// enhanced in v0.5.3d with upgrade prompt handling.
/// </para>
/// </remarks>
public partial class SearchResultItemView : UserControl
{
    private ContextPreviewViewModel? _currentContextPreview;

    /// <summary>
    /// Routed event raised when an upgrade prompt is requested for context expansion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Introduced in:</b> v0.5.3d for context expansion license gating.
    /// </para>
    /// <para>
    /// Handlers should display an upgrade dialog (e.g., UpgradePromptDialog) and
    /// then acknowledge the request via the event args or directly on the ViewModel.
    /// </para>
    /// </remarks>
    public static readonly RoutedEvent<UpgradePromptRequestedEventArgs> UpgradePromptRequestedEvent =
        RoutedEvent.Register<SearchResultItemView, UpgradePromptRequestedEventArgs>(
            nameof(UpgradePromptRequested),
            RoutingStrategies.Bubble);

    /// <summary>
    /// Event raised when an upgrade prompt is requested for context expansion.
    /// </summary>
    public event EventHandler<UpgradePromptRequestedEventArgs>? UpgradePromptRequested
    {
        add => AddHandler(UpgradePromptRequestedEvent, value);
        remove => RemoveHandler(UpgradePromptRequestedEvent, value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchResultItemView"/> class.
    /// </summary>
    public SearchResultItemView()
    {
        InitializeComponent();

        // LOGIC: Subscribe to DataContext changes to track context preview state (v0.5.3d).
        DataContextProperty.Changed.AddClassHandler<SearchResultItemView>(OnDataContextChanged);
    }

    /// <summary>
    /// Handles the DoubleTapped event to navigate to the source document.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    /// <remarks>
    /// LOGIC: Retrieves the DataContext as SearchResultItemViewModel and
    /// executes the NavigateCommand if available and can execute.
    /// </remarks>
    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is SearchResultItemViewModel vm && vm.NavigateCommand.CanExecute(null))
        {
            vm.NavigateCommand.Execute(null);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles DataContext changes to subscribe to ContextPreview property changes.
    /// </summary>
    private static void OnDataContextChanged(SearchResultItemView view, AvaloniaPropertyChangedEventArgs e)
    {
        // Unsubscribe from previous context preview
        view.UnsubscribeFromContextPreview();

        // Subscribe to new context preview if available
        if (e.NewValue is SearchResultItemViewModel vm && vm.ContextPreview is not null)
        {
            view.SubscribeToContextPreview(vm.ContextPreview);
        }
    }

    /// <summary>
    /// Subscribes to the ContextPreviewViewModel's property changes.
    /// </summary>
    /// <param name="contextPreview">The context preview ViewModel to observe.</param>
    private void SubscribeToContextPreview(ContextPreviewViewModel contextPreview)
    {
        _currentContextPreview = contextPreview;
        contextPreview.PropertyChanged += OnContextPreviewPropertyChanged;
    }

    /// <summary>
    /// Unsubscribes from the current ContextPreviewViewModel.
    /// </summary>
    private void UnsubscribeFromContextPreview()
    {
        if (_currentContextPreview is not null)
        {
            _currentContextPreview.PropertyChanged -= OnContextPreviewPropertyChanged;
            _currentContextPreview = null;
        }
    }

    /// <summary>
    /// Handles property changes on the ContextPreviewViewModel.
    /// </summary>
    private void OnContextPreviewPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ContextPreviewViewModel.ShowUpgradePromptRequested))
            return;

        if (sender is not ContextPreviewViewModel contextPreview)
            return;

        if (!contextPreview.ShowUpgradePromptRequested)
            return;

        // LOGIC: Raise routed event for upgrade prompt (v0.5.3d).
        // A parent control or window should handle this event and show
        // the upgrade dialog, then acknowledge via the event args.
        var args = new UpgradePromptRequestedEventArgs(
            UpgradePromptRequestedEvent,
            this,
            contextPreview);

        RaiseEvent(args);

        // LOGIC: If no handler acknowledged the prompt, do it ourselves
        // to prevent the ViewModel from staying in an invalid state.
        if (!args.Acknowledged)
        {
            contextPreview.AcknowledgeUpgradePrompt();
        }
    }

    /// <summary>
    /// Called when the control is being detached from the visual tree.
    /// </summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        // LOGIC: Clean up subscriptions when detached.
        UnsubscribeFromContextPreview();
    }
}

/// <summary>
/// Event args for the <see cref="SearchResultItemView.UpgradePromptRequestedEvent"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provides access to the <see cref="ContextPreviewViewModel"/> that requested the
/// upgrade prompt, allowing handlers to acknowledge the request after showing
/// the dialog.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.3d for context expansion license gating.
/// </para>
/// </remarks>
public class UpgradePromptRequestedEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="UpgradePromptRequestedEventArgs"/>.
    /// </summary>
    /// <param name="routedEvent">The routed event.</param>
    /// <param name="source">The event source.</param>
    /// <param name="contextPreview">The context preview ViewModel.</param>
    public UpgradePromptRequestedEventArgs(
        RoutedEvent routedEvent,
        object source,
        ContextPreviewViewModel contextPreview)
        : base(routedEvent, source)
    {
        ContextPreview = contextPreview ?? throw new ArgumentNullException(nameof(contextPreview));
    }

    /// <summary>
    /// Gets the context preview ViewModel that requested the upgrade prompt.
    /// </summary>
    public ContextPreviewViewModel ContextPreview { get; }

    /// <summary>
    /// Gets or sets whether the upgrade prompt has been acknowledged.
    /// </summary>
    /// <remarks>
    /// Set this to <c>true</c> after showing the upgrade dialog to prevent
    /// the SearchResultItemView from auto-acknowledging.
    /// </remarks>
    public bool Acknowledged { get; set; }

    /// <summary>
    /// Acknowledges the upgrade prompt request.
    /// </summary>
    /// <remarks>
    /// Call this after showing the upgrade dialog. Sets <see cref="Acknowledged"/>
    /// to <c>true</c> and calls <see cref="ContextPreviewViewModel.AcknowledgeUpgradePrompt"/>.
    /// </remarks>
    public void Acknowledge()
    {
        Acknowledged = true;
        ContextPreview.AcknowledgeUpgradePrompt();
    }
}
