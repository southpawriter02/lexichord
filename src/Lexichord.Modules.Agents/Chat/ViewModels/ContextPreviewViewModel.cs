// -----------------------------------------------------------------------
// <copyright file="ContextPreviewViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Modules.Agents.Context;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Chat.ViewModels;

/// <summary>
/// ViewModel for the Context Preview panel that displays real-time context
/// assembly results, strategy toggles, and token budget information.
/// </summary>
/// <remarks>
/// <para>
/// The Context Preview panel provides transparency into the context
/// provided to AI agents by displaying:
/// </para>
/// <list type="bullet">
///   <item><description>All context fragments with labels, token counts, and expandable content</description></item>
///   <item><description>Token budget usage with visual progress indicator</description></item>
///   <item><description>Strategy enable/disable toggles for user control</description></item>
///   <item><description>Assembly duration for performance monitoring</description></item>
/// </list>
/// <para>
/// <strong>Event Bridging:</strong>
/// The ViewModel subscribes to <see cref="ContextPreviewBridge"/> events
/// (which originate from MediatR notifications) and dispatches UI updates
/// via an injectable <see cref="Action{T}"/> delegate. This pattern avoids
/// direct dependency on Avalonia's dispatcher for testability.
/// </para>
/// <para>
/// <strong>Lifecycle:</strong>
/// Registered as Transient in DI. Subscribes to bridge events in the constructor
/// and unsubscribes in <see cref="Dispose"/>. Callers must dispose the instance
/// when the panel is closed.
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2d as part of the Context Preview Panel.
/// </para>
/// </remarks>
/// <seealso cref="ContextPreviewBridge"/>
/// <seealso cref="FragmentViewModel"/>
/// <seealso cref="StrategyToggleItem"/>
/// <seealso cref="IContextOrchestrator"/>
internal sealed partial class ContextPreviewViewModel : ObservableObject, IDisposable
{
    #region Constants

    /// <summary>
    /// Event ID for ViewModel initialized.
    /// </summary>
    private const int EventIdInitialized = 7105;

    /// <summary>
    /// Event ID for context preview updated with new fragments.
    /// </summary>
    private const int EventIdPreviewUpdated = 7106;

    /// <summary>
    /// Event ID for strategy toggled via the panel.
    /// </summary>
    private const int EventIdStrategyToggled = 7107;

    /// <summary>
    /// Event ID for copy all context action.
    /// </summary>
    private const int EventIdCopyAll = 7108;

    /// <summary>
    /// Event ID for ViewModel disposed.
    /// </summary>
    private const int EventIdDisposed = 7109;

    /// <summary>
    /// Default token budget when no assembly result has been received.
    /// </summary>
    private const int DefaultTokenBudget = 8000;

    #endregion

    #region Fields

    private readonly IContextOrchestrator _orchestrator;
    private readonly ContextPreviewBridge _bridge;
    private readonly ILogger<ContextPreviewViewModel> _logger;
    private readonly Action<Action> _dispatch;

    private bool _disposed;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextPreviewViewModel"/> class.
    /// </summary>
    /// <param name="orchestrator">The context orchestrator for strategy management.</param>
    /// <param name="bridge">The MediatR-to-ViewModel event bridge for assembly notifications.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <param name="dispatch">
    /// Optional UI thread dispatch delegate. Defaults to synchronous invocation
    /// (suitable for unit tests). In production, pass <c>Avalonia.Threading.Dispatcher.UIThread.Post</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="orchestrator"/>, <paramref name="bridge"/>,
    /// or <paramref name="logger"/> is null.
    /// </exception>
    public ContextPreviewViewModel(
        IContextOrchestrator orchestrator,
        ContextPreviewBridge bridge,
        ILogger<ContextPreviewViewModel> logger,
        Action<Action>? dispatch = null)
    {
        ArgumentNullException.ThrowIfNull(orchestrator);
        ArgumentNullException.ThrowIfNull(bridge);
        ArgumentNullException.ThrowIfNull(logger);

        _orchestrator = orchestrator;
        _bridge = bridge;
        _logger = logger;
        _dispatch = dispatch ?? (action => action());

        // LOGIC: Initialize strategy toggles from the orchestrator's registered strategies.
        InitializeStrategies();

        // LOGIC: Subscribe to bridge events for real-time UI updates.
        _bridge.ContextAssembled += OnContextAssembled;
        _bridge.StrategyToggled += OnStrategyToggled;

        _logger.Log(
            LogLevel.Debug,
            new EventId(EventIdInitialized, nameof(ContextPreviewViewModel)),
            "ContextPreviewViewModel initialized with {StrategyCount} strategies",
            Strategies.Count);
    }

