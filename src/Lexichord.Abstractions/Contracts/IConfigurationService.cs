// -----------------------------------------------------------------------
// <copyright file="IConfigurationService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for accessing application configuration sections.
/// </summary>
/// <remarks>
/// <para>
/// Provides a typed interface for reading configuration settings organized
/// by named sections. Configuration sources may include JSON files,
/// environment variables, or other providers.
/// </para>
/// <para>
/// Introduced in v0.7.7a to support workflow designer configuration
/// (storage paths, default categories, etc.).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var config = configService.GetSection&lt;WorkflowSettings&gt;("Workflows");
/// if (config != null)
/// {
///     var storagePath = config.StoragePath;
/// }
/// </code>
/// </example>
public interface IConfigurationService
{
    /// <summary>
    /// Gets a typed configuration section by name.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the configuration section into.</typeparam>
    /// <param name="sectionName">
    /// The name of the configuration section (e.g., "Workflows", "FeatureFlags").
    /// </param>
    /// <returns>
    /// The deserialized configuration section, or <c>null</c> if the section does not exist.
    /// </returns>
    T? GetSection<T>(string sectionName) where T : class;
}
