// =============================================================================
// File: ClaimExtractionStats.cs
// Project: Lexichord.Abstractions
// Description: Statistics from claim extraction operations.
// =============================================================================
// LOGIC: Captures aggregate metrics from claim extraction, including counts
//   of sentences processed, claims extracted, and extraction method usage.
//   Used for monitoring pipeline performance and quality.
//
// v0.5.6e: Claim Data Model (CKVS Phase 2b)
// Dependencies: None (pure record)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims;

/// <summary>
/// Statistics from a claim extraction operation.
/// </summary>
/// <remarks>
/// <para>
/// Captures aggregate metrics from processing a document or chunk, including:
/// </para>
/// <list type="bullet">
///   <item>Processing counts (sentences, claims).</item>
///   <item>Method-specific extraction counts.</item>
///   <item>Quality metrics (average confidence).</item>
///   <item>Deduplication statistics.</item>
/// </list>
/// <para>
/// <b>Usage:</b> Returned as part of <see cref="ClaimExtractionResult"/>
/// after processing. Used for dashboard displays and quality monitoring.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6e as part of the Claim Extraction pipeline.
/// </para>
/// </remarks>
public record ClaimExtractionStats
{
    /// <summary>
    /// Total sentences processed.
    /// </summary>
    /// <value>The number of sentences analyzed during extraction.</value>
    public int TotalSentences { get; init; }

    /// <summary>
    /// Sentences that yielded at least one claim.
    /// </summary>
    /// <value>The count of sentences containing extractable claims.</value>
    /// <remarks>
    /// LOGIC: A sentence may contain zero, one, or multiple claims.
    /// This metric indicates extraction coverage.
    /// </remarks>
    public int SentencesWithClaims { get; init; }

    /// <summary>
    /// Total claims extracted.
    /// </summary>
    /// <value>The final count of deduplicated claims.</value>
    public int TotalClaims { get; init; }

    /// <summary>
    /// Claims extracted via pattern matching.
    /// </summary>
    /// <value>Count of claims from <see cref="ClaimExtractionMethod.PatternRule"/>.</value>
    public int PatternMatches { get; init; }

    /// <summary>
    /// Claims extracted via semantic role labeling.
    /// </summary>
    /// <value>Count of claims from <see cref="ClaimExtractionMethod.SemanticRoleLabeling"/>.</value>
    public int SRLExtractions { get; init; }

    /// <summary>
    /// Claims extracted via dependency parsing.
    /// </summary>
    /// <value>Count of claims from <see cref="ClaimExtractionMethod.DependencyParsing"/>.</value>
    public int DependencyExtractions { get; init; }

    /// <summary>
    /// Duplicate claims removed during deduplication.
    /// </summary>
    /// <value>
    /// The count of claims discarded as duplicates of existing claims.
    /// </value>
    /// <remarks>
    /// LOGIC: Claims are deduplicated by comparing their canonical forms.
    /// A high duplicate count may indicate extraction overlap.
    /// </remarks>
    public int DuplicatesRemoved { get; init; }

    /// <summary>
    /// Average confidence score across all extracted claims.
    /// </summary>
    /// <value>
    /// A score from 0.0 to 1.0 representing overall extraction quality.
    /// </value>
    public float AverageConfidence { get; init; }

    /// <summary>
    /// Breakdown of claims by predicate type.
    /// </summary>
    /// <value>
    /// A dictionary mapping predicate strings to claim counts.
    /// Example: { "ACCEPTS": 15, "RETURNS": 8, "HAS_PROPERTY": 23 }
    /// </value>
    /// <remarks>
    /// LOGIC: Useful for understanding the distribution of claim types
    /// in a document. Predicates should match <see cref="ClaimPredicate"/> constants.
    /// </remarks>
    public IReadOnlyDictionary<string, int> ClaimsByPredicate { get; init; }
        = new Dictionary<string, int>();
}