    #endregion

    #region Observable Properties

    /// <summary>
    /// Gets or sets a value indicating whether context assembly is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isAssembling;

    /// <summary>
    /// Gets or sets the total token count across all fragments.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BudgetPercentage))]
    [NotifyPropertyChangedFor(nameof(BudgetStatusText))]
    [NotifyPropertyChangedFor(nameof(HasContext))]
    private int _totalTokens;

    /// <summary>
    /// Gets or sets the token budget limit for context assembly.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BudgetPercentage))]
    [NotifyPropertyChangedFor(nameof(BudgetStatusText))]
    private int _tokenBudget = DefaultTokenBudget;

    /// <summary>
    /// Gets or sets the duration of the last context assembly operation.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DurationText))]
    private TimeSpan _assemblyDuration;

    /// <summary>
    /// Gets or sets a value indicating whether the panel is expanded.
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded = true;

    #endregion

    #region Collections

    /// <summary>
    /// Gets the collection of fragment ViewModels for display.
    /// </summary>
    /// <remarks>
    /// Updated whenever a <see cref="ContextAssembledEvent"/> is received.
    /// Each element wraps a <see cref="ContextFragment"/> with UI-specific properties.
    /// </remarks>
    public ObservableCollection<FragmentViewModel> Fragments { get; } = new();

    /// <summary>
    /// Gets the collection of strategy toggle items for the settings section.
    /// </summary>
    /// <remarks>
    /// Populated during construction from the orchestrator's registered strategies.
    /// Toggle changes are forwarded to the orchestrator via the callback.
    /// </remarks>
    public ObservableCollection<StrategyToggleItem> Strategies { get; } = new();

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets a value indicating whether any context fragments are available.
    /// </summary>
    public bool HasContext => TotalTokens > 0;

    /// <summary>
    /// Gets the percentage of the token budget used (0.0 to 1.0).
    /// </summary>
    public double BudgetPercentage => TokenBudget > 0
        ? Math.Min(1.0, (double)TotalTokens / TokenBudget)
        : 0.0;

    /// <summary>
    /// Gets a human-readable budget status string.
    /// </summary>
    /// <example>"6,234 / 8,000 (78%)"</example>
    public string BudgetStatusText =>
        $"{TotalTokens:N0} / {TokenBudget:N0} ({BudgetPercentage:P0})";

    /// <summary>
    /// Gets the formatted assembly duration text.
    /// </summary>
    /// <example>"127ms"</example>
    public string DurationText => $"{AssemblyDuration.TotalMilliseconds:F0}ms";

    /// <summary>
    /// Gets the combined content of all fragments as a single string,
    /// formatted with markdown section headings.
    /// </summary>
    /// <remarks>
    /// Used by the Copy All command to produce a human-readable
    /// combined output of all context fragments.
    /// </remarks>
    public string CombinedContent => string.Join("\n\n",
        Fragments.Select(f => $"## {f.Label}\n{f.FullContent}"));

    #endregion

    #region Commands

    /// <summary>
    /// Toggles the panel expanded/collapsed state.
    /// </summary>
    [RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
    }

    /// <summary>
    /// Enables all context strategies.
    /// </summary>
    [RelayCommand]
    private void EnableAll()
    {
        foreach (var strategy in Strategies)
        {
            if (!strategy.IsEnabled)
            {
                strategy.IsEnabled = true;
            }
        }

        _logger.Log(
            LogLevel.Debug,
            new EventId(EventIdStrategyToggled, nameof(EnableAll)),
            "All strategies enabled");
    }

