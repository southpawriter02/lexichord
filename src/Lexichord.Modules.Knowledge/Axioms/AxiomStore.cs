// =============================================================================
// File: AxiomStore.cs
// Project: Lexichord.Modules.Knowledge
// Description: High-level API for querying and validating against axioms.
// =============================================================================
// LOGIC: Implements the public API for axiom consumers. The store:
//   - Maintains an in-memory cache of axioms indexed by target type.
//   - Delegates validation to IAxiomEvaluator.
//   - Integrates with IAxiomLoader for file loading and IAxiomRepository for persistence.
//   - Raises AxiomsLoaded event when axioms are (re)loaded.
//
// License Gating:
//   - WriterPro+: Read-only access (GetAxiomsForType, GetAllAxioms, Statistics).
//   - Teams+: Validation capabilities (ValidateEntity, ValidateRelationship).
//   - Teams+: Custom directory loading.
//
// Cache Strategy:
//   - Rebuild cache on LoadAxiomsAsync() or when AxiomsReloaded event is raised.
//   - Indexed by target type for O(1) retrieval.
//   - Separate index for enabled-only axioms.
//
// v0.4.6h: Axiom Query API (CKVS Phase 1e)
// Dependencies: Axiom data model (v0.4.6e), IAxiomRepository (v0.4.6f),
//               IAxiomLoader (v0.4.6g), ILicenseContext (v0.0.4c)
// =============================================================================

using System.Diagnostics;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Axioms;

/// <summary>
/// High-level API for querying and validating against axioms.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="AxiomStore"/> is the primary interface for consuming axioms.
/// It provides methods for retrieving axioms by type, validating entities and
/// relationships, and monitoring store state through statistics and events.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This implementation is thread-safe. The cache uses
/// immutable collections and is replaced atomically during reloads.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.6h as part of the Axiom Query API.
/// </para>
/// </remarks>
public sealed class AxiomStore : IAxiomStore
{
    private readonly IAxiomRepository _repository;
    private readonly IAxiomLoader _loader;
    private readonly IAxiomEvaluator _evaluator;
    private readonly ILicenseContext _license;
    private readonly ILogger<AxiomStore> _logger;

    // Immutable cache replaced atomically during reload
    private volatile AxiomCache _cache = AxiomCache.Empty;
    private readonly object _reloadLock = new();

    // Statistics tracking
    private DateTimeOffset? _lastLoadedAt;
    private TimeSpan? _lastLoadDuration;
    private int _sourceFilesLoaded;

    /// <inheritdoc/>
    public event EventHandler<AxiomsLoadedEventArgs>? AxiomsLoaded;

    /// <summary>
    /// Initializes a new instance of <see cref="AxiomStore"/>.
    /// </summary>
    /// <param name="repository">Repository for axiom persistence.</param>
    /// <param name="loader">Loader for axiom files.</param>
    /// <param name="evaluator">Evaluator for rule validation.</param>
    /// <param name="license">License context for gating.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public AxiomStore(
        IAxiomRepository repository,
        IAxiomLoader loader,
        IAxiomEvaluator evaluator,
        ILicenseContext license,
        ILogger<AxiomStore> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        _license = license ?? throw new ArgumentNullException(nameof(license));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Subscribe to loader's reload event
        _loader.AxiomsReloaded += OnAxiomsReloaded;

