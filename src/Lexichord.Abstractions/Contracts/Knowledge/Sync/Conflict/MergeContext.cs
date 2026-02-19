// =============================================================================
// File: MergeContext.cs
// Project: Lexichord.Abstractions
// Description: Context information for merge operations.
// =============================================================================
// LOGIC: MergeContext provides all necessary contextual information
//   for the merge operation. This includes the entity being merged,
//   the source document, conflict type, and any additional context
//   data that might influence the merge strategy.
//
// v0.7.6h: Conflict Resolver (CKVS Phase 4c)
// Dependencies: ConflictType (v0.7.6e), KnowledgeEntity (v0.4.5e),
//               Document (v0.4.1c)
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict;

/// <summary>
/// Context information for a merge operation.
/// </summary>
/// <remarks>
/// <para>
/// Provides contextual information to guide merge decisions:
/// </para>
/// <list type="bullet">
///   <item><b>Entity:</b> The entity being merged (optional).</item>
///   <item><b>Document:</b> The source document (optional).</item>
///   <item><b>ConflictType:</b> The type of conflict being resolved.</item>
///   <item><b>UserId:</b> The user initiating the merge (for manual merges).</item>
///   <item><b>ContextData:</b> Additional key-value context data.</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// var context = new MergeContext
/// {
///     Entity = knowledgeEntity,
///     Document = sourceDocument,
///     ConflictType = ConflictType.ValueMismatch,
///     UserId = currentUserId
/// };
/// var result = await merger.MergeAsync(docValue, graphValue, context);
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6h as part of the Conflict Resolver module.
/// </para>
/// </remarks>
public record MergeContext
{
    /// <summary>
    /// The entity involved in the merge.
    /// </summary>
    /// <value>
    /// The KnowledgeEntity being merged.
    /// Null if the merge is not entity-specific.
    /// </value>
    /// <remarks>
    /// LOGIC: Provides entity context for intelligent merging.
    /// Entity type and properties may influence merge strategy.
    /// </remarks>
    public KnowledgeEntity? Entity { get; init; }

    /// <summary>
    /// The source document for the merge.
    /// </summary>
    /// <value>
    /// The Document from which the document value was extracted.
    /// Null if the merge is not document-specific.
    /// </value>
    /// <remarks>
    /// LOGIC: Provides document context for temporal comparisons.
    /// Document metadata may influence merge decisions.
    /// </remarks>
    public Document? Document { get; init; }

    /// <summary>
    /// The type of conflict being resolved.
    /// </summary>
    /// <value>
    /// The conflict type. Null if unknown or not applicable.
    /// </value>
    /// <remarks>
    /// LOGIC: Different conflict types may require different merge strategies.
    /// ValueMismatch conflicts typically support intelligent merging.
    /// </remarks>
    public ConflictType? ConflictType { get; init; }

    /// <summary>
    /// The user initiating the merge.
    /// </summary>
    /// <value>
    /// The user ID for manual merge operations.
    /// Null for automatic merges.
    /// </value>
    /// <remarks>
    /// LOGIC: Used for audit logging and permission checks.
    /// Manual merges require user context for tracking.
    /// </remarks>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Additional context data for the merge.
    /// </summary>
    /// <value>
    /// A dictionary of key-value pairs with additional context.
    /// </value>
    /// <remarks>
    /// LOGIC: Extensibility point for merge strategies.
    /// Can contain strategy-specific configuration or metadata.
    /// Examples: "PreferSource" = "document", "MergeMode" = "append".
    /// </remarks>
    public Dictionary<string, object> ContextData { get; init; } = new();
}
