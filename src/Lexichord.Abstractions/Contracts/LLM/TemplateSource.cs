// -----------------------------------------------------------------------
// <copyright file="TemplateSource.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Defines the source location from which a prompt template was loaded.
/// </summary>
/// <remarks>
/// <para>
/// Templates can be loaded from multiple sources with different priority levels.
/// When templates share the same <see cref="IPromptTemplate.TemplateId"/>,
/// templates from higher-priority sources override those from lower-priority sources.
/// </para>
/// <para>
/// <strong>Loading Priority (lowest to highest):</strong>
/// </para>
/// <list type="number">
///   <item><description><see cref="Embedded"/> - Built-in templates shipped with the module</description></item>
///   <item><description><see cref="Global"/> - Templates from the global templates directory</description></item>
///   <item><description><see cref="User"/> - Templates from the user's personal templates directory</description></item>
/// </list>
/// <para>
/// <strong>Example:</strong> If both an embedded template and a user template have the ID
/// "co-pilot-editor", the user template takes precedence.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Check if a template is built-in
/// var info = repository.GetTemplateInfo("co-pilot-editor");
/// if (info?.Source == TemplateSource.Embedded)
/// {
///     Console.WriteLine("Using built-in template");
/// }
/// </code>
/// </example>
/// <seealso cref="IPromptTemplateRepository"/>
/// <seealso cref="TemplateInfo"/>
public enum TemplateSource
{
    /// <summary>
    /// Built-in templates embedded in the module assembly as resources.
    /// </summary>
    /// <remarks>
    /// These templates are always available regardless of license tier.
    /// They provide baseline functionality and can be overridden by
    /// global or user templates with the same template ID.
    /// </remarks>
    Embedded = 0,

    /// <summary>
    /// Templates loaded from the global templates directory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Global templates are stored in a shared location accessible to all users
    /// on the system. Typical location:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Windows: <c>%PROGRAMDATA%\Lexichord\Templates</c></description></item>
    ///   <item><description>macOS/Linux: <c>/usr/local/share/lexichord/templates</c></description></item>
    /// </list>
    /// <para>
    /// Requires <see cref="LicenseTier.WriterPro"/> or higher to load.
    /// </para>
    /// </remarks>
    Global = 1,

    /// <summary>
    /// Templates loaded from the user's personal templates directory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// User templates are stored in the user's application data folder.
    /// Typical location:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Windows: <c>%APPDATA%\Lexichord\Templates</c></description></item>
    ///   <item><description>macOS: <c>~/Library/Application Support/Lexichord/Templates</c></description></item>
    ///   <item><description>Linux: <c>~/.config/lexichord/templates</c></description></item>
    /// </list>
    /// <para>
    /// User templates have the highest priority and can override both
    /// embedded and global templates. Requires <see cref="LicenseTier.WriterPro"/>
    /// or higher to load.
    /// </para>
    /// </remarks>
    User = 2
}
