// =============================================================================
// File: KnowledgePromptTemplate.cs
// Project: Lexichord.Abstractions
// Description: Defines a prompt template for knowledge-aware LLM interactions.
// =============================================================================
// LOGIC: Knowledge prompt templates pair system and user prompt templates
//   with metadata about what context they require. Templates use Mustache
//   {{variable}} syntax and are rendered by the existing IPromptRenderer.
//
// NOTE: Named KnowledgePromptTemplate (not PromptTemplate) to avoid collision
//   with Lexichord.Abstractions.Contracts.LLM.PromptTemplate (v0.6.3a).
//
// v0.6.6i: Knowledge-Aware Prompts (CKVS Phase 3b)
// Dependencies:
//   - PromptRequirements (v0.6.6i)
//   - PromptOptions (v0.6.6i)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// A prompt template definition for knowledge-aware LLM interactions.
/// </summary>
/// <remarks>
/// <para>
/// Knowledge prompt templates define the system and user prompt structures
/// with Mustache-style <c>{{variable}}</c> placeholders. The prompt builder
/// fills these placeholders with formatted knowledge context data (entities,
/// relationships, axioms) and user request details.
/// </para>
/// <para>
/// <b>Available template variables:</b>
/// <list type="table">
///   <listheader><term>Variable</term><description>Description</description></listheader>
///   <item><term><c>{{query}}</c></term><description>User's request text</description></item>
///   <item><term><c>{{entities}}</c></term><description>Formatted knowledge entities</description></item>
///   <item><term><c>{{relationships}}</c></term><description>Formatted entity relationships</description></item>
///   <item><term><c>{{axioms}}</c></term><description>Formatted domain axiom rules</description></item>
///   <item><term><c>{{groundingInstructions}}</c></term><description>Grounding level rules</description></item>
///   <item><term><c>{{additionalInstructions}}</c></term><description>Custom instructions</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Naming:</b> This type is named <c>KnowledgePromptTemplate</c> rather than
/// <c>PromptTemplate</c> to avoid collision with
/// <see cref="Lexichord.Abstractions.Contracts.LLM.PromptTemplate"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6i as part of Knowledge-Aware Prompts.
/// </para>
/// </remarks>
public record KnowledgePromptTemplate
{
    /// <summary>
    /// Unique identifier for the template.
    /// </summary>
    /// <value>Kebab-case string (e.g., "copilot-knowledge-aware").</value>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable display name for the template.
    /// </summary>
    /// <value>A user-friendly name (e.g., "Knowledge-Aware Co-pilot").</value>
    public required string Name { get; init; }

    /// <summary>
    /// Description of the template's purpose and intended use case.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Mustache template for the system prompt.
    /// </summary>
    /// <remarks>
    /// Contains <c>{{variable}}</c> placeholders that are filled with
    /// grounding instructions, axioms, and additional instructions.
    /// </remarks>
    public required string SystemTemplate { get; init; }

    /// <summary>
    /// Mustache template for the user prompt.
    /// </summary>
    /// <remarks>
    /// Contains <c>{{variable}}</c> placeholders that are filled with
    /// entities, relationships, and the user's query.
    /// </remarks>
    public required string UserTemplate { get; init; }

    /// <summary>
    /// Default options for this template.
    /// </summary>
    /// <value>When <c>null</c>, the <see cref="PromptOptions"/> defaults are used.</value>
    public PromptOptions? DefaultOptions { get; init; }

    /// <summary>
    /// Required context elements for this template.
    /// </summary>
    /// <value>Defaults to a new <see cref="PromptRequirements"/> with entities required.</value>
    public PromptRequirements Requirements { get; init; } = new();
}
