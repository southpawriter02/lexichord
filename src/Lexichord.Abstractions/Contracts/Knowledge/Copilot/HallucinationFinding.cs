// =============================================================================
// File: HallucinationFinding.cs
// Project: Lexichord.Abstractions
// Description: Represents a potential hallucination in LLM-generated content.
// =============================================================================
// LOGIC: Each finding captures the unsupported claim text, its location in
//   the content, a confidence score, the hallucination type, and an optional
//   suggested correction. Immutable record for safe aggregation.
//
// v0.6.6g: Post-Generation Validator (CKVS Phase 3b)
// Dependencies: TextSpan (v0.4.6e), HallucinationType (v0.6.6g)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Represents a potential hallucination detected in generated content.
/// </summary>
/// <remarks>
/// <para>
/// A hallucination is content that is not supported by the knowledge context.
/// The <see cref="IHallucinationDetector"/> produces these findings by
/// comparing entity mentions and property values against the
/// <see cref="KnowledgeContext"/>.
/// </para>
/// <para>
/// <b>Confidence Interpretation:</b>
/// <list type="bullet">
///   <item>&gt; 0.8 — high confidence, treated as error by the validator.</item>
///   <item>0.6–0.8 — moderate confidence, treated as warning.</item>
///   <item>&lt; 0.6 — low confidence, may be a false positive.</item>
/// </list>
/// </para>
/// <para>
/// <b>Immutability:</b> This is an immutable record, safe for concurrent
/// aggregation from parallel detection steps.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6g as part of the Post-Generation Validator.
/// </para>
/// </remarks>
public record HallucinationFinding
{
    /// <summary>
    /// The unsupported claim or entity mention.
    /// </summary>
    /// <value>
    /// The text fragment that could not be verified against the
    /// knowledge context.
    /// </value>
    public required string ClaimText { get; init; }

    /// <summary>
    /// Location of the hallucination in the generated content.
    /// </summary>
    /// <value>
    /// A <see cref="TextSpan"/> identifying the character range, or
    /// <c>null</c> if the location could not be determined.
    /// </value>
    public TextSpan? Location { get; init; }

    /// <summary>
    /// Confidence that this is a hallucination (0–1).
    /// </summary>
    /// <value>
    /// A score from 0.0 (low confidence) to 1.0 (certain hallucination).
    /// </value>
    /// <remarks>
    /// LOGIC: For <see cref="HallucinationType.UnknownEntity"/>, confidence
    /// is computed as <c>0.6 + (mentionConfidence - 0.7)</c>. For
    /// <see cref="HallucinationType.ContradictoryValue"/>, a fixed 0.8.
    /// </remarks>
    public float Confidence { get; init; }

    /// <summary>
    /// The type of hallucination.
    /// </summary>
    /// <value>One of the <see cref="HallucinationType"/> values.</value>
    public HallucinationType Type { get; init; }

    /// <summary>
    /// Suggested correction for the hallucination.
    /// </summary>
    /// <value>
    /// A replacement string, or <c>null</c> if no correction can be
    /// suggested. For <see cref="HallucinationType.UnknownEntity"/>,
    /// this is the closest entity name by Levenshtein distance.
    /// </value>
    public string? SuggestedCorrection { get; init; }
}
