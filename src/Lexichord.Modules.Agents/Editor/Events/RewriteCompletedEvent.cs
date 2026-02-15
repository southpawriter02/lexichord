// -----------------------------------------------------------------------
// <copyright file="RewriteCompletedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: MediatR notification published when a rewrite operation completes
//   (v0.7.3b). Published by RewriteCommandHandler after the EditorAgent
//   returns a result (success or failure) and document application finishes.
//
//   Consumers:
//     - RewriteCommandViewModel (progress UI state reset)
//     - Usage tracking (token consumption)
//     - Analytics/telemetry handlers (future)
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Editor;
using MediatR;

namespace Lexichord.Modules.Agents.Editor.Events;

/// <summary>
/// Published when a rewrite operation completes (success or failure).
/// </summary>
/// <remarks>
/// <para>
/// Published by <see cref="RewriteCommandHandler"/> in the finally block
/// of <see cref="IRewriteCommandHandler.ExecuteAsync"/>. Always published
/// regardless of success or failure, paired with <see cref="RewriteStartedEvent"/>.
/// </para>
/// <para><b>Introduced in:</b> v0.7.3b</para>
/// </remarks>
/// <param name="Intent">The rewrite intent that was executed.</param>
/// <param name="Success">Whether the rewrite completed successfully.</param>
/// <param name="Usage">Token usage metrics from the LLM invocation.</param>
/// <param name="Duration">Total time taken for the operation.</param>
/// <param name="ErrorMessage">Error message if the rewrite failed, null on success.</param>
/// <param name="Timestamp">UTC timestamp when the event was created.</param>
public record RewriteCompletedEvent(
    RewriteIntent Intent,
    bool Success,
    UsageMetrics Usage,
    TimeSpan Duration,
    string? ErrorMessage,
    DateTime Timestamp) : INotification
{
    /// <summary>
    /// Creates a new <see cref="RewriteCompletedEvent"/> with the current UTC timestamp.
    /// </summary>
    /// <param name="intent">The rewrite intent that was executed.</param>
    /// <param name="success">Whether the rewrite completed successfully.</param>
    /// <param name="usage">Token usage metrics.</param>
    /// <param name="duration">Total operation duration.</param>
    /// <param name="errorMessage">Error message on failure, null on success.</param>
    /// <returns>A new <see cref="RewriteCompletedEvent"/>.</returns>
    public static RewriteCompletedEvent Create(
        RewriteIntent intent,
        bool success,
        UsageMetrics usage,
        TimeSpan duration,
        string? errorMessage = null) =>
        new(intent, success, usage, duration, errorMessage, DateTime.UtcNow);
}
