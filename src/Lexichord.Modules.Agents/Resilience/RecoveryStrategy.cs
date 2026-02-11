// -----------------------------------------------------------------------
// <copyright file="RecoveryStrategy.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Resilience;

/// <summary>
/// Defines the recovery strategy to apply when an agent error occurs.
/// </summary>
/// <remarks>
/// <para>
/// Each <see cref="AgentException"/> maps to a recommended <see cref="RecoveryStrategy"/>
/// that drives the error handling flow in <see cref="IErrorRecoveryService"/>:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="Retry"/>: Transient failures that may resolve on their own</description></item>
///   <item><description><see cref="Queue"/>: Rate-limited requests that should wait</description></item>
///   <item><description><see cref="Truncate"/>: Token budget exceeded; reduce context and retry</description></item>
///   <item><description><see cref="Fallback"/>: Switch to an alternate provider</description></item>
///   <item><description><see cref="None"/>: Permanent failure requiring user action</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.6.8d as part of the Error Handling &amp; Recovery layer.
/// </para>
/// </remarks>
/// <seealso cref="AgentException"/>
/// <seealso cref="IErrorRecoveryService"/>
public enum RecoveryStrategy
{
    /// <summary>
    /// Retry the request after a delay.
    /// </summary>
    /// <remarks>
    /// Used for transient errors such as network timeouts, provider unavailability,
    /// and invalid responses. The retry delay typically uses exponential backoff
    /// with jitter via the Polly resilience pipeline.
    /// </remarks>
    Retry,

    /// <summary>
    /// Queue the request for later processing.
    /// </summary>
    /// <remarks>
    /// Used when the provider signals rate limiting. The request is placed in an
    /// <see cref="IRateLimitQueue"/> and processed once the rate limit window expires.
    /// The user is notified of their queue position and estimated wait time.
    /// </remarks>
    Queue,

    /// <summary>
    /// Truncate content and retry.
    /// </summary>
    /// <remarks>
    /// Used when the conversation exceeds the model's token budget. The
    /// <see cref="ITokenBudgetManager"/> removes the oldest non-system messages
    /// until the conversation fits within the budget, then the request is retried.
    /// </remarks>
    Truncate,

    /// <summary>
    /// Switch to a fallback provider.
    /// </summary>
    /// <remarks>
    /// Used when the primary provider is persistently unavailable and an
    /// alternate LLM provider is configured. This strategy is only viable
    /// when the application has multiple providers registered.
    /// </remarks>
    Fallback,

    /// <summary>
    /// No recovery possible, show error to user.
    /// </summary>
    /// <remarks>
    /// Used for permanent failures such as authentication errors (invalid API key)
    /// or unrecognized exceptions. The user is shown an actionable error message
    /// directing them to resolve the issue (e.g., checking API key configuration).
    /// </remarks>
    None
}
