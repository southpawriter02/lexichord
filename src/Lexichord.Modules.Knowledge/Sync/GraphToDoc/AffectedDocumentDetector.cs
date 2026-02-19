// =============================================================================
// File: AffectedDocumentDetector.cs
// Project: Lexichord.Modules.Knowledge
// Description: Detects documents affected by graph changes.
// =============================================================================
// LOGIC: AffectedDocumentDetector queries document-entity relationships to
//   identify documents that reference changed graph entities. It supports
//   single change and batch detection with deduplication.
//
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// Dependencies: IAffectedDocumentDetector, IDocumentRepository, IGraphRepository,
//               GraphChange (v0.7.6e), AffectedDocument, DocumentEntityRelationship
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;
using Lexichord.Abstractions.Contracts.RAG;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Sync.GraphToDoc;

/// <summary>
/// Service for detecting documents affected by knowledge graph changes.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="IAffectedDocumentDetector"/> to find documents
/// referencing changed entities. Used by <see cref="GraphToDocumentSyncProvider"/>
/// during graph-to-doc sync operations.
/// </para>
/// <para>
/// <b>Detection Strategy:</b>
/// <list type="bullet">
///   <item>Query entity's source documents from the graph.</item>
///   <item>Determine relationship type (derived from source documents).</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6g as part of the Graph-to-Doc Sync module.
/// </para>
/// </remarks>
public sealed class AffectedDocumentDetector : IAffectedDocumentDetector
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IGraphRepository _graphRepository;
    private readonly ILogger<AffectedDocumentDetector> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="AffectedDocumentDetector"/>.
    /// </summary>
    /// <param name="documentRepository">The document repository for document queries.</param>
    /// <param name="graphRepository">The graph repository for entity queries.</param>
    /// <param name="logger">The logger instance.</param>
    public AffectedDocumentDetector(
        IDocumentRepository documentRepository,
        IGraphRepository graphRepository,
        ILogger<AffectedDocumentDetector> logger)
    {
        _documentRepository = documentRepository;
        _graphRepository = graphRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AffectedDocument>> DetectAsync(
        GraphChange change,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Detecting documents affected by entity {EntityId} ({ChangeType})",
            change.EntityId, change.ChangeType);

        var affected = new List<AffectedDocument>();

        try
        {
            // LOGIC: Step 1 - Get the entity to find its source documents.
            var entity = await _graphRepository.GetByIdAsync(change.EntityId, ct);

            // LOGIC: Step 2 - Get source documents from the entity.
            // These are documents that derived the entity (DerivedFrom relationship).
            var sourceDocumentIds = entity?.SourceDocuments ?? [];

            foreach (var documentId in sourceDocumentIds)
            {
                var document = await _documentRepository.GetByIdAsync(documentId, ct);
                if (document is not null)
                {
                    affected.Add(new AffectedDocument
                    {
                        DocumentId = document.Id,
                        DocumentName = document.Title ?? document.FilePath,
                        Relationship = DocumentEntityRelationship.DerivedFrom,
                        ReferenceCount = 1,
                        LastModifiedAt = document.IndexedAt.HasValue
                            ? new DateTimeOffset(document.IndexedAt.Value, TimeSpan.Zero)
                            : DateTimeOffset.MinValue,
                        LastSyncedAt = document.IndexedAt.HasValue
                            ? new DateTimeOffset(document.IndexedAt.Value, TimeSpan.Zero)
                            : null
                    });

                    _logger.LogDebug(
                        "Found source document {DocumentId} ({Title}) for entity {EntityId}",
                        document.Id, document.Title, change.EntityId);
                }
            }

            _logger.LogDebug(
                "Detected {Count} affected documents for entity {EntityId}",
                affected.Count, change.EntityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error detecting affected documents for entity {EntityId}",
                change.EntityId);
            // LOGIC: Return partial results rather than failing completely.
        }

        return affected;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AffectedDocument>> DetectBatchAsync(
        IReadOnlyList<GraphChange> changes,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Detecting documents affected by {Count} graph changes",
            changes.Count);

        // LOGIC: Use Dictionary for deduplication by DocumentId.
        var affectedByDocId = new Dictionary<Guid, AffectedDocument>();

        foreach (var change in changes)
        {
            ct.ThrowIfCancellationRequested();

            var affected = await DetectAsync(change, ct);

            foreach (var doc in affected)
            {
                if (!affectedByDocId.ContainsKey(doc.DocumentId))
                {
                    affectedByDocId[doc.DocumentId] = doc;
                }
                else
                {
                    // LOGIC: If document already seen, increment reference count.
                    var existing = affectedByDocId[doc.DocumentId];
                    affectedByDocId[doc.DocumentId] = existing with
                    {
                        ReferenceCount = existing.ReferenceCount + doc.ReferenceCount
                    };
                }
            }
        }

        var result = affectedByDocId.Values.ToList();

        _logger.LogDebug(
            "Batch detection found {Count} unique affected documents from {ChangeCount} changes",
            result.Count, changes.Count);

        return result;
    }

    /// <inheritdoc/>
    public async Task<DocumentEntityRelationship?> GetRelationshipAsync(
        Guid documentId,
        Guid entityId,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Getting relationship between document {DocumentId} and entity {EntityId}",
            documentId, entityId);

        try
        {
            // LOGIC: Check if entity lists this document as a source.
            var entity = await _graphRepository.GetByIdAsync(entityId, ct);
            if (entity?.SourceDocuments?.Contains(documentId) == true)
            {
                return DocumentEntityRelationship.DerivedFrom;
            }

            // LOGIC: If not in source documents, check if document content references the entity.
            // This would require content search which is not available in the current API.
            // Return null for now — future versions can add content-based detection.

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting relationship between document {DocumentId} and entity {EntityId}",
                documentId, entityId);
            return null;
        }
    }
}
