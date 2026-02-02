// =============================================================================
// File: IAxiomCacheService.cs
// Project: Lexichord.Modules.Knowledge
// Description: Cache abstraction for axiom storage.
// =============================================================================
// LOGIC: Provides in-memory caching for axioms to reduce database queries.
//   - Caches by axiom ID for single lookups.
//   - Caches by target type for batch lookups.
//   - Caches all enabled axioms for full scans.
//   - Supports targeted invalidation on write operations.
//
// v0.4.6f: Axiom Repository (CKVS Phase 1c)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge;

namespace Lexichord.Modules.Knowledge.Axioms;

/// <summary>
/// Cache abstraction for axiom storage.
/// </summary>
/// <remarks>
/// <para>
/// The cache service provides in-memory caching for frequently accessed axioms.
/// It uses a combination of sliding and absolute expiration to balance freshness
/// with performance.
/// </para>
/// <para>
/// <b>Cache Keys:</b>
/// <list type="bullet">
///   <item><description><c>axioms:id:{id}</c> — Single axiom by ID.</description></item>
///   <item><description><c>axioms:type:{type}</c> — Axioms by target type.</description></item>
///   <item><description><c>axioms:all</c> — All enabled axioms.</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IAxiomCacheService
{
    /// <summary>
    /// Attempts to retrieve a cached axiom by ID.
    /// </summary>
    /// <param name="axiomId">The unique axiom identifier.</param>
    /// <param name="axiom">The cached axiom, if found.</param>
    /// <returns><c>true</c> if the axiom was in cache; otherwise, <c>false</c>.</returns>
    bool TryGet(string axiomId, out Axiom? axiom);

    /// <summary>
    /// Attempts to retrieve cached axioms by target type.
    /// </summary>
    /// <param name="targetType">The target entity type.</param>
    /// <param name="axioms">The cached axioms, if found.</param>
    /// <returns><c>true</c> if the type was in cache; otherwise, <c>false</c>.</returns>
    bool TryGetByType(string targetType, out IReadOnlyList<Axiom>? axioms);

    /// <summary>
    /// Attempts to retrieve all cached axioms.
    /// </summary>
    /// <param name="axioms">The cached axioms, if found.</param>
    /// <returns><c>true</c> if all axioms were in cache; otherwise, <c>false</c>.</returns>
    bool TryGetAll(out IReadOnlyList<Axiom>? axioms);

    /// <summary>
    /// Caches a single axiom.
    /// </summary>
    /// <param name="axiom">The axiom to cache.</param>
    void Set(Axiom axiom);

    /// <summary>
    /// Caches axioms by target type.
    /// </summary>
    /// <param name="targetType">The target entity type.</param>
    /// <param name="axioms">The axioms to cache.</param>
    void SetByType(string targetType, IReadOnlyList<Axiom> axioms);

    /// <summary>
    /// Caches all enabled axioms.
    /// </summary>
    /// <param name="axioms">The axioms to cache.</param>
    void SetAll(IReadOnlyList<Axiom> axioms);

    /// <summary>
    /// Invalidates a cached axiom by ID.
    /// </summary>
    /// <param name="axiomId">The unique axiom identifier.</param>
    void Invalidate(string axiomId);

    /// <summary>
    /// Invalidates cached axioms by target type.
    /// </summary>
    /// <param name="targetType">The target entity type.</param>
    void InvalidateByType(string targetType);

    /// <summary>
    /// Invalidates all cached axioms.
    /// </summary>
    void InvalidateAll();
}
