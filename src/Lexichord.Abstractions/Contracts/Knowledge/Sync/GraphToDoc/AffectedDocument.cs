// =============================================================================
// File: AffectedDocument.cs
// Project: Lexichord.Abstractions
// Description: Represents a document affected by a graph change.
// =============================================================================
// LOGIC: Captures information about documents that reference a changed entity,
//   enabling impact assessment and prioritized review workflows.
//
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// Dependencies: DocumentEntityRelationship, SuggestedAction
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;

/// <summary>
/// A document affected by a knowledge graph change.
/// </summary>
/// <remarks>
/// <para>
/// When an entity in the knowledge graph changes, documents referencing
/// that entity may need review. This record captures:
/// </para>
/// <list type="bullet">
///   <item><b>Identity:</b> Document ID and name for navigation.</item>
///   <item><b>Relationship:</b> How the document references the entity.</item>
///   <item><b>Impact:</b> Reference count and suggested actions.</item>
///   <item><b>Timestamps:</b> Last modification and sync times.</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// var affected = await detector.DetectAsync(graphChange);
/// foreach (var doc in affected.OrderByDescending(d => d.ReferenceCount))
/// {
///     Console.WriteLine($"{doc.DocumentName}: {doc.ReferenceCount} references");
///     Console.WriteLine($"  Relationship: {doc.Relationship}");
///     if (doc.SuggestedAction is not null)
///     {
///         Console.WriteLine($"  Suggestion: {doc.SuggestedAction.Description}");
///     }
/// }
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6g as part of the Graph-to-Doc Sync module.
/// </para>
/// </remarks>
public record AffectedDocument
{
    /// <summary>
    /// Unique identifier of the affected document.
    /// </summary>
    /// <value>The document's GUID.</value>
    /// <remarks>
    /// LOGIC: Primary key for document lookup and flag creation.
    /// </remarks>
    public required Guid DocumentId { get; init; }

    /// <summary>
    /// Display name of the affected document.
    /// </summary>
    /// <value>Human-readable document name.</value>
    /// <remarks>
    /// LOGIC: Used for UI display. May be file name or document title
    /// depending on document type.
    /// </remarks>
    public required string DocumentName { get; init; }

    /// <summary>
    /// How the document relates to the changed entity.
    /// </summary>
    /// <value>The type of reference between document and entity.</value>
    /// <remarks>
    /// LOGIC: Determines impact severity and suggested actions.
    /// ExplicitReference typically has higher priority than ImplicitReference.
    /// </remarks>
    public required DocumentEntityRelationship Relationship { get; init; }

    /// <summary>
    /// Number of times the document references the entity.
    /// </summary>
    /// <value>Count of references found in document.</value>
    /// <remarks>
    /// LOGIC: Higher counts may indicate greater impact. Used for
    /// prioritization and estimating review effort.
    /// </remarks>
    public int ReferenceCount { get; init; }

    /// <summary>
    /// Suggested action for addressing the change.
    /// </summary>
    /// <value>Action suggestion, or null if none available.</value>
    /// <remarks>
    /// LOGIC: Populated when <see cref="GraphToDocSyncOptions.IncludeSuggestedActions"/>
    /// is true and confidence exceeds <see cref="GraphToDocSyncOptions.MinActionConfidence"/>.
    /// </remarks>
    public SuggestedAction? SuggestedAction { get; init; }

    /// <summary>
    /// When the document was last modified.
    /// </summary>
    /// <value>UTC timestamp of last modification.</value>
    /// <remarks>
    /// LOGIC: Used to assess staleness. Documents not modified since
    /// last sync may have lower priority.
    /// </remarks>
    public DateTimeOffset LastModifiedAt { get; init; }

    /// <summary>
    /// When the document was last synchronized.
    /// </summary>
    /// <value>UTC timestamp of last sync, or null if never synced.</value>
    /// <remarks>
    /// LOGIC: Null indicates the document has never been through
    /// doc-to-graph sync. Recent sync dates may indicate lower
    /// review priority.
    /// </remarks>
    public DateTimeOffset? LastSyncedAt { get; init; }
}
