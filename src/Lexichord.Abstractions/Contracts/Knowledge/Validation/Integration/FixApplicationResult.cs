// =============================================================================
// File: FixApplicationResult.cs
// Project: Lexichord.Abstractions
// Description: Result of applying ordered fixes.
// =============================================================================
// LOGIC: Reports which fixes were successfully applied, which failed, and
//   any warnings generated during the application process.
//
// v0.6.5j: Linter Integration (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;

/// <summary>
/// Result of applying a batch of <see cref="UnifiedFix"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Returned by <see cref="ILinterIntegration.ApplyAllFixesAsync"/>
/// and <see cref="ICombinedFixWorkflow.ApplyOrderedFixesAsync"/>.
/// Tracks success/failure for each individual fix.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5j as part of the Linter Integration.
/// </para>
/// </remarks>
public record FixApplicationResult
{
    /// <summary>
    /// Number of fixes successfully applied.
    /// </summary>
    public int Applied { get; init; }

    /// <summary>
    /// Number of fixes that failed to apply.
    /// </summary>
    public int Failed { get; init; }

    /// <summary>
    /// Warning messages generated during application.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>
    /// Whether all fixes were applied successfully.
    /// </summary>
    public bool IsSuccess => Failed == 0;

    /// <summary>
    /// Creates a fully successful result.
    /// </summary>
    /// <param name="count">Number of fixes applied.</param>
    /// <returns>A successful <see cref="FixApplicationResult"/>.</returns>
    public static FixApplicationResult Success(int count) => new()
    {
        Applied = count,
        Failed = 0
    };

    /// <summary>
    /// Creates an empty result (no fixes to apply).
    /// </summary>
    /// <returns>An empty <see cref="FixApplicationResult"/>.</returns>
    public static FixApplicationResult Empty() => new()
    {
        Applied = 0,
        Failed = 0
    };
}
