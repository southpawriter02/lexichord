// -----------------------------------------------------------------------
// <copyright file="QuickActionsPanelViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Agents.Models;
using Lexichord.Modules.Agents.Services;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.ViewModels;

/// <summary>
/// ViewModel for the floating quick actions panel.
/// </summary>
/// <remarks>
/// <para>
/// LOGIC: Manages the visibility, position, and action list of the floating
/// quick actions toolbar. Subscribes to <see cref="IEditorService.SelectionChanged"/>
/// to auto-show/hide the panel when the user selects text. Coordinates action
/// execution through <see cref="IQuickActionsService"/> and previews results
/// via <see cref="IEditorInsertionService"/>.
/// </para>
/// <para>
/// The panel appears after a debounce delay of 500ms to avoid flickering
/// during rapid selection changes (e.g., double-click word select followed
/// by shift-extend).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7d as part of the Quick Actions Panel feature.
/// </para>
/// </remarks>
public partial class QuickActionsPanelViewModel : ObservableObject
{
    private readonly IQuickActionsService _quickActionsService;
    private readonly IEditorInsertionService _insertionService;
    private readonly ISelectionContextService _selectionContextService;
    private readonly IEditorService _editorService;
    private readonly ILogger<QuickActionsPanelViewModel> _logger;

    /// <summary>
    /// Cancellation token source for debounce operations.
    /// </summary>
    /// <remarks>
    /// LOGIC: Each new selection change cancels the previous debounce timer,
    /// ensuring the panel only appears after the user has settled on a selection.
    /// </remarks>
    private CancellationTokenSource? _debounceCts;

    /// <summary>
    /// Indicates whether the quick actions panel is currently visible.
    /// </summary>
    /// <remarks>
    /// LOGIC: The panel is shown when the user has an active selection and
    /// the debounce timer has elapsed. Hidden when selection is cleared,
    /// an action begins execution, or the user dismisses the panel.
    /// </remarks>
    [ObservableProperty]
    private bool _isVisible;

    /// <summary>
    /// Indicates whether an action is currently executing.
    /// </summary>
    /// <remarks>
    /// LOGIC: Set to true when any action in the panel begins execution.
    /// The panel remains visible but all buttons are disabled during execution
    /// to prevent concurrent invocations.
    /// </remarks>
    [ObservableProperty]
    private bool _isExecuting;

    /// <summary>
    /// The vertical position (top) of the panel in editor coordinates.
    /// </summary>
    /// <remarks>
    /// LOGIC: Updated via <see cref="UpdatePosition"/> when the panel is shown.
    /// Positioned above or below the selection based on available viewport space.
    /// </remarks>
    [ObservableProperty]
    private double _top;

    /// <summary>
    /// The horizontal position (left) of the panel in editor coordinates.
    /// </summary>
    /// <remarks>
    /// LOGIC: Updated via <see cref="UpdatePosition"/> when the panel is shown.
    /// Centered on the selection midpoint, clamped to viewport bounds.
    /// </remarks>
    [ObservableProperty]
    private double _left;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuickActionsPanelViewModel"/> class.
    /// </summary>
    /// <param name="quickActionsService">Service for action registry and execution.</param>
    /// <param name="insertionService">Service for showing preview overlays.</param>
    /// <param name="selectionContextService">Service for selection state management.</param>
    /// <param name="editorService">Editor service for selection events and text access.</param>
    /// <param name="logger">Logger for structured logging.</param>
    public QuickActionsPanelViewModel(
        IQuickActionsService quickActionsService,
        IEditorInsertionService insertionService,
        ISelectionContextService selectionContextService,
        IEditorService editorService,
        ILogger<QuickActionsPanelViewModel> logger)
    {
        _quickActionsService = quickActionsService
            ?? throw new ArgumentNullException(nameof(quickActionsService));
        _insertionService = insertionService
            ?? throw new ArgumentNullException(nameof(insertionService));
        _selectionContextService = selectionContextService
            ?? throw new ArgumentNullException(nameof(selectionContextService));
        _editorService = editorService
            ?? throw new ArgumentNullException(nameof(editorService));
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        // LOGIC: Subscribe to selection changes to auto-show/hide the panel.
        // The event is debounced in the handler to avoid rapid show/hide cycles.
        _editorService.SelectionChanged += OnSelectionChanged;

        _logger.LogDebug("QuickActionsPanelViewModel initialized");
    }

