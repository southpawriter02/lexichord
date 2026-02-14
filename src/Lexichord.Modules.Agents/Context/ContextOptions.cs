// -----------------------------------------------------------------------
// <copyright file="ContextOptions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Context;

/// <summary>
/// Configuration options for the context assembly system.
/// Controls timeout, deduplication, parallelism, and budget defaults.
/// </summary>
/// <remarks>
/// <para>
/// Loaded from the application configuration section specified by <see cref="Section"/>.
/// All properties have sensible defaults, so explicit configuration is optional.
/// </para>
/// <para>
/// <strong>Configuration in appsettings.json:</strong>
/// </para>
/// <code>
/// {
///   "Context": {
///     "DefaultBudget": 8000,
///     "StrategyTimeout": 5000,
///     "EnableDeduplication": true,
///     "DeduplicationThreshold": 0.85,
///     "MaxParallelism": 6
///   }
/// }
/// </code>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2c as part of the Context Orchestrator.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register with defaults
/// services.AddOptions&lt;ContextOptions&gt;();
///
/// // Or bind from configuration
/// services.Configure&lt;ContextOptions&gt;(
///     configuration.GetSection(ContextOptions.Section));
///
/// // Injected via IOptions&lt;ContextOptions&gt;
/// public class ContextOrchestrator(IOptions&lt;ContextOptions&gt; options)
/// {
///     private readonly ContextOptions _options = options.Value;
/// }
/// </code>
/// </example>
public sealed class ContextOptions
{
    /// <summary>
    /// The configuration section name for binding.
    /// </summary>
    /// <value><c>"Context"</c></value>
    /// <remarks>
    /// LOGIC: Matches the JSON configuration path: <c>{ "Context": { ... } }</c>.
    /// Used by <c>IConfiguration.GetSection()</c> during options binding.
    /// </remarks>
    public const string Section = "Context";

    /// <summary>
    /// Gets or sets the default token budget when none is specified by the caller.
    /// </summary>
    /// <value>Default: <c>8000</c> tokens.</value>
    /// <remarks>
    /// LOGIC: 8000 tokens is a conservative default that fits most models while
    /// leaving room for system prompts (~500 tokens), user messages, and response
    /// generation (~1000-2000 tokens). For an 8K context window model, this allows
    /// up to 6000-7000 tokens of actual context after overhead.
    /// </remarks>
    public int DefaultBudget { get; set; } = 8000;

    /// <summary>
    /// Gets or sets the timeout in milliseconds for individual strategy execution.
    /// Strategies exceeding this timeout are cancelled and their results are discarded.
    /// </summary>
    /// <value>Default: <c>5000</c> ms (5 seconds).</value>
    /// <remarks>
    /// <para>
    /// LOGIC: Each strategy receives a linked cancellation token with this timeout.
    /// Slow strategies (e.g., RAG search with cold cache) are cancelled gracefully
    /// without blocking the overall assembly. The orchestrator logs a warning for
    /// timed-out strategies.
    /// </para>
    /// <para>
    /// <strong>Recommendation:</strong>
    /// Set to at least 2000ms for RAG-enabled configurations, as semantic search
    /// may require network round-trips. Reduce to 1000ms for testing.
    /// </para>
    /// </remarks>
    public int StrategyTimeout { get; set; } = 5000;

    /// <summary>
    /// Gets or sets whether content deduplication is enabled.
    /// When enabled, fragments with similar content are deduplicated.
    /// </summary>
    /// <value>Default: <c>true</c>.</value>
    /// <remarks>
    /// LOGIC: Deduplication prevents redundant context when multiple strategies
    /// produce overlapping content (e.g., selection overlapping with document
    /// content). Uses Jaccard word-set similarity comparison.
    /// Set to <c>false</c> to skip deduplication for maximum performance.
    /// </remarks>
    public bool EnableDeduplication { get; set; } = true;

    /// <summary>
    /// Gets or sets the similarity threshold for content deduplication.
    /// Fragments with Jaccard similarity above this threshold are considered duplicates.
    /// </summary>
    /// <value>Default: <c>0.85</c> (85% word overlap).</value>
    /// <remarks>
    /// <para>
    /// LOGIC: Range 0.0 to 1.0, where 1.0 means exact match required and 0.0
    /// means any overlap is considered a duplicate. The default 0.85 catches
    /// near-identical content while allowing genuinely different fragments that
    /// share common vocabulary.
    /// </para>
    /// <para>
    /// <strong>Tuning Guide:</strong>
    /// </para>
    /// <list type="bullet">
    ///   <item><description>0.90-1.00: Very conservative, only near-exact duplicates</description></item>
    ///   <item><description>0.80-0.90: Balanced, catches paraphrased duplicates (recommended)</description></item>
    ///   <item><description>0.60-0.80: Aggressive, may remove genuinely different content</description></item>
    /// </list>
    /// </remarks>
    public float DeduplicationThreshold { get; set; } = 0.85f;

    /// <summary>
    /// Gets or sets the maximum number of strategies to execute in parallel.
    /// </summary>
    /// <value>Default: <c>6</c> (matches the number of built-in strategies).</value>
    /// <remarks>
    /// LOGIC: Limits the degree of parallelism for <c>Parallel.ForEachAsync</c>.
    /// Set to a lower value on resource-constrained systems or when strategies
    /// compete for shared resources (e.g., database connections).
    /// </remarks>
    public int MaxParallelism { get; set; } = 6;
}
