// =============================================================================
// File: EntityCitationDetail.cs
// Project: Lexichord.Abstractions
// Description: Detailed information for a cited entity.
// =============================================================================
// LOGIC: Provides deep-dive information about a single cited entity:
//   the entity itself, which of its properties were used in generation,
//   which relationships were cited, which claims were derived from it,
//   and a link to view it in the graph browser.
//
// v0.6.6h: Entity Citation Renderer (CKVS Phase 3b)
// Dependencies: KnowledgeEntity (v0.4.5e), KnowledgeRelationship (v0.4.5e),
//               Claim (v0.5.6e)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Claims;

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Detailed information for a cited entity.
/// </summary>
/// <remarks>
/// <para>
/// Produced by <see cref="IEntityCitationRenderer.GetCitationDetail"/> when
/// the user hovers over or clicks on a citation. Provides transparency into
/// exactly how the entity informed the generated content.
/// </para>
/// <para>
/// <b>Properties Detection:</b> <see cref="UsedProperties"/> is populated by
/// checking which entity property values appear in the generated content
/// (case-insensitive substring matching).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6h as part of the Entity Citation Renderer.
/// </para>
/// </remarks>
public record EntityCitationDetail
{
    /// <summary>
    /// The cited entity.
    /// </summary>
    /// <value>The full <see cref="KnowledgeEntity"/> record from the graph.</value>
    public required KnowledgeEntity Entity { get; init; }

    /// <summary>
    /// Entity properties that were used in the generated content.
    /// </summary>
    /// <value>
    /// A subset of <see cref="KnowledgeEntity.Properties"/> whose values
    /// appear in the generated text. Empty if no properties were matched.
    /// </value>
    public IReadOnlyDictionary<string, object?> UsedProperties { get; init; } =
        new Dictionary<string, object?>();

    /// <summary>
    /// Relationships from the graph that were cited in context.
    /// </summary>
    /// <value>
    /// Knowledge graph relationships involving the cited entity.
    /// Empty if no relationships were provided in context.
    /// </value>
    public IReadOnlyList<KnowledgeRelationship> CitedRelationships { get; init; } = [];

    /// <summary>
    /// Claims derived from this entity during claim extraction.
    /// </summary>
    /// <value>
    /// Claims from the post-validation result whose subject entity matches
    /// this entity. Empty if no claims were extracted for this entity.
    /// </value>
    public IReadOnlyList<Claim> DerivedClaims { get; init; } = [];

    /// <summary>
    /// Deep link to view this entity in the graph browser.
    /// </summary>
    /// <value>
    /// A <c>lexichord://</c> protocol link (e.g., <c>lexichord://graph/entity/{id}</c>)
    /// for navigating to the entity in the Knowledge Graph browser, or
    /// <c>null</c> if the link is unavailable.
    /// </value>
    public string? BrowserLink { get; init; }
}
