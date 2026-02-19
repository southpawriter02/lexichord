// =============================================================================
// File: IConflictDetector.cs
// Project: Lexichord.Abstractions
// Description: Interface for detecting conflicts between documents and graph.
// =============================================================================
// LOGIC: IConflictDetector provides enhanced conflict detection beyond
//   the basic ISyncConflictDetector from v0.7.6e. It supports value
//   conflict detection, structural conflict detection, and change
//   tracking for entities since extraction.
//
// v0.7.6h: Conflict Resolver (CKVS Phase 4c)
// Dependencies: SyncConflict (v0.7.6e), ConflictDetail (v0.7.6h),
//               KnowledgeEntity (v0.4.5e), Document (v0.4.1c),
//               ExtractionResult (v0.4.5g), ExtractionRecord (v0.7.6f)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph;
using Lexichord.Abstractions.Contracts.RAG;

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict;

/// <summary>
/// Service for detecting conflicts between documents and the knowledge graph.
/// </summary>
/// <remarks>
/// <para>
/// Provides enhanced conflict detection:
/// </para>
/// <list type="bullet">
///   <item>Value conflict detection for specific entities.</item>
///   <item>Structural conflict detection (missing entities/relationships).</item>
///   <item>Entity change tracking since extraction.</item>
/// </list>
/// <para>
/// This extends the capabilities of <see cref="ISyncConflictDetector"/> from v0.7.6e
/// with more detailed conflict analysis.
/// </para>
/// <para>
/// <b>Implementation:</b> See <c>ConflictDetector</c> in Lexichord.Modules.Knowledge.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6h as part of the Conflict Resolver module.
/// </para>
/// </remarks>
public interface IConflictDetector
{
    /// <summary>
    /// Detects all conflicts for a document extraction.
    /// </summary>
    /// <param name="document">The source document.</param>
    /// <param name="extraction">The entities extracted from the document.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="SyncConflict"/> entries. Empty if no conflicts.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Combines value and structural conflict detection:
    /// </para>
    /// <list type="number">
    ///   <item>Detect value conflicts for extracted entities.</item>
    ///   <item>Detect structural conflicts (missing in graph/document).</item>
    ///   <item>Combine and return all detected conflicts.</item>
    /// </list>
    /// </remarks>
    Task<IReadOnlyList<SyncConflict>> DetectAsync(
        Document document,
        ExtractionResult extraction,
        CancellationToken ct = default);

    /// <summary>
    /// Detects value conflicts for specific entities.
    /// </summary>
    /// <param name="entities">The entities to check for value conflicts.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="ConflictDetail"/> entries for value conflicts.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: For each entity:
    /// </para>
    /// <list type="number">
    ///   <item>Look up corresponding entity in graph by ID.</item>
    ///   <item>Compare property values using <see cref="IEntityComparer"/>.</item>
    ///   <item>Create ConflictDetail for each difference.</item>
    ///   <item>Determine severity and suggest resolution strategy.</item>
    /// </list>
    /// </remarks>
    Task<IReadOnlyList<ConflictDetail>> DetectValueConflictsAsync(
        IReadOnlyList<KnowledgeEntity> entities,
        CancellationToken ct = default);

    /// <summary>
    /// Detects structural conflicts for a document.
    /// </summary>
    /// <param name="document">The source document.</param>
    /// <param name="extraction">The extraction result.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="ConflictDetail"/> entries for structural conflicts.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Detects entities that exist in one source but not the other:
    /// </para>
    /// <list type="bullet">
    ///   <item>MissingInGraph: Entity in document but not graph.</item>
    ///   <item>MissingInDocument: Entity in graph but not document.</item>
    /// </list>
    /// </remarks>
    Task<IReadOnlyList<ConflictDetail>> DetectStructuralConflictsAsync(
        Document document,
        ExtractionResult extraction,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if entities have changed since extraction.
    /// </summary>
    /// <param name="extraction">The extraction record to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if any entity has been modified since extraction.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Compares entity timestamps:
    /// </para>
    /// <list type="number">
    ///   <item>Retrieve current entities from graph by IDs in extraction.</item>
    ///   <item>Compare UpdatedAt timestamps against ExtractedAt.</item>
    ///   <item>Return true if any entity was modified after extraction.</item>
    /// </list>
    /// </remarks>
    Task<bool> EntitiesChangedAsync(
        ExtractionRecord extraction,
        CancellationToken ct = default);
}
