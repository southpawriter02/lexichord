// =============================================================================
// File: DocToGraphSyncOptions.cs
// Project: Lexichord.Abstractions
// Description: Configuration options for document-to-graph sync operations.
// =============================================================================
// LOGIC: Provides fine-grained control over sync behavior including validation,
//   error handling, lineage preservation, entity limits, and feature toggles.
//
// v0.7.6f: Doc-to-Graph Sync (CKVS Phase 4c)
// Dependencies: None
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph;

/// <summary>
/// Configuration options for document-to-graph synchronization operations.
/// </summary>
/// <remarks>
/// <para>
/// Controls the behavior of <see cref="IDocumentToGraphSyncProvider.SyncAsync"/>:
/// </para>
/// <list type="bullet">
///   <item><b>Validation:</b> Whether to validate before upsert and how to handle errors.</item>
///   <item><b>Lineage:</b> Whether to preserve extraction history for rollback.</item>
///   <item><b>Limits:</b> Maximum entities to prevent runaway extractions.</item>
///   <item><b>Features:</b> Toggle relationship creation, claim extraction, enrichment.</item>
///   <item><b>Timeout:</b> Maximum duration for the sync operation.</item>
/// </list>
/// <para>
/// <b>Default Configuration:</b> Validation enabled, lineage preserved, all
/// features on, 1000 entity limit, 10 minute timeout.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6f as part of the Doc-to-Graph Sync module.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new DocToGraphSyncOptions
/// {
///     ValidateBeforeUpsert = true,
///     AutoCorrectErrors = false,
///     MaxEntities = 500,
///     ExtractClaims = true,
///     EnrichWithGraphContext = true
/// };
/// var result = await provider.SyncAsync(document, options);
/// </code>
/// </example>
public record DocToGraphSyncOptions
{
    /// <summary>
    /// Whether to validate extracted data before upserting to the graph.
    /// </summary>
    /// <value>True to validate before upsert (default), false to skip validation.</value>
    /// <remarks>
    /// LOGIC: When enabled, extracted entities and relationships are validated
    /// against the graph schema before being written. Invalid data causes
    /// the sync to fail or return partial success depending on error handling.
    /// Disabling validation improves performance but risks schema violations.
    /// </remarks>
    public bool ValidateBeforeUpsert { get; init; } = true;

    /// <summary>
    /// Whether to automatically correct validation errors when possible.
    /// </summary>
    /// <value>True to attempt auto-correction, false to fail on errors (default).</value>
    /// <remarks>
    /// LOGIC: When enabled, the validator attempts to fix issues like:
    /// - Normalizing entity type names to match schema
    /// - Removing invalid properties
    /// - Coercing property types
    /// Critical errors cannot be auto-corrected regardless of this setting.
    /// </remarks>
    public bool AutoCorrectErrors { get; init; } = false;

    /// <summary>
    /// Whether to preserve extraction lineage for rollback capability.
    /// </summary>
    /// <value>True to record lineage (default), false to skip lineage tracking.</value>
    /// <remarks>
    /// LOGIC: When enabled, creates an <see cref="ExtractionRecord"/> that
    /// links the extraction to the document version, enabling later rollback
    /// via <see cref="IDocumentToGraphSyncProvider.RollbackSyncAsync"/>.
    /// Disabling lineage reduces storage but prevents version history.
    /// </remarks>
    public bool PreserveLineage { get; init; } = true;

    /// <summary>
    /// Maximum number of entities to extract from a single document.
    /// </summary>
    /// <value>Entity limit. Default is 1000.</value>
    /// <remarks>
    /// LOGIC: Prevents runaway extraction from extremely large documents.
    /// When extraction exceeds this limit, the entity list is truncated
    /// and a warning is logged. Adjust based on document corpus characteristics.
    /// </remarks>
    public int MaxEntities { get; init; } = 1000;

    /// <summary>
    /// Whether to create relationships between extracted entities.
    /// </summary>
    /// <value>True to create relationships (default), false to extract entities only.</value>
    /// <remarks>
    /// LOGIC: When enabled, the transformer analyzes co-occurrence and claims
    /// to establish relationships between entities. Disabling produces a
    /// flat entity list without graph edges, useful for simple indexing.
    /// </remarks>
    public bool CreateRelationships { get; init; } = true;

    /// <summary>
    /// Whether to extract and store claims (subject-predicate-object assertions).
    /// </summary>
    /// <value>True to extract claims (default), false to skip claim extraction.</value>
    /// <remarks>
    /// LOGIC: When enabled, invokes the claim extraction service to identify
    /// factual assertions in the document. Claims provide semantic context
    /// beyond simple entity mentions. Requires WriterPro tier or above.
    /// </remarks>
    public bool ExtractClaims { get; init; } = true;

    /// <summary>
    /// Whether to enrich entities with existing graph context.
    /// </summary>
    /// <value>True to enrich with graph context (default), false to skip enrichment.</value>
    /// <remarks>
    /// LOGIC: When enabled, newly extracted entities are matched against
    /// existing graph entities to add context (e.g., similar entities,
    /// existing relationships). Requires Teams tier or above for full access.
    /// </remarks>
    public bool EnrichWithGraphContext { get; init; } = true;

    /// <summary>
    /// Maximum duration for the sync operation before timeout.
    /// </summary>
    /// <value>Timeout duration. Default is 10 minutes.</value>
    /// <remarks>
    /// LOGIC: Prevents sync operations from running indefinitely. When
    /// exceeded, the operation is cancelled and returns a Failed status.
    /// Adjust based on expected document sizes and system performance.
    /// </remarks>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(10);
}
