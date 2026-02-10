// -----------------------------------------------------------------------
// <copyright file="UsageDisplayViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Agents.Chat.Services;

namespace Lexichord.Modules.Agents.Chat.ViewModels;

/// <summary>
/// ViewModel for displaying usage metrics in the UI.
/// </summary>
/// <remarks>
/// <para>
/// This ViewModel binds to the usage display in the chat panel footer.
/// It subscribes to the <see cref="UsageTracker.UsageRecorded"/> event
/// and throttles UI updates to prevent excessive redraws.
/// </para>
/// <para>
/// The ViewModel provides formatted display strings and visual states
/// based on usage thresholds.
/// </para>
/// <para>
/// Uses <see cref="ObservableObject"/> from CommunityToolkit.Mvvm
/// for property change notifications.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6d as part of the Usage Tracking feature.
/// </para>
/// </remarks>
public class UsageDisplayViewModel : ObservableObject, IDisposable
{
    private readonly UsageTracker _usageTracker;
    private readonly ISettingsService _settingsService;
    private readonly Action<Action> _dispatchAction;
    private readonly Timer _updateThrottle;

    private const int UpdateIntervalMs = 500;
    private bool _pendingUpdate;
    private bool _isDisposed;

    // LOGIC: Backing fields for observable properties.
    private string _tokenDisplay = "0 tokens";
    private string _costDisplay = "";
    private UsageDisplayState _displayState = UsageDisplayState.Normal;
    private bool _showLimitWarning;
    private string _limitWarningMessage = "";
    private int _usageProgress;

    /// <summary>
    /// Initializes a new instance of <see cref="UsageDisplayViewModel"/>.
    /// </summary>
    /// <param name="usageTracker">The usage tracker service.</param>
    /// <param name="settingsService">Settings service for thresholds.</param>
    /// <param name="dispatchAction">
    /// UI thread dispatch action. Defaults to direct invocation for testing.
    /// In production, pass <c>action => Dispatcher.UIThread.Post(action)</c>.
    /// </param>
    /// <remarks>
    /// LOGIC: Uses injectable dispatch action (same pattern as
    /// StreamingChatHandler in v0.6.5c) for testability without
    /// Avalonia Dispatcher dependency.
    /// </remarks>
    public UsageDisplayViewModel(
        UsageTracker usageTracker,
        ISettingsService settingsService,
        Action<Action>? dispatchAction = null)
    {
        _usageTracker = usageTracker ?? throw new ArgumentNullException(nameof(usageTracker));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _dispatchAction = dispatchAction ?? (action => action());

        _updateThrottle = new Timer(OnThrottleElapsed, null, Timeout.Infinite, Timeout.Infinite);
        _usageTracker.UsageRecorded += OnUsageRecorded;

        // LOGIC: Initialize display values on construction.
        UpdateDisplayValues();
    }

    /// <summary>
    /// Gets the formatted token count display.
    /// </summary>
    public string TokenDisplay
    {
        get => _tokenDisplay;
        private set => SetProperty(ref _tokenDisplay, value);
    }

    /// <summary>
    /// Gets the formatted cost display.
    /// </summary>
    public string CostDisplay
    {
        get => _costDisplay;
        private set => SetProperty(ref _costDisplay, value);
    }

    /// <summary>
    /// Gets the current usage state for styling.
    /// </summary>
    public UsageDisplayState DisplayState
    {
        get => _displayState;
        private set => SetProperty(ref _displayState, value);
    }

    /// <summary>
    /// Gets whether the usage limit warning is visible.
    /// </summary>
    public bool ShowLimitWarning
    {
        get => _showLimitWarning;
        private set => SetProperty(ref _showLimitWarning, value);
    }

    /// <summary>
    /// Gets the usage limit warning message.
    /// </summary>
    public string LimitWarningMessage
    {
        get => _limitWarningMessage;
        private set => SetProperty(ref _limitWarningMessage, value);
    }

    /// <summary>
    /// Gets the progress towards usage limit (0-100).
    /// </summary>
    public int UsageProgress
    {
        get => _usageProgress;
        private set => SetProperty(ref _usageProgress, value);
    }

    /// <summary>
    /// Handles the UsageRecorded event with debounced throttling.
    /// </summary>
    private void OnUsageRecorded(object? sender, UsageRecordedEventArgs e)
    {
        // LOGIC: Debounce updates to ~2 per second max.
        if (!_pendingUpdate)
        {
            _pendingUpdate = true;
            _updateThrottle.Change(UpdateIntervalMs, Timeout.Infinite);
        }
    }

    /// <summary>
    /// Timer callback for throttled display updates.
    /// </summary>
    private void OnThrottleElapsed(object? state)
    {
        _pendingUpdate = false;
        _dispatchAction(UpdateDisplayValues);
    }

    /// <summary>
    /// Updates all display properties from current usage state.
    /// </summary>
    /// <remarks>
    /// LOGIC: Reads current conversation usage, formats token/cost
    /// display, determines visual state based on configurable thresholds,
    /// and computes progress percentage.
    /// </remarks>
    internal void UpdateDisplayValues()
    {
        var usage = _usageTracker.ConversationUsage;
        var totalTokens = usage.TotalTokens;

        // LOGIC: Format token count with thousands separators.
        TokenDisplay = totalTokens switch
        {
            0 => "0 tokens",
            _ => $"{totalTokens:N0} tokens"
        };

        // LOGIC: Format cost (only show if non-zero).
        CostDisplay = usage.EstimatedCost > 0
            ? $"(~${usage.EstimatedCost:F4})"
            : "";

        // LOGIC: Determine display state based on configurable thresholds.
        var warningThreshold = _settingsService.Get("Usage:WarningThreshold", 75000);
        var criticalThreshold = _settingsService.Get("Usage:CriticalThreshold", 95000);
        var limit = _settingsService.Get("Usage:TokenLimit", 100000);

        DisplayState = totalTokens switch
        {
            _ when totalTokens >= criticalThreshold => UsageDisplayState.Critical,
            _ when totalTokens >= warningThreshold => UsageDisplayState.Warning,
            _ => UsageDisplayState.Normal
        };

        // LOGIC: Compute progress bar percentage (capped at 100).
        UsageProgress = Math.Min(100, (int)(totalTokens * 100.0 / limit));
        ShowLimitWarning = totalTokens >= warningThreshold;

        if (ShowLimitWarning)
        {
            var remaining = limit - totalTokens;
            LimitWarningMessage = remaining > 0
                ? $"~{remaining:N0} tokens remaining"
                : "Token limit reached";
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed) return;

        _usageTracker.UsageRecorded -= OnUsageRecorded;
        _updateThrottle.Dispose();
        _isDisposed = true;
    }
}

/// <summary>
/// Visual states for usage display.
/// </summary>
/// <remarks>
/// <b>Introduced in:</b> v0.6.6d as part of the Usage Tracking feature.
/// </remarks>
public enum UsageDisplayState
{
    /// <summary>
    /// Normal usage, no concerns. Standard styling.
    /// </summary>
    Normal,

    /// <summary>
    /// Approaching usage limit (75%+). Amber styling with warning.
    /// </summary>
    Warning,

    /// <summary>
    /// Near or at usage limit (95%+). Red pulsing animation.
    /// </summary>
    Critical
}
