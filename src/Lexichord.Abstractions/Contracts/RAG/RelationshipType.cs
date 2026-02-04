// =============================================================================
// File: RelationshipType.cs
// Project: Lexichord.Abstractions
// Description: Enumeration of semantic relationship types between chunks.
// =============================================================================
// VERSION: v0.5.9b (Relationship Classification)
// LOGIC: Defines the possible semantic relationships that can exist between
//   similar chunks, guiding the deduplication strategy (merge, link, flag).
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Defines the semantic relationship types between two similar chunks.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9b as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// Each relationship type implies a different action during deduplication:
/// </para>
/// <list type="table">
///   <listheader>
///     <term>Type</term>
///     <description>Action</description>
///   </listheader>
///   <item>
///     <term><see cref="Equivalent"/></term>
///     <description>Merge chunks, keeping only one copy.</description>
///   </item>
///   <item>
///     <term><see cref="Complementary"/></term>
///     <description>Link chunks together as related content.</description>
///   </item>
///   <item>
///     <term><see cref="Contradictory"/></term>
///     <description>Flag for user review due to conflicting information.</description>
///   </item>
///   <item>
///     <term><see cref="Superseding"/></term>
///     <description>Keep newer chunk, archive or remove older.</description>
///   </item>
///   <item>
///     <term><see cref="Subset"/></term>
///     <description>Keep superset chunk, link subset as partial.</description>
///   </item>
///   <item>
///     <term><see cref="Distinct"/></term>
///     <description>Keep both chunks; similarity was superficial.</description>
///   </item>
/// </list>
/// </remarks>
public enum RelationshipType
{
    /// <summary>
    /// The relationship could not be determined.
    /// </summary>
    /// <remarks>
    /// Returned when classification fails, license is insufficient,
    /// or confidence is too low to make a determination.
    /// </remarks>
    Unknown = 0,

    /// <summary>
    /// The chunks contain semantically equivalent information.
    /// </summary>
    /// <remarks>
    /// Implies the chunks can be merged, keeping only one copy.
    /// Typically detected when similarity is >= 0.95 and content
    /// structure is nearly identical.
    /// </remarks>
    Equivalent = 1,

    /// <summary>
    /// The chunks contain complementary, non-overlapping information.
    /// </summary>
    /// <remarks>
    /// Implies the chunks should be linked together as related content.
    /// Common for adjacent chunks from the same document or related
    /// sections across documents.
    /// </remarks>
    Complementary = 2,

    /// <summary>
    /// The chunks contain contradictory information.
    /// </summary>
    /// <remarks>
    /// Implies the chunks should be flagged for user review.
    /// Critical for maintaining knowledge base consistency.
    /// </remarks>
    Contradictory = 3,

    /// <summary>
    /// One chunk supersedes the other (typically newer replaces older).
    /// </summary>
    /// <remarks>
    /// Implies keeping the newer chunk and archiving or removing
    /// the older one. Detected based on document timestamps or
    /// explicit version indicators.
    /// </remarks>
    Superseding = 4,

    /// <summary>
    /// One chunk is a subset of the other.
    /// </summary>
    /// <remarks>
    /// Implies keeping the superset chunk and linking the subset
    /// as partial content. Detected when one chunk's content is
    /// fully contained within the other.
    /// </remarks>
    Subset = 5,

    /// <summary>
    /// The chunks are semantically distinct despite high similarity.
    /// </summary>
    /// <remarks>
    /// Implies keeping both chunks; the vector similarity was
    /// superficial or based on common vocabulary rather than
    /// semantic equivalence.
    /// </remarks>
    Distinct = 6
}
