// =============================================================================
// File: ClaimEntity.cs
// Project: Lexichord.Abstractions
// Description: Entity reference within a claim (subject or object).
// =============================================================================
// LOGIC: Represents an entity mention in a claim that may or may not be
//   resolved to a knowledge graph entity. Captures surface form, normalized
//   form, span positions, and linking confidence for entity resolution.
//
// v0.5.6e: Claim Data Model (CKVS Phase 2b)
// Dependencies: KnowledgeEntity (v0.4.5e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims;

/// <summary>
/// An entity reference within a claim (subject or object position).
/// </summary>
/// <remarks>
/// <para>
/// Claims follow a subject-predicate-object structure where both subject
/// and object may reference entities. This record captures an entity mention
/// that may be:
/// </para>
/// <list type="bullet">
///   <item><b>Unresolved:</b> The surface form was extracted but not yet
///     linked to a knowledge graph entity.</item>
///   <item><b>Resolved:</b> The entity has been linked to a
///     <see cref="KnowledgeEntity"/> in the graph.</item>
/// </list>
/// <para>
/// <b>Entity Linking Flow:</b>
/// <list type="number">
///   <item>Extraction pipeline identifies entity mention (surface form).</item>
///   <item>Normalizer creates canonical form.</item>
///   <item>Linker attempts to match to existing graph entity.</item>
///   <item>If matched, <see cref="EntityId"/> and <see cref="ResolvedEntity"/> are populated.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6e as part of the Claim Extraction pipeline.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Unresolved entity (not yet linked)
/// var unresolved = ClaimEntity.Unresolved(
///     surfaceForm: "/api/users",
///     entityType: "Endpoint",
///     startOffset: 4,
///     endOffset: 14);
///
/// // Resolved entity (linked to KnowledgeEntity)
/// var resolved = ClaimEntity.Resolved(
///     surfaceForm: "/api/users",
///     entity: knowledgeEntity,
///     confidence: 0.95f,
///     startOffset: 4,
///     endOffset: 14);
/// </code>
/// </example>
public record ClaimEntity
{
    /// <summary>
    /// ID of the linked knowledge graph entity.
    /// </summary>
    /// <value>
    /// The GUID of the <see cref="KnowledgeEntity"/> this mention links to,
    /// or null if unresolved.
    /// </value>
    /// <remarks>
    /// LOGIC: Populated by the entity linker (v0.5.5-KG) after successful
    /// resolution. Null for unresolved mentions.
    /// </remarks>
    public Guid? EntityId { get; init; }

    /// <summary>
    /// Expected entity type (label) for resolution.
    /// </summary>
    /// <value>
    /// The entity type name (e.g., "Endpoint", "Parameter", "Concept").
    /// Used to constrain entity linking candidates.
    /// </value>
    /// <remarks>
    /// LOGIC: Determined by the extraction pattern or SRL role.
    /// Must match a registered type in the Schema Registry (v0.4.5f).
    /// </remarks>
    public required string EntityType { get; init; }

    /// <summary>
    /// The original text as it appears in the document.
    /// </summary>
    /// <value>The exact string extracted from the source text.</value>
    /// <remarks>
    /// LOGIC: Preserves the original wording for display and verification.
    /// May contain variations like "the endpoint" vs "endpoint".
    /// </remarks>
    public required string SurfaceForm { get; init; }

    /// <summary>
    /// Normalized/canonical form of the entity mention.
    /// </summary>
    /// <value>
    /// The standardized form after normalization (lowercasing, article
    /// removal, etc.). Defaults to the surface form if not normalized.
    /// </value>
    /// <remarks>
    /// LOGIC: Used for entity linking comparison. The normalizer strips
    /// determiners, converts to lowercase, and removes trailing punctuation.
    /// </remarks>
    public string NormalizedForm { get; init; } = string.Empty;

