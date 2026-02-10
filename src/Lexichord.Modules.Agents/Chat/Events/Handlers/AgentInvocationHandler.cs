// -----------------------------------------------------------------------
// <copyright file="AgentInvocationHandler.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.Events;
using Lexichord.Abstractions.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Chat.Events.Handlers;

/// <summary>
/// Handles <see cref="AgentInvocationEvent"/> for telemetry.
/// </summary>
/// <remarks>
/// <para>
/// This handler receives agent invocation events published by the
/// <see cref="Services.UsageTracker"/> and forwards them to the
/// telemetry service for analytics collection.
/// </para>
/// <para>
/// The handler is lightweight and synchronous to avoid impacting
/// response latency. It uses <see cref="ITelemetryService"/> for
/// breadcrumb tracking and structured message capture.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6d as part of the Usage Tracking feature.
/// </para>
/// </remarks>
public class AgentInvocationHandler : INotificationHandler<AgentInvocationEvent>
{
    private readonly ITelemetryService _telemetry;
    private readonly ILogger<AgentInvocationHandler> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="AgentInvocationHandler"/>.
    /// </summary>
    /// <param name="telemetry">Telemetry service for event capture.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public AgentInvocationHandler(
        ITelemetryService telemetry,
        ILogger<AgentInvocationHandler> logger)
    {
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the agent invocation event by forwarding to telemetry.
    /// </summary>
    /// <param name="notification">The invocation event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Completed task.</returns>
    /// <remarks>
    /// LOGIC: Adds a breadcrumb for crash report context and logs
    /// a trace-level message with token counts and duration.
    /// </remarks>
    public Task Handle(AgentInvocationEvent notification, CancellationToken cancellationToken)
    {
        // LOGIC: Add breadcrumb for crash reporting context.
        _telemetry.AddBreadcrumb(
            $"Agent '{notification.AgentId}' invoked: {notification.TotalTokens} tokens, " +
            $"{notification.Duration.TotalMilliseconds:F0}ms, streamed={notification.Streamed}",
            "agent.invocation");

        _logger.LogTrace(
            "Telemetry: Agent {AgentId} invocation - {TotalTokens} tokens in {Duration}ms",
            notification.AgentId,
            notification.TotalTokens,
            notification.Duration.TotalMilliseconds);

        return Task.CompletedTask;
    }
}
