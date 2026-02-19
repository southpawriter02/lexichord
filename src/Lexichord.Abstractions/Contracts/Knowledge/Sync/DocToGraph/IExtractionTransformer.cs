// =============================================================================
// File: IExtractionTransformer.cs
// Project: Lexichord.Abstractions
// Description: Interface for transforming extraction results to graph format.
// =============================================================================
// LOGIC: Transforms raw extraction results (EntityMention, AggregatedEntity)
//   into KnowledgeEntity format suitable for graph storage, and provides
//   entity enrichment with existing graph context.
//
// v0.7.6f: Doc-to-Graph Sync (CKVS Phase 4c)
// Dependencies: ExtractionResult, Document, GraphIngestionData, KnowledgeEntity,
//               KnowledgeRelationship
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph;

/// <summary>
/// Transforms extraction results for knowledge graph ingestion.
/// </summary>
/// <remarks>
/// <para>
/// Converts raw extraction output to graph-ready format:
/// </para>
/// <list type="bullet">
///   <item><b>Entity Transform:</b> Convert <see cref="AggregatedEntity"/> to <see cref="KnowledgeEntity"/>.</item>
///   <item><b>Relationship Derivation:</b> Infer relationships from co-occurrence and claims.</item>
///   <item><b>Enrichment:</b> Add context from existing graph entities.</item>
/// </list>
/// <para>
/// <b>Transform Pipeline:</b>
/// <code>
/// ExtractionResult → TransformAsync → GraphIngestionData → Graph Upsert
/// </code>
/// </para>
/// <para>
/// <b>Implementation:</b> See <c>ExtractionTransformer</c> in
/// Lexichord.Modules.Knowledge.Sync.DocToGraph.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6f as part of the Doc-to-Graph Sync module.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var ingestionData = await transformer.TransformAsync(extractionResult, document, ct);
///
/// // Enrich entities with graph context
/// var enrichedEntities = await transformer.EnrichEntitiesAsync(ingestionData.Entities, ct);
/// </code>
/// </example>
public interface IExtractionTransformer
{
    /// <summary>
    /// Transforms an extraction result for graph ingestion.
    /// </summary>
    /// <param name="extraction">The raw extraction result from the entity extraction pipeline.</param>
    /// <param name="document">The source document for metadata.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="GraphIngestionData"/> containing transformed entities,
    /// derived relationships, and metadata.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Transform process:
    /// </para>
    /// <list type="number">
    ///   <item>Map <see cref="AggregatedEntity"/> to <see cref="KnowledgeEntity"/>.</item>
    ///   <item>Normalize entity types to match schema.</item>
    ///   <item>Extract properties and map to graph format.</item>
    ///   <item>Analyze co-occurrence to derive relationships.</item>
    ///   <item>Attach source document reference.</item>
    /// </list>
    /// </remarks>
    Task<GraphIngestionData> TransformAsync(
        ExtractionResult extraction,
        Document document,
        CancellationToken ct = default);

    /// <summary>
    /// Transforms aggregated entities to knowledge entities.
    /// </summary>
    /// <param name="entities">The aggregated entities from extraction.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="KnowledgeEntity"/> instances ready for graph storage.
    /// </returns>
    /// <remarks>
    /// LOGIC: Maps entity properties:
    /// - CanonicalValue → Name
    /// - EntityType → Type
    /// - MergedProperties → Properties
    /// - Generates new Guid for Id
    /// </remarks>
    Task<IReadOnlyList<KnowledgeEntity>> TransformEntitiesAsync(
        IReadOnlyList<AggregatedEntity> entities,
        CancellationToken ct = default);

    /// <summary>
    /// Derives relationships from entity co-occurrence.
    /// </summary>
    /// <param name="entities">The transformed knowledge entities.</param>
    /// <param name="extraction">The original extraction for context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="KnowledgeRelationship"/> instances connecting entities.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Relationship derivation strategies:
    /// </para>
    /// <list type="bullet">
    ///   <item><b>Co-occurrence:</b> Entities in same paragraph may be related.</item>
    ///   <item><b>Claim-based:</b> Subject-predicate-object implies relationship.</item>
    ///   <item><b>Pattern-based:</b> Known patterns (e.g., "X contains Y").</item>
    /// </list>
    /// </remarks>
    Task<IReadOnlyList<KnowledgeRelationship>> DeriveRelationshipsAsync(
        IReadOnlyList<KnowledgeEntity> entities,
        ExtractionResult extraction,
        CancellationToken ct = default);

    /// <summary>
    /// Enriches entities with existing graph context.
    /// </summary>
    /// <param name="entities">The entities to enrich.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Enriched entities with additional context from the graph.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Enrichment adds context from existing graph state:
    /// </para>
    /// <list type="bullet">
    ///   <item>Find similar/matching entities already in graph.</item>
    ///   <item>Add references to related entities.</item>
    ///   <item>Mark entities as updates vs. new.</item>
    ///   <item>Add "similarEntityIds" and "enrichedAt" properties.</item>
    /// </list>
    /// <para>
    /// Requires Teams tier or above for full enrichment capabilities.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<KnowledgeEntity>> EnrichEntitiesAsync(
        IReadOnlyList<KnowledgeEntity> entities,
        CancellationToken ct = default);
}
