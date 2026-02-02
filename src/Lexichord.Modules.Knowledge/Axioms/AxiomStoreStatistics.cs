// =============================================================================
// File: AxiomStoreStatistics.cs
// Project: Lexichord.Modules.Knowledge
// Description: Statistics record for monitoring axiom store state.
// =============================================================================
// LOGIC: Provides a snapshot of the axiom store's current state, including
//   counts, load timing, and source information. Used for monitoring,
//   diagnostics, and UI display (e.g., status bar indicators).
//
// v0.4.6h: Axiom Query API (CKVS Phase 1e)
// Dependencies: None (pure record)
// =============================================================================

namespace Lexichord.Modules.Knowledge.Axioms;

/// <summary>
/// Statistics about the current state of the <see cref="IAxiomStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// This record provides a read-only snapshot of the axiom store's state,
/// useful for monitoring, diagnostics, and displaying status information
/// in the UI. Statistics are updated after each load operation.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.6h as part of the Axiom Query API.
/// </para>
/// </remarks>
/// <example>
/// Accessing store statistics:
/// <code>
/// var stats = axiomStore.Statistics;
/// logger.LogInformation(
///     "Loaded {Total} axioms ({Enabled} enabled) from {Files} files in {Duration}ms",
///     stats.TotalAxioms,
///     stats.EnabledAxioms,
///     stats.SourceFilesLoaded,
///     stats.LastLoadDuration?.TotalMilliseconds);
/// </code>
/// </example>
public record AxiomStoreStatistics
{
    /// <summary>
    /// Total number of axioms in the store (enabled + disabled).
    /// </summary>
    /// <value>
    /// The count of all axioms regardless of their <see cref="Abstractions.Contracts.Knowledge.Axiom.IsEnabled"/> state.
    /// </value>
    public int TotalAxioms { get; init; }

    /// <summary>
    /// Number of axioms that are currently enabled for validation.
    /// </summary>
    /// <value>
    /// The count of axioms where <see cref="Abstractions.Contracts.Knowledge.Axiom.IsEnabled"/> is <c>true</c>.
    /// </value>
    /// <remarks>
    /// LOGIC: Only enabled axioms are evaluated during validation.
    /// Disabled axioms are retained in the store for reference but skipped.
    /// </remarks>
    public int EnabledAxioms { get; init; }

    /// <summary>
    /// Number of distinct entity/relationship types that have axioms defined.
    /// </summary>
    /// <value>
    /// The count of unique <see cref="Abstractions.Contracts.Knowledge.Axiom.TargetType"/> values.
    /// </value>
    /// <remarks>
    /// LOGIC: Used for coverage analysis. A high number indicates broad
    /// governance; a low number might indicate focused validation.
    /// </remarks>
    public int TypesWithAxioms { get; init; }

    /// <summary>
    /// Timestamp of the last successful axiom load operation.
    /// </summary>
    /// <value>
    /// UTC timestamp when axioms were last loaded, or <c>null</c> if never loaded.
    /// </value>
    public DateTimeOffset? LastLoadedAt { get; init; }

    /// <summary>
    /// Duration of the last load operation.
    /// </summary>
    /// <value>
    /// Time taken to load axioms, or <c>null</c> if never loaded.
    /// </value>
    /// <remarks>
    /// LOGIC: Useful for performance monitoring. Unusually long load times
    /// might indicate issues with file parsing or repository access.
    /// </remarks>
    public TimeSpan? LastLoadDuration { get; init; }

    /// <summary>
    /// Number of source files that were processed during the last load.
    /// </summary>
    /// <value>
    /// Count of YAML files and embedded resources processed.
    /// </value>
    /// <remarks>
    /// LOGIC: Includes both built-in (embedded) and workspace axiom files.
    /// Files with parse errors are still counted.
    /// </remarks>
    public int SourceFilesLoaded { get; init; }

    /// <summary>
    /// Creates an empty statistics record representing an unloaded store.
    /// </summary>
    /// <returns>A new <see cref="AxiomStoreStatistics"/> with all counts at zero.</returns>
    public static AxiomStoreStatistics Empty => new()
    {
        TotalAxioms = 0,
        EnabledAxioms = 0,
        TypesWithAxioms = 0,
        LastLoadedAt = null,
        LastLoadDuration = null,
        SourceFilesLoaded = 0
    };
}
