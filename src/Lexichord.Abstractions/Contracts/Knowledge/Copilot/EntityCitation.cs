// =============================================================================
// File: EntityCitation.cs
// Project: Lexichord.Abstractions
// Description: A single entity citation in a Co-pilot response.
// =============================================================================
// LOGIC: Represents one cited entity with its identity, display information,
//   confidence score, verification status, and type-specific icon. Produced
//   by IEntityCitationRenderer.GenerateCitations and rendered in the
//   citation panel UI.
//
// v0.6.6h: Entity Citation Renderer (CKVS Phase 3b)
// Dependencies: None (pure data record)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// A single entity citation in a Co-pilot response.
/// </summary>
/// <remarks>
/// <para>
/// Each <see cref="EntityCitation"/> represents one knowledge graph entity
/// that informed the Co-pilot's generated content. Citations are collected
/// into a <see cref="CitationMarkup"/> by the
/// <see cref="IEntityCitationRenderer"/>.
/// </para>
/// <para>
/// <b>Verification:</b> The <see cref="IsVerified"/> flag indicates whether
/// the entity was validated against the post-generation validation findings.
/// Verified entities show a ✓ mark; unverified entities show ?.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6h as part of the Entity Citation Renderer.
/// </para>
/// </remarks>
public record EntityCitation
{
    /// <summary>
    /// Unique identifier of the cited entity.
    /// </summary>
    /// <value>The GUID of the <see cref="KnowledgeEntity"/> in the graph.</value>
    public Guid EntityId { get; init; }

    /// <summary>
    /// Entity type name (e.g., "Endpoint", "Parameter", "Schema").
    /// </summary>
    /// <value>The type name from <see cref="KnowledgeEntity.Type"/>.</value>
    public required string EntityType { get; init; }

    /// <summary>
    /// Entity name (e.g., "GET /api/users").
    /// </summary>
    /// <value>The name from <see cref="KnowledgeEntity.Name"/>.</value>
    public required string EntityName { get; init; }

    /// <summary>
    /// Formatted display label for the citation.
    /// </summary>
    /// <value>
    /// A human-readable label, potentially enriched with type-specific
    /// context (e.g., "GET /api/users" for endpoints, "userId (query)" for parameters).
    /// </value>
    public required string DisplayLabel { get; init; }

    /// <summary>
    /// Citation confidence score (0.0–1.0).
    /// </summary>
    /// <value>
    /// Confidence that this entity was actually used in generation.
    /// Entities from verified context default to 1.0.
    /// </value>
    public float Confidence { get; init; }

    /// <summary>
    /// Whether the entity was verified against post-generation validation.
    /// </summary>
    /// <value>
    /// <c>true</c> if no error-level validation findings reference this entity;
    /// <c>false</c> if the entity has associated errors.
    /// </value>
    public bool IsVerified { get; init; }

    /// <summary>
    /// Icon for the entity type (e.g., 🔗 for Endpoint, 📝 for Parameter).
    /// </summary>
    /// <value>
    /// A Unicode emoji string representing the entity type, or <c>null</c>
    /// if no specific icon is available.
    /// </value>
    public string? TypeIcon { get; init; }
}
