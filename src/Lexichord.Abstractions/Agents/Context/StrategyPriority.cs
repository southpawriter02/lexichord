// -----------------------------------------------------------------------
// <copyright file="StrategyPriority.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Context;

/// <summary>
/// Standard priority levels for context strategies.
/// Provides semantic meaning to priority values for context strategy execution and trimming.
/// </summary>
/// <remarks>
/// <para>
/// Priority values control two behaviors:
/// </para>
/// <list type="bullet">
///   <item><description><b>Execution Order:</b> Higher priority strategies execute first (when sorted descending)</description></item>
///   <item><description><b>Budget Trimming:</b> Lower priority fragments are removed first when over budget</description></item>
/// </list>
/// <para>
/// <strong>Usage Pattern:</strong>
/// Strategies should assign priority based on how critical their context is to the agent's task.
/// For example, the document content strategy uses <see cref="Critical"/> (100) because the
/// current document is almost always essential context for editing or analysis tasks.
/// In contrast, style rules might use <see cref="Optional"/> (20) since they're helpful but
/// not essential for basic functionality.
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2a as part of the Context Strategy Interface.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class DocumentContextStrategy : IContextStrategy
/// {
///     // Critical priority - document content is essential
///     public int Priority => StrategyPriority.Critical;
/// }
///
/// public class StyleContextStrategy : IContextStrategy
/// {
///     // Optional priority - style rules are helpful but not essential
///     public int Priority => StrategyPriority.Optional;
/// }
/// </code>
/// </example>
public static class StrategyPriority
{
    /// <summary>
    /// Critical priority (100) - Always needed, never trimmed first.
    /// </summary>
    /// <value>100</value>
    /// <remarks>
    /// Use for context that is essential for the agent to perform its task.
    /// Fragments with this priority are only removed as a last resort when
    /// the budget is severely exceeded.
    /// </remarks>
    /// <example>
    /// Examples: Document content for edit requests, selected text for refactoring.
    /// </example>
    public const int Critical = 100;

    /// <summary>
    /// High priority (80) - Usually needed for good results.
    /// </summary>
    /// <value>80</value>
    /// <remarks>
    /// Use for context that significantly improves result quality but isn't
    /// absolutely required for basic functionality.
    /// </remarks>
    /// <example>
    /// Examples: Selected text for context-aware suggestions, cursor context.
    /// </example>
    public const int High = 80;

    /// <summary>
    /// Medium priority (60) - Helpful but not essential.
    /// </summary>
    /// <value>60</value>
    /// <remarks>
    /// Use for context that enhances results but can be omitted under
    /// budget constraints without losing core functionality.
    /// </remarks>
    /// <example>
    /// Examples: RAG search results for background knowledge, heading hierarchy.
    /// </example>
    public const int Medium = 60;

    /// <summary>
    /// Low priority (40) - Nice to have.
    /// </summary>
    /// <value>40</value>
    /// <remarks>
    /// Use for supplementary context that provides marginal value.
    /// This context is typically among the first to be trimmed.
    /// </remarks>
    /// <example>
    /// Examples: Document structure metadata, related file references.
    /// </example>
    public const int Low = 40;

    /// <summary>
    /// Optional priority (20) - Include only if budget allows.
    /// </summary>
    /// <value>20</value>
    /// <remarks>
    /// Use for context that is speculative or tangentially related.
    /// This context is trimmed first when approaching budget limits.
    /// </remarks>
    /// <example>
    /// Examples: Style rules, formatting preferences, historical context.
    /// </example>
    public const int Optional = 20;
}
