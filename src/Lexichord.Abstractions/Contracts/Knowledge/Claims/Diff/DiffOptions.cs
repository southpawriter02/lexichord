// =============================================================================
// File: DiffOptions.cs
// Project: Lexichord.Abstractions
// Description: Options for claim comparison operations.
// =============================================================================
// LOGIC: Configures behavior of the ClaimDiffService, including semantic
//   matching thresholds, confidence change tolerance, and grouping options.
//
// v0.5.6i: Claim Diff Service (Knowledge Graph Change Tracking)
// Dependencies: None (pure configuration)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims.Diff;

/// <summary>
/// Options for claim diff operations.
/// </summary>
/// <remarks>
/// <para>
/// Configures how claims are compared between versions. Options control:
/// </para>
/// <list type="bullet">
///   <item><b>Semantic Matching:</b> Whether to use fuzzy matching when
///     claims have different IDs but similar content.</item>
///   <item><b>Thresholds:</b> Minimum similarity for matches and
///     minimum confidence change to report.</item>
///   <item><b>Grouping:</b> Whether to group related changes together.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.5.6i as part of the Claim Diff Service.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new DiffOptions
/// {
///     UseSemanticMatching = true,
///     SemanticMatchThreshold = 0.8f,
///     IgnoreConfidenceChangeBelow = 0.05f,
///     GroupRelatedChanges = true
/// };
///
/// var result = diffService.Diff(oldClaims, newClaims, options);
/// </code>
/// </example>
public record DiffOptions
{
    /// <summary>
    /// Whether to use semantic matching for fuzzy comparison.
    /// </summary>
    /// <value>
    /// True to find semantically similar claims even with different IDs.
    /// Defaults to true.
    /// </value>
    /// <remarks>
    /// LOGIC: When enabled, the diff service will attempt to match claims
    /// by content similarity if no exact ID match is found. This helps
    /// detect claims that were re-extracted with new IDs.
    /// </remarks>
    public bool UseSemanticMatching { get; init; } = true;

    /// <summary>
    /// Minimum similarity threshold for semantic matches (0.0-1.0).
    /// </summary>
    /// <value>
    /// A score from 0.0 (no match) to 1.0 (exact match).
    /// Defaults to 0.8.
    /// </value>
    /// <remarks>
    /// LOGIC: Claims with similarity above this threshold are considered
    /// potential matches. Higher values reduce false positives.
    /// </remarks>
    public float SemanticMatchThreshold { get; init; } = 0.8f;

    /// <summary>
    /// Ignore confidence changes below this threshold.
    /// </summary>
    /// <value>
    /// A difference from 0.0 to 1.0. Defaults to 0.05 (5%).
    /// </value>
    /// <remarks>
    /// LOGIC: Small confidence fluctuations are often noise. This filter
    /// prevents reporting insignificant changes.
    /// </remarks>
    public float IgnoreConfidenceChangeBelow { get; init; } = 0.05f;

    /// <summary>
    /// Whether to consider validation status changes.
    /// </summary>
    /// <value>True to track validation status changes. Defaults to true.</value>
    public bool TrackValidationChanges { get; init; } = true;

    /// <summary>
    /// Whether to include evidence text in changes.
    /// </summary>
    /// <value>True to include source text excerpts. Defaults to true.</value>
    public bool IncludeEvidence { get; init; } = true;

    /// <summary>
    /// Whether to group related changes together.
    /// </summary>
    /// <value>
    /// True to group changes by subject entity. Defaults to true.
    /// </value>
    /// <remarks>
    /// LOGIC: Grouping helps users understand related changes at a glance.
    /// Changes are grouped by subject entity for easier review.
    /// </remarks>
    public bool GroupRelatedChanges { get; init; } = true;

    /// <summary>
    /// Default options with recommended settings.
    /// </summary>
    public static DiffOptions Default { get; } = new();

    /// <summary>
    /// Strict options with no semantic matching.
    /// </summary>
    public static DiffOptions Strict { get; } = new()
    {
        UseSemanticMatching = false,
        IgnoreConfidenceChangeBelow = 0.0f
    };

    /// <summary>
    /// Lenient options with lower match threshold.
    /// </summary>
    public static DiffOptions Lenient { get; } = new()
    {
        SemanticMatchThreshold = 0.6f,
        IgnoreConfidenceChangeBelow = 0.1f
    };
}
