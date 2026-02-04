// =============================================================================
// File: ClaimExtractionResult.cs
// Project: Lexichord.Abstractions
// Description: Result container for claim extraction operations.
// =============================================================================
// LOGIC: Encapsulates the output of a claim extraction operation, including
//   all extracted claims, processing statistics, and any failed sentences.
//   Provides an Empty static property for failed operations.
//
// v0.5.6e: Claim Data Model (CKVS Phase 2b)
// Dependencies: Claim, ClaimExtractionStats (v0.5.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims;

/// <summary>
/// Result of a claim extraction operation.
/// </summary>
/// <remarks>
/// <para>
/// Encapsulates the output of processing a document or chunk for claim
/// extraction. Includes:
/// </para>
/// <list type="bullet">
///   <item>All extracted <see cref="Claim"/> instances.</item>
///   <item>Aggregate <see cref="ClaimExtractionStats"/>.</item>
///   <item>Any sentences that failed to process.</item>
///   <item>Processing duration for performance monitoring.</item>
/// </list>
/// <para>
/// <b>Empty Results:</b> Use the <see cref="Empty"/> static property for
/// failed operations or documents with no extractable claims.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6e as part of the Claim Extraction pipeline.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Successful extraction
/// var result = new ClaimExtractionResult
/// {
///     Claims = extractedClaims,
///     DocumentId = documentGuid,
///     Duration = stopwatch.Elapsed,
///     Stats = computedStats
/// };
///
/// // Empty result for failed operation
/// var emptyResult = ClaimExtractionResult.Empty;
/// </code>
/// </example>
public record ClaimExtractionResult
{
    /// <summary>
    /// The extracted claims.
    /// </summary>
    /// <value>A readonly list of all claims extracted from the document.</value>
    public IReadOnlyList<Claim> Claims { get; init; } = Array.Empty<Claim>();

    /// <summary>
    /// ID of the source document.
    /// </summary>
    /// <value>The GUID of the document that was processed.</value>
    public Guid DocumentId { get; init; }

    /// <summary>
    /// Processing duration.
    /// </summary>
    /// <value>The time taken to extract claims from the document.</value>
    /// <remarks>
    /// LOGIC: Used for performance monitoring and identifying slow documents.
    /// </remarks>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Extraction statistics.
    /// </summary>
    /// <value>Aggregate metrics from the extraction operation.</value>
    public ClaimExtractionStats Stats { get; init; } = new();

    /// <summary>
    /// Sentences that failed to process.
    /// </summary>
    /// <value>
    /// A dictionary mapping sentence indices to error messages.
    /// Empty if all sentences processed successfully.
    /// </value>
    /// <remarks>
    /// LOGIC: Captures partial failures without aborting the entire operation.
    /// Failed sentences can be retried or manually reviewed.
    /// </remarks>
    public IReadOnlyDictionary<int, string> FailedSentences { get; init; }
        = new Dictionary<int, string>();

    /// <summary>
    /// Whether any claims were extracted.
    /// </summary>
    /// <value>True if <see cref="Claims"/> contains at least one claim.</value>
    public bool HasClaims => Claims.Count > 0;

    /// <summary>
    /// Whether any sentences failed to process.
    /// </summary>
    /// <value>True if <see cref="FailedSentences"/> is not empty.</value>
    public bool HasFailures => FailedSentences.Count > 0;

    /// <summary>
    /// An empty extraction result for failed operations.
    /// </summary>
    /// <value>A static instance with no claims and default statistics.</value>
    /// <remarks>
    /// LOGIC: Use for operations that fail completely or documents with
    /// no extractable claims. Avoids null returns.
    /// </remarks>
    public static ClaimExtractionResult Empty { get; } = new();
}
