// =============================================================================
// File: ManualDecisionType.cs
// Project: Lexichord.Abstractions
// Description: Enum defining types of manual deduplication decisions.
// =============================================================================
// VERSION: v0.5.9d (Deduplication Service)
// LOGIC: Defines the possible decisions an admin can make when reviewing
//   a queued deduplication candidate.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Types of manual decisions for queued review items.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9d as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// When chunks are queued for review (due to ambiguous classification), admins
/// use these decision types to resolve the deduplication question:
/// </para>
/// <list type="table">
///   <listheader>
///     <term>Decision</term>
///     <description>Effect</description>
///   </listheader>
///   <item>
///     <term><see cref="Merge"/></term>
///     <description>Merge into suggested canonical; chunk becomes variant.</description>
///   </item>
///   <item>
///     <term><see cref="KeepSeparate"/></term>
///     <description>Create new canonical; chunks are distinct facts.</description>
///   </item>
///   <item>
///     <term><see cref="Link"/></term>
///     <description>Establish relationship without merging.</description>
///   </item>
///   <item>
///     <term><see cref="FlagContradiction"/></term>
///     <description>Flag for contradiction resolution workflow.</description>
///   </item>
///   <item>
///     <term><see cref="Delete"/></term>
///     <description>Remove the queued chunk as unwanted duplicate.</description>
///   </item>
/// </list>
/// </remarks>
public enum ManualDecisionType
{
    /// <summary>
    /// Merge the chunk into the suggested canonical.
    /// </summary>
    /// <remarks>
    /// The queued chunk becomes a variant of the target canonical record.
    /// Use when the admin confirms the chunks represent the same fact.
    /// </remarks>
    Merge = 0,

    /// <summary>
    /// Keep the chunk as distinct/separate.
    /// </summary>
    /// <remarks>
    /// A new canonical record is created for the queued chunk.
    /// Use when the admin determines the chunks are distinct facts.
    /// </remarks>
    KeepSeparate = 1,

    /// <summary>
    /// Link the chunks without merging.
    /// </summary>
    /// <remarks>
    /// Establishes a relationship link between the chunks without
    /// merging them into the same canonical record. Use when chunks
    /// are related but represent different aspects of a topic.
    /// </remarks>
    Link = 2,

    /// <summary>
    /// Flag as contradiction for resolution.
    /// </summary>
    /// <remarks>
    /// Sends the chunk to the contradiction resolution workflow (v0.5.9e).
    /// Use when the admin identifies conflicting information that needs
    /// expert review to determine the correct fact.
    /// </remarks>
    FlagContradiction = 3,

    /// <summary>
    /// Delete the new chunk as unwanted duplicate.
    /// </summary>
    /// <remarks>
    /// Permanently removes the queued chunk from the index.
    /// Use when the chunk adds no value (e.g., exact duplicate with
    /// lower quality source, outdated information).
    /// </remarks>
    Delete = 4
}
