// -----------------------------------------------------------------------
// <copyright file="ContextPreviewBridge.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Context;

/// <summary>
/// Singleton bridge that receives MediatR notifications from the
/// <see cref="ContextOrchestrator"/> and re-publishes them as C# events
/// for consumption by transient ViewModels.
/// </summary>
/// <remarks>
/// <para>
/// MediatR notification handlers are resolved from the DI container at publish time.
/// Since ViewModels are registered as Transient, a direct <see cref="INotificationHandler{TNotification}"/>
/// implementation on the ViewModel would create a new instance per event — not the existing
/// instance displayed in the UI. This bridge solves the lifetime mismatch:
/// </para>
/// <list type="number">
///   <item><description>The bridge is registered as Singleton and receives all notifications</description></item>
///   <item><description>The bridge exposes C# events that ViewModels subscribe to in their constructor</description></item>
///   <item><description>ViewModels unsubscribe in their <c>Dispose()</c> method</description></item>
/// </list>
/// <para>
/// <strong>Thread Safety:</strong>
/// Event invocations occur on the MediatR publish thread (typically a background thread).
/// ViewModel subscribers must dispatch to the UI thread before modifying observable properties.
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2d as part of the Context Preview Panel.
/// </para>
/// </remarks>
/// <seealso cref="ContextAssembledEvent"/>
/// <seealso cref="StrategyToggleEvent"/>
internal sealed class ContextPreviewBridge :
    INotificationHandler<ContextAssembledEvent>,
    INotificationHandler<StrategyToggleEvent>
{
    #region Constants

    /// <summary>
    /// Event ID for context assembled notification received.
    /// </summary>
    private const int EventIdContextAssembledReceived = 7100;

    /// <summary>
    /// Event ID for strategy toggle notification received.
    /// </summary>
    private const int EventIdStrategyToggleReceived = 7101;

    #endregion

    #region Fields

    private readonly ILogger<ContextPreviewBridge> _logger;

    #endregion

    #region Events

    /// <summary>
    /// Raised when a <see cref="ContextAssembledEvent"/> is received from MediatR.
    /// </summary>
    /// <remarks>
    /// Subscribers should dispatch to the UI thread before updating observable properties.
    /// The event is invoked on the MediatR publish thread.
    /// </remarks>
    public event Action<ContextAssembledEvent>? ContextAssembled;

    /// <summary>
    /// Raised when a <see cref="StrategyToggleEvent"/> is received from MediatR.
    /// </summary>
    /// <remarks>
    /// Subscribers should dispatch to the UI thread before updating observable properties.
    /// The event is invoked on the MediatR publish thread.
    /// </remarks>
    public event Action<StrategyToggleEvent>? StrategyToggled;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextPreviewBridge"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    public ContextPreviewBridge(ILogger<ContextPreviewBridge> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    #endregion

    #region INotificationHandler

    /// <summary>
    /// Handles a <see cref="ContextAssembledEvent"/> notification from MediatR
    /// and forwards it to all subscribed ViewModels via the <see cref="ContextAssembled"/> event.
    /// </summary>
    /// <param name="notification">The context assembled notification containing fragments, tokens, and duration.</param>
    /// <param name="cancellationToken">Cancellation token (unused by this handler).</param>
    /// <returns>A completed task.</returns>
    public Task Handle(ContextAssembledEvent notification, CancellationToken cancellationToken)
    {
        _logger.Log(
            LogLevel.Debug,
            new EventId(EventIdContextAssembledReceived, nameof(Handle)),
            "Context assembled event received: {FragmentCount} fragments, {TotalTokens} tokens for agent {AgentId}",
            notification.Fragments.Count,
            notification.TotalTokens,
            notification.AgentId);

        // LOGIC: Invoke subscribers on the MediatR thread. ViewModel subscribers
        // are responsible for dispatching to the UI thread via their dispatch delegate.
        ContextAssembled?.Invoke(notification);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles a <see cref="StrategyToggleEvent"/> notification from MediatR
    /// and forwards it to all subscribed ViewModels via the <see cref="StrategyToggled"/> event.
    /// </summary>
    /// <param name="notification">The strategy toggle notification containing strategy ID and enabled state.</param>
    /// <param name="cancellationToken">Cancellation token (unused by this handler).</param>
    /// <returns>A completed task.</returns>
    public Task Handle(StrategyToggleEvent notification, CancellationToken cancellationToken)
    {
        _logger.Log(
            LogLevel.Debug,
            new EventId(EventIdStrategyToggleReceived, nameof(Handle)),
            "Strategy toggle event received: {StrategyId} → {Enabled}",
            notification.StrategyId,
            notification.IsEnabled);

        // LOGIC: Invoke subscribers on the MediatR thread. ViewModel subscribers
        // are responsible for dispatching to the UI thread via their dispatch delegate.
        StrategyToggled?.Invoke(notification);

        return Task.CompletedTask;
    }

    #endregion
}
