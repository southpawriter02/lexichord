// -----------------------------------------------------------------------
// <copyright file="IErrorRecoveryService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents;

namespace Lexichord.Modules.Agents.Resilience;

/// <summary>
/// Coordinates error recovery strategies for agent exceptions.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IErrorRecoveryService"/> acts as the central decision engine for
/// determining how to handle agent errors. It maps exception types to recovery
/// strategies, produces user-friendly messages, and orchestrates automatic
/// recovery when possible.
/// </para>
/// <para>
/// <b>Decision Flow:</b>
/// </para>
/// <list type="number">
///   <item><description>Check <see cref="CanRecover"/> to determine if recovery is possible</description></item>
///   <item><description>Get the <see cref="GetStrategy"/> for the error type</description></item>
///   <item><description>If recoverable, call <see cref="AttemptRecoveryAsync"/> for automatic recovery</description></item>
///   <item><description>If not recoverable, display <see cref="GetUserMessage"/> to the user</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.6.8d as part of the Error Handling &amp; Recovery layer.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// catch (AgentException ex)
/// {
///     if (_recovery.CanRecover(ex))
///     {
///         var recovered = await _recovery.AttemptRecoveryAsync(ex, request, ct);
///         if (recovered is not null) return recovered;
///     }
///     ShowError(_recovery.GetUserMessage(ex));
/// }
/// </code>
/// </example>
/// <seealso cref="ErrorRecoveryService"/>
/// <seealso cref="AgentException"/>
/// <seealso cref="RecoveryStrategy"/>
public interface IErrorRecoveryService
{
    /// <summary>
    /// Attempts automatic recovery from the specified exception.
    /// </summary>
    /// <param name="exception">The agent exception to recover from.</param>
    /// <param name="originalRequest">
    /// The original agent request that triggered the error. Used to reconstruct
    /// a modified request (e.g., with truncated history) for retry.
    /// </param>
    /// <param name="ct">Cancellation token to abort the recovery attempt.</param>
    /// <returns>
    /// A task containing the recovered <see cref="AgentResponse"/> if recovery succeeded,
    /// or <c>null</c> if recovery was not possible and the error should be propagated.
    /// </returns>
    /// <remarks>
    /// LOGIC: Recovery is attempted based on the exception's <see cref="RecoveryStrategy"/>:
    /// <list type="bullet">
    ///   <item><description><see cref="RecoveryStrategy.Truncate"/>: Truncates conversation and signals caller to retry</description></item>
    ///   <item><description><see cref="RecoveryStrategy.Queue"/>: Handled externally by <see cref="IRateLimitQueue"/></description></item>
    ///   <item><description>Other strategies return <c>null</c> to indicate no recovery was performed</description></item>
    /// </list>
    /// </remarks>
    Task<AgentResponse?> AttemptRecoveryAsync(
        AgentException exception,
        AgentRequest originalRequest,
        CancellationToken ct);

    /// <summary>
    /// Checks if the specified exception is recoverable automatically.
    /// </summary>
    /// <param name="exception">The exception to evaluate.</param>
    /// <returns>
    /// <c>true</c> if automatic recovery is possible; <c>false</c> if user
    /// intervention is required.
    /// </returns>
    /// <remarks>
    /// LOGIC: An exception is recoverable if both <see cref="AgentException.IsRecoverable"/>
    /// is <c>true</c> AND the exception's <see cref="AgentException.Strategy"/> is not
    /// <see cref="RecoveryStrategy.None"/>.
    /// </remarks>
    bool CanRecover(AgentException exception);

    /// <summary>
    /// Gets the recommended recovery strategy for the specified exception type.
    /// </summary>
    /// <param name="exception">The exception to get the strategy for.</param>
    /// <returns>
    /// The <see cref="RecoveryStrategy"/> mapped to the exception's type.
    /// Returns <see cref="RecoveryStrategy.None"/> for unrecognized exception types.
    /// </returns>
    /// <remarks>
    /// LOGIC: Uses an internal type-to-strategy mapping that can be extended
    /// as new exception types are added. The mapping is:
    /// <list type="bullet">
    ///   <item><description><see cref="AgentRateLimitException"/> → <see cref="RecoveryStrategy.Queue"/></description></item>
    ///   <item><description><see cref="TokenLimitException"/> → <see cref="RecoveryStrategy.Truncate"/></description></item>
    ///   <item><description><see cref="ProviderUnavailableException"/> → <see cref="RecoveryStrategy.Retry"/></description></item>
    ///   <item><description><see cref="AgentAuthenticationException"/> → <see cref="RecoveryStrategy.None"/></description></item>
    ///   <item><description><see cref="InvalidResponseException"/> → <see cref="RecoveryStrategy.Retry"/></description></item>
    /// </list>
    /// </remarks>
    RecoveryStrategy GetStrategy(AgentException exception);

    /// <summary>
    /// Gets a user-friendly error message for the specified exception.
    /// </summary>
    /// <param name="exception">The exception to produce a message for.</param>
    /// <returns>
    /// A non-technical, actionable message suitable for display in the UI.
    /// </returns>
    /// <remarks>
    /// LOGIC: Message templates are mapped to exception types. Some templates
    /// use format placeholders filled from exception properties (e.g., wait time
    /// from <see cref="AgentRateLimitException.RetryAfter"/>).
    /// </remarks>
    string GetUserMessage(AgentException exception);
}
