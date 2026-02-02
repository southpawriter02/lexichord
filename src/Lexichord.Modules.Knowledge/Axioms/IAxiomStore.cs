// =============================================================================
// File: IAxiomStore.cs
// Project: Lexichord.Modules.Knowledge
// Description: High-level interface for querying and validating against axioms.
// =============================================================================
// LOGIC: Defines the public API for axiom consumers. The store provides:
//   - Axiom retrieval by type (for display/filtering)
//   - Entity and relationship validation
//   - Statistics and event notifications
//
// License Gating:
//   - WriterPro+: Read-only access (GetAxiomsForType, GetAllAxioms, Statistics)
//   - Teams+: Validation capabilities (ValidateEntity, ValidateRelationship)
//
// v0.4.6h: Axiom Query API (CKVS Phase 1e)
// Dependencies: Axiom, AxiomValidationResult (v0.4.6e),
//               IAxiomRepository (v0.4.6f), IAxiomLoader (v0.4.6g),
//               KnowledgeEntity, KnowledgeRelationship (v0.4.5e)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;

namespace Lexichord.Modules.Knowledge.Axioms;

/// <summary>
/// High-level API for querying and validating against axioms.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IAxiomStore"/> is the primary interface for consuming axioms.
/// It provides methods for retrieving axioms by type, validating entities and
/// relationships, and monitoring store state through statistics and events.
/// </para>
/// <para>
/// <b>Architecture:</b> The store acts as a facade over:
/// <list type="bullet">
///   <item><see cref="IAxiomLoader"/>: Loads axioms from files and resources.</item>
///   <item><see cref="IAxiomRepository"/>: Persists axioms to the database.</item>
///   <item><see cref="IAxiomEvaluator"/>: Evaluates rules against properties.</item>
/// </list>
/// </para>
/// <para>
/// <b>Caching:</b> The store maintains an in-memory cache of axioms, indexed
/// by target type for fast retrieval. The cache is automatically refreshed
/// when the loader raises <c>AxiomsReloaded</c> events.
/// </para>
/// <para>
/// <b>License Requirements:</b>
/// <list type="bullet">
///   <item><c>WriterPro+</c>: Read-only access (retrieval, statistics).</item>
///   <item><c>Teams+</c>: Validation capabilities (ValidateEntity, ValidateRelationship).</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.6h as part of the Axiom Query API.
/// </para>
/// </remarks>
/// <example>
/// Basic usage:
/// <code>
/// // Get axioms for a specific type
/// var endpointAxioms = axiomStore.GetAxiomsForType("Endpoint");
///
/// // Validate an entity
/// var result = axiomStore.ValidateEntity(myEndpoint);
/// if (!result.IsValid)
/// {
///     foreach (var violation in result.Violations)
///     {
///         Console.WriteLine($"Error: {violation.Message}");
///     }
/// }
/// </code>
/// </example>
public interface IAxiomStore
{
    /// <summary>
    /// Gets all axioms targeting a specific entity or relationship type.
    /// </summary>
    /// <param name="targetType">The entity/relationship type (e.g., "Endpoint", "CONTAINS").</param>
    /// <returns>
    /// Axioms targeting the specified type. Empty list if none found.
    /// </returns>
    /// <remarks>
    /// LOGIC: Returns only enabled axioms. Results are retrieved from the
    /// in-memory cache for fast access.
    /// </remarks>
    /// <exception cref="FeatureNotLicensedException">
    /// Thrown when the current license tier is below WriterPro.
    /// </exception>
    IReadOnlyList<Axiom> GetAxiomsForType(string targetType);

    /// <summary>
    /// Gets all axioms currently loaded in the store.
    /// </summary>
    /// <returns>All loaded axioms (enabled and disabled).</returns>
    /// <remarks>
    /// LOGIC: Returns the complete list of axioms from the cache.
    /// Useful for admin/debug views that need to display all axioms.
    /// </remarks>
    /// <exception cref="FeatureNotLicensedException">
    /// Thrown when the current license tier is below WriterPro.
    /// </exception>
    IReadOnlyList<Axiom> GetAllAxioms();

    /// <summary>
    /// Validates an entity against all applicable axioms.
    /// </summary>
    /// <param name="entity">The entity to validate.</param>
    /// <returns>
    /// Validation result containing any violations found.
    /// </returns>
    /// <remarks>
    /// LOGIC: Finds all axioms targeting the entity's type, then evaluates
    /// each axiom's rules against the entity's properties. Disabled axioms
    /// are skipped. Returns aggregated violations with timing information.
    /// </remarks>
    /// <exception cref="FeatureNotLicensedException">
    /// Thrown when the current license tier is below Teams.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entity"/> is null.
    /// </exception>
    AxiomValidationResult ValidateEntity(KnowledgeEntity entity);

    /// <summary>
    /// Validates a relationship against all applicable axioms.
    /// </summary>
    /// <param name="relationship">The relationship to validate.</param>
    /// <param name="fromEntity">The source entity of the relationship.</param>
    /// <param name="toEntity">The target entity of the relationship.</param>
    /// <returns>
    /// Validation result containing any violations found.
    /// </returns>
    /// <remarks>
    /// LOGIC: Finds all axioms targeting the relationship's type, then evaluates
    /// each axiom's rules against a merged property dictionary containing
    /// relationship and entity properties.
    /// </remarks>
    /// <exception cref="FeatureNotLicensedException">
    /// Thrown when the current license tier is below Teams.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    AxiomValidationResult ValidateRelationship(
        KnowledgeRelationship relationship,
        KnowledgeEntity fromEntity,
        KnowledgeEntity toEntity);

    /// <summary>
    /// Loads or reloads axioms from all sources.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// LOGIC: Delegates to IAxiomLoader.LoadAllAsync(), then rebuilds the
    /// in-memory cache from the repository. Raises AxiomsLoaded event on completion.
    /// </remarks>
    /// <exception cref="FeatureNotLicensedException">
    /// Thrown when the current license tier is below WriterPro.
    /// </exception>
    Task LoadAxiomsAsync(CancellationToken ct = default);

    /// <summary>
    /// Loads axioms from a specific directory (workspace override).
    /// </summary>
    /// <param name="axiomDirectory">Path to the axiom directory.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// LOGIC: Loads axioms from the specified directory instead of the
    /// default workspace location. Useful for testing or custom deployments.
    /// </remarks>
    /// <exception cref="FeatureNotLicensedException">
    /// Thrown when the current license tier is below Teams.
    /// </exception>
    Task LoadAxiomsAsync(string axiomDirectory, CancellationToken ct = default);

    /// <summary>
    /// Gets current store statistics.
    /// </summary>
    /// <value>
    /// Statistics snapshot including counts, timing, and source information.
    /// </value>
    AxiomStoreStatistics Statistics { get; }

    /// <summary>
    /// Event raised when axioms are loaded or reloaded.
    /// </summary>
    /// <remarks>
    /// LOGIC: Raised after successful load operations. Consumers can subscribe
    /// to invalidate caches, trigger re-validation, or update UI indicators.
    /// </remarks>
    event EventHandler<AxiomsLoadedEventArgs>? AxiomsLoaded;
}
