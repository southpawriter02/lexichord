// -----------------------------------------------------------------------
// <copyright file="UsageSummaryTypes.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Chat.Services;

/// <summary>
/// Monthly usage summary (Teams feature).
/// </summary>
/// <remarks>
/// <para>
/// Aggregated usage data for a single calendar month, broken down
/// by agent and model. Only accessible to Teams-tier users.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6d as part of the Usage Tracking feature.
/// </para>
/// </remarks>
/// <param name="Month">The calendar month for this summary.</param>
/// <param name="TotalInvocations">Total number of agent invocations.</param>
/// <param name="TotalPromptTokens">Total prompt tokens consumed.</param>
/// <param name="TotalCompletionTokens">Total completion tokens generated.</param>
/// <param name="TotalCost">Total estimated cost in USD.</param>
/// <param name="ByAgent">Usage breakdown by agent identifier.</param>
/// <param name="ByModel">Usage breakdown by LLM model.</param>
public record MonthlyUsageSummary(
    DateOnly Month,
    int TotalInvocations,
    int TotalPromptTokens,
    int TotalCompletionTokens,
    decimal TotalCost,
    Dictionary<string, AgentUsageSummary> ByAgent,
    Dictionary<string, ModelUsageSummary> ByModel
);

/// <summary>
/// Per-agent usage breakdown within a monthly summary.
/// </summary>
/// <param name="AgentId">Agent identifier.</param>
/// <param name="InvocationCount">Number of invocations for this agent.</param>
/// <param name="TotalTokens">Total tokens consumed by this agent.</param>
/// <param name="TotalCost">Total estimated cost for this agent.</param>
public record AgentUsageSummary(
    string AgentId,
    int InvocationCount,
    int TotalTokens,
    decimal TotalCost
);

/// <summary>
/// Per-model usage breakdown within a monthly summary.
/// </summary>
/// <param name="Model">LLM model identifier.</param>
/// <param name="InvocationCount">Number of invocations using this model.</param>
/// <param name="TotalTokens">Total tokens consumed by this model.</param>
/// <param name="TotalCost">Total estimated cost for this model.</param>
public record ModelUsageSummary(
    string Model,
    int InvocationCount,
    int TotalTokens,
    decimal TotalCost
);

/// <summary>
/// Export format options for usage data.
/// </summary>
/// <remarks>
/// <b>Introduced in:</b> v0.6.6d as part of the Usage Tracking feature.
/// </remarks>
public enum ExportFormat
{
    /// <summary>
    /// Comma-separated values format.
    /// </summary>
    Csv,

    /// <summary>
    /// JSON format with indented formatting.
    /// </summary>
    Json
}
