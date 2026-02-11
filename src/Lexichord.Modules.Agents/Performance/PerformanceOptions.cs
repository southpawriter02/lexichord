// -----------------------------------------------------------------------
// <copyright file="PerformanceOptions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Performance;

/// <summary>
/// Performance tuning options for the Agents module.
/// </summary>
/// <remarks>
/// <para>
/// Configures thresholds and limits for:
/// </para>
/// <list type="bullet">
///   <item><description>Conversation memory management (message count and byte limits)</description></item>
///   <item><description>Request coalescing window for batching rapid sequential requests</description></item>
///   <item><description>Context cache duration for avoiding redundant RAG/style lookups</description></item>
///   <item><description>Compiled template cache size limit</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.6.8c as part of Performance Optimization.
/// </para>
/// <para>
/// Configuration can be bound from <c>appsettings.json</c> under the
/// <c>Agents:Performance</c> section.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In appsettings.json:
/// // {
/// //   "Agents": {
/// //     "Performance": {
/// //       "MaxConversationMessages": 100,
/// //       "CoalescingWindow": "00:00:00.200"
/// //     }
/// //   }
/// // }
///
/// // Programmatic configuration:
/// services.Configure&lt;PerformanceOptions&gt;(options =>
/// {
///     options.MaxConversationMessages = 100;
///     options.CoalescingWindow = TimeSpan.FromMilliseconds(200);
/// });
/// </code>
/// </example>
public sealed record PerformanceOptions
{
    /// <summary>
    /// Maximum number of messages before trimming (default: 50).
    /// </summary>
    /// <value>A positive integer. System messages are preserved during trimming.</value>
    /// <remarks>
    /// When the conversation history exceeds this limit, the oldest non-system
    /// messages are removed to stay within bounds. This prevents unbounded
    /// memory growth during long conversations.
    /// </remarks>
    public int MaxConversationMessages { get; init; } = 50;

    /// <summary>
    /// Maximum memory per conversation in bytes (default: 5MB).
    /// </summary>
    /// <value>A positive long value representing the byte limit.</value>
    /// <remarks>
    /// Estimated using UTF-16 character encoding (2 bytes per character)
    /// plus a per-message object overhead of approximately 100 bytes.
    /// When exceeded, aggressive trimming is triggered.
    /// </remarks>
    public long MaxConversationMemoryBytes { get; init; } = 5 * 1024 * 1024;

    /// <summary>
    /// Time window for request coalescing (default: 100ms).
    /// </summary>
    /// <value>A <see cref="TimeSpan"/> representing the coalescing window duration.</value>
    /// <remarks>
    /// Requests arriving within this window of each other are batched together
    /// to reduce API call pressure. A smaller window reduces latency but
    /// provides less coalescing benefit. A larger window increases latency
    /// but batches more requests together.
    /// </remarks>
    public TimeSpan CoalescingWindow { get; init; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Context cache duration (default: 30s).
    /// </summary>
    /// <value>A <see cref="TimeSpan"/> representing how long cached context remains valid.</value>
    /// <remarks>
    /// Cached context entries are evicted after this duration. A shorter duration
    /// ensures freshness but may reduce cache hit ratios. A longer duration
    /// improves hit ratios but may serve stale context.
    /// </remarks>
    public TimeSpan ContextCacheDuration { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum compiled template cache size (default: 100).
    /// </summary>
    /// <value>A positive integer representing the maximum number of cached compiled templates.</value>
    /// <remarks>
    /// Controls the upper bound of the in-memory template cache. When the limit
    /// is reached, the least recently used entries are evicted.
    /// </remarks>
    public int MaxCompiledTemplates { get; init; } = 100;
}
