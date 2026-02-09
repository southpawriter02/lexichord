// -----------------------------------------------------------------------
// <copyright file="UsageMetrics.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents;

/// <summary>
/// Immutable record tracking token consumption and estimated costs.
/// </summary>
/// <remarks>
/// <para>
/// Usage metrics provide transparency into AI resource consumption. They are
/// calculated for every agent invocation and displayed to users in the chat
/// panel footer.
/// </para>
/// <para>
/// Cost estimation uses configured pricing for the model used. Actual billing
/// may differ based on provider pricing changes or usage tiers.
/// </para>
/// <para>
/// <b>Thread safety:</b> This is an immutable record and is inherently thread-safe.
/// It can be safely shared across threads without synchronization.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6a as part of the Agent Abstractions layer.
/// </para>
/// </remarks>
/// <param name="PromptTokens">
/// Number of tokens in the prompt (system message, context, history, user message).
/// Reported by the LLM provider in the response. Must be non-negative.
/// </param>
/// <param name="CompletionTokens">
/// Number of tokens in the generated response.
/// Reported by the LLM provider in the response. Must be non-negative.
/// </param>
/// <param name="EstimatedCost">
/// Estimated cost in USD based on configured model pricing.
/// Calculated as: <c>(PromptTokens / 1000 * promptCost) + (CompletionTokens / 1000 * completionCost)</c>.
/// Must be non-negative.
/// </param>
/// <example>
/// <code>
/// // Typical usage metrics
/// var metrics = new UsageMetrics(
///     PromptTokens: 1500,
///     CompletionTokens: 500,
///     EstimatedCost: 0.045m  // $0.045
/// );
///
/// // Display in UI
/// Console.WriteLine(metrics.ToDisplayString());
/// // Output: "2,000 tokens (~$0.0450)"
///
/// // Accumulate across multiple invocations
/// var total = metrics.Add(new UsageMetrics(200, 100, 0.006m));
/// // total: 2,300 tokens, $0.051
/// </code>
/// </example>
public record UsageMetrics(
    int PromptTokens,
    int CompletionTokens,
    decimal EstimatedCost)
{
    /// <summary>
    /// Total tokens consumed (prompt + completion).
    /// </summary>
    /// <value>
    /// The sum of <see cref="PromptTokens"/> and <see cref="CompletionTokens"/>.
    /// </value>
    /// <remarks>
    /// LOGIC: Simple addition of prompt and completion tokens.
    /// Displayed in the chat panel footer and used for context window checks.
    /// </remarks>
    public int TotalTokens => PromptTokens + CompletionTokens;

    /// <summary>
    /// Zero usage metrics for error cases or when tracking is disabled.
    /// </summary>
    /// <value>
    /// A shared <see cref="UsageMetrics"/> instance with all values set to zero.
    /// </value>
    /// <remarks>
    /// LOGIC: Static sentinel instance to avoid allocating new zero-value records.
    /// Used by <see cref="AgentResponse.Empty"/> and <see cref="AgentResponse.Error"/>.
    /// </remarks>
    public static UsageMetrics Zero { get; } = new(0, 0, 0m);

    /// <summary>
    /// Adds two usage metrics together.
    /// </summary>
    /// <param name="other">The metrics to add.</param>
    /// <returns>
    /// A new <see cref="UsageMetrics"/> instance with the combined values.
    /// </returns>
    /// <remarks>
    /// LOGIC: Creates a new record with the sum of prompt tokens, completion tokens,
    /// and estimated costs. Used to accumulate metrics across multiple invocations
    /// within a conversation session.
    /// </remarks>
    /// <example>
    /// <code>
    /// var m1 = new UsageMetrics(100, 50, 0.003m);
    /// var m2 = new UsageMetrics(200, 100, 0.006m);
    /// var combined = m1.Add(m2);
    /// // combined: PromptTokens=300, CompletionTokens=150, EstimatedCost=0.009
    /// </code>
    /// </example>
    public UsageMetrics Add(UsageMetrics other) => new(
        PromptTokens + other.PromptTokens,
        CompletionTokens + other.CompletionTokens,
        EstimatedCost + other.EstimatedCost);

    /// <summary>
    /// Formats the metrics for display.
    /// </summary>
    /// <returns>
    /// A formatted string like <c>"1,500 tokens (~$0.0045)"</c>.
    /// </returns>
    /// <remarks>
    /// LOGIC: Uses N0 format for token count (thousands separator, no decimals)
    /// and F4 format for cost (4 decimal places in USD). The tilde (~) prefix
    /// indicates the cost is an estimate.
    /// </remarks>
    public string ToDisplayString() =>
        $"{TotalTokens:N0} tokens (~${EstimatedCost:F4})";

    /// <summary>
    /// Creates usage metrics from provider response data.
    /// </summary>
    /// <param name="promptTokens">Prompt tokens reported by the provider.</param>
    /// <param name="completionTokens">Completion tokens reported by the provider.</param>
    /// <param name="promptCostPer1K">Cost per 1,000 prompt tokens (in USD).</param>
    /// <param name="completionCostPer1K">Cost per 1,000 completion tokens (in USD).</param>
    /// <returns>
    /// A new <see cref="UsageMetrics"/> with the calculated estimated cost.
    /// </returns>
    /// <remarks>
    /// LOGIC: Calculates cost as:
    /// <c>(promptTokens / 1000 * promptCostPer1K) + (completionTokens / 1000 * completionCostPer1K)</c>.
    /// Uses decimal division (1000m) for precise cost calculation.
    /// </remarks>
    /// <example>
    /// <code>
    /// // GPT-4 pricing example
    /// var metrics = UsageMetrics.Calculate(
    ///     promptTokens: 1000,
    ///     completionTokens: 500,
    ///     promptCostPer1K: 0.01m,
    ///     completionCostPer1K: 0.03m);
    /// // EstimatedCost = (1000/1000 * 0.01) + (500/1000 * 0.03) = 0.025
    /// </code>
    /// </example>
    public static UsageMetrics Calculate(
        int promptTokens,
        int completionTokens,
        decimal promptCostPer1K,
        decimal completionCostPer1K) => new(
            promptTokens,
            completionTokens,
            (promptTokens / 1000m * promptCostPer1K) +
            (completionTokens / 1000m * completionCostPer1K));
}
