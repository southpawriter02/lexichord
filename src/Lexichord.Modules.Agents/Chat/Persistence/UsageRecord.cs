// -----------------------------------------------------------------------
// <copyright file="UsageRecord.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Chat.Persistence;

/// <summary>
/// Entity for persisted usage records.
/// </summary>
/// <remarks>
/// <para>
/// Represents a single agent invocation usage entry stored for
/// monthly summary aggregation and export functionality.
/// Only persisted for Teams-tier users.
/// </para>
/// <para>
/// Uses in-memory storage via <see cref="UsageRepository"/> rather
/// than EF Core, consistent with the module's lightweight persistence
/// approach.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6d as part of the Usage Tracking feature.
/// </para>
/// </remarks>
public class UsageRecord
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the invocation.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the conversation ID for correlation.
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// Gets or sets the agent identifier.
    /// </summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the LLM model identifier.
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of prompt tokens.
    /// </summary>
    public int PromptTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of completion tokens.
    /// </summary>
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Gets or sets the estimated cost in USD.
    /// </summary>
    public decimal EstimatedCost { get; set; }

    /// <summary>
    /// Gets or sets the invocation duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets whether the response was streamed.
    /// </summary>
    public bool Streamed { get; set; }

    /// <summary>
    /// Gets the month key for indexing (Year * 100 + Month).
    /// </summary>
    /// <remarks>
    /// LOGIC: Computed month key for efficient grouping.
    /// Example: January 2026 = 202601.
    /// </remarks>
    public int Month => Timestamp.Month + (Timestamp.Year * 100);
}
