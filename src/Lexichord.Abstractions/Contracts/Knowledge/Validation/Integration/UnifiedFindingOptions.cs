// =============================================================================
// File: UnifiedFindingOptions.cs
// Project: Lexichord.Abstractions
// Description: Options for unified finding retrieval.
// =============================================================================
// LOGIC: Controls which sources to include, minimum severity threshold,
//   category filter, and maximum result count. Delegates to
//   ValidationOptions for the validation engine pass.
//
// SPEC ADAPTATION:
//   - LinterOptions? removed (IStyleEngine.AnalyzeAsync has no options param)
//
// v0.6.5j: Linter Integration (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;

/// <summary>
/// Configuration options for <see cref="ILinterIntegration.GetUnifiedFindingsAsync"/>.
/// </summary>
/// <remarks>
/// <para>
/// Controls filtering, source selection, and limits for the combined
/// validation + linter pass. Immutable record with sensible defaults.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5j as part of the Linter Integration.
/// </para>
/// </remarks>
public record UnifiedFindingOptions
{
    /// <summary>
    /// Whether to include findings from the CKVS Validation Engine.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool IncludeValidation { get; init; } = true;

    /// <summary>
    /// Whether to include findings from the Lexichord style linter.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool IncludeLinter { get; init; } = true;

    /// <summary>
    /// Minimum severity to include. Findings below this threshold are excluded.
    /// Defaults to <see cref="UnifiedSeverity.Hint"/> (include everything).
    /// </summary>
    public UnifiedSeverity MinSeverity { get; init; } = UnifiedSeverity.Hint;

    /// <summary>
    /// Categories to include. <c>null</c> means all categories.
    /// </summary>
    public IReadOnlySet<FindingCategory>? Categories { get; init; }

    /// <summary>
    /// Maximum number of findings to return.
    /// Defaults to 200.
    /// </summary>
    public int MaxFindings { get; init; } = 200;

    /// <summary>
    /// Options for the validation engine pass (null uses defaults).
    /// </summary>
    public ValidationOptions? ValidationOptions { get; init; }

    /// <summary>
    /// Creates default options that include all sources and severities.
    /// </summary>
    /// <returns>A new <see cref="UnifiedFindingOptions"/> with sensible defaults.</returns>
    public static UnifiedFindingOptions Default() => new();
}
