// -----------------------------------------------------------------------
// <copyright file="InvocationContext.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Chat.Services;

/// <summary>
/// Context about an agent invocation for usage tracking.
/// </summary>
/// <remarks>
/// <para>
/// Captures metadata about an agent invocation that is not part of the
/// <see cref="Lexichord.Abstractions.Agents.UsageMetrics"/> record.
/// Used to enrich telemetry events and usage records.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6d as part of the Usage Tracking feature.
/// </para>
/// </remarks>
/// <param name="AgentId">Identifier of the invoked agent.</param>
/// <param name="Model">LLM model used for the invocation.</param>
/// <param name="Duration">Total invocation duration.</param>
/// <param name="Streamed">Whether the response was streamed.</param>
public record InvocationContext(
    string AgentId,
    string Model,
    TimeSpan Duration,
    bool Streamed
);
