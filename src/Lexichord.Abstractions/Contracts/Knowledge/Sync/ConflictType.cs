// =============================================================================
// File: ConflictType.cs
// Project: Lexichord.Abstractions
// Description: Defines types of conflicts that can occur during synchronization.
// =============================================================================
// LOGIC: When syncing documents with the knowledge graph, various types of
//   conflicts can arise. Each type has different characteristics and may
//   require different resolution strategies.
//
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync;

/// <summary>
/// Type of conflict detected during document-graph synchronization.
/// </summary>
/// <remarks>
/// <para>
/// Identifies the nature of a sync conflict to guide resolution:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="ValueMismatch"/>: Same property has different values.</description></item>
///   <item><description><see cref="MissingInGraph"/>: Entity exists in document but not graph.</description></item>
///   <item><description><see cref="MissingInDocument"/>: Entity exists in graph but not document.</description></item>
///   <item><description><see cref="RelationshipMismatch"/>: Relationship differs between sources.</description></item>
///   <item><description><see cref="ConcurrentEdit"/>: Concurrent modifications detected.</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6e as part of the Sync Service Core.
/// </para>
/// </remarks>
public enum ConflictType
{
    /// <summary>
    /// Property value differs between document and graph.
    /// </summary>
    /// <remarks>
    /// LOGIC: The same entity/property exists in both sources but with
    /// different values. Common when both document and graph are edited
    /// independently. Resolution: choose document value, graph value, or merge.
    /// </remarks>
    ValueMismatch = 0,

    /// <summary>
    /// Entity exists in document but not in the knowledge graph.
    /// </summary>
    /// <remarks>
    /// LOGIC: An entity extracted from the document has no corresponding
    /// node in the graph. May indicate a new entity or a deleted graph node.
    /// Resolution: create in graph or ignore.
    /// </remarks>
    MissingInGraph = 1,

    /// <summary>
    /// Entity exists in knowledge graph but not in the document.
    /// </summary>
    /// <remarks>
    /// LOGIC: A graph entity that was previously linked to this document
    /// is no longer found in the document content. May indicate intentional
    /// removal or content change. Resolution: delete from graph or flag.
    /// </remarks>
    MissingInDocument = 2,

    /// <summary>
    /// Relationship between entities differs between sources.
    /// </summary>
    /// <remarks>
    /// LOGIC: The relationship type or direction between two entities
    /// differs between the document extraction and graph state. Resolution:
    /// update relationship or preserve existing.
    /// </remarks>
    RelationshipMismatch = 3,

    /// <summary>
    /// Concurrent edits detected based on timestamp comparison.
    /// </summary>
    /// <remarks>
    /// LOGIC: Both document and graph entity have been modified since the
    /// last sync. Timestamp comparison reveals concurrent edits. Resolution:
    /// requires merge or explicit choice of which version to keep.
    /// </remarks>
    ConcurrentEdit = 4
}
