// -----------------------------------------------------------------------
// <copyright file="ContextBudget.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Context;

/// <summary>
/// Specifies token budget and strategy filtering for context assembly.
/// Controls which strategies run and how much total context can be included.
/// </summary>
/// <remarks>
/// <para>
/// The budget is used by the context orchestrator to:
/// </para>
/// <list type="bullet">
///   <item><description><b>Limit Total Tokens:</b> Ensure assembled context fits within model limits</description></item>
///   <item><description><b>Filter Strategies:</b> Exclude or require specific strategies</description></item>
///   <item><description><b>Prioritize Trimming:</b> Remove lower-priority fragments when over budget</description></item>
/// </list>
/// <para>
/// <strong>Exclusion vs. Requirement:</strong>
/// Excluded strategies take precedence - even if a strategy is in the required list,
/// it will be skipped if it's also in the excluded list. This prevents configuration
/// conflicts and provides a clear override mechanism.
/// </para>
/// <para>
/// <strong>Token Budget Calculation:</strong>
/// The <see cref="MaxTokens"/> value should account for:
/// </para>
/// <list type="bullet">
///   <item><description>System message overhead (~200-500 tokens)</description></item>
///   <item><description>Response generation space (~1000-2000 tokens)</description></item>
///   <item><description>Message formatting overhead (~4 tokens per message)</description></item>
/// </list>
/// <para>
/// For a 8K context window model, a budget of 6000-7000 tokens leaves room for
/// system prompts and response generation.
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2a as part of the Context Strategy Interface.
/// </para>
/// </remarks>
/// <param name="MaxTokens">Maximum total tokens for assembled context.</param>
/// <param name="RequiredStrategies">Strategy IDs that must be included even if over budget.</param>
/// <param name="ExcludedStrategies">Strategy IDs that should not be executed.</param>
/// <example>
/// <code>
/// // Editor agent with specific requirements
/// var budget = new ContextBudget(
///     MaxTokens: 8000,
///     RequiredStrategies: new[] { "document", "selection" },
///     ExcludedStrategies: new[] { "rag" });
///
/// // Check if a strategy should execute
/// if (budget.ShouldExecute("document"))
/// {
///     // Execute document strategy
/// }
///
/// // Default budget with no filtering
/// var defaultBudget = ContextBudget.Default; // 8000 tokens, no filters
///
/// // Budget with only token limit
/// var simpleBudget = ContextBudget.WithLimit(5000);
/// </code>
/// </example>
public record ContextBudget(
    int MaxTokens,
    IReadOnlyList<string>? RequiredStrategies,
    IReadOnlyList<string>? ExcludedStrategies)
{
    /// <summary>
    /// Gets the default budget with 8000 tokens and no strategy restrictions.
    /// </summary>
    /// <value>A new <see cref="ContextBudget"/> with 8000 max tokens.</value>
    /// <remarks>
    /// LOGIC: 8000 tokens is a conservative default that fits most models
    /// while leaving room for system prompts (~500 tokens), user messages,
    /// and response generation (~1000-2000 tokens). For an 8K context model,
    /// this allows up to 6000-7000 tokens of actual context after overhead.
    /// </remarks>
    public static ContextBudget Default => new(8000, null, null);

    /// <summary>
    /// Creates a budget with only a token limit.
    /// </summary>
    /// <param name="maxTokens">Maximum tokens for context assembly.</param>
    /// <returns>A new <see cref="ContextBudget"/> with specified token limit.</returns>
    /// <remarks>
    /// LOGIC: Convenience factory for the common case of only limiting tokens
    /// without strategy filtering. Simplifies budget creation when no
    /// inclusion/exclusion rules are needed.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a budget for a smaller model
    /// var budget = ContextBudget.WithLimit(4000);
    /// </code>
    /// </example>
    public static ContextBudget WithLimit(int maxTokens)
        => new(maxTokens, null, null);

    /// <summary>
    /// Checks if a strategy is required.
    /// </summary>
    /// <param name="strategyId">Strategy ID to check.</param>
    /// <returns><c>true</c> if the strategy is in the required list; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Required strategies are included even if they would exceed the budget,
    /// though a warning should be logged by the orchestrator. This allows agents
    /// to specify context that is absolutely essential for their task.
    /// </para>
    /// <para>
    /// <strong>Important:</strong> Even required strategies are skipped if they
    /// appear in <see cref="ExcludedStrategies"/> (exclusion takes precedence).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// if (budget.IsRequired("document"))
    /// {
    ///     // Include this strategy even if over budget
    /// }
    /// </code>
    /// </example>
    public bool IsRequired(string strategyId)
        => RequiredStrategies?.Contains(strategyId) == true;

    /// <summary>
    /// Checks if a strategy is excluded.
    /// </summary>
    /// <param name="strategyId">Strategy ID to check.</param>
    /// <returns><c>true</c> if the strategy is in the excluded list; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// LOGIC: Excluded strategies are never executed, even if they are also
    /// in the required list (exclusion takes precedence). This provides a
    /// clear override mechanism for disabling specific strategies.
    /// </remarks>
    /// <example>
    /// <code>
    /// if (budget.IsExcluded("rag"))
    /// {
    ///     // Skip RAG search entirely
    /// }
    /// </code>
    /// </example>
    public bool IsExcluded(string strategyId)
        => ExcludedStrategies?.Contains(strategyId) == true;

    /// <summary>
    /// Checks if a strategy should be executed based on inclusion/exclusion rules.
    /// </summary>
    /// <param name="strategyId">Strategy ID to check.</param>
    /// <returns><c>true</c> if the strategy should execute; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: A strategy should execute unless it's explicitly excluded.
    /// This method is the primary check used by the orchestrator before
    /// running a strategy. It implements the rule that exclusions take
    /// precedence over requirements.
    /// </para>
    /// <para>
    /// <strong>Note:</strong> This method only checks exclusion, not requirements.
    /// Requirements affect budget enforcement (required strategies can exceed budget),
    /// not execution filtering (all non-excluded strategies execute).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In orchestrator logic
    /// foreach (var strategy in allStrategies)
    /// {
    ///     if (budget.ShouldExecute(strategy.StrategyId))
    ///     {
    ///         var fragment = await strategy.GatherAsync(request, ct);
    ///         // ... process fragment
    ///     }
    /// }
    /// </code>
    /// </example>
    public bool ShouldExecute(string strategyId)
        => !IsExcluded(strategyId);
}
