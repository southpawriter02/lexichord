// -----------------------------------------------------------------------
// <copyright file="PromptTemplateOptions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Templates;

/// <summary>
/// Configuration options for the <see cref="PromptTemplateRepository"/>.
/// </summary>
/// <remarks>
/// <para>
/// These options control how templates are loaded, cached, and monitored for changes.
/// Options can be configured via the Microsoft.Extensions.Options pattern:
/// </para>
/// <code>
/// // In appsettings.json
/// {
///   "Agents": {
///     "TemplateRepository": {
///       "EnableHotReload": true,
///       "FileWatcherDebounceMs": 200
///     }
///   }
/// }
///
/// // Or programmatically
/// services.Configure&lt;PromptTemplateOptions&gt;(options =>
/// {
///     options.EnableHotReload = true;
///     options.FileWatcherDebounceMs = 500;
/// });
/// </code>
/// <para>
/// <strong>License Gating:</strong>
/// </para>
/// <list type="bullet">
///   <item><description>Loading from Global/User directories requires <see cref="LicenseTier.WriterPro"/> or higher</description></item>
///   <item><description>Hot-reload functionality requires <see cref="LicenseTier.Teams"/> or higher</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Register with custom options
/// services.AddTemplateRepository(options =>
/// {
///     options.EnableHotReload = true;
///     options.FileWatcherDebounceMs = 300;
///     options.UserTemplatesPath = Path.Combine(
///         Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
///         "LexichordTemplates");
/// });
/// </code>
/// </example>
/// <seealso cref="PromptTemplateRepository"/>
public class PromptTemplateOptions
{
    /// <summary>
    /// The configuration section name for binding these options.
    /// </summary>
    /// <value>The string "Agents:TemplateRepository".</value>
    public const string SectionName = "Agents:TemplateRepository";

    /// <summary>
    /// Gets or sets a value indicating whether built-in embedded templates should be loaded.
    /// </summary>
    /// <value>
    /// <c>true</c> to load built-in templates; <c>false</c> to skip them.
    /// Default is <c>true</c>.
    /// </value>
    /// <remarks>
    /// Built-in templates provide baseline functionality and are always available
    /// regardless of license tier. Disabling this is typically only useful for testing.
    /// </remarks>
    public bool EnableBuiltInTemplates { get; set; } = true;

    /// <summary>
    /// Gets or sets the path to the global templates directory.
    /// </summary>
    /// <value>
    /// The directory path where global (system-wide) templates are stored.
    /// Default is <c>{CommonApplicationData}/Lexichord/Templates</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// Global templates are accessible to all users on the system and have
    /// medium priority (below user templates, above embedded templates).
    /// </para>
    /// <para>
    /// Default paths by platform:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Windows: <c>C:\ProgramData\Lexichord\Templates</c></description></item>
    ///   <item><description>macOS: <c>/Library/Application Support/Lexichord/Templates</c></description></item>
    ///   <item><description>Linux: <c>/var/lib/lexichord/templates</c></description></item>
    /// </list>
    /// </remarks>
    public string GlobalTemplatesPath { get; set; } = GetDefaultGlobalPath();

    /// <summary>
    /// Gets or sets the path to the user templates directory.
    /// </summary>
    /// <value>
    /// The directory path where user-specific templates are stored.
    /// Default is <c>{ApplicationData}/Lexichord/Templates</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// User templates have the highest priority and can override both
    /// embedded and global templates with the same template ID.
    /// </para>
    /// <para>
    /// Default paths by platform:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Windows: <c>%APPDATA%\Lexichord\Templates</c></description></item>
    ///   <item><description>macOS: <c>~/Library/Application Support/Lexichord/Templates</c></description></item>
    ///   <item><description>Linux: <c>~/.config/lexichord/templates</c></description></item>
    /// </list>
    /// </remarks>
    public string UserTemplatesPath { get; set; } = GetDefaultUserPath();

    /// <summary>
    /// Gets or sets a value indicating whether hot-reload of templates is enabled.
    /// </summary>
    /// <value>
    /// <c>true</c> to automatically reload templates when files change;
    /// <c>false</c> to require manual reload via <see cref="IPromptTemplateRepository.ReloadTemplatesAsync"/>.
    /// Default is <c>true</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// When enabled, the repository monitors the global and user template directories
    /// for file changes and automatically reloads affected templates.
    /// </para>
    /// <para>
    /// <strong>Note:</strong> Hot-reload requires <see cref="LicenseTier.Teams"/> or higher.
    /// If the license tier is insufficient, hot-reload is disabled regardless of this setting.
    /// </para>
    /// </remarks>
    public bool EnableHotReload { get; set; } = true;

    /// <summary>
    /// Gets or sets the file extensions to recognize as template files.
    /// </summary>
    /// <value>
    /// A list of file extensions (including the dot) to scan for templates.
    /// Default is <c>[".yaml", ".yml"]</c>.
    /// </value>
    /// <remarks>
    /// Only files with these extensions will be loaded from the template directories.
    /// Extensions are matched case-insensitively.
    /// </remarks>
    public IReadOnlyList<string> TemplateExtensions { get; set; } = [".yaml", ".yml"];

    /// <summary>
    /// Gets or sets the debounce delay in milliseconds for file system change detection.
    /// </summary>
    /// <value>
    /// The number of milliseconds to wait after a file change before processing.
    /// Default is 200ms.
    /// </value>
    /// <remarks>
    /// <para>
    /// File system watchers can generate multiple events for a single logical change
    /// (e.g., when a file is saved). The debounce delay consolidates rapid changes
    /// into a single reload operation.
    /// </para>
    /// <para>
    /// Increase this value if you experience excessive reloads during editing.
    /// Decrease it for faster response to changes.
    /// </para>
    /// </remarks>
    public int FileWatcherDebounceMs { get; set; } = 200;

    /// <summary>
    /// Gets the default options instance with all default values.
    /// </summary>
    /// <value>A new <see cref="PromptTemplateOptions"/> instance with default settings.</value>
    public static PromptTemplateOptions Default => new();

    /// <summary>
    /// Gets the default global templates path for the current platform.
    /// </summary>
    /// <returns>The default global templates directory path.</returns>
    private static string GetDefaultGlobalPath()
    {
        // Use CommonApplicationData for system-wide templates
        var basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        return Path.Combine(basePath, "Lexichord", "Templates");
    }

    /// <summary>
    /// Gets the default user templates path for the current platform.
    /// </summary>
    /// <returns>The default user templates directory path.</returns>
    private static string GetDefaultUserPath()
    {
        // Use ApplicationData for per-user templates
        var basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(basePath, "Lexichord", "Templates");
    }
}
