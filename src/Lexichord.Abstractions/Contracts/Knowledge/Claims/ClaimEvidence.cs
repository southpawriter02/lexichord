// =============================================================================
// File: ClaimEvidence.cs
// Project: Lexichord.Abstractions
// Description: Evidence linking a claim to its source text.
// =============================================================================
// LOGIC: Captures the provenance of a claim by linking it to the specific
//   sentence, context, and location in the source document. Enables users
//   to verify claims against their original source text.
//
// v0.5.6e: Claim Data Model (CKVS Phase 2b)
// Dependencies: ClaimExtractionMethod (v0.5.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims;

/// <summary>
/// Evidence linking a claim to its source text in the document.
/// </summary>
/// <remarks>
/// <para>
/// Every claim is extracted from a specific sentence or passage in a document.
/// The <see cref="ClaimEvidence"/> record captures this provenance information,
/// enabling users to:
/// </para>
/// <list type="bullet">
///   <item>Navigate to the source text for verification.</item>
///   <item>Review the extraction context for accuracy.</item>
///   <item>Understand why a particular extraction pattern matched.</item>
/// </list>
/// <para>
/// <b>Extraction Metadata:</b> The <see cref="ExtractionMethod"/> and
/// <see cref="PatternId"/> properties identify how the claim was extracted,
/// supporting debugging and pattern improvement.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6e as part of the Claim Extraction pipeline.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var evidence = new ClaimEvidence
/// {
///     Sentence = "The /users endpoint accepts a limit parameter.",
///     Context = "...The /users endpoint accepts a limit parameter. This controls...",
///     StartOffset = 42,
///     EndOffset = 91,
///     LineNumber = 15,
///     Section = "API Reference > Users",
///     ChunkId = chunkGuid,
///     ExtractionMethod = ClaimExtractionMethod.PatternRule,
///     PatternId = "accepts-parameter-v1"
/// };
/// </code>
/// </example>
public record ClaimEvidence
{
    /// <summary>
    /// The sentence from which the claim was extracted.
    /// </summary>
    /// <value>The exact sentence text containing the claim assertion.</value>
    /// <remarks>
    /// LOGIC: The primary source text for the claim. May be normalized
    /// (whitespace collapsed, newlines removed) from the original.
    /// </remarks>
    public required string Sentence { get; init; }

    /// <summary>
    /// Surrounding context for the sentence.
    /// </summary>
    /// <value>
    /// Additional text before and after the sentence for context.
    /// Typically includes the preceding and following sentences.
    /// </value>
    /// <remarks>
    /// LOGIC: Provides disambiguation context when the sentence alone
    /// is ambiguous. Limited to ~500 characters for storage efficiency.
    /// </remarks>
    public string? Context { get; init; }

    /// <summary>
    /// Start character offset in the document.
    /// </summary>
    /// <value>Zero-based character index where the sentence begins.</value>
    /// <remarks>
    /// LOGIC: Points to the start of <see cref="Sentence"/> within the
    /// full document content. Used for navigation and highlighting.
    /// </remarks>
    public int StartOffset { get; init; }

    /// <summary>
    /// End character offset in the document (exclusive).
    /// </summary>
    /// <value>Zero-based character index where the sentence ends (exclusive).</value>
    /// <remarks>
    /// LOGIC: Points past the last character of <see cref="Sentence"/>.
    /// The length of the sentence is <c>EndOffset - StartOffset</c>.
    /// </remarks>
    public int EndOffset { get; init; }

    /// <summary>
    /// Length of the sentence in characters.
    /// </summary>
    /// <value>Computed as <c>EndOffset - StartOffset</c>.</value>
    public int Length => EndOffset - StartOffset;

    /// <summary>
    /// Line number in the source document (1-based).
    /// </summary>
    /// <value>
    /// The line number where the sentence begins. Defaults to 0 if unknown.
    /// </value>
    /// <remarks>
    /// LOGIC: Used for citation generation and editor navigation.
    /// Line numbers are 1-based to match editor conventions.
    /// </remarks>
    public int LineNumber { get; init; }

    /// <summary>
    /// Document section path (heading hierarchy).
    /// </summary>
    /// <value>
    /// The heading hierarchy path, e.g., "API Reference > Users > List".
    /// Null if the document has no headings.
    /// </value>
    /// <remarks>
    /// LOGIC: Extracted from the markdown heading structure by the
    /// <see cref="IHeadingHierarchyService"/> (v0.5.3c). Provides
    /// semantic context for the claim's location.
    /// </remarks>
    public string? Section { get; init; }

    /// <summary>
    /// ID of the chunk containing this sentence.
    /// </summary>
    /// <value>
    /// The GUID of the parent <see cref="TextChunk"/> (v0.4.3a).
    /// </value>
    /// <remarks>
    /// LOGIC: Links to the RAG infrastructure for vector search.
    /// The chunk's embedding can be used for semantic similarity.
    /// </remarks>
    public Guid ChunkId { get; init; }

    /// <summary>
    /// Method used to extract this claim.
    /// </summary>
    /// <value>The extraction technique that identified this claim.</value>
    /// <remarks>
    /// LOGIC: Used for confidence calibration and debugging.
    /// Different methods have different precision characteristics.
    /// </remarks>
    public ClaimExtractionMethod ExtractionMethod { get; init; }

    /// <summary>
    /// ID of the extraction pattern (if pattern-based).
    /// </summary>
    /// <value>
    /// The pattern identifier for <see cref="ClaimExtractionMethod.PatternRule"/>
    /// extractions. Null for other extraction methods.
    /// </value>
    /// <remarks>
    /// LOGIC: References a pattern definition in the pattern library.
    /// Used to track pattern performance and improve extraction rules.
    /// </remarks>
    public string? PatternId { get; init; }
}
