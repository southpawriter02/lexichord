// =============================================================================
// File: SyncDirection.cs
// Project: Lexichord.Abstractions
// Description: Defines the direction of synchronization operations.
// =============================================================================
// LOGIC: Sync can flow from document to graph, graph to document, or both.
//   Different license tiers have access to different directions.
//   WriterPro: document-to-graph only. Teams+: bidirectional.
//
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync;

/// <summary>
/// Direction of a synchronization operation.
/// </summary>
/// <remarks>
/// <para>
/// Determines the flow of data during sync:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="DocumentToGraph"/>: Push document changes to graph.</description></item>
///   <item><description><see cref="GraphToDocument"/>: Propagate graph changes to documents.</description></item>
///   <item><description><see cref="Bidirectional"/>: Full two-way synchronization.</description></item>
/// </list>
/// <para>
/// <b>License Requirements:</b>
/// <list type="bullet">
///   <item><description>WriterPro: <see cref="DocumentToGraph"/> only.</description></item>
///   <item><description>Teams: All directions.</description></item>
///   <item><description>Enterprise: All directions with custom policies.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6e as part of the Sync Service Core.
/// </para>
/// </remarks>
public enum SyncDirection
{
    /// <summary>
    /// Synchronize from document to knowledge graph.
    /// </summary>
    /// <remarks>
    /// LOGIC: Extract entities/claims from the document and upsert them
    /// to the knowledge graph. The document is the source of truth.
    /// Available to WriterPro tier and above.
    /// </remarks>
    DocumentToGraph = 0,

    /// <summary>
    /// Propagate knowledge graph changes to affected documents.
    /// </summary>
    /// <remarks>
    /// LOGIC: When graph entities change (edited directly or via another
    /// document), find all documents that reference those entities and
    /// flag them for review. Available to Teams tier and above.
    /// </remarks>
    GraphToDocument = 1,

    /// <summary>
    /// Full bidirectional synchronization.
    /// </summary>
    /// <remarks>
    /// LOGIC: Both directions are active. Document changes push to graph,
    /// and graph changes propagate back to documents. Requires conflict
    /// resolution when both sides have changed. Available to Teams tier and above.
    /// </remarks>
    Bidirectional = 2
}
