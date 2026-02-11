// -----------------------------------------------------------------------
// <copyright file="ContextAssemblyException.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Resilience;

/// <summary>
/// Exception thrown when context assembly fails during agent execution.
/// </summary>
/// <remarks>
/// <para>
/// This exception is raised when the <see cref="Performance.ICachedContextAssembler"/> or
/// <see cref="Services.ContextInjector"/> encounters an error while assembling context
/// from RAG, style rules, or document sources.
/// </para>
/// <para>
/// Context assembly failures are treated as recoverable because the agent can
/// still produce a response without injected context — the quality may be lower,
/// but the conversation can continue.
/// </para>
/// <para>
/// <b>Recovery Strategy:</b> <see cref="RecoveryStrategy.Retry"/>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.8d as part of the Error Handling &amp; Recovery layer.
/// </para>
/// </remarks>
/// <seealso cref="AgentException"/>
public sealed class ContextAssemblyException : AgentException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContextAssemblyException"/> class.
    /// </summary>
    /// <param name="userMessage">
    /// A user-friendly message. Defaults to a generic context assembly failure message.
    /// </param>
    /// <param name="inner">
    /// Optional inner exception with the original assembly error.
    /// </param>
    /// <remarks>
    /// LOGIC: Context assembly failures are typically caused by transient issues
    /// with external data sources (database, file system) and can often be resolved
    /// with a retry.
    /// </remarks>
    public ContextAssemblyException(
        string? userMessage = null,
        Exception? inner = null)
        : base(userMessage ?? "Failed to assemble context. Response may be less accurate.", inner)
    {
    }
}
