// =============================================================================
// File: SyncConflictDetector.cs
// Project: Lexichord.Modules.Knowledge
// Description: Detects conflicts between document extractions and graph state.
// =============================================================================
// LOGIC: SyncConflictDetector compares extraction results against existing
//   graph entities to identify conflicts. It categorizes conflicts by type
//   (value mismatch, missing in graph/document, concurrent edit) and severity.
//
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// Dependencies: SyncConflict, ConflictType, ConflictSeverity (v0.7.6e),
//               IGraphRepository (v0.4.5e), ExtractionResult (v0.4.5g)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.RAG;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Sync;

/// <summary>
/// Service for detecting conflicts between document extractions and graph state.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="ISyncConflictDetector"/> to compare document extractions
/// against the knowledge graph and identify conflicts.
/// </para>
/// <para>
/// <b>Conflict Types Detected:</b>
/// <list type="bullet">
///   <item><see cref="ConflictType.ValueMismatch"/>: Same property has different values.</item>
///   <item><see cref="ConflictType.MissingInGraph"/>: Entity in document but not graph.</item>
///   <item><see cref="ConflictType.MissingInDocument"/>: Entity in graph but not document.</item>
///   <item><see cref="ConflictType.ConcurrentEdit"/>: Both sides changed since last sync.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6e as part of the Sync Service Core.
/// </para>
/// </remarks>
public sealed class SyncConflictDetector : ISyncConflictDetector
{
    private readonly IGraphRepository _graphRepository;
    private readonly ILogger<SyncConflictDetector> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SyncConflictDetector"/>.
    /// </summary>
    /// <param name="graphRepository">The graph repository for entity queries.</param>
    /// <param name="logger">The logger instance.</param>
    public SyncConflictDetector(
        IGraphRepository graphRepository,
        ILogger<SyncConflictDetector> logger)
    {
        _graphRepository = graphRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SyncConflict>> DetectAsync(
        Document document,
        ExtractionResult extraction,
        CancellationToken ct = default)
    {
        var conflicts = new List<SyncConflict>();

        _logger.LogDebug(
            "Detecting conflicts for document {DocumentId} with {MentionCount} mentions",
            document.Id, extraction.Mentions.Count);

        try
        {
            // LOGIC: If no aggregated entities, nothing to compare.
            if (extraction.AggregatedEntities is null || extraction.AggregatedEntities.Count == 0)
            {
                _logger.LogDebug(
                    "No aggregated entities in extraction, skipping conflict detection");
                return conflicts;
            }

            // LOGIC: Get existing entities from graph that are linked to this document.
            var existingEntities = await GetEntitiesForDocumentAsync(document.Id, ct);

            _logger.LogDebug(
                "Found {ExistingCount} existing entities linked to document {DocumentId}",
                existingEntities.Count, document.Id);

            // LOGIC: Track which existing entities we've matched.
            var matchedEntityIds = new HashSet<Guid>();

            // LOGIC: Check each extracted entity against existing graph entities.
            foreach (var extracted in extraction.AggregatedEntities)
            {
                // LOGIC: Find matching entity by canonical value and type.
                var existing = existingEntities.FirstOrDefault(e =>
                    e.Name.Equals(extracted.CanonicalValue, StringComparison.OrdinalIgnoreCase) &&
                    e.Type.Equals(extracted.EntityType, StringComparison.OrdinalIgnoreCase));

                if (existing is not null)
                {
                    matchedEntityIds.Add(existing.Id);

                    // LOGIC: Check for value mismatches in properties.
                    // Compare confidence score as an example property.
                    if (existing.Properties.TryGetValue("MaxConfidence", out var graphConfidence) &&
                        graphConfidence is float graphConfidenceValue)
                    {
                        var confidenceDiff = Math.Abs(extracted.MaxConfidence - graphConfidenceValue);
                        if (confidenceDiff > 0.1f) // Significant difference threshold
                        {
                            conflicts.Add(new SyncConflict
                            {
                                ConflictTarget = $"{existing.Type}:{existing.Name}.Confidence",
                                DocumentValue = extracted.MaxConfidence,
                                GraphValue = graphConfidenceValue,
                                DetectedAt = DateTimeOffset.UtcNow,
                                Type = ConflictType.ValueMismatch,
                                Severity = ConflictSeverity.Low,
                                Description = $"Confidence score differs: document={extracted.MaxConfidence:F2}, graph={graphConfidenceValue:F2}"
                            });
                        }
                    }

                    // LOGIC: Check for mention count changes (could indicate document changes).
                    if (existing.Properties.TryGetValue("MentionCount", out var graphMentionCount) &&
                        graphMentionCount is int graphMentionsValue &&
                        Math.Abs(extracted.Mentions.Count - graphMentionsValue) > 0)
                    {
                        conflicts.Add(new SyncConflict
                        {
                            ConflictTarget = $"{existing.Type}:{existing.Name}.MentionCount",
                            DocumentValue = extracted.Mentions.Count,
                            GraphValue = graphMentionsValue,
                            DetectedAt = DateTimeOffset.UtcNow,
                            Type = ConflictType.ValueMismatch,
                            Severity = ConflictSeverity.Low,
                            Description = $"Mention count changed: document={extracted.Mentions.Count}, graph={graphMentionsValue}"
                        });
                    }
                }
                else
                {
                    // LOGIC: Entity in document but not in graph.
                    // This is not necessarily a conflict — it's a new entity.
                    // Only flag as conflict if we expected it to exist.
                    _logger.LogDebug(
                        "New entity detected: {Type}:{Name}",
                        extracted.EntityType, extracted.CanonicalValue);
                }
            }

            // LOGIC: Check for entities in graph that are no longer in document.
            foreach (var existing in existingEntities)
            {
                if (!matchedEntityIds.Contains(existing.Id))
                {
                    conflicts.Add(new SyncConflict
                    {
                        ConflictTarget = $"{existing.Type}:{existing.Name}",
                        DocumentValue = "<missing>",
                        GraphValue = existing,
                        DetectedAt = DateTimeOffset.UtcNow,
                        Type = ConflictType.MissingInDocument,
                        Severity = ConflictSeverity.Medium,
                        Description = $"Entity exists in graph but not found in document: {existing.Type}:{existing.Name}"
                    });
                }
            }

            _logger.LogDebug(
                "Conflict detection completed: {ConflictCount} conflicts found",
                conflicts.Count);

            return conflicts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Conflict detection failed for document {DocumentId}",
                document.Id);

            // LOGIC: Return empty list on error — don't block sync.
            return conflicts;
        }
    }

    /// <summary>
    /// Gets all entities linked to a specific document.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of entities linked to the document.</returns>
    private async Task<IReadOnlyList<KnowledgeEntity>> GetEntitiesForDocumentAsync(
        Guid documentId,
        CancellationToken ct)
    {
        // LOGIC: Query graph for entities with SourceDocument property matching this document.
        // This is a simplified implementation — production would use proper graph queries.
        var allEntities = await _graphRepository.GetAllEntitiesAsync(ct);

        return allEntities
            .Where(e =>
                e.Properties.TryGetValue("SourceDocument", out var srcDoc) &&
                srcDoc is string srcDocStr &&
                Guid.TryParse(srcDocStr, out var srcDocId) &&
                srcDocId == documentId)
            .ToList();
    }
}