    /// <summary>
    /// The resolved knowledge graph entity.
    /// </summary>
    /// <value>
    /// The full <see cref="KnowledgeEntity"/> record if resolved, null otherwise.
    /// </value>
    /// <remarks>
    /// LOGIC: Populated when <see cref="EntityId"/> is set. Provides access
    /// to entity properties without a separate lookup.
    /// </remarks>
    public KnowledgeEntity? ResolvedEntity { get; init; }

    /// <summary>
    /// Whether this entity has been resolved to a graph entity.
    /// </summary>
    /// <value>True if <see cref="EntityId"/> is not null.</value>
    public bool IsResolved => EntityId.HasValue;

    /// <summary>
    /// Confidence in the entity linking (0.0-1.0).
    /// </summary>
    /// <value>
    /// A score from 0.0 (low confidence) to 1.0 (certain).
    /// Defaults to 0.0 for unresolved entities.
    /// </value>
    /// <remarks>
    /// LOGIC: Computed by the entity linker based on string similarity,
    /// context matching, and prior linking decisions.
    /// </remarks>
    public float LinkingConfidence { get; init; }

    /// <summary>
    /// Start character offset of the mention in the source sentence.
    /// </summary>
    /// <value>Zero-based index where the mention begins.</value>
    public int StartOffset { get; init; }

    /// <summary>
    /// End character offset of the mention in the source sentence (exclusive).
    /// </summary>
    /// <value>Zero-based index past the last character of the mention.</value>
    public int EndOffset { get; init; }

    /// <summary>
    /// Creates an unresolved entity mention.
    /// </summary>
    /// <param name="surfaceForm">The text as it appears in the document.</param>
    /// <param name="entityType">The expected entity type.</param>
    /// <param name="startOffset">Start character offset.</param>
    /// <param name="endOffset">End character offset (exclusive).</param>
    /// <param name="normalizedForm">
    /// Optional normalized form. Defaults to lowercase surface form.
    /// </param>
    /// <returns>A new <see cref="ClaimEntity"/> without resolution.</returns>
    /// <remarks>
    /// LOGIC: Factory method for extraction pipeline output before linking.
    /// </remarks>
    public static ClaimEntity Unresolved(
        string surfaceForm,
        string entityType,
        int startOffset,
        int endOffset,
        string? normalizedForm = null)
    {
        return new ClaimEntity
        {
            EntityType = entityType,
            SurfaceForm = surfaceForm,
            NormalizedForm = normalizedForm ?? surfaceForm.ToLowerInvariant(),
            StartOffset = startOffset,
            EndOffset = endOffset
        };
    }

    /// <summary>
    /// Creates a resolved entity mention linked to a knowledge graph entity.
    /// </summary>
    /// <param name="surfaceForm">The text as it appears in the document.</param>
    /// <param name="entity">The resolved knowledge graph entity.</param>
    /// <param name="confidence">Linking confidence (0.0-1.0).</param>
    /// <param name="startOffset">Start character offset.</param>
    /// <param name="endOffset">End character offset (exclusive).</param>
    /// <returns>A new <see cref="ClaimEntity"/> with resolution.</returns>
    /// <remarks>
    /// LOGIC: Factory method for linked entity mentions. Automatically
    /// populates <see cref="EntityId"/>, <see cref="EntityType"/>, and
    /// <see cref="NormalizedForm"/> from the resolved entity.
    /// </remarks>
    public static ClaimEntity Resolved(
        string surfaceForm,
        KnowledgeEntity entity,
        float confidence,
        int startOffset,
        int endOffset)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new ClaimEntity
        {
            EntityId = entity.Id,
            EntityType = entity.Type,
            SurfaceForm = surfaceForm,
            NormalizedForm = entity.Name.ToLowerInvariant(),
            ResolvedEntity = entity,
            LinkingConfidence = confidence,
            StartOffset = startOffset,
            EndOffset = endOffset
        };
    }
}
