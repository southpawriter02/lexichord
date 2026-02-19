// =============================================================================
// File: DocToGraphSyncResult.cs
// Project: Lexichord.Abstractions
// Description: Result record for document-to-graph sync operations.
// =============================================================================
// LOGIC: Comprehensive result containing operation status, upserted entities,
//   created relationships, extracted claims, validation errors, lineage record,
//   timing information, and summary message.
//
// v0.7.6f: Doc-to-Graph Sync (CKVS Phase 4c)
// Dependencies: SyncOperationStatus (v0.7.6e), KnowledgeEntity, KnowledgeRelationship,
//               Claim, ValidationError, ExtractionRecord
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Claims;

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph;

/// <summary>
/// Result of a document-to-graph synchronization operation.
/// </summary>
/// <remarks>
/// <para>
/// Contains comprehensive information about the sync outcome:
/// </para>
/// <list type="bullet">
///   <item><b>Status:</b> Overall operation result (see <see cref="SyncOperationStatus"/>).</item>
///   <item><b>Entities:</b> Knowledge entities upserted to the graph.</item>
///   <item><b>Relationships:</b> Knowledge relationships created between entities.</item>
///   <item><b>Claims:</b> Claims extracted from the document content.</item>
///   <item><b>Validation:</b> Validation errors encountered during processing.</item>
///   <item><b>Lineage:</b> Extraction record for rollback capability.</item>
///   <item><b>Timing:</b> Operation duration and entity count.</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// var result = await provider.SyncAsync(document, options);
/// if (result.Status == SyncOperationStatus.Success)
/// {
///     Console.WriteLine($"Synced {result.TotalEntitiesAffected} entities in {result.Duration.TotalSeconds}s");
///     Console.WriteLine($"Created {result.CreatedRelationships.Count} relationships");
///     Console.WriteLine($"Extracted {result.ExtractedClaims.Count} claims");
/// }
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6f as part of the Doc-to-Graph Sync module.
/// </para>
/// </remarks>
public record DocToGraphSyncResult
{
    /// <summary>
    /// Status of the sync operation.
    /// </summary>
    /// <value>The overall outcome of the synchronization.</value>
    /// <remarks>
    /// LOGIC: Determines how the caller should handle the result:
    /// - Success: All entities synced, no issues.
    /// - SuccessWithConflicts: Synced but conflicts detected.
    /// - PartialSuccess: Some entities synced, validation errors on others.
    /// - Failed: Sync failed completely.
    /// - NoChanges: Document unchanged since last sync.
    /// </remarks>
    public required SyncOperationStatus Status { get; init; }

    /// <summary>
    /// Knowledge entities upserted to the graph.
    /// </summary>
    /// <value>
    /// A read-only list of entities that were created or updated in the
    /// knowledge graph. Empty if no entities were extracted.
    /// </value>
    /// <remarks>
    /// LOGIC: Includes both newly created entities and existing entities
    /// that were updated with new properties or source document references.
    /// </remarks>
    public IReadOnlyList<KnowledgeEntity> UpsertedEntities { get; init; } = [];

    /// <summary>
    /// Knowledge relationships created between entities.
    /// </summary>
    /// <value>
    /// A read-only list of relationships that were created in the graph.
    /// Empty if <see cref="DocToGraphSyncOptions.CreateRelationships"/> was false.
    /// </value>
    /// <remarks>
    /// LOGIC: Relationships are derived from entity co-occurrence and
    /// claim analysis. Only includes newly created relationships, not
    /// pre-existing ones.
    /// </remarks>
    public IReadOnlyList<KnowledgeRelationship> CreatedRelationships { get; init; } = [];

    /// <summary>
    /// Claims extracted from the document.
    /// </summary>
    /// <value>
    /// A read-only list of claims (subject-predicate-object assertions)
    /// extracted from the document content. Empty if claim extraction was
    /// disabled or no claims were found.
    /// </value>
    /// <remarks>
    /// LOGIC: Claims provide semantic context beyond entity mentions.
    /// Each claim links a subject entity to an object via a predicate.
    /// Only included when <see cref="DocToGraphSyncOptions.ExtractClaims"/> is true.
    /// </remarks>
    public IReadOnlyList<Claim> ExtractedClaims { get; init; } = [];

    /// <summary>
    /// Validation errors encountered during sync.
    /// </summary>
    /// <value>
    /// A read-only list of validation errors. Empty if validation passed
    /// or was disabled.
    /// </value>
    /// <remarks>
    /// LOGIC: Contains errors that caused partial failure or that were
    /// auto-corrected. Even successful syncs may have warnings in this list.
    /// Review for data quality assessment.
    /// </remarks>
    public IReadOnlyList<ValidationError> ValidationErrors { get; init; } = [];

    /// <summary>
    /// Extraction lineage record for rollback.
    /// </summary>
    /// <value>
    /// The extraction record if lineage preservation was enabled, null otherwise.
    /// </value>
    /// <remarks>
    /// LOGIC: Links this extraction to the document version, enabling
    /// <see cref="IDocumentToGraphSyncProvider.RollbackSyncAsync"/> to
    /// restore the graph to a previous state.
    /// </remarks>
    public ExtractionRecord? ExtractionRecord { get; init; }

    /// <summary>
    /// Duration of the sync operation.
    /// </summary>
    /// <value>Elapsed time from start to completion of the sync.</value>
    /// <remarks>
    /// LOGIC: Measured using Stopwatch for accuracy. Used for performance
    /// monitoring, logging, and timeout enforcement.
    /// </remarks>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Total count of entities affected by the sync.
    /// </summary>
    /// <value>Number of entities created or updated.</value>
    /// <remarks>
    /// LOGIC: Convenience count equivalent to <c>UpsertedEntities.Count</c>.
    /// Useful for logging and UI display without materializing the full list.
    /// </remarks>
    public int TotalEntitiesAffected { get; init; }

    /// <summary>
    /// Human-readable message summarizing the sync result.
    /// </summary>
    /// <value>Summary message for logging and UI display.</value>
    /// <remarks>
    /// LOGIC: Provides context about the outcome. Examples:
    /// - "Synced 15 entities, 8 claims, 12 relationships"
    /// - "Sync failed: Validation error on entity 'UserService'"
    /// - "No changes detected since last sync"
    /// </remarks>
    public string? Message { get; init; }
}
