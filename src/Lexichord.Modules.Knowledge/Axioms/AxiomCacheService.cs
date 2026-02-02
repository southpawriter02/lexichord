// =============================================================================
// File: AxiomCacheService.cs
// Project: Lexichord.Modules.Knowledge
// Description: In-memory cache implementation for axiom storage.
// =============================================================================
// LOGIC: Uses IMemoryCache for caching axioms with:
//   - 5-minute sliding expiration for active access patterns.
//   - 30-minute absolute expiration to ensure eventual refresh.
//   - Targeted invalidation on write operations.
//
// v0.4.6f: Axiom Repository (CKVS Phase 1c)
// Dependencies: Microsoft.Extensions.Caching.Memory
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Axioms;

/// <summary>
/// In-memory cache implementation for axiom storage using <see cref="IMemoryCache"/>.
/// </summary>
/// <remarks>
/// <para>
/// This service provides efficient caching for axiom queries, reducing database
/// load for frequently accessed axioms.
/// </para>
/// <para>
/// <b>Expiration Policy:</b>
/// <list type="bullet">
///   <item><description>Sliding expiration: 5 minutes.</description></item>
///   <item><description>Absolute expiration: 30 minutes.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> This implementation is thread-safe. <see cref="IMemoryCache"/>
/// handles concurrent access internally.
/// </para>
/// </remarks>
public sealed class AxiomCacheService : IAxiomCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<AxiomCacheService> _logger;
    private readonly MemoryCacheEntryOptions _entryOptions;

    private const string AllKey = "axioms:all";
    private const string ByTypePrefix = "axioms:type:";
    private const string ByIdPrefix = "axioms:id:";

    /// <summary>
    /// Creates a new <see cref="AxiomCacheService"/> instance.
    /// </summary>
    /// <param name="cache">The memory cache instance.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cache"/> or <paramref name="logger"/> is null.
    /// </exception>
    public AxiomCacheService(
        IMemoryCache cache,
        ILogger<AxiomCacheService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // LOGIC: Configure cache entry options with sliding and absolute expiration.
        _entryOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(5),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };
    }

    #region Read Operations

    /// <inheritdoc />
    public bool TryGet(string axiomId, out Axiom? axiom)
    {
        var key = $"{ByIdPrefix}{axiomId}";
        var found = _cache.TryGetValue(key, out axiom);

        _logger.LogDebug("Cache {Result} for axiom {AxiomId}",
            found ? "hit" : "miss", axiomId);

        return found;
    }

    /// <inheritdoc />
    public bool TryGetByType(string targetType, out IReadOnlyList<Axiom>? axioms)
    {
        var key = $"{ByTypePrefix}{targetType}";
        var found = _cache.TryGetValue(key, out axioms);

        _logger.LogDebug("Cache {Result} for type {TargetType}",
            found ? "hit" : "miss", targetType);

        return found;
    }

    /// <inheritdoc />
    public bool TryGetAll(out IReadOnlyList<Axiom>? axioms)
    {
        var found = _cache.TryGetValue(AllKey, out axioms);

        _logger.LogDebug("Cache {Result} for all axioms",
            found ? "hit" : "miss");

        return found;
    }

    #endregion

    #region Write Operations

    /// <inheritdoc />
    public void Set(Axiom axiom)
    {
        ArgumentNullException.ThrowIfNull(axiom);

        var key = $"{ByIdPrefix}{axiom.Id}";
        _cache.Set(key, axiom, _entryOptions);

        _logger.LogDebug("Cached axiom {AxiomId}", axiom.Id);
    }

    /// <inheritdoc />
    public void SetByType(string targetType, IReadOnlyList<Axiom> axioms)
    {
        ArgumentException.ThrowIfNullOrEmpty(targetType);
        ArgumentNullException.ThrowIfNull(axioms);

        var key = $"{ByTypePrefix}{targetType}";
        _cache.Set(key, axioms, _entryOptions);

        _logger.LogDebug("Cached {Count} axioms for type {TargetType}",
            axioms.Count, targetType);
    }

    /// <inheritdoc />
    public void SetAll(IReadOnlyList<Axiom> axioms)
    {
        ArgumentNullException.ThrowIfNull(axioms);

        _cache.Set(AllKey, axioms, _entryOptions);

        _logger.LogDebug("Cached all {Count} axioms", axioms.Count);
    }

    #endregion

    #region Invalidation

    /// <inheritdoc />
    public void Invalidate(string axiomId)
    {
        var key = $"{ByIdPrefix}{axiomId}";
        _cache.Remove(key);

        // LOGIC: Also invalidate the "all" cache since it may contain this axiom.
        _cache.Remove(AllKey);

        _logger.LogDebug("Invalidated cache for axiom {AxiomId}", axiomId);
    }

    /// <inheritdoc />
    public void InvalidateByType(string targetType)
    {
        var key = $"{ByTypePrefix}{targetType}";
        _cache.Remove(key);

        // LOGIC: Also invalidate the "all" cache since it may contain axioms of this type.
        _cache.Remove(AllKey);

        _logger.LogDebug("Invalidated cache for type {TargetType}", targetType);
    }

    /// <inheritdoc />
    public void InvalidateAll()
    {
        // LOGIC: IMemoryCache does not support prefix-based clearing.
        // We remove the "all" key. Individual and type keys will expire naturally.
        // For more aggressive invalidation, consider using a CancellationTokenSource.
        _cache.Remove(AllKey);

        _logger.LogDebug("Invalidated all axiom cache entries");
    }

    #endregion
}