    /// <summary>
    /// Disables all context strategies.
    /// </summary>
    [RelayCommand]
    private void DisableAll()
    {
        foreach (var strategy in Strategies)
        {
            if (strategy.IsEnabled)
            {
                strategy.IsEnabled = false;
            }
        }

        _logger.Log(
            LogLevel.Debug,
            new EventId(EventIdStrategyToggled, nameof(DisableAll)),
            "All strategies disabled");
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Populates the <see cref="Strategies"/> collection from the orchestrator's
    /// registered strategies.
    /// </summary>
    private void InitializeStrategies()
    {
        foreach (var strategy in _orchestrator.GetStrategies())
        {
            Strategies.Add(new StrategyToggleItem(
                strategyId: strategy.StrategyId,
                displayName: strategy.DisplayName,
                isEnabled: _orchestrator.IsStrategyEnabled(strategy.StrategyId),
                onToggle: (id, enabled) => _orchestrator.SetStrategyEnabled(id, enabled),
                logger: _logger));
        }
    }

    /// <summary>
    /// Handles a <see cref="ContextAssembledEvent"/> from the bridge.
    /// Updates fragments, token counts, and duration on the UI thread.
    /// </summary>
    /// <param name="event">The context assembled event.</param>
    private void OnContextAssembled(ContextAssembledEvent @event)
    {
        _dispatch(() =>
        {
            if (_disposed) return;

            // LOGIC: Update scalar properties first for immediate UI feedback.
            IsAssembling = false;
            TotalTokens = @event.TotalTokens;
            AssemblyDuration = @event.Duration;

            // LOGIC: Clear and rebuild the fragment collection.
            Fragments.Clear();
            foreach (var fragment in @event.Fragments)
            {
                Fragments.Add(new FragmentViewModel(fragment, _logger));
            }

            _logger.Log(
                LogLevel.Debug,
                new EventId(EventIdPreviewUpdated, nameof(OnContextAssembled)),
                "Context preview updated: {FragmentCount} fragments, {TotalTokens} tokens in {Duration}ms",
                @event.Fragments.Count,
                @event.TotalTokens,
                @event.Duration.TotalMilliseconds);
        });
    }

    /// <summary>
    /// Handles a <see cref="StrategyToggleEvent"/> from the bridge.
    /// Synchronizes the toggle state in the UI with the orchestrator's state.
    /// </summary>
    /// <param name="event">The strategy toggle event.</param>
    private void OnStrategyToggled(StrategyToggleEvent @event)
    {
        _dispatch(() =>
        {
            if (_disposed) return;

            // LOGIC: Find and update the matching strategy toggle item.
            var item = Strategies.FirstOrDefault(s => s.StrategyId == @event.StrategyId);
            if (item is not null && item.IsEnabled != @event.IsEnabled)
            {
                // LOGIC: Temporarily detach the callback to avoid re-triggering
                // the orchestrator when we're just syncing state from it.
                // The OnIsEnabledChanged partial method will still fire for logging,
                // but the toggle callback will also fire — which is acceptable since
                // SetStrategyEnabled is idempotent.
                item.IsEnabled = @event.IsEnabled;
            }

            _logger.Log(
                LogLevel.Debug,
                new EventId(EventIdStrategyToggled, nameof(OnStrategyToggled)),
                "Strategy toggle synced from event: {StrategyId} → {Enabled}",
                @event.StrategyId,
                @event.IsEnabled);
        });
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes resources and unsubscribes from bridge events.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // LOGIC: Unsubscribe from bridge events to prevent memory leaks
        // and stale UI updates after the panel is closed.
        _bridge.ContextAssembled -= OnContextAssembled;
        _bridge.StrategyToggled -= OnStrategyToggled;

        _disposed = true;

        _logger.Log(
            LogLevel.Debug,
            new EventId(EventIdDisposed, nameof(Dispose)),
            "ContextPreviewViewModel disposed");
    }

    #endregion
}
