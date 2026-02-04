// =============================================================================
// File: ClassificationMethod.cs
// Project: Lexichord.Abstractions
// Description: Enumeration of how a relationship classification was determined.
// =============================================================================
// VERSION: v0.5.9b (Relationship Classification)
// LOGIC: Tracks classification method for observability and debugging.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Specifies how a relationship classification was determined.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9b as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// This enum enables observability and debugging by tracking whether
/// classifications came from the fast-path rule engine, the LLM, or cache.
/// </para>
/// </remarks>
public enum ClassificationMethod
{
    /// <summary>
    /// Classification was determined by rule-based logic.
    /// </summary>
    /// <remarks>
    /// Fast-path classification for high-confidence cases (similarity >= 0.95).
    /// Target latency: &lt; 5ms.
    /// </remarks>
    RuleBased = 0,

    /// <summary>
    /// Classification was determined by LLM analysis.
    /// </summary>
    /// <remarks>
    /// Used for ambiguous cases where rule-based classification is insufficient.
    /// Higher latency but more nuanced understanding of semantic relationships.
    /// </remarks>
    LlmBased = 1,

    /// <summary>
    /// Classification was retrieved from cache.
    /// </summary>
    /// <remarks>
    /// Cache key is derived from sorted chunk IDs to ensure hit regardless
    /// of pair ordering. Default cache duration is 1 hour.
    /// </remarks>
    Cached = 2
}
