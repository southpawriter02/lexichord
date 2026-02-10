// =============================================================================
// File: ValidatedGenerationResult.cs
// Project: Lexichord.Abstractions
// Description: Aggregated result of validated LLM content generation.
// =============================================================================
// LOGIC: Bundles the generated content with its source entities and post-
//   validation results. This is the primary input to the Entity Citation
//   Renderer, providing all the data needed to generate citation markup.
//
// v0.6.6h: Entity Citation Renderer (CKVS Phase 3b)
// Dependencies: KnowledgeEntity (v0.4.5e), PostValidationResult (v0.6.6g)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Aggregated result of a validated LLM content generation.
/// </summary>
/// <remarks>
/// <para>
/// Bundles the generated content string with the knowledge graph entities
/// that were used as context and the post-validation results. Serves as
/// the primary input to <see cref="IEntityCitationRenderer.GenerateCitations"/>
/// and <see cref="IEntityCitationRenderer.GetCitationDetail"/>.
/// </para>
/// <para>
/// <b>Lifecycle:</b> Created after the LLM generates a response and the
/// <see cref="IPostGenerationValidator"/> validates it. Passed to the
/// Entity Citation Renderer to produce user-facing citation markup.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6h as part of the Entity Citation Renderer.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = new ValidatedGenerationResult
/// {
///     Content = "The GET /api/users endpoint returns a list of users.",
///     SourceEntities = contextEntities,
///     PostValidation = validationResult
/// };
/// var citations = renderer.GenerateCitations(result, new CitationOptions());
/// </code>
/// </example>
public record ValidatedGenerationResult
{
    /// <summary>
    /// The LLM-generated content string.
    /// </summary>
    /// <value>
    /// The full text of the generated response. Used by
    /// <see cref="IEntityCitationRenderer.GetCitationDetail"/> to detect
    /// which entity properties were referenced in the output.
    /// </value>
    public required string Content { get; init; }

    /// <summary>
    /// Knowledge graph entities that were provided as context for generation.
    /// </summary>
    /// <value>
    /// The entities from the <see cref="KnowledgeContext"/> that were used
    /// during LLM prompt injection. These are the candidate citation sources.
    /// </value>
    public required IReadOnlyList<KnowledgeEntity> SourceEntities { get; init; }

    /// <summary>
    /// Post-generation validation result.
    /// </summary>
    /// <value>
    /// The <see cref="PostValidationResult"/> from the
    /// <see cref="IPostGenerationValidator"/>. Contains validation findings,
    /// hallucination detections, and an overall validation status used to
    /// determine citation verification state and validation icons.
    /// </value>
    public required PostValidationResult PostValidation { get; init; }
}
