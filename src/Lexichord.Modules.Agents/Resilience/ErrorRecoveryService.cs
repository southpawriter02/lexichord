// -----------------------------------------------------------------------
// <copyright file="ErrorRecoveryService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Resilience;

/// <summary>
/// Coordinates recovery strategies for different agent error types.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ErrorRecoveryService"/> uses internal lookup tables to map exception
/// types to recovery strategies and user-friendly message templates. It serves as the
/// central decision engine referenced by <see cref="ResilientChatService"/> when errors occur.
/// </para>
/// <para>
/// <b>Strategy Mapping:</b>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="AgentRateLimitException"/> → <see cref="RecoveryStrategy.Queue"/></description></item>
///   <item><description><see cref="TokenLimitException"/> → <see cref="RecoveryStrategy.Truncate"/></description></item>
///   <item><description><see cref="ProviderUnavailableException"/> → <see cref="RecoveryStrategy.Retry"/></description></item>
///   <item><description><see cref="AgentAuthenticationException"/> → <see cref="RecoveryStrategy.None"/></description></item>
///   <item><description><see cref="InvalidResponseException"/> → <see cref="RecoveryStrategy.Retry"/></description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.6.8d as part of the Error Handling &amp; Recovery layer.
/// </para>
/// </remarks>
/// <seealso cref="IErrorRecoveryService"/>
/// <seealso cref="ResilientChatService"/>
public sealed class ErrorRecoveryService : IErrorRecoveryService
{
    private readonly ITokenBudgetManager _tokenBudget;
    private readonly ILogger<ErrorRecoveryService> _logger;

    /// <summary>
    /// Maps exception types to their recommended recovery strategies.
    /// </summary>
    /// <remarks>
    /// LOGIC: This dictionary provides O(1) strategy lookup by exception type.
    /// Unrecognized exception types fall through to <see cref="RecoveryStrategy.None"/>.
    /// </remarks>
    private static readonly Dictionary<Type, RecoveryStrategy> StrategyMap = new()
    {
        [typeof(AgentRateLimitException)] = RecoveryStrategy.Queue,
        [typeof(TokenLimitException)] = RecoveryStrategy.Truncate,
        [typeof(ProviderUnavailableException)] = RecoveryStrategy.Retry,
        [typeof(AgentAuthenticationException)] = RecoveryStrategy.None,
        [typeof(InvalidResponseException)] = RecoveryStrategy.Retry,
        [typeof(ContextAssemblyException)] = RecoveryStrategy.Retry,
    };

