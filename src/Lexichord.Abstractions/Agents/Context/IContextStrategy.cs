// -----------------------------------------------------------------------
// <copyright file="IContextStrategy.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Context;

/// <summary>
/// Defines a pluggable strategy for gathering contextual information
/// to provide to AI agents during request processing.
/// </summary>
/// <remarks>
/// <para>
/// Each strategy focuses on a specific type of context source:
/// </para>
/// <list type="bullet">
///   <item><description>Document content</description></item>
///   <item><description>User selection</description></item>
///   <item><description>Cursor position and surrounding context</description></item>
///   <item><description>Heading hierarchy and document structure</description></item>
///   <item><description>RAG (semantic search) results</description></item>
///   <item><description>Style rules and formatting preferences</description></item>
/// </list>
/// <para>
/// <strong>Execution Model:</strong>
/// Strategies are executed in parallel by the context orchestrator (v0.7.2c)
/// and their results are combined based on priority and token budget. Each
/// strategy operates independently and should not depend on the presence
/// or results of other strategies.
/// </para>
/// <para>
/// <strong>Implementation Guidelines:</strong>
/// </para>
/// <list type="bullet">
///   <item><description>Return <c>null</c> if no relevant context is available (normal case)</description></item>
///   <item><description>Throw exceptions for actual errors (file access denied, service unavailable)</description></item>
///   <item><description>Respect <see cref="MaxTokens"/> to prevent context overflow</description></item>
///   <item><description>Use <see cref="Priority"/> to indicate importance (higher = more important)</description></item>
///   <item><description>Set <see cref="ContextFragment.Relevance"/> to help with trimming decisions</description></item>
///   <item><description>Handle cancellation via <see cref="CancellationToken"/> for timeout scenarios</description></item>
///   <item><description>Be stateless - all request-specific data comes from <see cref="ContextGatheringRequest"/></description></item>
/// </list>
/// <para>
/// <strong>Error Handling Philosophy:</strong>
/// Strategies should distinguish between "no context available" (return null)
/// and "error occurred" (throw exception). Null returns are silent and expected;
/// exceptions indicate problems that should be logged and potentially reported.
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2a as part of the Context Strategy Interface.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class DocumentContextStrategy : ContextStrategyBase
/// {
///     public override string StrategyId => "document";
///     public override string DisplayName => "Document Content";
///     public override int Priority => StrategyPriority.Critical;
///     public override int MaxTokens => 4000;
///
///     public override async Task&lt;ContextFragment?&gt; GatherAsync(
///         ContextGatheringRequest request,
///         CancellationToken ct)
///     {
///         // Return null if prerequisites not met
///         if (!request.HasDocument) return null;
///
///         // Gather context with cancellation support
///         var content = await ReadDocumentAsync(request.DocumentPath!, ct);
///
///         // Return null if no meaningful content
///         if (string.IsNullOrWhiteSpace(content)) return null;
///
///         // Use base class helper to create fragment with token estimation
///         return CreateFragment(content, relevance: 1.0f);
///     }
/// }
/// </code>
/// </example>
public interface IContextStrategy
{
    /// <summary>
    /// Gets the unique identifier for this strategy.
    /// Used for configuration, logging, and deduplication.
    /// </summary>
    /// <value>
    /// A unique string identifier in lowercase-kebab-case format.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Naming Convention:</strong>
    /// Strategy IDs should be lowercase kebab-case (e.g., "document", "cursor-context", "rag-search").
    /// This ensures consistent naming across the system and avoids case-sensitivity issues
    /// in configuration files.
    /// </para>
    /// <para>
    /// <strong>Uniqueness:</strong>
    /// IDs must be unique within the application. The factory will refuse to register
    /// strategies with duplicate IDs.
    /// </para>
    /// </remarks>
    /// <example>
    /// Examples: "document", "selection", "rag", "style", "heading", "cursor"
    /// </example>
    string StrategyId { get; }

    /// <summary>
    /// Gets the human-readable display name for UI presentation.
    /// Shown in the Context Preview panel and debugging interfaces.
    /// </summary>
    /// <value>
    /// A concise, user-friendly name for this strategy.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Naming Guidelines:</strong>
    /// Display names should be clear and descriptive without being verbose.
    /// Use title case and keep to 2-3 words when possible.
    /// </para>
    /// </remarks>
    /// <example>
    /// Examples: "Document Content", "Selected Text", "Related Documentation", "Cursor Context"
    /// </example>
    string DisplayName { get; }

