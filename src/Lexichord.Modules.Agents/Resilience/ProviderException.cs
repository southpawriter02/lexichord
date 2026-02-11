// -----------------------------------------------------------------------
// <copyright file="ProviderException.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Resilience;

/// <summary>
/// Exception thrown when an LLM provider reports an error during agent execution.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ProviderException"/> is the base class for all provider-specific errors and
/// adds a <see cref="Provider"/> property to identify which LLM service caused the failure.
/// </para>
/// <para>
/// <b>Derived Types:</b>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="AgentRateLimitException"/>: API rate limit exceeded</description></item>
///   <item><description><see cref="AgentAuthenticationException"/>: Invalid or expired API key</description></item>
///   <item><description><see cref="ProviderUnavailableException"/>: Service down or unreachable</description></item>
///   <item><description><see cref="InvalidResponseException"/>: Malformed or empty provider response</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.6.8d as part of the Error Handling &amp; Recovery layer.
/// </para>
/// </remarks>
/// <seealso cref="AgentException"/>
/// <seealso cref="RecoveryStrategy"/>
public class ProviderException : AgentException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderException"/> class.
    /// </summary>
    /// <param name="provider">
    /// The identifier of the LLM provider that caused the error (e.g., "openai", "anthropic", "local").
    /// </param>
    /// <param name="userMessage">
    /// A user-friendly error message for display in the UI.
    /// </param>
    /// <param name="inner">
    /// Optional inner exception providing the root cause.
    /// </param>
    /// <remarks>
    /// LOGIC: The provider name is captured to enable provider-specific error handling
    /// and fallback strategies in <see cref="IErrorRecoveryService"/>.
    /// </remarks>
    public ProviderException(string provider, string userMessage, Exception? inner = null)
        : base(userMessage, inner)
    {
        Provider = provider;
    }

    /// <summary>
    /// Gets the identifier of the LLM provider that caused the error.
    /// </summary>
    /// <value>
    /// The provider name, e.g., "openai", "anthropic", "lmstudio", "ollama".
    /// </value>
    /// <remarks>
    /// LOGIC: This property is used by the recovery service to determine if a fallback
    /// provider is available, and by logging to identify which provider triggered the error.
    /// </remarks>
    public string Provider { get; }
}

/// <summary>
/// Exception thrown when an API rate limit is exceeded during agent execution.
/// </summary>
/// <remarks>
/// <para>
/// When a rate limit is detected, the <see cref="IRateLimitQueue"/> enqueues the
/// request and waits for the rate limit window to expire before retrying.
/// </para>
/// <para>
/// The <see cref="RetryAfter"/> property indicates how long to wait before retrying,
/// and <see cref="QueuePosition"/> reflects the request's position in the queue.
/// </para>
/// <para>
/// <b>Recovery Strategy:</b> <see cref="RecoveryStrategy.Queue"/>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.8d as part of the Error Handling &amp; Recovery layer.
/// </para>
/// </remarks>
/// <seealso cref="IRateLimitQueue"/>
/// <seealso cref="RateLimitStatusEventArgs"/>
public sealed class AgentRateLimitException : ProviderException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentRateLimitException"/> class.
    /// </summary>
    /// <param name="provider">The identifier of the rate-limiting provider.</param>
    /// <param name="retryAfter">The duration to wait before retrying.</param>
    /// <param name="queuePosition">
    /// The request's position in the rate limit queue. Defaults to 0 (not yet queued).
    /// </param>
    /// <remarks>
    /// LOGIC: The user message is auto-generated from the <paramref name="retryAfter"/> duration
    /// to provide an immediate, actionable indication of wait time.
    /// </remarks>
    public AgentRateLimitException(string provider, TimeSpan retryAfter, int queuePosition = 0)
        : base(provider, $"Rate limit exceeded. Please wait {retryAfter.TotalSeconds:F0} seconds.")
    {
        RetryAfter = retryAfter;
        QueuePosition = queuePosition;
    }

    /// <summary>
    /// Gets the duration to wait before retrying the request.
    /// </summary>
    /// <value>
    /// The retry-after duration as reported by the provider's rate limit headers.
    /// </value>
    public TimeSpan RetryAfter { get; }

    /// <summary>
    /// Gets the request's position in the rate limit queue.
    /// </summary>
    /// <value>
    /// Zero-based queue position. A value of 0 means the request has not been queued yet.
    /// </value>
    public int QueuePosition { get; }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Rate-limited requests are queued rather than retried immediately,
    /// ensuring the rate limit window is respected.
    /// </remarks>
    public override RecoveryStrategy Strategy => RecoveryStrategy.Queue;
}

