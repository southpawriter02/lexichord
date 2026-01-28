using System.Text.Json;
using Dapper;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Microsoft.Extensions.Logging;

namespace Lexichord.Infrastructure.Data;

/// <summary>
/// Repository implementation for SystemSettings.
/// </summary>
/// <remarks>
/// LOGIC: Provides key-value storage operations with typed accessors.
///
/// Value conversion:
/// - Primitive types are parsed directly (int, bool, double, etc.)
/// - Complex types are JSON serialized/deserialized
/// - Failed conversions return the default value with a warning log
/// </remarks>
public sealed class SystemSettingsRepository : ISystemSettingsRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<SystemSettingsRepository> _logger;

    /// <summary>
    /// Creates a new SystemSettingsRepository instance.
    /// </summary>
    public SystemSettingsRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<SystemSettingsRepository> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = @"SELECT * FROM ""SystemSettings"" WHERE ""Key"" = @Key";
        var command = new CommandDefinition(sql, new { Key = key }, cancellationToken: cancellationToken);
        var result = await connection.QuerySingleOrDefaultAsync<SystemSetting>(command);
        _logger.LogDebug("GetByKey '{Key}': {Result}", key, result is not null ? "Found" : "NotFound");
        return result;
    }

    /// <inheritdoc />
    public async Task<string> GetValueAsync(
        string key,
        string defaultValue = "",
        CancellationToken cancellationToken = default)
    {
        var setting = await GetByKeyAsync(key, cancellationToken);
        return setting?.Value ?? defaultValue;
    }

    /// <inheritdoc />
    public async Task<T> GetValueAsync<T>(
        string key,
        T defaultValue,
        CancellationToken cancellationToken = default)
    {
        var setting = await GetByKeyAsync(key, cancellationToken);
        if (setting is null)
        {
            return defaultValue;
        }

        try
        {
            return ConvertFromString<T>(setting.Value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to convert setting '{Key}' to {Type}, using default", key, typeof(T).Name);
            return defaultValue;
        }
    }

    /// <inheritdoc />
    public async Task SetValueAsync(
        string key,
        string value,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        // PostgreSQL UPSERT using ON CONFLICT
        var sql = @"
            INSERT INTO ""SystemSettings"" (""Key"", ""Value"", ""Description"", ""UpdatedAt"")
            VALUES (@Key, @Value, @Description, NOW())
            ON CONFLICT (""Key"")
            DO UPDATE SET ""Value"" = @Value, ""UpdatedAt"" = NOW()";
        var command = new CommandDefinition(
            sql,
            new { Key = key, Value = value, Description = description },
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
        _logger.LogDebug("SetValue '{Key}' = '{Value}'", key, TruncateValue(value));
    }

    /// <inheritdoc />
    public async Task SetValueAsync<T>(
        string key,
        T value,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var stringValue = ConvertToString(value);
        await SetValueAsync(key, stringValue, description, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SystemSetting>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = @"SELECT * FROM ""SystemSettings"" ORDER BY ""Key""";
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var results = await connection.QueryAsync<SystemSetting>(command);
        _logger.LogDebug("GetAll: {Count} settings", results.Count());
        return results;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SystemSetting>> GetByPrefixAsync(
        string prefix,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = @"SELECT * FROM ""SystemSettings"" WHERE ""Key"" LIKE @Prefix ORDER BY ""Key""";
        var command = new CommandDefinition(
            sql,
            new { Prefix = prefix + "%" },
            cancellationToken: cancellationToken);
        var results = await connection.QueryAsync<SystemSetting>(command);
        _logger.LogDebug("GetByPrefix '{Prefix}': {Count} settings", prefix, results.Count());
        return results;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = @"DELETE FROM ""SystemSettings"" WHERE ""Key"" = @Key";
        var command = new CommandDefinition(sql, new { Key = key }, cancellationToken: cancellationToken);
        var affected = await connection.ExecuteAsync(command);
        var result = affected > 0;
        _logger.LogDebug("Delete '{Key}': {Result}", key, result ? "Success" : "NotFound");
        return result;
    }

    #region Type Conversion Helpers

    /// <summary>
    /// Converts a typed value to a string for storage.
    /// </summary>
    private static string ConvertToString<T>(T value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        // LOGIC: Primitives are stored as-is, complex types as JSON
        return Type.GetTypeCode(typeof(T)) switch
        {
            TypeCode.Boolean => value.ToString()!.ToLowerInvariant(),
            TypeCode.String => value.ToString()!,
            TypeCode.Int32 or TypeCode.Int64 or TypeCode.Double or TypeCode.Decimal =>
                value.ToString()!,
            _ => JsonSerializer.Serialize(value)
        };
    }

    /// <summary>
    /// Converts a string value to the requested type.
    /// </summary>
    private static T ConvertFromString<T>(string value)
    {
        var targetType = typeof(T);

        // LOGIC: Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var typeCode = Type.GetTypeCode(underlyingType);

        return typeCode switch
        {
            TypeCode.Boolean => (T)(object)bool.Parse(value),
            TypeCode.String => (T)(object)value,
            TypeCode.Int32 => (T)(object)int.Parse(value),
            TypeCode.Int64 => (T)(object)long.Parse(value),
            TypeCode.Double => (T)(object)double.Parse(value),
            TypeCode.Decimal => (T)(object)decimal.Parse(value),
            _ => JsonSerializer.Deserialize<T>(value)!
        };
    }

    /// <summary>
    /// Truncates long values for logging.
    /// </summary>
    private static string TruncateValue(string value, int maxLength = 50)
    {
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }

    #endregion
}
