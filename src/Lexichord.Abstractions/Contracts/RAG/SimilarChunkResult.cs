// =============================================================================
// File: SimilarChunkResult.cs
// Project: Lexichord.Abstractions
// Description: Result record for similarity detection queries.
// =============================================================================
// VERSION: v0.5.9a (Similarity Detection Infrastructure)
// LOGIC: Represents a match found during similarity detection between chunks.
//   - Contains both the source chunk identifier and matched chunk details.
//   - SimilarityScore is a cosine similarity value in [0.0, 1.0] range.
//   - Includes matched chunk content and document path for deduplication decisions.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Represents a similar chunk found during deduplication detection.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9a as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// This record contains all information needed to evaluate whether two chunks
/// are duplicates and to present the match to users for review. The similarity
/// score uses cosine similarity where 1.0 indicates identical vectors.
/// </para>
/// <para>
/// <b>Threshold Guidance:</b>
/// </para>
/// <list type="bullet">
///   <item><description>0.95+: Near-certain duplicates, safe for automatic merging</description></item>
///   <item><description>0.90-0.95: Likely duplicates, may warrant human review</description></item>
///   <item><description>0.85-0.90: Related content, possibly paraphrased</description></item>
///   <item><description>&lt;0.85: Semantically similar but distinct content</description></item>
/// </list>
/// </remarks>
/// <param name="SourceChunkId">The ID of the chunk being checked for duplicates.</param>
/// <param name="MatchedChunkId">The ID of the similar chunk found in the index.</param>
/// <param name="SimilarityScore">
/// Cosine similarity score between the two chunk embeddings.
/// Range: [0.0, 1.0] where 1.0 indicates identical vectors.
/// </param>
/// <param name="MatchedChunkContent">
/// The text content of the matched chunk for human review.
/// May be null if content was not retrieved (performance optimization).
/// </param>
/// <param name="MatchedDocumentPath">
/// The file path of the document containing the matched chunk.
/// Used to identify cross-document duplicates vs. same-document repeats.
/// </param>
/// <param name="MatchedChunkIndex">
/// The zero-based index of the matched chunk within its document.
/// Useful for determining if duplicates are adjacent (same section repeated).
/// </param>
public sealed record SimilarChunkResult(
    Guid SourceChunkId,
    Guid MatchedChunkId,
    double SimilarityScore,
    string? MatchedChunkContent,
    string? MatchedDocumentPath,
    int MatchedChunkIndex)
{
    /// <summary>
    /// Indicates whether the matched chunk is from a different document than the source.
    /// </summary>
    /// <remarks>
    /// Cross-document duplicates are typically more actionable for knowledge consolidation
    /// than same-document repeats (which may be intentional emphasis or section summaries).
    /// </remarks>
    public bool IsCrossDocumentMatch { get; init; }
}
