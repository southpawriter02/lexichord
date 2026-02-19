// =============================================================================
// File: GraphIngestionData.cs
// Project: Lexichord.Abstractions
// Description: Data structure prepared for knowledge graph ingestion.
// =============================================================================
// LOGIC: Contains transformed entities, relationships, and claims ready for
//   upsert to the knowledge graph, along with metadata about the source
//   document and transformation.
//
// v0.7.6f: Doc-to-Graph Sync (CKVS Phase 4c)
// Dependencies: KnowledgeEntity, KnowledgeRelationship, Claim
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Claims;

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph;

/// <summary>
/// Data structured and ready for ingestion into the knowledge graph.
/// </summary>
/// <remarks>
/// <para>
/// Output of the <see cref="IExtractionTransformer.TransformAsync"/> method.
/// Contains all artifacts prepared for graph upsert:
/// </para>
/// <list type="bullet">
///   <item><b>Entities:</b> Transformed knowledge entities ready for upsert.</item>
///   <item><b>Relationships:</b> Edges connecting entities.</item>
///   <item><b>Claims:</b> Subject-predicate-object assertions.</item>
///   <item><b>Metadata:</b> Additional context about the transformation.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6f as part of the Doc-to-Graph Sync module.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var ingestionData = await transformer.TransformAsync(extractionResult, document, ct);
///
/// // Upsert to graph
/// await graphRepository.UpsertEntitiesAsync(ingestionData.Entities, ct);
/// await graphRepository.CreateRelationshipsAsync(ingestionData.Relationships, ct);
/// await graphRepository.UpsertClaimsAsync(ingestionData.Claims, ct);
/// </code>
/// </example>
public record GraphIngestionData
{
    /// <summary>
    /// Knowledge entities to upsert to the graph.
    /// </summary>
    /// <value>A read-only list of transformed entities.</value>
    /// <remarks>
    /// LOGIC: These entities have been transformed from raw extraction
    /// results (EntityMention/AggregatedEntity) into KnowledgeEntity format
    /// suitable for Neo4j storage. Includes type normalization and property mapping.
    /// </remarks>
    public required IReadOnlyList<KnowledgeEntity> Entities { get; init; }

    /// <summary>
    /// Relationships to create or update in the graph.
    /// </summary>
    /// <value>
    /// A read-only list of relationships between entities. Empty if
    /// relationship creation was disabled.
    /// </value>
    /// <remarks>
    /// LOGIC: Derived from entity co-occurrence analysis and claim extraction.
    /// Each relationship connects two entities via FromEntityId/ToEntityId.
    /// </remarks>
    public IReadOnlyList<KnowledgeRelationship> Relationships { get; init; } = [];

    /// <summary>
    /// Claims extracted from the document.
    /// </summary>
    /// <value>
    /// A read-only list of claims to store. Empty if claim extraction
    /// was disabled or no claims were found.
    /// </value>
    /// <remarks>
    /// LOGIC: Claims are subject-predicate-object assertions extracted
    /// from the document text. They provide semantic context linking
    /// entities through factual relationships.
    /// </remarks>
    public IReadOnlyList<Claim> Claims { get; init; } = [];

    /// <summary>
    /// Metadata about the transformation and source.
    /// </summary>
    /// <value>
    /// Key-value pairs containing contextual information about the
    /// transformation process.
    /// </value>
    /// <remarks>
    /// LOGIC: Typical metadata includes:
    /// - "documentTitle": Source document title
    /// - "documentHash": Content hash at extraction time
    /// - "extractedEntityCount": Original extraction count
    /// - "transformedAt": Transformation timestamp
    /// Useful for debugging, logging, and provenance.
    /// </remarks>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// ID of the source document.
    /// </summary>
    /// <value>The unique identifier of the document being synced.</value>
    /// <remarks>
    /// LOGIC: Links the ingestion data back to its source for provenance
    /// tracking. Stored on entities as part of SourceDocuments list.
    /// </remarks>
    public Guid SourceDocumentId { get; init; }

    /// <summary>
    /// Timestamp when the ingestion data was prepared.
    /// </summary>
    /// <value>UTC timestamp of transformation completion.</value>
    /// <remarks>
    /// LOGIC: Records when the data was ready for ingestion. Used for
    /// timing analysis and cache invalidation.
    /// </remarks>
    public DateTimeOffset PreparedAt { get; init; } = DateTimeOffset.UtcNow;
}
