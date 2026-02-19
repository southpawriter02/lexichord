// =============================================================================
// File: ConflictDetail.cs
// Project: Lexichord.Abstractions
// Description: Detailed information about a detected conflict.
// =============================================================================
// LOGIC: ConflictDetail provides comprehensive information about a
//   conflict including the entity involved, specific field in conflict,
//   values from both sources, timestamps, and suggested resolution.
//   This extends the simpler SyncConflict with entity-level context.
//
// v0.7.6h: Conflict Resolver (CKVS Phase 4c)
// Dependencies: ConflictType, ConflictSeverity, ConflictResolutionStrategy (v0.7.6e),
//               KnowledgeEntity (v0.4.5e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict;

/// <summary>
/// Detailed information about a conflict between document and graph.
/// </summary>
/// <remarks>
/// <para>
/// Provides comprehensive conflict details including:
/// </para>
/// <list type="bullet">
///   <item><b>Entity Context:</b> The entity involved in the conflict.</item>
///   <item><b>Field Identification:</b> The specific property/field in conflict.</item>
///   <item><b>Values:</b> Both document and graph values.</item>
///   <item><b>Timestamps:</b> When each source was last modified.</item>
///   <item><b>Resolution Suggestion:</b> Recommended strategy with confidence.</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// var detail = new ConflictDetail
/// {
///     ConflictId = Guid.NewGuid(),
///     Entity = knowledgeEntity,
///     ConflictField = "Description",
///     DocumentValue = "New value",
///     GraphValue = "Old value",
///     Type = ConflictType.ValueMismatch,
///     Severity = ConflictSeverity.Medium,
///     DetectedAt = DateTimeOffset.UtcNow,
///     SuggestedStrategy = ConflictResolutionStrategy.Merge,
///     ResolutionConfidence = 0.85f
/// };
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6h as part of the Conflict Resolver module.
/// </para>
/// </remarks>
public record ConflictDetail
{
    /// <summary>
    /// Unique identifier for this conflict.
    /// </summary>
    /// <value>A GUID uniquely identifying this conflict instance.</value>
    /// <remarks>
    /// LOGIC: Used to track and reference individual conflicts.
    /// Generated when the conflict is detected.
    /// </remarks>
    public required Guid ConflictId { get; init; }

    /// <summary>
    /// The entity involved in the conflict.
    /// </summary>
    /// <value>The KnowledgeEntity that has the conflicting property.</value>
    /// <remarks>
    /// LOGIC: Provides full entity context for the conflict.
    /// Usually the graph entity, as it represents the existing state.
    /// </remarks>
    public required KnowledgeEntity Entity { get; init; }

    /// <summary>
    /// The specific field/property in conflict.
    /// </summary>
    /// <value>
    /// The name of the property that differs between sources.
    /// May be "Entity" for structural conflicts (entity presence).
    /// </value>
    /// <remarks>
    /// LOGIC: Identifies which property on the entity is in conflict.
    /// Used to construct UI displays and resolution targets.
    /// </remarks>
    public required string ConflictField { get; init; }

    /// <summary>
    /// The value from the document.
    /// </summary>
    /// <value>
    /// The property value as extracted from the document.
    /// Boxed as object to support any value type.
    /// </value>
    /// <remarks>
    /// LOGIC: Represents the "proposed" or "new" value from the document.
    /// May be special string like "(deleted from document)" for structural conflicts.
    /// </remarks>
    public required object DocumentValue { get; init; }

    /// <summary>
    /// The value from the knowledge graph.
    /// </summary>
    /// <value>
    /// The property value as stored in the graph.
    /// Boxed as object to support any value type.
    /// </value>
    /// <remarks>
    /// LOGIC: Represents the "existing" or "current" value from the graph.
    /// May be the full entity for structural conflicts.
    /// </remarks>
    public required object GraphValue { get; init; }

    /// <summary>
    /// Type of the conflict.
    /// </summary>
    /// <value>The nature of the divergence. See <see cref="ConflictType"/>.</value>
    /// <remarks>
    /// LOGIC: Determines applicable resolution strategies.
    /// ValueMismatch can use merge; MissingInGraph suggests creation.
    /// </remarks>
    public required ConflictType Type { get; init; }

    /// <summary>
    /// Severity of the conflict.
    /// </summary>
    /// <value>How urgently the conflict needs attention.</value>
    /// <remarks>
    /// LOGIC: Determines auto-resolution eligibility.
    /// Low severity may be auto-resolved; High requires manual review.
    /// </remarks>
    public ConflictSeverity Severity { get; init; } = ConflictSeverity.Medium;

    /// <summary>
    /// Timestamp when the conflict was detected.
    /// </summary>
    /// <value>UTC timestamp of conflict detection.</value>
    /// <remarks>
    /// LOGIC: Used for conflict history and staleness checks.
    /// </remarks>
    public required DateTimeOffset DetectedAt { get; init; }

    /// <summary>
    /// Timestamp when the document was last modified.
    /// </summary>
    /// <value>
    /// The document's last modification timestamp.
    /// Null if not available.
    /// </value>
    /// <remarks>
    /// LOGIC: Used for MostRecent merge strategy.
    /// Compared against GraphModifiedAt to determine recency.
    /// </remarks>
    public DateTimeOffset? DocumentModifiedAt { get; init; }

    /// <summary>
    /// Timestamp when the graph entity was last modified.
    /// </summary>
    /// <value>
    /// The entity's last modification timestamp from the graph.
    /// Null if not available.
    /// </value>
    /// <remarks>
    /// LOGIC: Used for MostRecent merge strategy.
    /// Compared against DocumentModifiedAt to determine recency.
    /// </remarks>
    public DateTimeOffset? GraphModifiedAt { get; init; }

    /// <summary>
    /// Suggested resolution strategy for this conflict.
    /// </summary>
    /// <value>
    /// The recommended <see cref="ConflictResolutionStrategy"/>.
    /// Null if no suggestion is available.
    /// </value>
    /// <remarks>
    /// LOGIC: Auto-generated based on conflict type, severity, and confidence.
    /// High-confidence conflicts suggest Merge; others suggest Manual.
    /// </remarks>
    public ConflictResolutionStrategy? SuggestedStrategy { get; init; }

    /// <summary>
    /// Confidence in the suggested resolution.
    /// </summary>
    /// <value>
    /// A value between 0.0 and 1.0 indicating suggestion confidence.
    /// Higher values indicate more reliable suggestions.
    /// </value>
    /// <remarks>
    /// LOGIC: Used to determine if auto-resolution is appropriate.
    /// Conflicts with ResolutionConfidence ≥ 0.8 may be auto-resolved.
    /// </remarks>
    public float ResolutionConfidence { get; init; }
}
