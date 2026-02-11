// -----------------------------------------------------------------------
// <copyright file="AgentErrorEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using MediatR;

namespace Lexichord.Modules.Agents.Resilience;

/// <summary>
/// MediatR notification published when an agent error occurs.
/// </summary>
/// <remarks>
/// <para>
/// Published by <see cref="ResilientChatService"/> for every handled error,
/// enabling centralized telemetry, usage tracking, and UI notification without
/// tight coupling between the error handling and presentation layers.
/// </para>
/// <para>
/// Handlers may include:
/// </para>
/// <list type="bullet">
///   <item><description>Telemetry handlers that log error metrics</description></item>
///   <item><description>UI handlers that display toast notifications</description></item>
///   <item><description>Usage tracking handlers that record error rates</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.6.8d as part of the Error Handling &amp; Recovery layer.
/// </para>
/// </remarks>
/// <param name="ErrorType">
/// The simple name of the exception type (e.g., "AgentRateLimitException").
/// Used for error classification in telemetry dashboards.
/// </param>
/// <param name="UserMessage">
/// The user-friendly error message that was (or will be) displayed to the user.
/// </param>
/// <param name="TechnicalDetails">
/// Optional technical details for diagnostic purposes. May include HTTP status codes,
/// provider error codes, or stack trace summaries.
/// </param>
/// <param name="WasRecovered">
/// <c>true</c> if automatic recovery was successful; <c>false</c> if the error
/// was propagated to the user.
/// </param>
/// <param name="Timestamp">
/// The UTC timestamp when the error occurred. Used for error rate calculations
/// and time-series analysis.
/// </param>
/// <example>
/// <code>
/// // Publishing an error event
/// await _mediator.Publish(new AgentErrorEvent(
///     ErrorType: nameof(AgentRateLimitException),
///     UserMessage: "Rate limit exceeded. Please wait 30 seconds.",
///     TechnicalDetails: "HTTP 429 from openai. Retry-After: 30s",
///     WasRecovered: true,
///     Timestamp: DateTimeOffset.UtcNow));
/// </code>
/// </example>
/// <seealso cref="ResilientChatService"/>
/// <seealso cref="AgentException"/>
public sealed record AgentErrorEvent(
    string ErrorType,
    string UserMessage,
    string? TechnicalDetails,
    bool WasRecovered,
    DateTimeOffset Timestamp) : INotification;

/// <summary>
/// Event arguments for rate limit status changes.
/// </summary>
/// <remarks>
/// <para>
/// Published by <see cref="IRateLimitQueue"/> when the rate limiting state changes,
/// enabling UI updates such as progress indicators and wait time estimates.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.8d as part of the Error Handling &amp; Recovery layer.
/// </para>
/// </remarks>
/// <param name="IsRateLimited">
/// <c>true</c> if the provider is currently rate-limited; <c>false</c> if the
/// rate limit window has expired and requests can proceed.
/// </param>
/// <param name="EstimatedWait">
/// The estimated time until the rate limit expires. <see cref="TimeSpan.Zero"/>
/// when not rate-limited.
/// </param>
/// <param name="QueueDepth">
/// The number of requests currently waiting in the queue.
/// </param>
/// <example>
/// <code>
/// rateLimitQueue.StatusChanged += (sender, args) =>
/// {
///     if (args.IsRateLimited)
///         statusBar.Show($"Rate limited. Wait: {args.EstimatedWait.TotalSeconds:F0}s ({args.QueueDepth} queued)");
///     else
///         statusBar.Clear();
/// };
/// </code>
/// </example>
/// <seealso cref="IRateLimitQueue"/>
public sealed record RateLimitStatusEventArgs(
    bool IsRateLimited,
    TimeSpan EstimatedWait,
    int QueueDepth);
