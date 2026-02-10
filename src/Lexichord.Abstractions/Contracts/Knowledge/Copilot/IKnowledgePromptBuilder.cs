// =============================================================================
// File: IKnowledgePromptBuilder.cs
// Project: Lexichord.Abstractions
// Description: Interface for building prompts with knowledge graph context.
// =============================================================================
// LOGIC: Defines the contract for transforming a user request + knowledge
//   context into a fully rendered LLM prompt. Supports multiple templates
//   and custom template registration.
//
// v0.6.6i: Knowledge-Aware Prompts (CKVS Phase 3b)
// Dependencies:
//   - AgentRequest (v0.6.6a)
//   - KnowledgeContext (v0.6.6e)
//   - KnowledgePrompt, KnowledgePromptTemplate, PromptOptions (v0.6.6i)
// =============================================================================

using Lexichord.Abstractions.Agents;

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Builds LLM prompts with knowledge graph context injected.
/// </summary>
/// <remarks>
/// <para>
/// The Knowledge Prompt Builder is the bridge between the knowledge graph
/// and the LLM. It takes the user's request and relevant knowledge context,
/// selects an appropriate prompt template, and renders a complete prompt
/// with entities, axioms, and grounding instructions injected.
/// </para>
/// <para>
/// <b>Template Selection:</b> When <see cref="PromptOptions.TemplateId"/>
/// is not specified, the default <c>"copilot-knowledge-aware"</c> template
/// is used. Custom templates can be registered via
/// <see cref="RegisterTemplate"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6i as part of Knowledge-Aware Prompts.
/// </para>
/// </remarks>
/// <seealso cref="KnowledgePrompt"/>
/// <seealso cref="KnowledgePromptTemplate"/>
/// <seealso cref="PromptOptions"/>
public interface IKnowledgePromptBuilder
{
    /// <summary>
    /// Builds a knowledge-aware prompt from a user request and knowledge context.
    /// </summary>
    /// <param name="request">The user's agent request containing the query.</param>
    /// <param name="context">Knowledge graph context with entities, relationships, and axioms.</param>
    /// <param name="options">Options controlling template selection, formatting, and grounding.</param>
    /// <returns>A fully rendered <see cref="KnowledgePrompt"/> ready for LLM submission.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the specified template ID is not found in the registry.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/>, <paramref name="context"/>,
    /// or <paramref name="options"/> is <c>null</c>.
    /// </exception>
    KnowledgePrompt BuildPrompt(
        AgentRequest request,
        KnowledgeContext context,
        PromptOptions options);

    /// <summary>
    /// Gets the list of all registered prompt templates.
    /// </summary>
    /// <returns>A read-only list of available <see cref="KnowledgePromptTemplate"/> instances.</returns>
    IReadOnlyList<KnowledgePromptTemplate> GetTemplates();

    /// <summary>
    /// Registers a custom prompt template.
    /// </summary>
    /// <param name="template">The template to register. If a template with the same ID already exists, it is overwritten.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="template"/> is <c>null</c>.
    /// </exception>
    void RegisterTemplate(KnowledgePromptTemplate template);
}
