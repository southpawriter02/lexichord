// =============================================================================
// File: IAffectedDocumentDetector.cs
// Project: Lexichord.Abstractions
// Description: Interface for detecting documents affected by graph changes.
// =============================================================================
// LOGIC: Provides document detection capabilities for graph-to-doc sync.
//   Queries document-entity relationships to find documents referencing
//   changed entities.
//
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// Dependencies: GraphChange (v0.7.6e), AffectedDocument, DocumentEntityRelationship
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;

/// <summary>
/// Detects documents affected by knowledge graph changes.
/// </summary>
/// <remarks>
/// <para>
/// Used by <see cref="IGraphToDocumentSyncProvider"/> to identify documents
/// that may need review when entities change:
/// </para>
/// <list type="bullet">
///   <item><b>DetectAsync:</b> Find documents affected by a single change.</item>
///   <item><b>DetectBatchAsync:</b> Find documents affected by multiple changes.</item>
///   <item><b>GetRelationshipAsync:</b> Query specific document-entity relationships.</item>
/// </list>
/// <para>
/// <b>Implementation:</b> See <c>AffectedDocumentDetector</c> in
/// Lexichord.Modules.Knowledge.Sync.GraphToDoc.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6g as part of the Graph-to-Doc Sync module.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Detect documents affected by a graph change
/// var affected = await detector.DetectAsync(graphChange);
///
/// foreach (var doc in affected)
/// {
///     Console.WriteLine($"{doc.DocumentName}: {doc.Relationship}");
/// }
/// </code>
/// </example>
public interface IAffectedDocumentDetector
{
    /// <summary>
    /// Detects documents referencing a changed entity.
    /// </summary>
    /// <param name="change">The graph change to analyze.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="AffectedDocument"/> records describing
    /// documents that reference the changed entity.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Detection process:
    /// </para>
    /// <list type="number">
    ///   <item>Query document-entity relationship store for entity references.</item>
    ///   <item>Resolve document metadata (name, last modified, last sync).</item>
    ///   <item>Determine relationship type for each document.</item>
    ///   <item>Calculate reference counts.</item>
    /// </list>
    /// </remarks>
    Task<IReadOnlyList<AffectedDocument>> DetectAsync(
        GraphChange change,
        CancellationToken ct = default);

    /// <summary>
    /// Detects documents referencing any entity in a batch of changes.
    /// </summary>
    /// <param name="changes">The graph changes to analyze.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A deduplicated list of <see cref="AffectedDocument"/> records.
    /// Documents appearing in multiple changes are included only once.
    /// </returns>
    /// <remarks>
    /// LOGIC: Processes each change individually, then deduplicates
    /// the results by document ID. More efficient than calling
    /// <see cref="DetectAsync"/> multiple times when processing
    /// batched changes.
    /// </remarks>
    Task<IReadOnlyList<AffectedDocument>> DetectBatchAsync(
        IReadOnlyList<GraphChange> changes,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the relationship between a document and entity.
    /// </summary>
    /// <param name="documentId">The document ID to query.</param>
    /// <param name="entityId">The entity ID to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The <see cref="DocumentEntityRelationship"/> type, or null if
    /// no relationship exists between the document and entity.
    /// </returns>
    /// <remarks>
    /// LOGIC: Queries the relationship store for a specific document-entity
    /// pair. Used for detailed inspection of individual relationships.
    /// </remarks>
    Task<DocumentEntityRelationship?> GetRelationshipAsync(
        Guid documentId,
        Guid entityId,
        CancellationToken ct = default);
}
