// =============================================================================
// File: AxiomRepository.cs
// Project: Lexichord.Modules.Knowledge
// Description: Dapper implementation of IAxiomRepository for axiom persistence.
// =============================================================================
// LOGIC: Provides CRUD operations for axioms in PostgreSQL.
//   - Read operations use caching via IAxiomCacheService.
//   - Write operations require Teams+ license tier.
//   - JSON serialization for Rules and Tags using System.Text.Json.
//   - Comprehensive logging at Debug level for troubleshooting.
//
// v0.4.6f: Axiom Repository (CKVS Phase 1c)
// Dependencies: IDbConnectionFactory (v0.0.5b), ILicenseContext (v0.0.4c),
//               Axiom (v0.4.6e), Dapper, System.Text.Json
// =============================================================================

using System.Text.Json;
using Dapper;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Microsoft.Extensions.Logging;
using DapperCommandDefinition = Dapper.CommandDefinition;

namespace Lexichord.Modules.Knowledge.Axioms;

/// <summary>
/// Dapper-based implementation of <see cref="IAxiomRepository"/> for managing
/// axioms in PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// This repository provides all CRUD operations for the <c>Axioms</c> table,
/// with in-memory caching and license-gated write operations.
/// </para>
/// <para>
/// <b>Connection Management:</b> Connections are obtained via <see cref="IDbConnectionFactory"/>
/// and disposed using <c>await using</c> to prevent connection leaks.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This implementation is thread-safe. Each method creates
/// its own connection and uses parameterized queries. Suitable for concurrent use.
/// </para>
/// <para>
/// <b>License Requirements:</b>
/// <list type="bullet">
///   <item><description>Read operations: WriterPro+ tier.</description></item>
///   <item><description>Write operations: Teams+ tier.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class AxiomRepository : IAxiomRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IAxiomCacheService _cache;
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<AxiomRepository> _logger;

    private const string TableName = "Axioms";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Creates a new <see cref="AxiomRepository"/> instance.
    /// </summary>
    /// <param name="connectionFactory">Factory for database connections.</param>
    /// <param name="cache">Cache service for axiom storage.</param>
    /// <param name="licenseContext">License context for tier checks.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public AxiomRepository(
        IDbConnectionFactory connectionFactory,
        IAxiomCacheService cache,
        ILicenseContext licenseContext,
        ILogger<AxiomRepository> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Read Operations

    /// <inheritdoc />
    public async Task<IReadOnlyList<Axiom>> GetByTypeAsync(string entityType, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(entityType);

        // LOGIC: Check cache first.
        if (_cache.TryGetByType(entityType, out var cached) && cached is not null)
        {
            _logger.LogDebug("GetByType cache hit for {EntityType}", entityType);
            return cached;
        }

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = """
            SELECT "Id", "Name", "Description", "TargetType", "TargetKind",
                   "RulesJson", "Severity", "Category", "TagsJson",
                   "IsEnabled", "SourceFile", "SchemaVersion",
                   "CreatedAt", "UpdatedAt", "Version"
            FROM "Axioms"
            WHERE "TargetType" = @TargetType AND "IsEnabled" = true
            ORDER BY "Name"
            """;

        var command = new DapperCommandDefinition(sql, new { TargetType = entityType }, cancellationToken: ct);
        var entities = await connection.QueryAsync<AxiomEntity>(command);
        var axioms = entities.Select(MapToAxiom).ToList();

        // LOGIC: Cache the result for future queries.
        _cache.SetByType(entityType, axioms);

        _logger.LogDebug("GetByType {Table} TargetType={TargetType}: {Count} axioms",
            TableName, entityType, axioms.Count);

        return axioms;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Axiom>> GetAsync(AxiomFilter filter, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        // LOGIC: Build dynamic WHERE clause based on filter criteria.
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(filter.TargetType))
        {
            conditions.Add("""
"TargetType" = @TargetType
""");
            parameters.Add("TargetType", filter.TargetType);
        }

        if (!string.IsNullOrEmpty(filter.Category))
        {
            conditions.Add("""
"Category" = @Category
""");
            parameters.Add("Category", filter.Category);
        }

        if (filter.IsEnabled.HasValue)
        {
            conditions.Add("""
"IsEnabled" = @IsEnabled
""");
            parameters.Add("IsEnabled", filter.IsEnabled.Value);
        }

        if (filter.Tags is { Count: > 0 })
        {
            // LOGIC: JSONB containment check — axiom must have ALL specified tags.
            conditions.Add("""
"TagsJson" @> @TagsJson::jsonb
""");
            parameters.Add("TagsJson", JsonSerializer.Serialize(filter.Tags, JsonOptions));
        }

        var whereClause = conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", conditions)
            : string.Empty;

        var sql = $"""
            SELECT "Id", "Name", "Description", "TargetType", "TargetKind",
                   "RulesJson", "Severity", "Category", "TagsJson",
                   "IsEnabled", "SourceFile", "SchemaVersion",
                   "CreatedAt", "UpdatedAt", "Version"
            FROM "Axioms"
            {whereClause}
            ORDER BY "Name"
            """;

        var command = new DapperCommandDefinition(sql, parameters, cancellationToken: ct);
        var entities = await connection.QueryAsync<AxiomEntity>(command);
        var axioms = entities.Select(MapToAxiom).ToList();

        _logger.LogDebug("GetAsync {Table} with filter: {Count} axioms",
            TableName, axioms.Count);

        return axioms;
    }

    /// <inheritdoc />
    public async Task<Axiom?> GetByIdAsync(string axiomId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(axiomId);

        // LOGIC: Check cache first.
        if (_cache.TryGet(axiomId, out var cached))
        {
            _logger.LogDebug("GetById cache hit for {AxiomId}", axiomId);
            return cached;
        }

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = """
            SELECT "Id", "Name", "Description", "TargetType", "TargetKind",
                   "RulesJson", "Severity", "Category", "TagsJson",
                   "IsEnabled", "SourceFile", "SchemaVersion",
                   "CreatedAt", "UpdatedAt", "Version"
            FROM "Axioms"
            WHERE "Id" = @Id
            """;

        var command = new DapperCommandDefinition(sql, new { Id = axiomId }, cancellationToken: ct);
        var entity = await connection.QuerySingleOrDefaultAsync<AxiomEntity>(command);

        if (entity is null)
        {
            _logger.LogDebug("GetById {Table} Id={AxiomId}: NotFound", TableName, axiomId);
            return null;
        }

        var axiom = MapToAxiom(entity);

        // LOGIC: Cache the result for future queries.
        _cache.Set(axiom);

        _logger.LogDebug("GetById {Table} Id={AxiomId}: Found", TableName, axiomId);
        return axiom;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Axiom>> GetAllAsync(CancellationToken ct = default)
    {
        // LOGIC: Check cache first.
        if (_cache.TryGetAll(out var cached) && cached is not null)
        {
            _logger.LogDebug("GetAll cache hit");
            return cached;
        }

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = """
            SELECT "Id", "Name", "Description", "TargetType", "TargetKind",
                   "RulesJson", "Severity", "Category", "TagsJson",
                   "IsEnabled", "SourceFile", "SchemaVersion",
                   "CreatedAt", "UpdatedAt", "Version"
            FROM "Axioms"
            WHERE "IsEnabled" = true
            ORDER BY "TargetType", "Name"
            """;

        var command = new DapperCommandDefinition(sql, cancellationToken: ct);
        var entities = await connection.QueryAsync<AxiomEntity>(command);
        var axioms = entities.Select(MapToAxiom).ToList();

        // LOGIC: Cache the result for future queries.
        _cache.SetAll(axioms);

        _logger.LogDebug("GetAll {Table}: {Count} axioms", TableName, axioms.Count);
        return axioms;
    }

    /// <inheritdoc />
    public async Task<AxiomStatistics> GetStatisticsAsync(CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        // LOGIC: Aggregate counts in a single query for efficiency.
        const string sql = """
            SELECT 
                COUNT(*) AS "TotalCount",
                COUNT(*) FILTER (WHERE "IsEnabled" = true) AS "EnabledCount",
                COUNT(*) FILTER (WHERE "IsEnabled" = false) AS "DisabledCount"
            FROM "Axioms"
            """;

        var command = new DapperCommandDefinition(sql, cancellationToken: ct);
        var counts = await connection.QuerySingleAsync<dynamic>(command);

        // LOGIC: Get counts by category.
        const string categorySQL = """
            SELECT COALESCE("Category", '') AS "Category", COUNT(*) AS "Count"
            FROM "Axioms"
            GROUP BY "Category"
            """;

        var categoryCommand = new DapperCommandDefinition(categorySQL, cancellationToken: ct);
        var categoryResults = await connection.QueryAsync<dynamic>(categoryCommand);
        var byCategory = categoryResults.ToDictionary(
            r => (string)r.Category,
            r => (int)r.Count);

        // LOGIC: Get counts by target type.
        const string typeSQL = """
            SELECT "TargetType", COUNT(*) AS "Count"
            FROM "Axioms"
            GROUP BY "TargetType"
            """;

        var typeCommand = new DapperCommandDefinition(typeSQL, cancellationToken: ct);
        var typeResults = await connection.QueryAsync<dynamic>(typeCommand);
        var byTargetType = typeResults.ToDictionary(
            r => (string)r.TargetType,
            r => (int)r.Count);

        var statistics = new AxiomStatistics
        {
            TotalCount = (int)counts.TotalCount,
            EnabledCount = (int)counts.EnabledCount,
            DisabledCount = (int)counts.DisabledCount,
            ByCategory = byCategory,
            ByTargetType = byTargetType
        };

        _logger.LogDebug("GetStatistics: {TotalCount} total, {EnabledCount} enabled",
            statistics.TotalCount, statistics.EnabledCount);

        return statistics;
    }

    #endregion

    #region Write Operations

    /// <inheritdoc />
    public async Task<Axiom> SaveAsync(Axiom axiom, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(axiom);

        // LOGIC: Check license tier for write operations.
        EnsureTeamsLicense();

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        var entity = MapToEntity(axiom);
        var now = DateTimeOffset.UtcNow;

        // LOGIC: Upsert with version increment on update.
        const string sql = """
            INSERT INTO "Axioms" (
                "Id", "Name", "Description", "TargetType", "TargetKind",
                "RulesJson", "Severity", "Category", "TagsJson",
                "IsEnabled", "SourceFile", "SchemaVersion",
                "CreatedAt", "UpdatedAt", "Version"
            )
            VALUES (
                @Id, @Name, @Description, @TargetType, @TargetKind,
                @RulesJson::jsonb, @Severity, @Category, @TagsJson::jsonb,
                @IsEnabled, @SourceFile, @SchemaVersion,
                @CreatedAt, @UpdatedAt, 1
            )
            ON CONFLICT ("Id") DO UPDATE SET
                "Name" = EXCLUDED."Name",
                "Description" = EXCLUDED."Description",
                "TargetType" = EXCLUDED."TargetType",
                "TargetKind" = EXCLUDED."TargetKind",
                "RulesJson" = EXCLUDED."RulesJson",
                "Severity" = EXCLUDED."Severity",
                "Category" = EXCLUDED."Category",
                "TagsJson" = EXCLUDED."TagsJson",
                "IsEnabled" = EXCLUDED."IsEnabled",
                "SourceFile" = EXCLUDED."SourceFile",
                "SchemaVersion" = EXCLUDED."SchemaVersion",
                "UpdatedAt" = EXCLUDED."UpdatedAt",
                "Version" = "Axioms"."Version" + 1
            RETURNING "Id", "Name", "Description", "TargetType", "TargetKind",
                      "RulesJson", "Severity", "Category", "TagsJson",
                      "IsEnabled", "SourceFile", "SchemaVersion",
                      "CreatedAt", "UpdatedAt", "Version"
            """;

        var parameters = new
        {
            entity.Id,
            entity.Name,
            entity.Description,
            entity.TargetType,
            entity.TargetKind,
            entity.RulesJson,
            entity.Severity,
            entity.Category,
            entity.TagsJson,
            entity.IsEnabled,
            entity.SourceFile,
            entity.SchemaVersion,
            CreatedAt = now,
            UpdatedAt = now
        };

        var command = new DapperCommandDefinition(sql, parameters, cancellationToken: ct);
        var saved = await connection.QuerySingleAsync<AxiomEntity>(command);

        // LOGIC: Invalidate cache entries.
        _cache.Invalidate(axiom.Id);
        _cache.InvalidateByType(axiom.TargetType);

        _logger.LogInformation("Saved axiom {AxiomId} (version {Version})",
            saved.Id, saved.Version);

        return MapToAxiom(saved);
    }

    /// <inheritdoc />
    public async Task<int> SaveBatchAsync(IEnumerable<Axiom> axioms, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(axioms);

        // LOGIC: Check license tier for write operations.
        EnsureTeamsLicense();

        var axiomList = axioms.ToList();
        if (axiomList.Count == 0)
        {
            return 0;
        }

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        var now = DateTimeOffset.UtcNow;
        var savedCount = 0;

        try
        {
            foreach (var axiom in axiomList)
            {
                var entity = MapToEntity(axiom);

                const string sql = """
                    INSERT INTO "Axioms" (
                        "Id", "Name", "Description", "TargetType", "TargetKind",
                        "RulesJson", "Severity", "Category", "TagsJson",
                        "IsEnabled", "SourceFile", "SchemaVersion",
                        "CreatedAt", "UpdatedAt", "Version"
                    )
                    VALUES (
                        @Id, @Name, @Description, @TargetType, @TargetKind,
                        @RulesJson::jsonb, @Severity, @Category, @TagsJson::jsonb,
                        @IsEnabled, @SourceFile, @SchemaVersion,
                        @CreatedAt, @UpdatedAt, 1
                    )
                    ON CONFLICT ("Id") DO UPDATE SET
                        "Name" = EXCLUDED."Name",
                        "Description" = EXCLUDED."Description",
                        "TargetType" = EXCLUDED."TargetType",
                        "TargetKind" = EXCLUDED."TargetKind",
                        "RulesJson" = EXCLUDED."RulesJson",
                        "Severity" = EXCLUDED."Severity",
                        "Category" = EXCLUDED."Category",
                        "TagsJson" = EXCLUDED."TagsJson",
                        "IsEnabled" = EXCLUDED."IsEnabled",
                        "SourceFile" = EXCLUDED."SourceFile",
                        "SchemaVersion" = EXCLUDED."SchemaVersion",
                        "UpdatedAt" = EXCLUDED."UpdatedAt",
                        "Version" = "Axioms"."Version" + 1
                    """;

                var parameters = new
                {
                    entity.Id,
                    entity.Name,
                    entity.Description,
                    entity.TargetType,
                    entity.TargetKind,
                    entity.RulesJson,
                    entity.Severity,
                    entity.Category,
                    entity.TagsJson,
                    entity.IsEnabled,
                    entity.SourceFile,
                    entity.SchemaVersion,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                var command = new DapperCommandDefinition(sql, parameters, transaction, cancellationToken: ct);
                await connection.ExecuteAsync(command);
                savedCount++;
            }

            await transaction.CommitAsync(ct);

            // LOGIC: Invalidate all cache entries after batch save.
            _cache.InvalidateAll();

            _logger.LogInformation("Batch saved {Count} axioms", savedCount);
            return savedCount;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string axiomId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(axiomId);

        // LOGIC: Check license tier for write operations.
        EnsureTeamsLicense();

        // LOGIC: Get the axiom first to know its target type for cache invalidation.
        var existing = await GetByIdAsync(axiomId, ct);

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = """
            DELETE FROM "Axioms" WHERE "Id" = @Id
            """;

        var command = new DapperCommandDefinition(sql, new { Id = axiomId }, cancellationToken: ct);
        var affected = await connection.ExecuteAsync(command);

        if (affected > 0)
        {
            // LOGIC: Invalidate cache entries.
            _cache.Invalidate(axiomId);
            if (existing is not null)
            {
                _cache.InvalidateByType(existing.TargetType);
            }

            _logger.LogInformation("Deleted axiom {AxiomId}", axiomId);
            return true;
        }

        _logger.LogDebug("Delete {Table} Id={AxiomId}: NotFound", TableName, axiomId);
        return false;
    }

    /// <inheritdoc />
    public async Task<int> DeleteBySourceAsync(string sourceFile, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceFile);

        // LOGIC: Check license tier for write operations.
        EnsureTeamsLicense();

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = """
            DELETE FROM "Axioms" WHERE "SourceFile" = @SourceFile
            """;

        var command = new DapperCommandDefinition(sql, new { SourceFile = sourceFile }, cancellationToken: ct);
        var affected = await connection.ExecuteAsync(command);

        if (affected > 0)
        {
            // LOGIC: Invalidate all cache entries after source-based deletion.
            _cache.InvalidateAll();

            _logger.LogInformation("Deleted {Count} axioms from source {SourceFile}",
                affected, sourceFile);
        }
        else
        {
            _logger.LogDebug("DeleteBySource {Table} SourceFile={SourceFile}: No axioms found",
                TableName, sourceFile);
        }

        return affected;
    }

    #endregion

    #region Mapping

    private static Axiom MapToAxiom(AxiomEntity entity)
    {
        var rules = JsonSerializer.Deserialize<IReadOnlyList<AxiomRule>>(entity.RulesJson, JsonOptions)
            ?? Array.Empty<AxiomRule>();
        var tags = JsonSerializer.Deserialize<IReadOnlyList<string>>(entity.TagsJson, JsonOptions)
            ?? Array.Empty<string>();

        return new Axiom
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            TargetType = entity.TargetType,
            TargetKind = Enum.Parse<AxiomTargetKind>(entity.TargetKind),
            Rules = rules,
            Severity = Enum.Parse<AxiomSeverity>(entity.Severity),
            Category = entity.Category,
            Tags = tags,
            IsEnabled = entity.IsEnabled,
            SourceFile = entity.SourceFile,
            SchemaVersion = entity.SchemaVersion,
            CreatedAt = entity.CreatedAt
        };
    }

    private static AxiomEntity MapToEntity(Axiom axiom)
    {
        return new AxiomEntity
        {
            Id = axiom.Id,
            Name = axiom.Name,
            Description = axiom.Description,
            TargetType = axiom.TargetType,
            TargetKind = axiom.TargetKind.ToString(),
            RulesJson = JsonSerializer.Serialize(axiom.Rules, JsonOptions),
            Severity = axiom.Severity.ToString(),
            Category = axiom.Category,
            TagsJson = JsonSerializer.Serialize(axiom.Tags, JsonOptions),
            IsEnabled = axiom.IsEnabled,
            SourceFile = axiom.SourceFile,
            SchemaVersion = axiom.SchemaVersion,
            CreatedAt = axiom.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = 1
        };
    }

    #endregion

    #region License Checks

    private void EnsureTeamsLicense()
    {
        if (_licenseContext.GetCurrentTier() < LicenseTier.Teams)
        {
            _logger.LogWarning("License check failed: Teams+ required for axiom management");
            throw new FeatureNotLicensedException(
                "Axiom management requires a Teams license or higher.",
                LicenseTier.Teams);
        }
    }

    #endregion
}
