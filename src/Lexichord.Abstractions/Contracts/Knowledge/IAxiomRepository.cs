// =============================================================================
// File: IAxiomRepository.cs
// Project: Lexichord.Abstractions
// Description: Repository interface for axiom persistence and querying.
// =============================================================================
// LOGIC: Defines the contract for CRUD operations on axioms.
//   - Read operations are available to WriterPro+ tier users.
//   - Write operations require Teams+ tier (license-gated).
//   - Supports filtering by type, category, tags, and enabled status.
//   - Provides caching integration via IAxiomCacheService.
//
// v0.4.6f: Axiom Repository (CKVS Phase 1c)
// Dependencies: Axiom (v0.4.6e), AxiomFilter, AxiomStatistics
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge;

/// <summary>
/// Repository interface for axiom persistence and querying.
/// </summary>
/// <remarks>
/// <para>
/// The Axiom Repository provides persistent storage for domain axioms using
/// PostgreSQL. Axioms define structural invariants for Knowledge Graph entities,
/// relationships, and claims.
/// </para>
/// <para>
/// <b>Caching:</b> Read operations leverage <see cref="IAxiomCacheService"/> for
/// in-memory caching. The cache is automatically invalidated on write operations.
/// </para>
/// <para>
/// <b>License Requirements:</b>
/// <list type="bullet">
///   <item><description>Read operations: WriterPro+ tier.</description></item>
///   <item><description>Write operations: Teams+ tier.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.6f as part of the Axiom Store (CKVS Phase 1).
/// </para>
/// </remarks>
public interface IAxiomRepository
{
    #region Read Operations

    /// <summary>
    /// Retrieves all axioms targeting a specific entity type.
    /// </summary>
    /// <param name="entityType">The target entity type (e.g., "Endpoint", "Concept").</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>A read-only list of axioms for the specified type.</returns>
    /// <remarks>
    /// LOGIC: Results are cached by target type. Cache is invalidated when axioms
    /// for this type are saved or deleted.
    /// </remarks>
    Task<IReadOnlyList<Axiom>> GetByTypeAsync(string entityType, CancellationToken ct = default);

    /// <summary>
    /// Retrieves axioms matching the specified filter criteria.
    /// </summary>
    /// <param name="filter">The filter criteria for querying axioms.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>A read-only list of axioms matching the filter.</returns>
    /// <remarks>
    /// LOGIC: Supports filtering by target type, category, tags, and enabled status.
    /// Results are not cached (dynamic queries).
    /// </remarks>
    Task<IReadOnlyList<Axiom>> GetAsync(AxiomFilter filter, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a single axiom by its unique identifier.
    /// </summary>
    /// <param name="axiomId">The unique axiom identifier.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>The axiom if found; otherwise, <c>null</c>.</returns>
    /// <remarks>
    /// LOGIC: Results are cached by ID. Cache is invalidated when the axiom is
    /// saved or deleted.
    /// </remarks>
    Task<Axiom?> GetByIdAsync(string axiomId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all enabled axioms.
    /// </summary>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>A read-only list of all enabled axioms.</returns>
    /// <remarks>
    /// LOGIC: Results are cached. Call <see cref="GetAsync"/> with a filter
    /// if you need disabled axioms or more specific criteria.
    /// </remarks>
    Task<IReadOnlyList<Axiom>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves aggregate statistics about stored axioms.
    /// </summary>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>Statistics including counts by category and target type.</returns>
    Task<AxiomStatistics> GetStatisticsAsync(CancellationToken ct = default);

    #endregion

    #region Write Operations

    /// <summary>
    /// Saves or updates a single axiom.
    /// </summary>
    /// <param name="axiom">The axiom to save.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>The saved axiom with updated metadata.</returns>
    /// <exception cref="FeatureNotLicensedException">
    /// Thrown when the current license tier is below Teams.
    /// </exception>
    /// <remarks>
    /// LOGIC: Performs an upsert based on axiom ID. Increments version on update.
    /// Invalidates relevant cache entries.
    /// </remarks>
    Task<Axiom> SaveAsync(Axiom axiom, CancellationToken ct = default);

    /// <summary>
    /// Saves or updates multiple axioms in a batch operation.
    /// </summary>
    /// <param name="axioms">The axioms to save.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>The number of axioms saved.</returns>
    /// <exception cref="FeatureNotLicensedException">
    /// Thrown when the current license tier is below Teams.
    /// </exception>
    /// <remarks>
    /// LOGIC: More efficient than multiple <see cref="SaveAsync"/> calls.
    /// Performs upserts in a single transaction. Invalidates all cache entries.
    /// </remarks>
    Task<int> SaveBatchAsync(IEnumerable<Axiom> axioms, CancellationToken ct = default);

    /// <summary>
    /// Deletes an axiom by its unique identifier.
    /// </summary>
    /// <param name="axiomId">The unique axiom identifier.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns><c>true</c> if the axiom was deleted; otherwise, <c>false</c>.</returns>
    /// <exception cref="FeatureNotLicensedException">
    /// Thrown when the current license tier is below Teams.
    /// </exception>
    Task<bool> DeleteAsync(string axiomId, CancellationToken ct = default);

    /// <summary>
    /// Deletes all axioms from a specific source file.
    /// </summary>
    /// <param name="sourceFile">The source file path to match.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>The number of axioms deleted.</returns>
    /// <exception cref="FeatureNotLicensedException">
    /// Thrown when the current license tier is below Teams.
    /// </exception>
    /// <remarks>
    /// LOGIC: Useful for reloading axioms from a YAML file. Delete all from source,
    /// then re-save the parsed axioms.
    /// </remarks>
    Task<int> DeleteBySourceAsync(string sourceFile, CancellationToken ct = default);

    #endregion
}
