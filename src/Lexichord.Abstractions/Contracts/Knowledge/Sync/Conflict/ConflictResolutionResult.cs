// =============================================================================
// File: ConflictResolutionResult.cs
// Project: Lexichord.Abstractions
// Description: Result of resolving a single conflict.
// =============================================================================
// LOGIC: ConflictResolutionResult captures the outcome of applying a
//   resolution strategy to a conflict. It includes the original conflict,
//   the strategy used, whether it succeeded, and metadata about the
//   resolution process.
//
// v0.7.6h: Conflict Resolver (CKVS Phase 4c)
// Dependencies: SyncConflict, ConflictResolutionStrategy (v0.7.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict;

/// <summary>
/// Result of resolving a single conflict.
/// </summary>
/// <remarks>
/// <para>
/// Captures the resolution outcome:
/// </para>
/// <list type="bullet">
///   <item><b>Conflict:</b> The conflict that was resolved.</item>
///   <item><b>Strategy:</b> The strategy that was applied.</item>
///   <item><b>Succeeded:</b> Whether resolution completed successfully.</item>
///   <item><b>ResolvedValue:</b> The value after resolution.</item>
///   <item><b>Metadata:</b> Timestamp, user, automatic flag.</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// var result = await resolver.ResolveConflictAsync(conflict, strategy);
/// if (result.Succeeded)
/// {
///     Console.WriteLine($"Resolved to: {result.ResolvedValue}");
/// }
/// else
/// {
///     Console.WriteLine($"Resolution failed: {result.ErrorMessage}");
/// }
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6h as part of the Conflict Resolver module.
/// </para>
/// </remarks>
public record ConflictResolutionResult
{
    /// <summary>
    /// The conflict that was resolved.
    /// </summary>
    /// <value>The original <see cref="SyncConflict"/> instance.</value>
    /// <remarks>
    /// LOGIC: Preserved for context and audit trail.
    /// Links the resolution result to the original conflict.
    /// </remarks>
    public required SyncConflict Conflict { get; init; }

    /// <summary>
    /// The strategy that was applied.
    /// </summary>
    /// <value>The <see cref="ConflictResolutionStrategy"/> used.</value>
    /// <remarks>
    /// LOGIC: Records which resolution approach was taken.
    /// May differ from the requested strategy if fallback occurred.
    /// </remarks>
    public required ConflictResolutionStrategy Strategy { get; init; }

    /// <summary>
    /// Indicates whether the resolution succeeded.
    /// </summary>
    /// <value>True if resolution completed successfully; otherwise, false.</value>
    /// <remarks>
    /// LOGIC: Primary success indicator.
    /// When false, the conflict remains unresolved.
    /// </remarks>
    public required bool Succeeded { get; init; }

    /// <summary>
    /// The resolved value if successful.
    /// </summary>
    /// <value>
    /// The value chosen or computed during resolution.
    /// Null if resolution failed or no value applies (e.g., Manual strategy).
    /// </value>
    /// <remarks>
    /// LOGIC: The value to be persisted to the graph.
    /// May be from document, graph, or a merge result.
    /// </remarks>
    public object? ResolvedValue { get; init; }

    /// <summary>
    /// Error message if resolution failed.
    /// </summary>
    /// <value>
    /// A description of why the resolution failed.
    /// Null if resolution succeeded.
    /// </value>
    /// <remarks>
    /// LOGIC: Provides diagnostic information.
    /// Should be user-readable for display in UI.
    /// </remarks>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Timestamp of the resolution.
    /// </summary>
    /// <value>UTC timestamp when resolution completed.</value>
    /// <remarks>
    /// LOGIC: Used for audit trail and timing analysis.
    /// Defaults to current time.
    /// </remarks>
    public DateTimeOffset ResolvedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The user who performed the resolution.
    /// </summary>
    /// <value>
    /// User ID for manual resolutions.
    /// Null for automatic resolutions.
    /// </value>
    /// <remarks>
    /// LOGIC: Provides accountability for manual interventions.
    /// Null indicates system-initiated automatic resolution.
    /// </remarks>
    public Guid? ResolvedBy { get; init; }

    /// <summary>
    /// Indicates whether the resolution was automatic.
    /// </summary>
    /// <value>True if resolution was performed automatically; otherwise, false.</value>
    /// <remarks>
    /// LOGIC: Distinguishes automatic from manual resolutions.
    /// Automatic resolutions use UseDocument, UseGraph, or Merge strategies
    /// based on configuration without user intervention.
    /// </remarks>
    public bool IsAutomatic { get; init; }

    /// <summary>
    /// Creates a successful resolution result.
    /// </summary>
    /// <param name="conflict">The resolved conflict.</param>
    /// <param name="strategy">The strategy used.</param>
    /// <param name="resolvedValue">The resolved value.</param>
    /// <param name="isAutomatic">Whether resolution was automatic.</param>
    /// <returns>A successful ConflictResolutionResult.</returns>
    public static ConflictResolutionResult Success(
        SyncConflict conflict,
        ConflictResolutionStrategy strategy,
        object? resolvedValue,
        bool isAutomatic = true)
    {
        return new ConflictResolutionResult
        {
            Conflict = conflict,
            Strategy = strategy,
            Succeeded = true,
            ResolvedValue = resolvedValue,
            IsAutomatic = isAutomatic
        };
    }

    /// <summary>
    /// Creates a failed resolution result.
    /// </summary>
    /// <param name="conflict">The conflict that couldn't be resolved.</param>
    /// <param name="strategy">The strategy that was attempted.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failed ConflictResolutionResult.</returns>
    public static ConflictResolutionResult Failure(
        SyncConflict conflict,
        ConflictResolutionStrategy strategy,
        string errorMessage)
    {
        return new ConflictResolutionResult
        {
            Conflict = conflict,
            Strategy = strategy,
            Succeeded = false,
            ErrorMessage = errorMessage,
            IsAutomatic = true
        };
    }

    /// <summary>
    /// Creates a result indicating manual intervention is required.
    /// </summary>
    /// <param name="conflict">The conflict requiring manual resolution.</param>
    /// <returns>A ConflictResolutionResult indicating manual intervention needed.</returns>
    public static ConflictResolutionResult RequiresManualIntervention(SyncConflict conflict)
    {
        return new ConflictResolutionResult
        {
            Conflict = conflict,
            Strategy = ConflictResolutionStrategy.Manual,
            Succeeded = false,
            ErrorMessage = "Conflict requires manual intervention",
            IsAutomatic = false
        };
    }
}
