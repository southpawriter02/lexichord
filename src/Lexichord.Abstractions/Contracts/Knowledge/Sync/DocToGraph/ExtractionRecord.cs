// =============================================================================
// File: ExtractionRecord.cs
// Project: Lexichord.Abstractions
// Description: Lineage record linking an extraction to its source document version.
// =============================================================================
// LOGIC: Tracks extraction history for rollback capability. Each extraction
//   records the document version, timestamp, extracted artifacts (entities,
//   claims, relationships), and a content hash for change detection.
//
// v0.7.6f: Doc-to-Graph Sync (CKVS Phase 4c)
// Dependencies: None
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph;

/// <summary>
/// Record of a document extraction for lineage tracking and rollback support.
/// </summary>
/// <remarks>
/// <para>
/// Captures the state of an extraction operation to enable:
/// </para>
/// <list type="bullet">
///   <item><b>Provenance:</b> Track which document version produced which entities.</item>
///   <item><b>Rollback:</b> Restore the graph to a previous extraction state.</item>
///   <item><b>Change Detection:</b> Identify if re-extraction is needed via hash comparison.</item>
///   <item><b>Audit:</b> Record who initiated the extraction and when.</item>
/// </list>
/// <para>
/// <b>Storage:</b> Persisted in the document repository's extraction lineage table.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6f as part of the Doc-to-Graph Sync module.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var record = new ExtractionRecord
/// {
///     ExtractionId = Guid.NewGuid(),
///     DocumentId = document.Id,
///     DocumentHash = document.Hash,
///     ExtractedAt = DateTimeOffset.UtcNow,
///     ExtractedBy = currentUser.Id,
///     EntityIds = upsertedEntities.Select(e => e.Id).ToList(),
///     ClaimIds = extractedClaims.Select(c => c.Id).ToList(),
///     ExtractionHash = ComputeHash(extractionResult)
/// };
/// </code>
/// </example>
public record ExtractionRecord
{
    /// <summary>
    /// Unique identifier for this extraction.
    /// </summary>
    /// <value>A globally unique identifier for the extraction operation.</value>
    /// <remarks>
    /// LOGIC: Generated once per extraction. Used to identify the specific
    /// extraction for rollback and lineage queries.
    /// </remarks>
    public required Guid ExtractionId { get; init; }

    /// <summary>
    /// ID of the document that was extracted.
    /// </summary>
    /// <value>The unique identifier of the source document.</value>
    /// <remarks>
    /// LOGIC: Links the extraction back to its source document. Used for
    /// lineage queries and rollback target identification.
    /// </remarks>
    public required Guid DocumentId { get; init; }

    /// <summary>
    /// Hash of the document content at extraction time.
    /// </summary>
    /// <value>The content hash of the document when extracted.</value>
    /// <remarks>
    /// LOGIC: Captures the document version via its content hash (from
    /// <see cref="Contracts.RAG.Document.Hash"/>). Used for change detection
    /// to determine if re-extraction is needed.
    /// </remarks>
    public required string DocumentHash { get; init; }

    /// <summary>
    /// Timestamp when the extraction was performed.
    /// </summary>
    /// <value>UTC timestamp of the extraction operation.</value>
    /// <remarks>
    /// LOGIC: Used for chronological ordering of extractions and
    /// determining the target version for rollback operations.
    /// </remarks>
    public required DateTimeOffset ExtractedAt { get; init; }

    /// <summary>
    /// User who initiated the extraction.
    /// </summary>
    /// <value>The user ID if initiated by a user, null if automated.</value>
    /// <remarks>
    /// LOGIC: Records who triggered the sync for audit purposes. Null
    /// indicates automatic background sync or system-initiated extraction.
    /// </remarks>
    public Guid? ExtractedBy { get; init; }

    /// <summary>
    /// IDs of entities created or updated by this extraction.
    /// </summary>
    /// <value>List of entity GUIDs produced by the extraction.</value>
    /// <remarks>
    /// LOGIC: Enables rollback by identifying which entities to remove
    /// or revert. Also useful for impact analysis.
    /// </remarks>
    public IReadOnlyList<Guid> EntityIds { get; init; } = [];

    /// <summary>
    /// IDs of claims extracted in this operation.
    /// </summary>
    /// <value>List of claim GUIDs produced by the extraction.</value>
    /// <remarks>
    /// LOGIC: Enables rollback by identifying which claims to remove.
    /// Claims are linked to their source extraction for provenance.
    /// </remarks>
    public IReadOnlyList<Guid> ClaimIds { get; init; } = [];

    /// <summary>
    /// IDs of relationships created by this extraction.
    /// </summary>
    /// <value>List of relationship GUIDs produced by the extraction.</value>
    /// <remarks>
    /// LOGIC: Enables rollback by identifying which relationships to remove.
    /// Relationships are directional edges between entities.
    /// </remarks>
    public IReadOnlyList<Guid> RelationshipIds { get; init; } = [];

    /// <summary>
    /// Hash of the extraction output for change detection.
    /// </summary>
    /// <value>A hash computed from the extracted entities and claims.</value>
    /// <remarks>
    /// LOGIC: Computed from the extraction result (entity names, types, etc.).
    /// Comparing extraction hashes across versions identifies semantic changes
    /// even when document formatting changes.
    /// </remarks>
    public string ExtractionHash { get; init; } = string.Empty;

    /// <summary>
    /// Validation errors that occurred during extraction.
    /// </summary>
    /// <value>Error messages from validation failures.</value>
    /// <remarks>
    /// LOGIC: Preserved for debugging and audit. Even successful extractions
    /// may have warnings recorded here. Stored as strings for simplicity.
    /// </remarks>
    public IReadOnlyList<string> ValidationErrors { get; init; } = [];
}
