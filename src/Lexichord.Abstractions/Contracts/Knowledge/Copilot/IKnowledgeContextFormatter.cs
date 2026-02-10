// =============================================================================
// File: IKnowledgeContextFormatter.cs
// Project: Lexichord.Abstractions
// Description: Interface for formatting knowledge context for prompt injection.
// =============================================================================
// LOGIC: Defines the contract for transforming knowledge graph data (entities,
//   relationships, axioms) into formatted strings suitable for LLM prompt
//   injection. Supports multiple output formats (Markdown, YAML, JSON, Plain).
//   Named IKnowledgeContextFormatter to avoid collision with the existing
//   IContextFormatter in Lexichord.Modules.Agents.Templates.Formatters.
//
// v0.6.6e: Graph Context Provider (CKVS Phase 3b)
// Dependencies: KnowledgeEntity, KnowledgeRelationship (v0.4.5e),
//               Axiom (v0.4.6e), ContextFormatOptions (v0.6.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Formats knowledge context for prompt injection.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IKnowledgeContextFormatter"/> transforms structured knowledge
/// graph data into formatted strings for inclusion in LLM prompts. It separates
/// formatting concerns from data retrieval (handled by
/// <see cref="IKnowledgeContextProvider"/>).
/// </para>
/// <para>
/// <b>Supported Formats:</b>
/// <list type="bullet">
///   <item><see cref="ContextFormat.Markdown"/>: Headers and bullet lists.</item>
///   <item><see cref="ContextFormat.Yaml"/>: Structured YAML.</item>
///   <item><see cref="ContextFormat.Json"/>: Indented JSON.</item>
///   <item><see cref="ContextFormat.Plain"/>: Plain text.</item>
/// </list>
/// </para>
/// <para>
/// <b>Naming Note:</b> Named <c>IKnowledgeContextFormatter</c> (not
/// <c>IContextFormatter</c>) to avoid collision with the existing
/// <c>IContextFormatter</c> in <c>Lexichord.Modules.Agents</c> which
/// formats style rules and RAG chunks.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6e as part of the Graph Context Provider.
/// </para>
/// </remarks>
public interface IKnowledgeContextFormatter
{
    /// <summary>
    /// Formats entities, relationships, and axioms as text.
    /// </summary>
    /// <param name="entities">Entities to format.</param>
    /// <param name="relationships">Optional relationships to format.</param>
    /// <param name="axioms">Optional axioms (domain rules) to format.</param>
    /// <param name="options">Formatting options.</param>
    /// <returns>
    /// A formatted string containing the knowledge context. Empty string
    /// if <paramref name="entities"/> is empty.
    /// </returns>
    /// <remarks>
    /// LOGIC: Delegates to format-specific methods based on
    /// <see cref="ContextFormatOptions.Format"/>. Each entity is formatted
    /// with its type, name, and up to <see cref="ContextFormatOptions.MaxPropertiesPerEntity"/>
    /// properties.
    /// </remarks>
    string FormatContext(
        IReadOnlyList<KnowledgeEntity> entities,
        IReadOnlyList<KnowledgeRelationship>? relationships,
        IReadOnlyList<Axiom>? axioms,
        ContextFormatOptions options);

    /// <summary>
    /// Estimates token count for a formatted context string.
    /// </summary>
    /// <param name="formattedContext">The formatted context to estimate.</param>
    /// <returns>
    /// Estimated token count using GPT-style approximation (~4 characters
    /// per token).
    /// </returns>
    /// <remarks>
    /// LOGIC: Uses the simple heuristic of dividing character count by 4.
    /// This matches the estimation approach used industry-wide for
    /// quick token budget calculations.
    /// </remarks>
    int EstimateTokens(string formattedContext);
}
