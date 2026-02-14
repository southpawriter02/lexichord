// -----------------------------------------------------------------------
// <copyright file="StrategyToggleItem.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Chat.ViewModels;

/// <summary>
/// ViewModel for a strategy toggle control in the Context Preview panel.
/// Represents a single context strategy with its enabled/disabled state
/// and triggers the orchestrator toggle callback when changed.
/// </summary>
/// <remarks>
/// <para>
/// Each <see cref="StrategyToggleItem"/> corresponds to an
/// <see cref="Lexichord.Abstractions.Agents.Context.IContextStrategy"/>
/// registered in the system. The toggle item displays the strategy's name
/// and allows users to enable or disable it for future context assembly.
/// </para>
/// <para>
/// <strong>Toggle Callback:</strong>
/// When <see cref="IsEnabled"/> changes, the <c>onToggle</c> callback
/// (provided at construction) is invoked with the strategy ID and new state.
/// This callback typically delegates to
/// <see cref="Lexichord.Abstractions.Agents.Context.IContextOrchestrator.SetStrategyEnabled"/>.
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2d as part of the Context Preview Panel.
/// </para>
/// </remarks>
/// <seealso cref="ContextPreviewViewModel"/>
internal sealed partial class StrategyToggleItem : ObservableObject
{
    #region Constants

    /// <summary>
    /// Event ID for strategy toggle changed.
    /// </summary>
    private const int EventIdToggleChanged = 7104;

    #endregion

    #region Fields

    private readonly Action<string, bool> _onToggle;
    private readonly ILogger? _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyToggleItem"/> class.
    /// </summary>
    /// <param name="strategyId">The unique identifier of the context strategy.</param>
    /// <param name="displayName">The human-readable display name for the strategy.</param>
    /// <param name="isEnabled">The initial enabled state of the strategy.</param>
    /// <param name="onToggle">
    /// Callback invoked when the enabled state changes, receiving the strategy ID
    /// and the new enabled state.
    /// </param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="strategyId"/>, <paramref name="displayName"/>,
    /// or <paramref name="onToggle"/> is null.
    /// </exception>
    public StrategyToggleItem(
        string strategyId,
        string displayName,
        bool isEnabled,
        Action<string, bool> onToggle,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(strategyId);
        ArgumentNullException.ThrowIfNull(displayName);
        ArgumentNullException.ThrowIfNull(onToggle);

        StrategyId = strategyId;
        DisplayName = displayName;
        _isEnabled = isEnabled;
        _onToggle = onToggle;
        _logger = logger;
    }

    #endregion

    #region Read-Only Properties

    /// <summary>
    /// Gets the unique identifier of the context strategy.
    /// </summary>
    public string StrategyId { get; }

    /// <summary>
    /// Gets the human-readable display name for the strategy.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the tooltip text explaining what context this strategy provides.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Updated in:</strong> v0.7.2h to add the <c>"knowledge"</c> tooltip.
    /// </para>
    /// </remarks>
    public string Tooltip => StrategyId switch
    {
        "document" => "Include the current document content",
        "selection" => "Include selected text with surrounding context",
        "cursor" => "Include text around the cursor position",
        "heading" => "Include document heading structure",
        "rag" => "Include semantically related documentation",
        "style" => "Include active style rules",
        "knowledge" => "Include knowledge graph entities and relationships",
        _ => $"Context from {DisplayName}"
    };

    #endregion

    #region Observable Properties

    /// <summary>
    /// Gets or sets a value indicating whether the strategy is enabled.
    /// </summary>
    /// <remarks>
    /// When this value changes, the <c>onToggle</c> callback is invoked
    /// to notify the orchestrator of the state change.
    /// </remarks>
    [ObservableProperty]
    private bool _isEnabled;

    #endregion

    #region Partial Methods

    partial void OnIsEnabledChanged(bool value)
    {
        _logger?.Log(
            LogLevel.Information,
            new EventId(EventIdToggleChanged, nameof(OnIsEnabledChanged)),
            "Strategy {StrategyId} toggled to {Enabled}",
            StrategyId,
            value);

        // LOGIC: Delegate to the orchestrator via the callback. This decouples
        // the toggle item from direct IContextOrchestrator dependency, allowing
        // the parent ViewModel to control the toggle behavior.
        _onToggle(StrategyId, value);
    }

    #endregion
}
