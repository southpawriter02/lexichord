// =============================================================================
// File: CosineSimilarity.cs
// Project: Lexichord.Modules.Knowledge
// Description: Cosine similarity computation for embedding vectors.
// =============================================================================
// LOGIC: Computes the cosine similarity between two float[] embedding vectors
//   using the standard formula: dot(a, b) / (||a|| * ||b||). The result is
//   clamped to [0, 1] for use as a relevance signal (negative similarity
//   is treated as no relevance).
//
// Separated from EntityRelevanceScorer for independent testability.
//
// v0.7.2f: Entity Relevance Scorer (CKVS Phase 4a)
// Dependencies: None (pure computation)
// =============================================================================

namespace Lexichord.Modules.Knowledge.Copilot.Context.Scoring;

/// <summary>
/// Computes cosine similarity between embedding vectors.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="CosineSimilarity"/> class provides a static method for
/// computing the cosine similarity between two float arrays representing
/// embedding vectors. This is the core mathematical operation behind the
/// semantic similarity signal in <see cref="EntityRelevanceScorer"/>.
/// </para>
/// <para>
/// <b>Formula:</b>
/// <c>similarity = dot(a, b) / (||a|| × ||b||)</c>
/// </para>
/// <para>
/// <b>Edge Cases:</b>
/// <list type="bullet">
///   <item>Null or empty vectors return 0.0f.</item>
///   <item>Vectors of different lengths return 0.0f.</item>
///   <item>Zero-magnitude vectors return 0.0f (avoids division by zero).</item>
///   <item>Negative similarity values are clamped to 0.0f.</item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Stateless and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.2f as part of the Entity Relevance Scorer.
/// </para>
/// </remarks>
internal static class CosineSimilarity
{
    /// <summary>
    /// Computes the cosine similarity between two embedding vectors.
    /// </summary>
    /// <param name="a">The first embedding vector.</param>
    /// <param name="b">The second embedding vector.</param>
    /// <returns>
    /// A float in the range [0.0, 1.0] representing the cosine similarity.
    /// Returns 0.0f for null, empty, or mismatched-length vectors.
    /// </returns>
    /// <remarks>
    /// LOGIC: Standard cosine similarity computation:
    /// <list type="number">
    ///   <item>Validate inputs (null, empty, length mismatch).</item>
    ///   <item>Compute dot product and both vector norms in a single pass.</item>
    ///   <item>Guard against zero-magnitude vectors (division by zero).</item>
    ///   <item>Clamp result to [0.0, 1.0] — negative similarity treated as no relevance.</item>
    /// </list>
    /// </remarks>
    public static float Compute(float[] a, float[] b)
    {
        // Guard: null or empty vectors
        if (a is null || b is null || a.Length == 0 || b.Length == 0)
        {
            return 0.0f;
        }

        // Guard: length mismatch
        if (a.Length != b.Length)
        {
            return 0.0f;
        }

        // Single-pass computation of dot product and norms
        float dot = 0f;
        float normA = 0f;
        float normB = 0f;

        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        // Guard: zero-magnitude vector (avoid division by zero)
        float denominator = MathF.Sqrt(normA) * MathF.Sqrt(normB);
        if (denominator == 0f)
        {
            return 0.0f;
        }

        // Compute similarity and clamp to [0, 1]
        float similarity = dot / denominator;
        return Math.Clamp(similarity, 0.0f, 1.0f);
    }
}
