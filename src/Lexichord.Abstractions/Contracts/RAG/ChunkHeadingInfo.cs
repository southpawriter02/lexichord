// =============================================================================
// File: ChunkHeadingInfo.cs
// Project: Lexichord.Abstractions
// Description: Lightweight record for heading hierarchy queries.
// =============================================================================
// LOGIC: Used by IHeadingHierarchyService to efficiently query heading metadata
//   without loading full chunk content and embeddings. Contains only the fields
//   needed for building heading trees and resolving breadcrumbs.
// =============================================================================
// VERSION: v0.5.3c (Heading Hierarchy)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Lightweight record containing heading metadata for a chunk.
/// </summary>
/// <remarks>
/// <para>
/// This record is optimized for heading hierarchy queries. It includes only the
/// fields needed to build heading trees and resolve breadcrumbs, avoiding the
/// overhead of loading full chunk content and embeddings.
/// </para>
/// <para>
/// <b>Usage:</b> Returned by <see cref="IChunkRepository.GetChunksWithHeadingsAsync"/>
/// and consumed by <see cref="IHeadingHierarchyService"/> for tree construction.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.3c as part of The Context Window feature.
/// </para>
/// </remarks>
/// <param name="Id">The unique identifier for this chunk.</param>
/// <param name="DocumentId">The identifier of the parent document.</param>
/// <param name="ChunkIndex">The zero-based position of this chunk within its parent document.</param>
/// <param name="Heading">The section heading text (never null when returned from GetChunksWithHeadingsAsync).</param>
/// <param name="HeadingLevel">The heading level (1-6 for H1-H6).</param>
public record ChunkHeadingInfo(
    Guid Id,
    Guid DocumentId,
    int ChunkIndex,
    string? Heading,
    int HeadingLevel)
{
    /// <summary>
    /// Gets whether this chunk has a valid heading.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Heading"/> is not null or empty; otherwise, <c>false</c>.
    /// </value>
    public bool HasHeading => !string.IsNullOrEmpty(Heading);
}