/// <summary>
/// Exception thrown when API authentication fails during agent execution.
/// </summary>
/// <remarks>
/// <para>
/// This exception indicates that the API key or authentication credentials are invalid,
/// expired, or missing. The user must correct their provider configuration before retrying.
/// </para>
/// <para>
/// <b>Recovery Strategy:</b> <see cref="RecoveryStrategy.None"/> — user intervention required.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.8d as part of the Error Handling &amp; Recovery layer.
/// </para>
/// </remarks>
/// <seealso cref="Lexichord.Abstractions.Contracts.LLM.AuthenticationException"/>
public sealed class AgentAuthenticationException : ProviderException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentAuthenticationException"/> class.
    /// </summary>
    /// <param name="provider">The identifier of the provider that rejected authentication.</param>
    /// <param name="userMessage">
    /// A user-friendly message directing the user to fix their API key.
    /// Defaults to a message pointing to Settings → AI Providers.
    /// </param>
    /// <param name="inner">Optional inner exception with the original authentication error.</param>
    /// <remarks>
    /// LOGIC: Authentication failures are never recoverable automatically because
    /// the user must manually update their API key in the application settings.
    /// </remarks>
    public AgentAuthenticationException(
        string provider,
        string? userMessage = null,
        Exception? inner = null)
        : base(provider, userMessage ?? "Invalid API key. Please check Settings → AI Providers.", inner)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Authentication errors cannot be recovered automatically — the user
    /// must update their API key configuration.
    /// </remarks>
    public override bool IsRecoverable => false;

    /// <inheritdoc />
    public override RecoveryStrategy Strategy => RecoveryStrategy.None;
}

/// <summary>
/// Exception thrown when an LLM provider is unavailable (down, unreachable, or circuit-broken).
/// </summary>
/// <remarks>
/// <para>
/// This exception is thrown when the provider cannot be reached due to network failure,
/// service outage, or when the circuit breaker has opened after repeated failures.
/// </para>
/// <para>
/// The <see cref="EstimatedRecovery"/> property provides a hint about when the
/// service might recover, based on circuit breaker break duration or provider status.
/// </para>
/// <para>
/// <b>Recovery Strategy:</b> <see cref="RecoveryStrategy.Retry"/> (or <see cref="RecoveryStrategy.Fallback"/>
/// if an alternate provider is configured).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.8d as part of the Error Handling &amp; Recovery layer.
/// </para>
/// </remarks>
public sealed class ProviderUnavailableException : ProviderException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderUnavailableException"/> class.
    /// </summary>
    /// <param name="provider">The identifier of the unavailable provider.</param>
    /// <param name="EstimatedRecovery">
    /// Optional estimated time until the provider recovers. When available, this is
    /// displayed to the user as a wait time estimate.
    /// </param>
    /// <param name="inner">Optional inner exception with the original connection error.</param>
    /// <remarks>
    /// LOGIC: The user message includes the estimated recovery time when available
    /// to set expectations about service availability.
    /// </remarks>
    public ProviderUnavailableException(
        string provider,
        TimeSpan? EstimatedRecovery = null,
        Exception? inner = null)
        : base(
            provider,
            EstimatedRecovery.HasValue
                ? $"AI service temporarily unavailable. Estimated recovery: {EstimatedRecovery.Value.TotalSeconds:F0}s."
                : "AI service temporarily unavailable. Please try again shortly.",
            inner)
    {
        this.EstimatedRecovery = EstimatedRecovery;
    }

    /// <summary>
    /// Gets the estimated time until provider recovery.
    /// </summary>
    /// <value>
    /// The estimated recovery duration, or <c>null</c> if unknown.
    /// Derived from circuit breaker break duration or provider-reported status.
    /// </value>
    public TimeSpan? EstimatedRecovery { get; }
}

/// <summary>
/// Exception thrown when the provider returns an invalid, malformed, or empty response.
/// </summary>
/// <remarks>
/// <para>
/// This exception covers scenarios such as:
/// </para>
/// <list type="bullet">
///   <item><description>Empty response body from the provider API</description></item>
///   <item><description>Malformed JSON that cannot be deserialized</description></item>
///   <item><description>Missing required fields in the response payload</description></item>
///   <item><description>Content filter violations that block the response</description></item>
/// </list>
/// <para>
/// <b>Recovery Strategy:</b> <see cref="RecoveryStrategy.Retry"/> — the issue may
/// be transient and a retry could produce a valid response.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.8d as part of the Error Handling &amp; Recovery layer.
/// </para>
/// </remarks>
public sealed class InvalidResponseException : ProviderException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidResponseException"/> class.
    /// </summary>
    /// <param name="provider">The identifier of the provider that returned an invalid response.</param>
    /// <param name="userMessage">
    /// A user-friendly message. Defaults to a generic retry message.
    /// </param>
    /// <param name="inner">Optional inner exception with the parsing error details.</param>
    /// <remarks>
    /// LOGIC: Invalid responses are treated as transient because they may be caused by
    /// intermittent API issues that resolve on retry.
    /// </remarks>
    public InvalidResponseException(
        string provider,
        string? userMessage = null,
        Exception? inner = null)
        : base(provider, userMessage ?? "Received invalid response. Retrying...", inner)
    {
    }
}
