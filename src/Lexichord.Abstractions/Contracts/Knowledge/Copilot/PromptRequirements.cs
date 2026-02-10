// =============================================================================
// File: PromptRequirements.cs
// Project: Lexichord.Abstractions
// Description: Defines the context requirements for a knowledge prompt template.
// =============================================================================
// LOGIC: Each knowledge prompt template declares what context elements it
//   needs (entities, relationships, axioms, claims) and any minimum counts.
//   The prompt builder validates these requirements before rendering.
//
// v0.6.6i: Knowledge-Aware Prompts (CKVS Phase 3b)
// Dependencies: None (pure abstraction)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Specifies the required context elements for a knowledge prompt template.
/// </summary>
/// <remarks>
/// <para>
/// Templates declare their requirements so the prompt builder can validate
/// that sufficient context is available before attempting to render. This
/// prevents generating prompts with missing or incomplete knowledge data.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6i as part of Knowledge-Aware Prompts.
/// </para>
/// </remarks>
public record PromptRequirements
{
    /// <summary>
    /// Whether the template requires knowledge entities in the context.
    /// </summary>
    /// <value>Defaults to <c>true</c>. Most templates need at least some entities.</value>
    public bool RequiresEntities { get; init; } = true;

    /// <summary>
    /// Whether the template requires relationship data between entities.
    /// </summary>
    /// <value>Defaults to <c>false</c>. Only documentation-style templates typically need this.</value>
    public bool RequiresRelationships { get; init; } = false;

    /// <summary>
    /// Whether the template requires axiom rules in the system prompt.
    /// </summary>
    /// <value>Defaults to <c>false</c>. Knowledge-aware and strict templates use axioms.</value>
    public bool RequiresAxioms { get; init; } = false;

    /// <summary>
    /// Whether the template requires claim data from the knowledge graph.
    /// </summary>
    /// <value>Defaults to <c>false</c>. Reserved for future claim-aware templates.</value>
    public bool RequiresClaims { get; init; } = false;

    /// <summary>
    /// Minimum number of entities required for the template to render.
    /// </summary>
    /// <value>Defaults to <c>0</c> (no minimum). Set to 1 or higher for strict templates.</value>
    public int MinEntities { get; init; } = 0;
}