    /// <summary>
    /// Gets the collection of action item ViewModels displayed in the panel.
    /// </summary>
    /// <remarks>
    /// LOGIC: Populated by <see cref="ShowAsync"/> with context-filtered actions
    /// from <see cref="IQuickActionsService.GetAvailableActionsAsync"/>. Each item
    /// wraps a <see cref="QuickAction"/> with an execution callback that delegates
    /// to <see cref="ExecuteActionAsync"/>.
    /// </remarks>
    public ObservableCollection<QuickActionItemViewModel> Actions { get; } = new();

    /// <summary>
    /// Shows the quick actions panel with context-filtered actions.
    /// </summary>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Show flow:
    /// <list type="number">
    ///   <item><description>Verify the editor has an active selection</description></item>
    ///   <item><description>Load available actions for the current context</description></item>
    ///   <item><description>Populate the <see cref="Actions"/> collection</description></item>
    ///   <item><description>Update panel position relative to selection</description></item>
    ///   <item><description>Set <see cref="IsVisible"/> to true</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// If no actions are available for the current context (e.g., unsupported
    /// content type or insufficient license), the panel remains hidden.
    /// </para>
    /// </remarks>
    public async Task ShowAsync(CancellationToken ct = default)
    {
        // LOGIC: Do not show if no text is selected.
        if (!_editorService.HasSelection)
        {
            _logger.LogDebug("ShowAsync skipped: no active selection");
            return;
        }

        _logger.LogDebug("ShowAsync: loading available actions");

        var availableActions = await _quickActionsService.GetAvailableActionsAsync(ct);

        // LOGIC: Do not show an empty panel.
        if (availableActions.Count == 0)
        {
            _logger.LogDebug("ShowAsync skipped: no available actions for current context");
            return;
        }

        // LOGIC: Clear and rebuild the actions collection. Each item receives
        // a callback to ExecuteActionAsync for coordinated execution.
        Actions.Clear();

        foreach (var action in availableActions)
        {
            Actions.Add(new QuickActionItemViewModel(action, ExecuteActionAsync));
        }

        _logger.LogDebug(
            "ShowAsync: populated {Count} actions",
            Actions.Count);

        // LOGIC: Update panel position relative to the current selection.
        UpdatePosition();

        IsVisible = true;

        _logger.LogInformation(
            "Quick actions panel shown with {Count} actions",
            Actions.Count);
    }

    /// <summary>
    /// Hides the quick actions panel.
    /// </summary>
    /// <remarks>
    /// LOGIC: Sets <see cref="IsVisible"/> to false and logs the hide event.
    /// Does not clear the actions collection (they are cleared on next show).
    /// </remarks>
    public void Hide()
    {
        IsVisible = false;
        _logger.LogDebug("Quick actions panel hidden");
    }

