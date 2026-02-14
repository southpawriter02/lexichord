// =============================================================================
// File: FormattedContext.cs
// Project: Lexichord.Abstractions
// Description: Formatted knowledge context with metadata for token budgeting.
// =============================================================================
// LOGIC: Wraps the raw formatted string from IKnowledgeContextFormatter with
//   metadata about the content: token count, format used, entity/relationship/
//   axiom counts, and truncation status. Enables callers to make informed
//   decisions about context inclusion and budget allocation without parsing
//   the formatted string.
//
// v0.7.2g: Knowledge Context Formatter (CKVS Phase 4a)
// Dependencies: ContextFormat (v0.6.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Formatted knowledge context with metadata for token budget management.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="FormattedContext"/> record wraps the raw formatted string
/// produced by <see cref="IKnowledgeContextFormatter"/> with metadata about
/// the content. This enables callers to make informed decisions about context
/// inclusion and budget allocation without parsing the formatted string.
/// </para>
/// <para>
/// <b>Metadata Fields:</b>
/// <list type="bullet">
///   <item><see cref="TokenCount"/>: Estimated token count for budget management.</item>
///   <item><see cref="Format"/>: The output format used to produce the content.</item>
///   <item><see cref="EntityCount"/>, <see cref="RelationshipCount"/>, <see cref="AxiomCount"/>: Counts of included items.</item>
///   <item><see cref="WasTruncated"/>: Whether content was trimmed to fit a token budget.</item>
/// </list>
/// </para>
/// <para>
/// <b>Immutability:</b> This record is immutable. Use <c>with</c>-expressions
/// to create modified copies (e.g., after truncation).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.2g as part of the Knowledge Context Formatter enhancement.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var context = formatter.FormatWithMetadata(entities, relationships, axioms, options);
/// if (context.TokenCount > budget)
/// {
///     context = formatter.TruncateToTokenBudget(context, budget);
/// }
/// Console.WriteLine($"Using {context.TokenCount} tokens, truncated={context.WasTruncated}");
/// </code>
/// </example>
public sealed record FormattedContext
{
    /// <summary>
    /// The formatted content string ready for prompt injection.
    /// </summary>
    /// <remarks>
    /// LOGIC: Contains the fully formatted knowledge context in the format
    /// specified by <see cref="Format"/>. May be empty if no entities were
    /// provided for formatting.
    /// </remarks>
    public required string Content { get; init; }

    /// <summary>
    /// Estimated token count of <see cref="Content"/>.
    /// </summary>
    /// <remarks>
    /// LOGIC: When <c>ITokenCounter</c> is available, uses accurate
    /// tokenizer-based counting. Otherwise falls back to the ~4 characters
    /// per token heuristic.
    /// </remarks>
    public int TokenCount { get; init; }

    /// <summary>
    /// The output format used to produce <see cref="Content"/>.
    /// </summary>
    /// <remarks>
    /// LOGIC: Matches the <see cref="ContextFormatOptions.Format"/> value
    /// passed to the formatter. Defaults to <see cref="ContextFormat.Markdown"/>.
    /// </remarks>
    public ContextFormat Format { get; init; }

    /// <summary>
    /// Number of entities included in the formatted output.
    /// </summary>
    public int EntityCount { get; init; }

    /// <summary>
    /// Number of relationships included in the formatted output.
    /// </summary>
    public int RelationshipCount { get; init; }

    /// <summary>
    /// Number of axioms (domain rules) included in the formatted output.
    /// </summary>
    public int AxiomCount { get; init; }

    /// <summary>
    /// Whether the content was truncated to fit within a token budget.
    /// </summary>
    /// <remarks>
    /// LOGIC: Set to <c>true</c> by <see cref="IKnowledgeContextFormatter.TruncateToTokenBudget"/>
    /// when the content exceeds the specified maximum tokens. When <c>true</c>,
    /// the content ends with a "[Context truncated due to token limit]" marker.
    /// </remarks>
    public bool WasTruncated { get; init; }

    /// <summary>
    /// Creates an empty formatted context with no content.
    /// </summary>
    /// <remarks>
    /// LOGIC: Returns a context with empty content, zero counts, and Markdown format.
    /// Used when no entities are available for formatting.
    /// </remarks>
    public static FormattedContext Empty => new()
    {
        Content = string.Empty,
        TokenCount = 0,
        Format = ContextFormat.Markdown
    };
}
