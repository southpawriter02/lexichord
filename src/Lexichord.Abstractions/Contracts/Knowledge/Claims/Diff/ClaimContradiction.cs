// =============================================================================
// File: ClaimContradiction.cs
// Project: Lexichord.Abstractions
// Description: A detected contradiction between claims.
// =============================================================================
// LOGIC: Represents conflicting claims that make incompatible assertions.
//   Enables review workflows for resolving inconsistencies.
//
// v0.5.6i: Claim Diff Service (Knowledge Graph Change Tracking)
// Dependencies: Claim (v0.5.6e), ContradictionType/Status (v0.5.6i)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims.Diff;

/// <summary>
/// A detected contradiction between claims.
/// </summary>
/// <remarks>
/// <para>
/// Contradictions occur when claims make incompatible assertions.
/// The Claim Diff Service detects these patterns:
/// </para>
/// <list type="bullet">
///   <item><b>Direct:</b> Same subject/predicate, different objects.</item>
///   <item><b>Deprecation:</b> Conflicting IS_DEPRECATED claims.</item>
///   <item><b>Requirement:</b> Incompatible REQUIRES claims.</item>
///   <item><b>Value:</b> Numeric inconsistencies.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.5.6i as part of the Claim Diff Service.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var contradictions = await diffService.FindContradictionsAsync(projectId);
/// foreach (var c in contradictions.Where(x => x.Status == ContradictionStatus.Open))
/// {
///     Console.WriteLine($"Contradiction: {c.Description}");
///     Console.WriteLine($"  Claim 1: {c.Claim1.Id}");
///     Console.WriteLine($"  Claim 2: {c.Claim2.Id}");
/// }
/// </code>
/// </example>
public record ClaimContradiction
{
    /// <summary>
    /// Unique identifier for this contradiction.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Project this contradiction belongs to.
    /// </summary>
    public required Guid ProjectId { get; init; }

    /// <summary>
    /// First claim in the contradiction.
    /// </summary>
    public required Claim Claim1 { get; init; }

    /// <summary>
    /// Second claim in the contradiction.
    /// </summary>
    public required Claim Claim2 { get; init; }

    /// <summary>
    /// Type of contradiction detected.
    /// </summary>
    public required ContradictionType Type { get; init; }

    /// <summary>
    /// Current resolution status.
    /// </summary>
    public ContradictionStatus Status { get; init; } = ContradictionStatus.Open;

    /// <summary>
    /// Human-readable description of the contradiction.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Confidence that this is a real contradiction (0.0-1.0).
    /// </summary>
    /// <value>
    /// Higher values indicate more certain contradictions.
    /// </value>
    /// <remarks>
    /// LOGIC: Some contradictions may be false positives due to context
    /// differences. Lower confidence contradictions may require more review.
    /// </remarks>
    public float Confidence { get; init; } = 1.0f;

    /// <summary>
    /// When the contradiction was detected.
    /// </summary>
    public DateTimeOffset DetectedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the contradiction was resolved (if applicable).
    /// </summary>
    public DateTimeOffset? ResolvedAt { get; init; }

    /// <summary>
    /// Who resolved the contradiction (if applicable).
    /// </summary>
    public string? ResolvedBy { get; init; }

    /// <summary>
    /// Notes about the resolution (if resolved).
    /// </summary>
    public string? ResolutionNotes { get; init; }

    /// <summary>
    /// Optional suggested resolution.
    /// </summary>
    /// <value>
    /// A suggestion for how to resolve the contradiction.
    /// </value>
    public string? SuggestedResolution { get; init; }
}
