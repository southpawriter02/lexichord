// -----------------------------------------------------------------------
// <copyright file="IContextOrchestrator.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Context;

/// <summary>
/// Coordinates multiple <see cref="IContextStrategy"/> instances to assemble
/// comprehensive context for AI agent requests.
/// </summary>
/// <remarks>
/// <para>
/// The orchestrator is the central coordination point for context assembly.
/// It handles:
/// </para>
/// <list type="bullet">
///   <item><description><b>Parallel Execution:</b> All enabled strategies execute concurrently for minimal latency</description></item>
///   <item><description><b>Token Budget Enforcement:</b> Total assembled context stays within <see cref="ContextBudget.MaxTokens"/></description></item>
///   <item><description><b>Content Deduplication:</b> Overlapping content from multiple strategies is removed</description></item>
///   <item><description><b>Priority Sorting:</b> Higher-priority fragments are retained when trimming to budget</description></item>
///   <item><description><b>Strategy Toggle:</b> Strategies can be enabled/disabled at runtime without restart</description></item>
///   <item><description><b>Event Publishing:</b> Publishes <c>ContextAssembledEvent</c> after each assembly for UI updates</description></item>
/// </list>
/// <para>
/// <strong>Execution Flow:</strong>
/// </para>
/// <list type="number">
///   <item><description>Filter strategies by enabled state, budget exclusions, and license tier</description></item>
///   <item><description>Execute all eligible strategies in parallel with per-strategy timeout</description></item>
///   <item><description>Collect non-null, non-empty fragments</description></item>
///   <item><description>Deduplicate fragments with &gt;85% content similarity (Jaccard)</description></item>
///   <item><description>Sort fragments by priority (descending) then relevance (descending)</description></item>
///   <item><description>Trim to fit token budget, truncating or dropping low-priority fragments</description></item>
///   <item><description>Extract template variables and return assembled context</description></item>
/// </list>
/// <para>
/// <strong>Error Handling:</strong>
/// Individual strategy failures are caught and logged without affecting other strategies.
/// Only <see cref="OperationCanceledException"/> from the top-level cancellation token
/// propagates to the caller.
/// </para>
/// <para>
/// <strong>Thread Safety:</strong>
/// The orchestrator is thread-safe. Strategy enable/disable state is managed via
/// <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}"/>.
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2c as part of the Context Assembler system.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic usage from an agent
/// var request = new ContextGatheringRequest(
///     DocumentPath: "/docs/chapter1.md",
///     CursorPosition: 1234,
///     SelectedText: "The old man and the sea",
///     AgentId: "editor",
///     Hints: null);
///
/// var budget = new ContextBudget(
///     MaxTokens: 8000,
///     RequiredStrategies: new[] { "document" },
///     ExcludedStrategies: new[] { "rag" });
///
/// var assembled = await orchestrator.AssembleAsync(request, budget, ct);
///
/// // Use the assembled context
/// foreach (var fragment in assembled.Fragments)
/// {
///     Console.WriteLine($"{fragment.Label}: {fragment.TokenEstimate} tokens");
/// }
///
/// // Check for specific context
/// if (assembled.HasFragmentFrom("style"))
/// {
///     var styleFragment = assembled.GetFragment("style");
///     // Apply style-aware processing
/// }
///
/// // Disable a strategy at runtime
/// orchestrator.SetStrategyEnabled("rag", false);
/// </code>
/// </example>
/// <seealso cref="IContextStrategy"/>
/// <seealso cref="IContextStrategyFactory"/>
/// <seealso cref="AssembledContext"/>
/// <seealso cref="ContextBudget"/>
public interface IContextOrchestrator
{
    /// <summary>
    /// Assembles context by executing all enabled strategies and combining results.
    /// </summary>
    /// <param name="request">
    /// Context gathering parameters including document path, cursor position,
    /// selected text, agent ID, and optional strategy hints.
    /// </param>
    /// <param name="budget">
    /// Token budget and strategy filtering constraints. Specifies maximum tokens,
    /// required strategies (included even if over budget), and excluded strategies
    /// (skipped regardless of other settings).
    /// </param>
    /// <param name="ct">
    /// Cancellation token for the entire assembly operation. Individual strategies
    /// also receive linked tokens with per-strategy timeouts.
    /// </param>
    /// <returns>
    /// An <see cref="AssembledContext"/> containing all gathered fragments (sorted by
    /// priority, deduplicated, and trimmed to budget), along with metadata such as
    /// total token count, extracted variables, and assembly duration.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the top-level <paramref name="ct"/> is cancelled. Individual strategy
    /// timeouts are handled internally and do not propagate.
    /// </exception>
    /// <remarks>
    /// <para>
    /// LOGIC: The method follows a pipeline of operations:
    /// </para>
    /// <list type="number">
    ///   <item><description>Filter strategies by enabled state and budget constraints</description></item>
    ///   <item><description>Execute strategies in parallel with per-strategy timeout</description></item>
    ///   <item><description>Deduplicate overlapping content (configurable threshold)</description></item>
    ///   <item><description>Sort by priority descending, then relevance descending</description></item>
    ///   <item><description>Trim to fit within <see cref="ContextBudget.MaxTokens"/></description></item>
    ///   <item><description>Extract template variables from request and fragments</description></item>
    ///   <item><description>Publish <c>ContextAssembledEvent</c> for observability</description></item>
    /// </list>
    /// <para>
    /// <strong>Performance:</strong>
    /// Strategies execute concurrently via <c>Parallel.ForEachAsync</c>. The total
    /// assembly time is approximately the duration of the slowest strategy (plus
    /// sorting and deduplication overhead).
    /// </para>
    /// <para>
    /// <strong>Empty Result:</strong>
    /// Returns <see cref="AssembledContext.Empty"/> when no strategies are available
    /// or all strategies return null/empty results.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Assemble context for an editor agent
    /// var request = new ContextGatheringRequest(
    ///     "/docs/chapter1.md", 42, "selected text", "editor", null);
    /// var budget = ContextBudget.Default;
    ///
    /// var result = await orchestrator.AssembleAsync(request, budget, ct);
    ///
    /// // Log performance
    /// logger.LogInformation(
    ///     "Assembled {Tokens} tokens in {Duration}ms",
    ///     result.TotalTokens,
    ///     result.AssemblyDuration.TotalMilliseconds);
    /// </code>
    /// </example>
    Task<AssembledContext> AssembleAsync(
        ContextGatheringRequest request,
        ContextBudget budget,
        CancellationToken ct);

