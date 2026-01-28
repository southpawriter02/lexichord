using Lexichord.Abstractions.Entities;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Repository for SystemSettings key-value operations.
/// </summary>
/// <remarks>
/// LOGIC: Provides a simplified interface for key-value settings operations.
/// Unlike IGenericRepository, this interface is optimized for key-based access.
/// </remarks>
public interface ISystemSettingsRepository
{
    /// <summary>
    /// Gets a setting by key.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The setting, or null if not found.</returns>
    Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a setting value by key, returning a default if not found.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="defaultValue">The default value if setting not found.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The setting value, or the default.</returns>
    Task<string> GetValueAsync(
        string key,
        string defaultValue = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a typed setting value by key.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="defaultValue">The default value if setting not found or conversion fails.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The typed setting value, or the default.</returns>
    /// <example>
    /// <code>
    /// var maxRetries = await settings.GetValueAsync&lt;int&gt;("app:max_retries", 3);
    /// var isEnabled = await settings.GetValueAsync&lt;bool&gt;("feature:dark_mode", false);
    /// </code>
    /// </example>
    Task<T> GetValueAsync<T>(
        string key,
        T defaultValue,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a setting value (upsert).
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The setting value.</param>
    /// <param name="description">Optional description (only set on insert, not update).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetValueAsync(
        string key,
        string value,
        string? description = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a typed setting value (upsert).
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The setting value.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetValueAsync<T>(
        string key,
        T value,
        string? description = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All settings.</returns>
    Task<IEnumerable<SystemSetting>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all settings matching a key prefix.
    /// </summary>
    /// <param name="prefix">The key prefix (e.g., "feature:" returns all feature flags).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching settings.</returns>
    Task<IEnumerable<SystemSetting>> GetByPrefixAsync(
        string prefix,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a setting by key.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);
}
