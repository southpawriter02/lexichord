// -----------------------------------------------------------------------
// <copyright file="ISettingsService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Provides type-safe access to application settings.
/// </summary>
/// <remarks>
/// <para>
/// The settings service abstracts settings retrieval from configuration sources
/// (user preferences, appsettings.json, environment variables). All reads use
/// a key-and-default pattern to ensure callers always receive a usable value.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe for concurrent reads.
/// Settings may be updated at runtime (e.g., user preference changes), but reads
/// should always return consistent values.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.1.6a as part of the Settings Infrastructure.
/// Interface defined in v0.6.6b for agent configuration access.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MyAgent(ISettingsService settings)
/// {
///     var temperature = settings.Get("Agent:Temperature", 0.7f);
///     var maxTokens = settings.Get("Agent:MaxResponseTokens", 2000);
///     var streaming = settings.Get("Agent:EnableStreaming", true);
/// }
/// </code>
/// </example>
public interface ISettingsService
{
    /// <summary>
    /// Gets a setting value by key, returning a default if not found.
    /// </summary>
    /// <typeparam name="T">
    /// The expected value type. Supported types include primitives
    /// (int, float, double, bool, string, decimal) and complex types
    /// that can be deserialized from the configuration source.
    /// </typeparam>
    /// <param name="key">
    /// The settings key using colon-delimited sections (e.g., "Agent:Temperature").
    /// Keys are case-insensitive.
    /// </param>
    /// <param name="defaultValue">
    /// Value to return if the key is not found or the stored value
    /// cannot be converted to <typeparamref name="T"/>.
    /// </param>
    /// <returns>
    /// The setting value if found and convertible; otherwise, <paramref name="defaultValue"/>.
    /// </returns>
    /// <remarks>
    /// LOGIC: Implementations should never throw for missing keys.
    /// The default value pattern ensures callers always get a usable value
    /// and keeps configuration optional rather than required.
    /// </remarks>
    T Get<T>(string key, T defaultValue);

    /// <summary>
    /// Sets a setting value by key.
    /// </summary>
    /// <typeparam name="T">
    /// The value type. Supported types include primitives
    /// (int, float, double, bool, string, decimal) and complex types
    /// that can be serialized to the configuration store.
    /// </typeparam>
    /// <param name="key">
    /// The settings key using colon-delimited sections (e.g., "Agent:CustomAgents").
    /// Keys are case-insensitive.
    /// </param>
    /// <param name="value">The value to store.</param>
    /// <remarks>
    /// LOGIC: Implementations should persist the value to the underlying
    /// configuration store (user settings file, database, etc.).
    /// <para><b>Introduced in:</b> v0.6.6c for custom agent persistence.</para>
    /// </remarks>
    void Set<T>(string key, T value);
}
