// =============================================================================
// File: SiblingCacheKey.cs
// Project: Lexichord.Abstractions
// Description: Immutable cache key for sibling chunk queries.
// Version: v0.5.3b
// =============================================================================
// LOGIC: Value-based equality for efficient dictionary lookup.
//   - Uses record struct for stack allocation and efficient equality.
//   - Combines all query parameters to uniquely identify a sibling request.
//   - DocumentId identifies the source document.
//   - CenterIndex, BeforeCount, AfterCount define the query range.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Immutable cache key for sibling chunk queries.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SiblingCacheKey"/> is a value type used as the key in the
/// <c>SiblingCache</c> dictionary. It uniquely identifies a sibling chunk query
/// based on the document, center chunk index, and the before/after counts.
/// </para>
/// <para>
/// <b>Equality:</b> As a <c>record struct</c>, equality is value-based, comparing
/// all four fields. Two keys are equal if and only if all four components match.
/// </para>
/// <para>
/// <b>Hash Code:</b> The hash code is computed from all four fields, ensuring
/// consistent dictionary behavior.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.3b as part of Sibling Chunk Retrieval caching.
/// </para>
/// </remarks>
/// <param name="DocumentId">The document containing the chunks.</param>
/// <param name="CenterIndex">The chunk index at the center of the query.</param>
/// <param name="BeforeCount">Number of chunks to retrieve before center (0-5).</param>
/// <param name="AfterCount">Number of chunks to retrieve after center (0-5).</param>
/// <example>
/// <code>
/// // Create a cache key for a sibling query
/// var key = new SiblingCacheKey(documentId, centerIndex: 5, beforeCount: 1, afterCount: 1);
///
/// // Two keys with same values are equal
/// var key2 = new SiblingCacheKey(documentId, centerIndex: 5, beforeCount: 1, afterCount: 1);
/// Debug.Assert(key == key2); // true
/// </code>
/// </example>
public readonly record struct SiblingCacheKey(
    Guid DocumentId,
    int CenterIndex,
    int BeforeCount,
    int AfterCount);