    /// <summary>
    /// Maps exception types to user-friendly message templates.
    /// </summary>
    /// <remarks>
    /// LOGIC: Templates support <see cref="string.Format(string, object[])"/> placeholders
    /// for dynamic content (e.g., wait time from <see cref="AgentRateLimitException.RetryAfter"/>).
    /// </remarks>
    private static readonly Dictionary<Type, string> UserMessageMap = new()
    {
        [typeof(AgentRateLimitException)] = "Request queued. Estimated wait: {0}",
        [typeof(TokenLimitException)] = "Message truncated to fit context limit.",
        [typeof(ProviderUnavailableException)] = "AI service temporarily unavailable. Retrying...",
        [typeof(AgentAuthenticationException)] = "Invalid API key. Please check Settings → AI Providers.",
        [typeof(InvalidResponseException)] = "Received invalid response. Retrying...",
        [typeof(ContextAssemblyException)] = "Context assembly failed. Response may be less accurate.",
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorRecoveryService"/> class.
    /// </summary>
    /// <param name="tokenBudget">
    /// The token budget manager used for truncation-based recovery.
    /// </param>
    /// <param name="logger">Logger for recovery operation diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="tokenBudget"/> or <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public ErrorRecoveryService(
        ITokenBudgetManager tokenBudget,
        ILogger<ErrorRecoveryService> logger)
    {
        _tokenBudget = tokenBudget ?? throw new ArgumentNullException(nameof(tokenBudget));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug(
            "ErrorRecoveryService initialized with {StrategyCount} strategy mappings",
            StrategyMap.Count);
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: An exception is recoverable when BOTH conditions are met:
    /// <list type="number">
    ///   <item><description>The exception's <see cref="AgentException.IsRecoverable"/> is <c>true</c></description></item>
    ///   <item><description>The exception's <see cref="AgentException.Strategy"/> is not <see cref="RecoveryStrategy.None"/></description></item>
    /// </list>
    /// This dual check ensures that even if a subclass reports itself as "recoverable",
    /// we don't attempt recovery on <see cref="RecoveryStrategy.None"/> strategies.
    /// </remarks>
    public bool CanRecover(AgentException exception)
    {
        ArgumentNullException.ThrowIfNull(exception, nameof(exception));

        var canRecover = exception.IsRecoverable && exception.Strategy != RecoveryStrategy.None;

        _logger.LogDebug(
            "CanRecover check for {ErrorType}: IsRecoverable={IsRecoverable}, Strategy={Strategy}, Result={CanRecover}",
            exception.GetType().Name,
            exception.IsRecoverable,
            exception.Strategy,
            canRecover);

        return canRecover;
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Uses the static <see cref="StrategyMap"/> for type-based lookup.
    /// Falls back to <see cref="RecoveryStrategy.None"/> for unmapped exception types,
    /// ensuring graceful degradation for future exception types.
    /// </remarks>
    public RecoveryStrategy GetStrategy(AgentException exception)
    {
        ArgumentNullException.ThrowIfNull(exception, nameof(exception));

        var strategy = StrategyMap.GetValueOrDefault(exception.GetType(), RecoveryStrategy.None);

        _logger.LogDebug(
            "Strategy lookup for {ErrorType}: {Strategy}",
            exception.GetType().Name,
            strategy);

        return strategy;
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Uses a pattern match to format dynamic message content. For example,
    /// <see cref="AgentRateLimitException"/> inserts the retry-after duration into
    /// the template placeholder. Unrecognized types fall through to using the
    /// exception's own <see cref="AgentException.UserMessage"/>.
    /// </remarks>
    public string GetUserMessage(AgentException exception)
    {
        ArgumentNullException.ThrowIfNull(exception, nameof(exception));

        var template = UserMessageMap.GetValueOrDefault(exception.GetType(), exception.UserMessage);

        var message = exception switch
        {
            AgentRateLimitException rl => string.Format(template, $"{rl.RetryAfter.TotalSeconds:F0}s"),
            _ => template
        };

        _logger.LogDebug(
            "User message for {ErrorType}: \"{Message}\"",
            exception.GetType().Name,
            message);

        return message;
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Currently only the <see cref="RecoveryStrategy.Truncate"/> strategy
    /// has an automatic recovery path (truncating conversation history). Other
    /// strategies (<see cref="RecoveryStrategy.Queue"/>, <see cref="RecoveryStrategy.Retry"/>)
    /// are handled by the caller (<see cref="ResilientChatService"/>) rather than
    /// by this service.
    /// </remarks>
    public async Task<AgentResponse?> AttemptRecoveryAsync(
        AgentException exception,
        AgentRequest originalRequest,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(exception, nameof(exception));
        ArgumentNullException.ThrowIfNull(originalRequest, nameof(originalRequest));

        var strategy = GetStrategy(exception);

        _logger.LogInformation(
            "Attempting recovery for {ErrorType} using {Strategy}",
            exception.GetType().Name,
            strategy);

        return strategy switch
        {
            RecoveryStrategy.Truncate => await TruncateAndRetryAsync(exception, originalRequest, ct),
            _ => null // Other strategies handled elsewhere (queue, retry by Polly)
        };
    }

    /// <summary>
    /// Handles the truncation recovery path for token limit exceptions.
    /// </summary>
    /// <param name="exception">The original token limit exception.</param>
    /// <param name="originalRequest">The original agent request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Always returns <c>null</c> to signal the caller to retry with truncated messages.
    /// The actual truncation is performed by the caller using <see cref="ITokenBudgetManager.TruncateToFit"/>.
    /// </returns>
    /// <remarks>
    /// LOGIC: This method logs the truncation intent but defers the actual truncation
    /// to the caller because the caller holds the reference to the mutable message list.
    /// The return value of <c>null</c> signals "recovery strategy applied; caller should retry."
    /// </remarks>
    private async Task<AgentResponse?> TruncateAndRetryAsync(
        AgentException exception,
        AgentRequest originalRequest,
        CancellationToken ct)
    {
        // LOGIC: Ensure this is actually a token limit exception before proceeding.
        if (exception is not TokenLimitException tl)
        {
            _logger.LogWarning(
                "TruncateAndRetry called for non-token-limit exception {ErrorType}. Skipping.",
                exception.GetType().Name);

            return null;
        }

        _logger.LogInformation(
            "Truncating conversation from {From:N0} to {To:N0} tokens (max: {Max:N0})",
            tl.RequestedTokens,
            tl.TruncatedTo,
            tl.MaxTokens);

        // LOGIC: Truncation is handled by the caller (ResilientChatService) using
        // ITokenBudgetManager.TruncateToFit. Returning null signals the caller
        // to apply truncation and retry the request.
        await Task.CompletedTask;
        return null;
    }
}
