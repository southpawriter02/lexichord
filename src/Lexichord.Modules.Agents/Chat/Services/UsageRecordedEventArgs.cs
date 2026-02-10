// -----------------------------------------------------------------------
// <copyright file="UsageRecordedEventArgs.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents;

namespace Lexichord.Modules.Agents.Chat.Services;

/// <summary>
/// Event arguments for usage recorded events.
/// </summary>
/// <remarks>
/// <para>
/// Raised by <see cref="UsageTracker"/> after each usage recording.
/// Provides the latest invocation metrics along with accumulated
/// conversation and session totals for UI binding.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6d as part of the Usage Tracking feature.
/// </para>
/// </remarks>
public class UsageRecordedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="UsageRecordedEventArgs"/>.
    /// </summary>
    /// <param name="thisInvocation">Usage metrics for the current invocation.</param>
    /// <param name="conversationTotal">Accumulated conversation total.</param>
    /// <param name="sessionTotal">Accumulated session total.</param>
    public UsageRecordedEventArgs(
        UsageMetrics thisInvocation,
        UsageMetrics conversationTotal,
        UsageMetrics sessionTotal)
    {
        ThisInvocation = thisInvocation;
        ConversationTotal = conversationTotal;
        SessionTotal = sessionTotal;
    }

    /// <summary>
    /// Gets the usage metrics for the latest invocation.
    /// </summary>
    public UsageMetrics ThisInvocation { get; }

    /// <summary>
    /// Gets the accumulated usage for the current conversation.
    /// </summary>
    public UsageMetrics ConversationTotal { get; }

    /// <summary>
    /// Gets the accumulated usage for the current session.
    /// </summary>
    public UsageMetrics SessionTotal { get; }
}