    /// <summary>
    /// Gets all registered context strategies, regardless of enabled state.
    /// </summary>
    /// <returns>
    /// A read-only list of all available <see cref="IContextStrategy"/> instances,
    /// filtered by the current user's license tier.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Delegates to <see cref="IContextStrategyFactory.CreateAllStrategies"/>
    /// to retrieve all strategies available for the current license tier. This includes
    /// both enabled and disabled strategies.
    /// </para>
    /// <para>
    /// <strong>Usage:</strong>
    /// Primarily used by the Context Preview panel (v0.7.2d) to display all available
    /// strategies with their enable/disable state.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Display all strategies in UI
    /// foreach (var strategy in orchestrator.GetStrategies())
    /// {
    ///     var enabled = orchestrator.IsStrategyEnabled(strategy.StrategyId);
    ///     Console.WriteLine($"{strategy.DisplayName}: {(enabled ? "On" : "Off")}");
    /// }
    /// </code>
    /// </example>
    IReadOnlyList<IContextStrategy> GetStrategies();

    /// <summary>
    /// Enables or disables a strategy for subsequent assembly operations.
    /// </summary>
    /// <param name="strategyId">
    /// The <see cref="IContextStrategy.StrategyId"/> of the strategy to toggle.
    /// </param>
    /// <param name="enabled">
    /// <c>true</c> to enable the strategy; <c>false</c> to disable it.
    /// Disabled strategies are skipped during <see cref="AssembleAsync"/>.
    /// </param>
    /// <remarks>
    /// <para>
    /// LOGIC: Updates the internal enabled state stored in a
    /// <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}"/>.
    /// Publishes a <c>StrategyToggleEvent</c> notification for UI synchronization.
    /// </para>
    /// <para>
    /// <strong>Default State:</strong>
    /// All strategies are enabled by default. Disabling a strategy persists only
    /// for the current application session (not serialized to configuration).
    /// </para>
    /// <para>
    /// <strong>Thread Safety:</strong>
    /// This method is thread-safe and can be called from any thread (e.g., UI thread
    /// for toggle switches in the Context Preview panel).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Disable RAG context for performance
    /// orchestrator.SetStrategyEnabled("rag", false);
    ///
    /// // Re-enable later
    /// orchestrator.SetStrategyEnabled("rag", true);
    /// </code>
    /// </example>
    void SetStrategyEnabled(string strategyId, bool enabled);

    /// <summary>
    /// Checks whether a strategy is currently enabled.
    /// </summary>
    /// <param name="strategyId">
    /// The <see cref="IContextStrategy.StrategyId"/> of the strategy to check.
    /// </param>
    /// <returns>
    /// <c>true</c> if the strategy is enabled (default); <c>false</c> if it has
    /// been explicitly disabled via <see cref="SetStrategyEnabled"/>.
    /// </returns>
    /// <remarks>
    /// LOGIC: Returns <c>true</c> (enabled) by default if the strategy has never
    /// been explicitly toggled. This ensures all strategies participate in assembly
    /// unless explicitly disabled by the user.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Check before manual operations
    /// if (orchestrator.IsStrategyEnabled("rag"))
    /// {
    ///     // RAG context will be included in next assembly
    /// }
    /// </code>
    /// </example>
    bool IsStrategyEnabled(string strategyId);
}
