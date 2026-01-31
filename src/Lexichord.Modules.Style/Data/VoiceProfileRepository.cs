// <copyright file="VoiceProfileRepository.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using System.Text.Json;
using Dapper;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;
using DapperCommandDefinition = Dapper.CommandDefinition;

namespace Lexichord.Modules.Style.Data;

/// <summary>
/// Repository implementation for Voice Profile persistence.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.4a - Handles CRUD operations for custom (non-built-in) profiles only.
/// Built-in profiles are defined in code and not persisted to the database.
/// 
/// Design decisions:
/// - Uses PostgreSQL for persistence consistent with other domain entities
/// - ForbiddenCategories stored as JSON array in TEXT column
/// - Self-contained implementation following TerminologyRepository patterns
/// </remarks>
public class VoiceProfileRepository : IVoiceProfileRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<VoiceProfileRepository> _logger;

    private const string TableName = "voice_profiles";

    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceProfileRepository"/> class.
    /// </summary>
    /// <param name="connectionFactory">Database connection factory.</param>
    /// <param name="logger">Logger instance.</param>
    public VoiceProfileRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<VoiceProfileRepository> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VoiceProfile>> GetAllAsync(CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT 
                id,
                name,
                description,
                target_grade_level,
                grade_level_tolerance,
                max_sentence_length,
                allow_passive_voice,
                max_passive_voice_percentage,
                flag_adverbs,
                flag_weasel_words,
                forbidden_categories,
                is_built_in,
                sort_order
            FROM voice_profiles
            ORDER BY sort_order, name";

        var command = new DapperCommandDefinition(sql, cancellationToken: ct);
        var rows = await connection.QueryAsync<VoiceProfileRow>(command);
        var profiles = rows.Select(MapToProfile).ToList().AsReadOnly();

        _logger.LogDebug("GetAll {Table}: {Count} custom profiles", TableName, profiles.Count);
        return profiles;
    }

    /// <inheritdoc />
    public async Task<VoiceProfile?> GetAsync(Guid id, CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT 
                id,
                name,
                description,
                target_grade_level,
                grade_level_tolerance,
                max_sentence_length,
                allow_passive_voice,
                max_passive_voice_percentage,
                flag_adverbs,
                flag_weasel_words,
                forbidden_categories,
                is_built_in,
                sort_order
            FROM voice_profiles
            WHERE id = @Id";

        var command = new DapperCommandDefinition(sql, new { Id = id }, cancellationToken: ct);
        var row = await connection.QuerySingleOrDefaultAsync<VoiceProfileRow>(command);

        if (row is null)
        {
            _logger.LogDebug("GetById {Table} Id={Id}: NotFound", TableName, id);
            return null;
        }

        _logger.LogDebug("GetById {Table} Id={Id}: Found", TableName, id);
        return MapToProfile(row);
    }

    /// <inheritdoc />
    public async Task CreateAsync(VoiceProfile profile, CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            INSERT INTO voice_profiles (
                id,
                name,
                description,
                target_grade_level,
                grade_level_tolerance,
                max_sentence_length,
                allow_passive_voice,
                max_passive_voice_percentage,
                flag_adverbs,
                flag_weasel_words,
                forbidden_categories,
                is_built_in,
                sort_order,
                created_at,
                updated_at
            ) VALUES (
                @Id,
                @Name,
                @Description,
                @TargetGradeLevel,
                @GradeLevelTolerance,
                @MaxSentenceLength,
                @AllowPassiveVoice,
                @MaxPassiveVoicePercentage,
                @FlagAdverbs,
                @FlagWeaselWords,
                @ForbiddenCategories,
                @IsBuiltIn,
                @SortOrder,
                NOW(),
                NOW()
            )";

        var parameters = new
        {
            profile.Id,
            profile.Name,
            profile.Description,
            profile.TargetGradeLevel,
            profile.GradeLevelTolerance,
            profile.MaxSentenceLength,
            profile.AllowPassiveVoice,
            profile.MaxPassiveVoicePercentage,
            profile.FlagAdverbs,
            profile.FlagWeaselWords,
            ForbiddenCategories = SerializeCategories(profile.ForbiddenCategories),
            profile.IsBuiltIn,
            profile.SortOrder
        };

        var command = new DapperCommandDefinition(sql, parameters, cancellationToken: ct);
        await connection.ExecuteAsync(command);

        _logger.LogDebug("Create {Table}: Success (Id={Id}, Name={Name})", TableName, profile.Id, profile.Name);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(VoiceProfile profile, CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            UPDATE voice_profiles SET
                name = @Name,
                description = @Description,
                target_grade_level = @TargetGradeLevel,
                grade_level_tolerance = @GradeLevelTolerance,
                max_sentence_length = @MaxSentenceLength,
                allow_passive_voice = @AllowPassiveVoice,
                max_passive_voice_percentage = @MaxPassiveVoicePercentage,
                flag_adverbs = @FlagAdverbs,
                flag_weasel_words = @FlagWeaselWords,
                forbidden_categories = @ForbiddenCategories,
                sort_order = @SortOrder,
                updated_at = NOW()
            WHERE id = @Id";

        var parameters = new
        {
            profile.Id,
            profile.Name,
            profile.Description,
            profile.TargetGradeLevel,
            profile.GradeLevelTolerance,
            profile.MaxSentenceLength,
            profile.AllowPassiveVoice,
            profile.MaxPassiveVoicePercentage,
            profile.FlagAdverbs,
            profile.FlagWeaselWords,
            ForbiddenCategories = SerializeCategories(profile.ForbiddenCategories),
            profile.SortOrder
        };

        var command = new DapperCommandDefinition(sql, parameters, cancellationToken: ct);
        var affected = await connection.ExecuteAsync(command);

        _logger.LogDebug("Update {Table} Id={Id}: {Result}", TableName, profile.Id, affected > 0 ? "Success" : "NotFound");
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = "DELETE FROM voice_profiles WHERE id = @Id";

        var command = new DapperCommandDefinition(sql, new { Id = id }, cancellationToken: ct);
        var affected = await connection.ExecuteAsync(command);

        _logger.LogDebug("Delete {Table} Id={Id}: {Result}", TableName, id, affected > 0 ? "Success" : "NotFound");
    }

    #region Mapping

    private static VoiceProfile MapToProfile(VoiceProfileRow row)
    {
        return new VoiceProfile
        {
            Id = row.Id,
            Name = row.Name,
            Description = row.Description,
            TargetGradeLevel = row.TargetGradeLevel,
            GradeLevelTolerance = row.GradeLevelTolerance,
            MaxSentenceLength = row.MaxSentenceLength,
            AllowPassiveVoice = row.AllowPassiveVoice,
            MaxPassiveVoicePercentage = row.MaxPassiveVoicePercentage,
            FlagAdverbs = row.FlagAdverbs,
            FlagWeaselWords = row.FlagWeaselWords,
            ForbiddenCategories = DeserializeCategories(row.ForbiddenCategories),
            IsBuiltIn = row.IsBuiltIn,
            SortOrder = row.SortOrder
        };
    }

    private static string? SerializeCategories(IReadOnlyList<string> categories)
    {
        if (categories.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(categories);
    }

    private static IReadOnlyList<string> DeserializeCategories(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }

        try
        {
            var result = JsonSerializer.Deserialize<List<string>>(json);
            if (result is not null)
            {
                return result.AsReadOnly();
            }
            return Array.Empty<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    #endregion

    #region Row Type

    /// <summary>
    /// Internal row type for Dapper mapping.
    /// </summary>
    private sealed record VoiceProfileRow
    {
        public Guid Id { get; init; }
        public required string Name { get; init; }
        public string? Description { get; init; }
        public double? TargetGradeLevel { get; init; }
        public double GradeLevelTolerance { get; init; }
        public int MaxSentenceLength { get; init; }
        public bool AllowPassiveVoice { get; init; }
        public double MaxPassiveVoicePercentage { get; init; }
        public bool FlagAdverbs { get; init; }
        public bool FlagWeaselWords { get; init; }
        public string? ForbiddenCategories { get; init; }
        public bool IsBuiltIn { get; init; }
        public int SortOrder { get; init; }
    }

    #endregion
}
