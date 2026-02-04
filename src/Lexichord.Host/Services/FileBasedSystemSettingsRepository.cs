// -----------------------------------------------------------------------
// <copyright file="FileBasedSystemSettingsRepository.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Services;

/// <summary>
/// File-based implementation of ISystemSettingsRepository for local development.
/// </summary>
/// <remarks>
/// <para>
/// LOGIC: This implementation stores system settings as JSON in the user's AppData directory.
/// It provides a database-free fallback for development scenarios where PostgreSQL
/// isn't running. For production use with persistence across devices, the database-backed
/// repository in Lexichord.Infrastructure should be used.
/// </para>
/// <para>
/// v0.6.3b: Created to support Workspace module initialization without requiring
/// a database connection. The Workspace module uses ISystemSettingsRepository for
/// storing recent workspaces configuration.
/// </para>
/// </remarks>
public sealed class FileBasedSystemSettingsRepository : ISystemSettingsRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _filePath;
    private readonly ILogger<FileBasedSystemSettingsRepository> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="FileBasedSystemSettingsRepository"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public FileBasedSystemSettingsRepository(ILogger<FileBasedSystemSettingsRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // LOGIC: Store in user's AppData alongside other Lexichord settings
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var lexichordDir = Path.Combine(appData, "Lexichord");
        Directory.CreateDirectory(lexichordDir);
        _filePath = Path.Combine(lexichordDir, "system-settings.json");

        _logger.LogDebug(
            "FileBasedSystemSettingsRepository initialized. Path: {FilePath}",
            _filePath);
    }

    /// <inheritdoc />
    public async Task<SystemSetting?> GetByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var settings = await LoadSettingsAsync(cancellationToken).ConfigureAwait(false);
            return settings.TryGetValue(key, out var setting) ? setting : null;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<string> GetValueAsync(
        string key,
        string defaultValue = "",
        CancellationToken cancellationToken = default)
    {
        var setting = await GetByKeyAsync(key, cancellationToken).ConfigureAwait(false);
        return setting?.Value ?? defaultValue;
    }

    /// <inheritdoc />
    public async Task<T> GetValueAsync<T>(
        string key,
        T defaultValue,
        CancellationToken cancellationToken = default)
    {
        var stringValue = await GetValueAsync(key, string.Empty, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(stringValue))
        {
            return defaultValue;
        }

        try
        {
            // LOGIC: Handle primitive types directly, use JSON for complex types
            var targetType = typeof(T);

            if (targetType == typeof(string))
            {
                return (T)(object)stringValue;
            }

            if (targetType == typeof(bool))
            {
                return bool.TryParse(stringValue, out var boolResult)
                    ? (T)(object)boolResult
                    : defaultValue;
            }

            if (targetType == typeof(int))
            {
                return int.TryParse(stringValue, out var intResult)
                    ? (T)(object)intResult
                    : defaultValue;
            }

            if (targetType == typeof(long))
            {
                return long.TryParse(stringValue, out var longResult)
                    ? (T)(object)longResult
                    : defaultValue;
            }

            if (targetType == typeof(double))
            {
                return double.TryParse(stringValue, out var doubleResult)
                    ? (T)(object)doubleResult
                    : defaultValue;
            }

            // LOGIC: For complex types, try JSON deserialization
            var result = JsonSerializer.Deserialize<T>(stringValue, JsonOptions);
            return result ?? defaultValue;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to convert setting '{Key}' value '{Value}' to type {Type}, using default",
                key,
                stringValue,
                typeof(T).Name);
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
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var settings = await LoadSettingsAsync(cancellationToken).ConfigureAwait(false);

            // LOGIC: Upsert - update existing or create new
            if (settings.TryGetValue(key, out var existing))
            {
                // Keep existing description if not provided
                settings[key] = existing with
                {
                    Value = value,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
            }
            else
            {
                settings[key] = new SystemSetting
                {
                    Key = key,
                    Value = value,
                    Description = description,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
            }

            await SaveSettingsAsync(settings, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Set system setting: {Key}", key);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task SetValueAsync<T>(
        string key,
        T value,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        // LOGIC: Convert value to string based on type
        string stringValue;

        if (value is string strValue)
        {
            stringValue = strValue;
        }
        else if (value is bool or int or long or double)
        {
            stringValue = value?.ToString() ?? string.Empty;
        }
        else
        {
            // LOGIC: For complex types, use JSON serialization
            stringValue = JsonSerializer.Serialize(value, JsonOptions);
        }

        await SetValueAsync(key, stringValue, description, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SystemSetting>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var settings = await LoadSettingsAsync(cancellationToken).ConfigureAwait(false);
            return settings.Values.ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SystemSetting>> GetByPrefixAsync(
        string prefix,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var settings = await LoadSettingsAsync(cancellationToken).ConfigureAwait(false);
            return settings.Values
                .Where(s => s.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var settings = await LoadSettingsAsync(cancellationToken).ConfigureAwait(false);

            if (settings.Remove(key))
            {
                await SaveSettingsAsync(settings, cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("Deleted system setting: {Key}", key);
                return true;
            }

            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Loads settings from the JSON file.
    /// </summary>
    private async Task<Dictionary<string, SystemSetting>> LoadSettingsAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new Dictionary<string, SystemSetting>(StringComparer.OrdinalIgnoreCase);
            }

            var json = await File.ReadAllTextAsync(_filePath, cancellationToken)
                .ConfigureAwait(false);

            var list = JsonSerializer.Deserialize<List<SystemSetting>>(json, JsonOptions);

            return list?.ToDictionary(
                s => s.Key,
                s => s,
                StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, SystemSetting>(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to load system settings from {FilePath}",
                _filePath);
            return new Dictionary<string, SystemSetting>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Saves settings to the JSON file.
    /// </summary>
    private async Task SaveSettingsAsync(
        Dictionary<string, SystemSetting> settings,
        CancellationToken cancellationToken)
    {
        try
        {
            var list = settings.Values.OrderBy(s => s.Key).ToList();
            var json = JsonSerializer.Serialize(list, JsonOptions);
            await File.WriteAllTextAsync(_filePath, json, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to save system settings to {FilePath}",
                _filePath);
        }
    }
}