    /// <summary>
    /// Gets the execution priority for this strategy.
    /// Higher values indicate higher priority for both execution order and budget retention.
    /// </summary>
    /// <value>
    /// An integer priority value. See <see cref="StrategyPriority"/> for standard ranges.
    /// </value>
    /// <remarks>
    /// <para>
    /// Priority controls two behaviors:
    /// </para>
    /// <list type="number">
    ///   <item><description><b>Execution Order:</b> Higher priority strategies execute first (when sorted descending). This can be useful for dependencies or ensuring critical context is gathered early.</description></item>
    ///   <item><description><b>Budget Trimming:</b> Lower priority fragments are removed first when the total context exceeds <see cref="ContextBudget.MaxTokens"/>. This ensures essential context is preserved.</description></item>
    /// </list>
    /// <para>
    /// <strong>Standard Priority Levels</strong> (from <see cref="StrategyPriority"/>):
    /// </para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Value</term>
    ///     <description>Usage</description>
    ///   </listheader>
    ///   <item>
    ///     <term>100 (Critical)</term>
    ///     <description>Always needed, never trimmed first. Use for context that is absolutely essential for the agent to function.</description>
    ///   </item>
    ///   <item>
    ///     <term>80 (High)</term>
    ///     <description>Usually needed for good results. Use for context that significantly improves output quality.</description>
    ///   </item>
    ///   <item>
    ///     <term>60 (Medium)</term>
    ///     <description>Helpful but not essential. Use for context that enhances results but isn't required.</description>
    ///   </item>
    ///   <item>
    ///     <term>40 (Low)</term>
    ///     <description>Nice to have. Use for supplementary context with marginal value.</description>
    ///   </item>
    ///   <item>
    ///     <term>20 (Optional)</term>
    ///     <description>Include only if budget allows. Use for speculative or tangentially related context.</description>
    ///   </item>
    /// </list>
    /// <para>
    /// <strong>Custom Priorities:</strong>
    /// While the standard constants are recommended, strategies can use custom
    /// values between 1 and 100 for fine-grained control.
    /// </para>
    /// </remarks>
    int Priority { get; }

    /// <summary>
    /// Gets the maximum tokens this strategy should contribute.
    /// Prevents any single strategy from consuming the entire budget.
    /// </summary>
    /// <value>
    /// A positive integer representing the token limit for this strategy's output.
    /// </value>
    /// <remarks>
    /// <para>
    /// LOGIC: The orchestrator will truncate fragments that exceed this limit,
    /// but strategies should ideally self-limit to avoid unnecessary work.
    /// Pre-truncating at the strategy level can improve performance by avoiding
    /// redundant token counting and content processing.
    /// </para>
    /// <para>
    /// <strong>Guidelines:</strong>
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Document/Selection: 3000-5000 tokens (full content)</description></item>
    ///   <item><description>RAG/Search: 1000-2000 tokens (summary results)</description></item>
    ///   <item><description>Cursor/Heading: 500-1000 tokens (focused context)</description></item>
    ///   <item><description>Style: 200-500 tokens (rule lists)</description></item>
    /// </list>
    /// </remarks>
    int MaxTokens { get; }

    /// <summary>
    /// Gathers relevant context based on the provided request.
    /// </summary>
    /// <param name="request">
    /// The context gathering request containing document path, cursor position,
    /// selection, agent ID, and optional hints.
    /// </param>
    /// <param name="ct">Cancellation token for timeout handling.</param>
    /// <returns>
    /// A <see cref="ContextFragment"/> containing the gathered context,
    /// or <c>null</c> if no relevant context is available for this request.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the cancellation token is triggered (e.g., timeout).
    /// The orchestrator handles this gracefully by skipping the strategy.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown when file access fails (e.g., document not found, permission denied).
    /// The orchestrator logs the error and continues with other strategies.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the strategy is in an invalid state or encounters an unexpected condition.
    /// </exception>
    /// <remarks>
    /// <para>
    /// LOGIC: Strategies should return <c>null</c> rather than an empty fragment
    /// when no context is available. This allows the orchestrator to skip
    /// unnecessary processing and logging. Returning null is a normal, expected
    /// outcome - not an error condition.
    /// </para>
    /// <para>
    /// <strong>Return Value Guidelines:</strong>
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Return <c>null</c>: No relevant context available (normal case)</description></item>
    ///   <item><description>Return empty fragment: Never (use null instead)</description></item>
    ///   <item><description>Throw exception: Actual errors (file not found, service unavailable)</description></item>
    /// </list>
    /// <para>
    /// <strong>Error Handling:</strong>
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Missing prerequisites (no document, no selection): Return <c>null</c></description></item>
    ///   <item><description>Empty results (no search hits, no headings): Return <c>null</c></description></item>
    ///   <item><description>Timeout/cancellation: Throw <see cref="OperationCanceledException"/></description></item>
    ///   <item><description>File access errors: Throw <see cref="IOException"/></description></item>
    ///   <item><description>Service failures: Throw appropriate exception with message</description></item>
    /// </list>
    /// <para>
    /// <strong>Performance:</strong>
    /// Strategies execute in parallel, so blocking operations should use async/await.
    /// Respect the cancellation token to allow the orchestrator to enforce timeouts.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public async Task&lt;ContextFragment?&gt; GatherAsync(
    ///     ContextGatheringRequest request,
    ///     CancellationToken ct)
    /// {
    ///     // Check prerequisites using base class helper
    ///     if (!ValidateRequest(request, requireDocument: true))
    ///         return null;
    ///
    ///     try
    ///     {
    ///         // Gather context with cancellation support
    ///         var content = await FetchContextAsync(request, ct);
    ///
    ///         // Return null if no meaningful content
    ///         if (string.IsNullOrWhiteSpace(content))
    ///             return null;
    ///
    ///         // Truncate to MaxTokens before returning
    ///         var truncated = TruncateToMaxTokens(content);
    ///
    ///         // Create fragment with metadata
    ///         return CreateFragment(
    ///             content: truncated,
    ///             relevance: CalculateRelevance(request, content));
    ///     }
    ///     catch (FileNotFoundException ex)
    ///     {
    ///         // Log and rethrow - orchestrator will handle
    ///         _logger.LogWarning(ex, "Document not found: {Path}", request.DocumentPath);
    ///         throw;
    ///     }
    /// }
    /// </code>
    /// </example>
    Task<ContextFragment?> GatherAsync(
        ContextGatheringRequest request,
        CancellationToken ct);
}
