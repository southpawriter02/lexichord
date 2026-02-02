// =============================================================================
// File: AxiomsLoadedEventArgs.cs
// Project: Lexichord.Modules.Knowledge
// Description: Event arguments for the AxiomsLoaded event.
// =============================================================================
// LOGIC: Published when axioms are loaded or reloaded, allowing consumers
//   to react to changes in the axiom store (e.g., invalidate caches,
//   re-validate entities, update UI indicators).
//
// v0.4.6h: Axiom Query API (CKVS Phase 1e)
// Dependencies: AxiomLoadResult (v0.4.6g)
// =============================================================================

namespace Lexichord.Modules.Knowledge.Axioms;

/// <summary>
/// Event arguments for the <see cref="IAxiomStore.AxiomsLoaded"/> event.
/// </summary>
/// <remarks>
/// <para>
/// This record is raised when axioms are loaded or reloaded, providing
/// consumers with information about the load operation. Subscribers can
/// use this to invalidate caches, trigger re-validation, or update UI.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.6h as part of the Axiom Query API.
/// </para>
/// </remarks>
/// <example>
/// Subscribing to axiom load events:
/// <code>
/// axiomStore.AxiomsLoaded += (sender, args) =>
/// {
///     if (args.IsReload)
///     {
///         logger.LogInformation("Axioms reloaded: {Count} axioms", args.AxiomCount);
///         InvalidateValidationCache();
///     }
/// };
/// </code>
/// </example>
public record AxiomsLoadedEventArgs
{
    /// <summary>
    /// Number of axioms now loaded in the store.
    /// </summary>
    /// <value>
    /// The total count of axioms after the load operation completed.
    /// </value>
    public int AxiomCount { get; init; }

    /// <summary>
    /// Detailed result of the load operation.
    /// </summary>
    /// <value>
    /// Contains lists of loaded axioms, errors encountered, and files processed.
    /// </value>
    /// <remarks>
    /// LOGIC: Provides access to the full <see cref="AxiomLoadResult"/> from
    /// the loader, including any parse errors or validation warnings that
    /// occurred during loading.
    /// </remarks>
    public required AxiomLoadResult LoadResult { get; init; }

    /// <summary>
    /// Whether this event represents a reload (vs. initial load).
    /// </summary>
    /// <value>
    /// <c>true</c> if axioms were previously loaded and this is a refresh;
    /// <c>false</c> for the initial load.
    /// </value>
    /// <remarks>
    /// LOGIC: Consumers may handle reloads differently (e.g., showing a
    /// notification to the user, or triggering incremental re-validation
    /// instead of full re-validation).
    /// </remarks>
    public bool IsReload { get; init; }
}
