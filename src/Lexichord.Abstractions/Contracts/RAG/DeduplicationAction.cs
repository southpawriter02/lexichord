// =============================================================================
// File: DeduplicationAction.cs
// Project: Lexichord.Abstractions
// Description: Enum defining actions the deduplication service can take.
// =============================================================================
// VERSION: v0.5.9d (Deduplication Service)
// LOGIC: Defines the outcome categories for chunk processing through the
//   deduplication pipeline. Each action represents a distinct result state.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Actions the deduplication service can take during chunk processing.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9d as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// Each action represents a distinct outcome from the deduplication pipeline:
/// </para>
/// <list type="table">
///   <listheader>
///     <term>Action</term>
///     <description>Scenario</description>
///   </listheader>
///   <item>
///     <term><see cref="StoredAsNew"/></term>
///     <description>No duplicates found or deduplication bypassed (unlicensed).</description>
///   </item>
///   <item>
///     <term><see cref="MergedIntoExisting"/></term>
///     <description>Chunk merged as variant of existing canonical record.</description>
///   </item>
///   <item>
///     <term><see cref="LinkedToExisting"/></term>
///     <description>Complementary content linked but not merged.</description>
///   </item>
///   <item>
///     <term><see cref="FlaggedAsContradiction"/></term>
///     <description>Contradictory content detected and flagged for review.</description>
///   </item>
///   <item>
///     <term><see cref="QueuedForReview"/></term>
///     <description>Ambiguous classification requires manual decision.</description>
///   </item>
///   <item>
///     <term><see cref="SupersededExisting"/></term>
///     <description>New chunk replaces outdated existing content.</description>
///   </item>
/// </list>
/// </remarks>
public enum DeduplicationAction
{
    /// <summary>
    /// Chunk stored as new content (no duplicates found or dedup disabled).
    /// </summary>
    /// <remarks>
    /// This is the default outcome when:
    /// <list type="bullet">
    ///   <item><description>No similar chunks meet the similarity threshold.</description></item>
    ///   <item><description>The user does not have Writer Pro license (dedup bypassed).</description></item>
    ///   <item><description>Classification determines the chunk is distinct from all candidates.</description></item>
    /// </list>
    /// A new canonical record is created for the chunk.
    /// </remarks>
    StoredAsNew = 0,

    /// <summary>
    /// Chunk merged into an existing canonical record.
    /// </summary>
    /// <remarks>
    /// This outcome occurs when:
    /// <list type="bullet">
    ///   <item><description>Relationship classified as <see cref="RelationshipType.Equivalent"/>.</description></item>
    ///   <item><description>Similarity score >= AutoMergeThreshold (default 0.95).</description></item>
    ///   <item><description>Classification confidence >= 0.9 (or LLM confirms if required).</description></item>
    /// </list>
    /// The chunk is stored as a variant of the existing canonical.
    /// </remarks>
    MergedIntoExisting = 1,

    /// <summary>
    /// Chunk linked to related content but not merged.
    /// </summary>
    /// <remarks>
    /// This outcome occurs when relationship is classified as
    /// <see cref="RelationshipType.Complementary"/>. The chunks contain
    /// related information that should be accessible together but represent
    /// distinct facts that should not be merged.
    /// </remarks>
    LinkedToExisting = 2,

    /// <summary>
    /// Chunk flagged as contradicting existing content.
    /// </summary>
    /// <remarks>
    /// This outcome occurs when relationship is classified as
    /// <see cref="RelationshipType.Contradictory"/> and
    /// <see cref="DeduplicationOptions.EnableContradictionDetection"/> is true.
    /// The chunk is stored but flagged for resolution via the contradiction
    /// detection workflow (v0.5.9e).
    /// </remarks>
    FlaggedAsContradiction = 3,

    /// <summary>
    /// Chunk queued for manual review due to ambiguous classification.
    /// </summary>
    /// <remarks>
    /// This outcome occurs when:
    /// <list type="bullet">
    ///   <item><description>Classification confidence is below threshold (0.7).</description></item>
    ///   <item><description><see cref="DeduplicationOptions.EnableManualReviewQueue"/> is true.</description></item>
    /// </list>
    /// The chunk is stored in the pending_reviews table for admin decision.
    /// </remarks>
    QueuedForReview = 4,

    /// <summary>
    /// Chunk superseded existing content (existing archived).
    /// </summary>
    /// <remarks>
    /// This outcome occurs when relationship is classified as
    /// <see cref="RelationshipType.Superseding"/>. The new chunk becomes
    /// the canonical and the old canonical is demoted to a variant with
    /// an archived status.
    /// </remarks>
    SupersededExisting = 5
}
