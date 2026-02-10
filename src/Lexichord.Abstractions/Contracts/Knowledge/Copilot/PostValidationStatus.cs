// =============================================================================
// File: PostValidationStatus.cs
// Project: Lexichord.Abstractions
// Description: Status enum for post-generation validation results.
// =============================================================================
// LOGIC: Represents the overall outcome of post-generation validation.
//   Valid = all content verified, ValidWithWarnings = minor issues but usable,
//   Invalid = significant issues (errors or high-confidence hallucinations),
//   Inconclusive = validation could not complete (e.g., service failure).
//
// v0.6.6g: Post-Generation Validator (CKVS Phase 3b)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Status of post-generation validation.
/// </summary>
/// <remarks>
/// <para>
/// Used by <see cref="PostValidationResult.Status"/> to communicate the
/// overall validation outcome. The status determines how the Co-pilot
/// presents the generated content to the user.
/// </para>
/// <para>
/// <b>Status Determination:</b>
/// <list type="bullet">
///   <item><see cref="Valid"/> — no findings or hallucinations.</item>
///   <item><see cref="ValidWithWarnings"/> — non-blocking findings or
///     low-confidence hallucinations present.</item>
///   <item><see cref="Invalid"/> — error-level findings or high-confidence
///     hallucinations (confidence &gt; 0.8).</item>
///   <item><see cref="Inconclusive"/> — validation pipeline failed
///     (e.g., claim extraction error, timeout).</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6g as part of the Post-Generation Validator.
/// </para>
/// </remarks>
public enum PostValidationStatus
{
    /// <summary>
    /// All content verified against the knowledge graph.
    /// </summary>
    /// <remarks>
    /// No validation findings or hallucinations detected. Content is safe
    /// to present to the user without caveats.
    /// </remarks>
    Valid,

    /// <summary>
    /// Minor issues found, content is still usable.
    /// </summary>
    /// <remarks>
    /// Non-blocking findings or low-confidence hallucinations present.
    /// Content can be presented with a warning indicator.
    /// </remarks>
    ValidWithWarnings,

    /// <summary>
    /// Significant issues found in the generated content.
    /// </summary>
    /// <remarks>
    /// Error-level validation findings or high-confidence hallucinations
    /// (confidence &gt; 0.8) detected. Content should be flagged or
    /// corrected before presenting to the user.
    /// </remarks>
    Invalid,

    /// <summary>
    /// Validation could not complete.
    /// </summary>
    /// <remarks>
    /// The validation pipeline encountered an error (e.g., claim extraction
    /// failed, timeout). Content may be presented with a notice that
    /// validation was not performed.
    /// </remarks>
    Inconclusive
}