        _logger.LogDebug("AxiomStore initialized");
    }

    /// <inheritdoc/>
    public AxiomStoreStatistics Statistics => new()
    {
        TotalAxioms = _cache.AllAxioms.Count,
        EnabledAxioms = _cache.EnabledAxioms.Count,
        TypesWithAxioms = _cache.EnabledByType.Count,
        LastLoadedAt = _lastLoadedAt,
        LastLoadDuration = _lastLoadDuration,
        SourceFilesLoaded = _sourceFilesLoaded
    };

    #region Read-Only Operations (WriterPro+)

    /// <inheritdoc/>
    public IReadOnlyList<Axiom> GetAxiomsForType(string targetType)
    {
        RequireLicenseTier(LicenseTier.WriterPro, "Axiom retrieval");

        if (string.IsNullOrWhiteSpace(targetType))
        {
            _logger.LogDebug("GetAxiomsForType called with empty target type");
            return Array.Empty<Axiom>();
        }

        if (_cache.EnabledByType.TryGetValue(targetType, out var axioms))
        {
            _logger.LogTrace(
                "Retrieved {Count} axioms for type {Type}",
                axioms.Count, targetType);
            return axioms;
        }

        _logger.LogTrace("No axioms found for type {Type}", targetType);
        return Array.Empty<Axiom>();
    }

    /// <inheritdoc/>
    public IReadOnlyList<Axiom> GetAllAxioms()
    {
        RequireLicenseTier(LicenseTier.WriterPro, "Axiom retrieval");

        _logger.LogTrace("Retrieved all {Count} axioms", _cache.AllAxioms.Count);
        return _cache.AllAxioms;
    }

    #endregion

    #region Validation Operations (Teams+)

    /// <inheritdoc/>
    public AxiomValidationResult ValidateEntity(KnowledgeEntity entity)
    {
        RequireLicenseTier(LicenseTier.Teams, "Entity validation");
        ArgumentNullException.ThrowIfNull(entity);

        var sw = Stopwatch.StartNew();

        _logger.LogDebug(
            "Validating entity {EntityType}:{EntityId}",
            entity.Type, entity.Id);

        // Get all enabled axioms for this entity type
        var axioms = GetAxiomsForTypeInternal(entity.Type);
        if (axioms.Count == 0)
        {
            _logger.LogTrace(
                "No axioms found for entity type {Type}, validation skipped",
                entity.Type);
            return AxiomValidationResult.Valid(0, 0);
        }

        var allViolations = new List<AxiomViolation>();
        var totalRules = 0;

        foreach (var axiom in axioms)
        {
            totalRules += axiom.Rules.Count;
            var violations = _evaluator.Evaluate(axiom, entity);
            allViolations.AddRange(violations);
        }

        sw.Stop();

        if (allViolations.Count > 0)
        {
            _logger.LogInformation(
                "Entity {EntityType}:{EntityId} validation found {ViolationCount} violations in {Elapsed:F2}ms",
                entity.Type, entity.Id, allViolations.Count, sw.Elapsed.TotalMilliseconds);
        }
        else
        {
            _logger.LogDebug(
                "Entity {EntityType}:{EntityId} validation passed ({AxiomCount} axioms, {RuleCount} rules) in {Elapsed:F2}ms",
                entity.Type, entity.Id, axioms.Count, totalRules, sw.Elapsed.TotalMilliseconds);
        }

        return allViolations.Count > 0
            ? AxiomValidationResult.WithViolations(allViolations, axioms.Count, totalRules)
            : AxiomValidationResult.Valid(axioms.Count, totalRules);
    }

    /// <inheritdoc/>
    public AxiomValidationResult ValidateRelationship(
        KnowledgeRelationship relationship,
        KnowledgeEntity fromEntity,
        KnowledgeEntity toEntity)
    {
        RequireLicenseTier(LicenseTier.Teams, "Relationship validation");
        ArgumentNullException.ThrowIfNull(relationship);
        ArgumentNullException.ThrowIfNull(fromEntity);
        ArgumentNullException.ThrowIfNull(toEntity);

        var sw = Stopwatch.StartNew();

        _logger.LogDebug(
            "Validating relationship {RelType}:{RelId}",
            relationship.Type, relationship.Id);

        // Get all enabled axioms for this relationship type
        var axioms = GetAxiomsForTypeInternal(relationship.Type);
        if (axioms.Count == 0)
        {
            _logger.LogTrace(
                "No axioms found for relationship type {Type}, validation skipped",
                relationship.Type);
            return AxiomValidationResult.Valid(0, 0);
        }

        var allViolations = new List<AxiomViolation>();
        var totalRules = 0;

        foreach (var axiom in axioms)
        {
            totalRules += axiom.Rules.Count;
            var violations = _evaluator.Evaluate(axiom, relationship, fromEntity, toEntity);
            allViolations.AddRange(violations);
        }

        sw.Stop();

        if (allViolations.Count > 0)
        {
            _logger.LogInformation(
                "Relationship {RelType}:{RelId} validation found {ViolationCount} violations in {Elapsed:F2}ms",
                relationship.Type, relationship.Id, allViolations.Count, sw.Elapsed.TotalMilliseconds);
        }
        else
        {
            _logger.LogDebug(
                "Relationship {RelType}:{RelId} validation passed ({AxiomCount} axioms, {RuleCount} rules) in {Elapsed:F2}ms",
                relationship.Type, relationship.Id, axioms.Count, totalRules, sw.Elapsed.TotalMilliseconds);
        }

        return allViolations.Count > 0
            ? AxiomValidationResult.WithViolations(allViolations, axioms.Count, totalRules)
            : AxiomValidationResult.Valid(axioms.Count, totalRules);
    }

    #endregion

    #region Load Operations

    /// <inheritdoc/>
    public async Task LoadAxiomsAsync(CancellationToken ct = default)
    {
        RequireLicenseTier(LicenseTier.WriterPro, "Axiom loading");

        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Loading axioms from all sources...");

        try
        {
            var result = await _loader.LoadAllAsync(ct);
            await RebuildCacheFromRepositoryAsync(result, sw, isReload: false, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load axioms");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task LoadAxiomsAsync(string axiomDirectory, CancellationToken ct = default)
    {
        RequireLicenseTier(LicenseTier.Teams, "Custom axiom directory loading");
        ArgumentNullException.ThrowIfNull(axiomDirectory);

        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Loading axioms from directory: {Directory}", axiomDirectory);

        try
        {
            var result = await _loader.LoadWorkspaceAsync(axiomDirectory, ct);
            await RebuildCacheFromRepositoryAsync(result, sw, isReload: false, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load axioms from {Directory}", axiomDirectory);
            throw;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Internal retrieval without license check (for validation methods that already checked).
    /// </summary>
    private IReadOnlyList<Axiom> GetAxiomsForTypeInternal(string targetType)
    {
        if (string.IsNullOrWhiteSpace(targetType))
            return Array.Empty<Axiom>();

        return _cache.EnabledByType.TryGetValue(targetType, out var axioms)
            ? axioms
            : Array.Empty<Axiom>();
    }

    /// <summary>
    /// Rebuilds the in-memory cache from the repository.
    /// </summary>
    private async Task RebuildCacheFromRepositoryAsync(
        AxiomLoadResult result,
        Stopwatch sw,
        bool isReload,
        CancellationToken ct)
    {
        // Await repository call outside the lock
        var allAxioms = (await _repository.GetAllAsync(ct)).ToList();

        lock (_reloadLock)
        {
            // Build indexes
            var enabledAxioms = allAxioms.Where(a => a.IsEnabled).ToList();
            var enabledByType = enabledAxioms
                .GroupBy(a => a.TargetType, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<Axiom>)g.ToList().AsReadOnly(),
                    StringComparer.OrdinalIgnoreCase);

            // Replace cache atomically
            _cache = new AxiomCache(
                allAxioms.AsReadOnly(),
                enabledAxioms.AsReadOnly(),
                enabledByType);

            // Update statistics
            sw.Stop();
            _lastLoadedAt = DateTimeOffset.UtcNow;
            _lastLoadDuration = sw.Elapsed;
            _sourceFilesLoaded = result.FilesProcessed.Count;
        }

        _logger.LogInformation(
            "Axiom cache rebuilt: {Total} total, {Enabled} enabled, {Types} types in {Elapsed:F2}ms",
            _cache.AllAxioms.Count,
            _cache.EnabledAxioms.Count,
            _cache.EnabledByType.Count,
            sw.Elapsed.TotalMilliseconds);

        // Raise event
        OnAxiomsLoadedInternal(result, isReload);
    }

    /// <summary>
    /// Handler for loader's AxiomsReloaded event (hot-reload).
    /// </summary>
    private async void OnAxiomsReloaded(object? sender, AxiomLoadResult result)
    {
        _logger.LogInformation("AxiomLoader triggered reload, rebuilding cache...");

        try
        {
            var sw = Stopwatch.StartNew();
            await RebuildCacheFromRepositoryAsync(result, sw, isReload: true, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rebuild cache on reload");
        }
    }

    /// <summary>
    /// Raises the AxiomsLoaded event.
    /// </summary>
    private void OnAxiomsLoadedInternal(AxiomLoadResult result, bool isReload)
    {
        var args = new AxiomsLoadedEventArgs
        {
            AxiomCount = _cache.AllAxioms.Count,
            LoadResult = result,
            IsReload = isReload
        };

        try
        {
            AxiomsLoaded?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception in AxiomsLoaded event handler");
        }
    }

    /// <summary>
    /// Checks license tier and throws if not met.
    /// </summary>
    private void RequireLicenseTier(LicenseTier required, string featureName)
    {
        var current = _license.GetCurrentTier();
        if (current < required)
        {
            _logger.LogWarning(
                "{Feature} requires {Required} license, current tier is {Current}",
                featureName, required, current);
            throw new FeatureNotLicensedException(
                $"{featureName} requires {required} license or higher.",
                required);
        }
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// Immutable cache structure for thread-safe access.
    /// </summary>
    private sealed class AxiomCache
    {
        public static readonly AxiomCache Empty = new(
            Array.Empty<Axiom>(),
            Array.Empty<Axiom>(),
            new Dictionary<string, IReadOnlyList<Axiom>>());

        public IReadOnlyList<Axiom> AllAxioms { get; }
        public IReadOnlyList<Axiom> EnabledAxioms { get; }
        public IReadOnlyDictionary<string, IReadOnlyList<Axiom>> EnabledByType { get; }

        public AxiomCache(
            IReadOnlyList<Axiom> allAxioms,
            IReadOnlyList<Axiom> enabledAxioms,
            IReadOnlyDictionary<string, IReadOnlyList<Axiom>> enabledByType)
        {
            AllAxioms = allAxioms;
            EnabledAxioms = enabledAxioms;
            EnabledByType = enabledByType;
        }
    }

    #endregion
}
