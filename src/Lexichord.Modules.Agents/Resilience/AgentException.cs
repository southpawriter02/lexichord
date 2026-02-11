// -----------------------------------------------------------------------
// <copyright file="AgentException.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Resilience;

/// <summary>
/// Base exception for all agent-related errors in the Agents module.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AgentException"/> provides a structured error model that separates
/// user-facing messages from technical details, enabling both user-friendly error
/// display and detailed diagnostic logging.
/// </para>
/// <para>
/// Every <see cref="AgentException"/> carries:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="UserMessage"/>: A non-technical, actionable message for the UI</description></item>
///   <item><description><see cref="IsRecoverable"/>: Whether automatic recovery is possible</description></item>
///   <item><description><see cref="Strategy"/>: The recommended <see cref="RecoveryStrategy"/></description></item>
///   <item><description><see cref="TechnicalDetails"/>: Optional diagnostic information for logs</description></item>
/// </list>
/// <para>
/// <b>Exception Hierarchy:</b>
/// </para>
/// <code>
/// AgentException
///   ├── ProviderException
///   │     ├── AgentRateLimitException
///   │     ├── AgentAuthenticationException
///   │     ├── ProviderUnavailableException
///   │     └── InvalidResponseException
///   ├── TokenLimitException
///   └── ContextAssemblyException
/// </code>
/// <para>
/// <b>Introduced in:</b> v0.6.8d as part of the Error Handling &amp; Recovery layer.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// try
/// {
///     var response = await agent.ProcessAsync(request, ct);
/// }
/// catch (AgentRateLimitException rl)
/// {
///     statusBar.Show($"Rate limited. Wait {rl.RetryAfter.TotalSeconds:F0}s");
/// }
/// catch (AgentException ex) when (ex.IsRecoverable)
/// {
///     var recovered = await recoveryService.AttemptRecoveryAsync(ex, request, ct);
/// }
/// catch (AgentException ex)
/// {
///     ShowError(ex.UserMessage);
/// }
/// </code>
/// </example>
/// <seealso cref="RecoveryStrategy"/>
/// <seealso cref="IErrorRecoveryService"/>
public class AgentException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentException"/> class
    /// with a user-friendly error message.
    /// </summary>
    /// <param name="userMessage">
    /// A non-technical, actionable message suitable for display in the UI.
    /// Should guide the user toward resolving the issue.
    /// </param>
    /// <param name="inner">
    /// Optional inner exception providing the root cause for diagnostic purposes.
    /// </param>
    /// <remarks>
    /// LOGIC: The <paramref name="userMessage"/> is stored both as the standard
    /// <see cref="Exception.Message"/> and in the dedicated <see cref="UserMessage"/>
    /// property to ensure consistent access regardless of which property callers use.
    /// </remarks>
    public AgentException(string userMessage, Exception? inner = null)
        : base(userMessage, inner)
    {
        UserMessage = userMessage;
    }

    /// <summary>
    /// Gets the user-friendly error message for display in the UI.
    /// </summary>
    /// <value>
    /// A non-technical, actionable message. Never null.
    /// </value>
    /// <remarks>
    /// LOGIC: This message is designed for end-user consumption and should be free
    /// of stack traces, exception type names, or other technical jargon. Examples:
    /// <list type="bullet">
    ///   <item><description>"The AI service is temporarily unavailable. Please try again in a few seconds."</description></item>
    ///   <item><description>"Message too long (12,000 tokens). Truncated to 8,000 tokens."</description></item>
    ///   <item><description>"Invalid API key. Please check Settings → AI Providers."</description></item>
    /// </list>
    /// </remarks>
    public string UserMessage { get; }

    /// <summary>
    /// Gets a value indicating whether automatic recovery is possible.
    /// </summary>
    /// <value>
    /// <c>true</c> if the error can be recovered from automatically (e.g., retry, truncate);
    /// <c>false</c> if user intervention is required (e.g., invalid API key).
    /// Defaults to <c>true</c> for the base class.
    /// </value>
    /// <remarks>
    /// LOGIC: Subclasses override this to indicate non-recoverable states.
    /// <see cref="AgentAuthenticationException"/> returns <c>false</c> because
    /// the user must manually correct their API key.
    /// </remarks>
    public virtual bool IsRecoverable => true;

    /// <summary>
    /// Gets the recommended recovery strategy for this error.
    /// </summary>
    /// <value>
    /// The <see cref="RecoveryStrategy"/> to apply. Defaults to <see cref="RecoveryStrategy.Retry"/>
    /// for the base class.
    /// </value>
    /// <remarks>
    /// LOGIC: Subclasses override this property to reflect their specific recovery path:
    /// <list type="bullet">
    ///   <item><description><see cref="AgentRateLimitException"/>: <see cref="RecoveryStrategy.Queue"/></description></item>
    ///   <item><description><see cref="TokenLimitException"/>: <see cref="RecoveryStrategy.Truncate"/></description></item>
    ///   <item><description><see cref="AgentAuthenticationException"/>: <see cref="RecoveryStrategy.None"/></description></item>
    /// </list>
    /// </remarks>
    public virtual RecoveryStrategy Strategy => RecoveryStrategy.Retry;

    /// <summary>
    /// Gets or initializes the technical details for diagnostic logging.
    /// </summary>
    /// <value>
    /// Detailed technical information including HTTP status codes, provider error codes,
    /// request IDs, etc. May be <c>null</c> when no additional details are available.
    /// </value>
    /// <remarks>
    /// LOGIC: This property is logged at Debug/Information level for diagnostics but
    /// never shown to the user. Use <c>init</c> to set during construction:
    /// <code>
    /// throw new AgentException("Service unavailable")
    /// {
    ///     TechnicalDetails = $"HTTP 503 from {provider}. Request ID: {requestId}"
    /// };
    /// </code>
    /// </remarks>
    public string? TechnicalDetails { get; init; }
}
