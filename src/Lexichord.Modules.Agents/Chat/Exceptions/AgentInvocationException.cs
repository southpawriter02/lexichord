// -----------------------------------------------------------------------
// <copyright file="AgentInvocationException.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Wraps underlying failures (LLM errors, context errors, etc.)
//   during agent invocation to provide a consistent error handling pattern
//   for the UI layer. The inner exception preserves the original failure
//   detail for diagnostics and logging.
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Chat.Exceptions;

/// <summary>
/// Exception thrown when an agent invocation fails.
/// </summary>
/// <remarks>
/// <para>
/// This exception wraps underlying failures (LLM errors, context assembly errors,
/// prompt rendering errors, etc.) to provide a consistent error handling pattern
/// for the UI. The <see cref="Exception.InnerException"/> preserves the original
/// error for diagnostics.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6b as part of the Co-pilot Agent implementation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// try
/// {
///     var response = await agent.InvokeAsync(request, ct);
/// }
/// catch (AgentInvocationException ex)
/// {
///     logger.LogError(ex, "Agent invocation failed");
///     // Show user-friendly error in UI
/// }
/// </code>
/// </example>
public sealed class AgentInvocationException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="AgentInvocationException"/>
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The error message describing the failure.</param>
    public AgentInvocationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of <see cref="AgentInvocationException"/>
    /// with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="innerException">The exception that caused the invocation failure.</param>
    public AgentInvocationException(string message, Exception innerException)
        : base(message, innerException) { }
}
