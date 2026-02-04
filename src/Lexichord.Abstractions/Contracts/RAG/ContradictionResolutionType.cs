// =============================================================================
// File: ContradictionResolutionType.cs
// Project: Lexichord.Abstractions
// Description: Enumeration of resolution types for contradictions.
// =============================================================================
// VERSION: v0.5.9e (Contradiction Detection & Resolution)
// LOGIC: Defines the available resolution strategies when an admin resolves
//   a detected contradiction between chunks. Each type implies different
//   actions on the canonical records and chunk visibility.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Defines the resolution strategies for resolving a contradiction.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9e as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// Each resolution type has specific implications:
/// </para>
/// <list type="table">
///   <listheader>
///     <term>Type</term>
///     <description>Action</description>
///   </listheader>
///   <item>
///     <term><see cref="KeepOlder"/></term>
///     <description>Keep the earlier chunk as canonical; archive the later chunk.</description>
///   </item>
///   <item>
///     <term><see cref="KeepNewer"/></term>
///     <description>Keep the later chunk as canonical; archive the earlier chunk.</description>
///   </item>
///   <item>
///     <term><see cref="KeepBoth"/></term>
///     <description>Keep both chunks as independent canonicals; mark as acknowledged.</description>
///   </item>
///   <item>
///     <term><see cref="CreateSynthesis"/></term>
///     <description>Create a new synthesized chunk from both sources.</description>
///   </item>
///   <item>
///     <term><see cref="DeleteBoth"/></term>
///     <description>Remove both chunks; contradicting info is invalid.</description>
///   </item>
/// </list>
/// </remarks>
public enum ContradictionResolutionType
{
    /// <summary>
    /// Keep the older (earlier indexed) chunk as authoritative.
    /// </summary>
    /// <remarks>
    /// The chunk with the earlier <c>IndexedAt</c> timestamp is retained as the
    /// canonical representation. The newer chunk is archived or marked as superseded.
    /// Use when the original source is authoritative and the newer source is incorrect.
    /// </remarks>
    KeepOlder = 0,

    /// <summary>
    /// Keep the newer (later indexed) chunk as authoritative.
    /// </summary>
    /// <remarks>
    /// The chunk with the later <c>IndexedAt</c> timestamp is retained as the
    /// canonical representation. The older chunk is archived or marked as superseded.
    /// Use when the newer source corrects outdated information.
    /// </remarks>
    KeepNewer = 1,

    /// <summary>
    /// Keep both chunks as valid, independent representations.
    /// </summary>
    /// <remarks>
    /// Both chunks are retained as separate canonicals. The contradiction flag
    /// is cleared but a link is maintained to indicate they contain intentionally
    /// different perspectives. Use when both representations are valid in their
    /// respective contexts (e.g., different time periods, different audiences).
    /// </remarks>
    KeepBoth = 2,

    /// <summary>
    /// Create a new synthesized chunk combining both sources.
    /// </summary>
    /// <remarks>
    /// A new chunk is created that reconciles the conflicting information.
    /// Both original chunks are archived as variants of the new synthesis.
    /// Use when the contradiction can be resolved by creating a unified statement.
    /// Requires providing the synthesized content in the resolution.
    /// </remarks>
    CreateSynthesis = 3,

    /// <summary>
    /// Delete both conflicting chunks.
    /// </summary>
    /// <remarks>
    /// Both chunks are removed from the index entirely. Their canonical records
    /// are deleted. Use when both sources are determined to be invalid or when
    /// the conflicting information should not be in the knowledge base at all.
    /// </remarks>
    DeleteBoth = 4
}