    /// <summary>
    /// Executes a quick action on the current editor selection.
    /// </summary>
    /// <param name="action">The quick action to execute.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Execution flow:
    /// <list type="number">
    ///   <item><description>Set <see cref="IsExecuting"/> to true (disables all panel buttons)</description></item>
    ///   <item><description>Get the selected text from the editor</description></item>
    ///   <item><description>Invoke <see cref="IQuickActionsService.ExecuteAsync"/> with the action and selection</description></item>
    ///   <item><description>On success, show the result as a preview overlay via <see cref="IEditorInsertionService.ShowPreviewAsync"/></description></item>
    ///   <item><description>Hide the panel (the preview overlay takes over)</description></item>
    ///   <item><description>Reset <see cref="IsExecuting"/> to false</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// If execution fails, the panel remains visible so the user can try another action.
    /// The error is logged but not surfaced directly to the user.
    /// </para>
    /// </remarks>
    public async Task ExecuteActionAsync(QuickAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        _logger.LogDebug(
            "ExecuteActionAsync: starting action {ActionId}",
            action.ActionId);

        IsExecuting = true;

        try
        {
            // LOGIC: Get the current selection text from the editor.
            var selectedText = _editorService.GetSelectedText();

            if (string.IsNullOrEmpty(selectedText))
            {
                _logger.LogWarning(
                    "ExecuteActionAsync: no selected text for action {ActionId}",
                    action.ActionId);
                return;
            }

            _logger.LogDebug(
                "ExecuteActionAsync: invoking action {ActionId} on {CharCount} chars",
                action.ActionId, selectedText.Length);

            // LOGIC: Execute the action through the service.
            var result = await _quickActionsService.ExecuteAsync(
                action.ActionId,
                selectedText);

            if (result.Success)
            {
                _logger.LogDebug(
                    "ExecuteActionAsync: action {ActionId} succeeded, showing preview",
                    action.ActionId);

                // LOGIC: Show the result as a preview overlay at the selection location.
                // The user can then accept or reject the change.
                var location = new TextSpan(
                    _editorService.SelectionStart,
                    _editorService.SelectionLength);

                await _insertionService.ShowPreviewAsync(result.Text, location);

                // LOGIC: Hide the panel — the preview overlay provides accept/reject controls.
                Hide();
            }
            else
            {
                _logger.LogWarning(
                    "ExecuteActionAsync: action {ActionId} failed: {Error}",
                    action.ActionId, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "ExecuteActionAsync: unexpected error executing action {ActionId}",
                action.ActionId);
        }
        finally
        {
            IsExecuting = false;
        }
    }

    /// <summary>
    /// Executes a quick action by its index in the <see cref="Actions"/> collection.
    /// </summary>
    /// <param name="index">The zero-based index of the action to execute.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// LOGIC: Used by keyboard shortcut handlers (e.g., pressing "1" executes
    /// the first action). Silently ignores out-of-range indices as a safety
    /// measure against stale shortcut bindings.
    /// </remarks>
    public async Task ExecuteByIndexAsync(int index)
    {
        if (index < 0 || index >= Actions.Count)
        {
            _logger.LogDebug(
                "ExecuteByIndexAsync: index {Index} out of range (count={Count})",
                index, Actions.Count);
            return;
        }

        await ExecuteActionAsync(Actions[index].Action);
    }

    /// <summary>
    /// Handles editor selection changes with a debounce delay.
    /// </summary>
    /// <param name="sender">The event source (<see cref="IEditorService"/>).</param>
    /// <param name="e">The selection change event arguments.</param>
    /// <remarks>
    /// <para>
    /// LOGIC: Debounce strategy:
    /// <list type="bullet">
    ///   <item><description>If selection is cleared (null/empty), hide the panel immediately</description></item>
    ///   <item><description>If selection is active, cancel any pending debounce and start a new 500ms timer</description></item>
    ///   <item><description>After the timer elapses without cancellation, show the panel</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The 500ms delay is specified in the v0.6.7 SBD success criteria to prevent
    /// the panel from flickering during rapid selection changes.
    /// </para>
    /// </remarks>
    private async void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // LOGIC: If selection is cleared, hide immediately without debounce.
        if (string.IsNullOrEmpty(e.NewSelection))
        {
            _logger.LogDebug("OnSelectionChanged: selection cleared, hiding panel");

            _debounceCts?.Cancel();

            Dispatcher.UIThread.Post(Hide);
            return;
        }

        _logger.LogDebug(
            "OnSelectionChanged: selection detected ({Length} chars), starting debounce",
            e.NewSelection.Length);

        // LOGIC: Cancel any pending debounce timer from a prior selection change.
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();

        var token = _debounceCts.Token;

        try
        {
            // LOGIC: 500ms debounce as specified in v0.6.7 SBD success criteria.
            await Task.Delay(500, token);

            // LOGIC: After debounce, show the panel on the UI thread.
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (!token.IsCancellationRequested)
                {
                    await ShowAsync(token);
                }
            });
        }
        catch (TaskCanceledException)
        {
            // LOGIC: Expected when a new selection change cancels the debounce.
            _logger.LogDebug("OnSelectionChanged: debounce cancelled by new selection");
        }
    }

    /// <summary>
    /// Updates the panel position relative to the current editor selection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// LOGIC: Calculates the panel's Top/Left coordinates based on the
    /// selection's screen position. Currently a stub — full implementation
    /// requires editor-to-screen coordinate translation that depends on
    /// the editor control's layout system.
    /// </para>
    /// <para>
    /// The panel is typically positioned just above the selection. If there
    /// is insufficient space above, it is positioned below.
    /// </para>
    /// </remarks>
    private void UpdatePosition()
    {
        // LOGIC: Position calculation requires coordinate translation from
        // editor character offsets to screen coordinates. This is a stub that
        // will be connected to the editor control's coordinate system when
        // the layout integration is implemented.
        //
        // GetSelectionBounds() is not yet available on IEditorService.
        // When implemented, the flow will be:
        //   var bounds = _editorService.GetSelectionBounds();
        //   Top = bounds.Top - PanelHeight - Margin;
        //   Left = bounds.Left + (bounds.Width / 2) - (PanelWidth / 2);
        //   Clamp to viewport bounds.

        _logger.LogDebug(
            "UpdatePosition: using default position (coordinate translation not yet available)");
    }
}
