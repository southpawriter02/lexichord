// -----------------------------------------------------------------------
// <copyright file="TemplateInfo.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Provides metadata about a loaded prompt template including its source, category, and load information.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TemplateInfo"/> contains metadata that supplements the core template definition
/// in <see cref="IPromptTemplate"/>. This includes:
/// </para>
/// <list type="bullet">
///   <item><description>Source information (where the template was loaded from)</description></item>
///   <item><description>Categorization (category and tags for organization)</description></item>
///   <item><description>Load timing (when the template was loaded into the repository)</description></item>
///   <item><description>File path (for templates loaded from disk)</description></item>
/// </list>
/// <para>
/// Use <see cref="IPromptTemplateRepository.GetTemplateInfo"/> to retrieve metadata for a specific template.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var info = repository.GetTemplateInfo("co-pilot-editor");
/// if (info != null)
/// {
///     Console.WriteLine($"Template: {info.Name}");
///     Console.WriteLine($"Category: {info.Category}");
///     Console.WriteLine($"Source: {info.Source}");
///     Console.WriteLine($"Built-in: {info.IsBuiltIn}");
///     if (info.HasFilePath)
///     {
///         Console.WriteLine($"File: {info.FilePath}");
///     }
/// }
/// </code>
/// </example>
/// <param name="TemplateId">The unique identifier of the template in kebab-case format.</param>
/// <param name="Name">The human-readable display name of the template.</param>
/// <param name="Category">The category for organizing templates (e.g., "editing", "review", "analysis"), or <c>null</c> if uncategorized.</param>
/// <param name="Tags">A collection of tags for filtering and searching templates.</param>
/// <param name="Source">The source from which the template was loaded.</param>
/// <param name="LoadedAt">The UTC timestamp when the template was loaded into the repository.</param>
/// <param name="FilePath">The file path for templates loaded from disk, or <c>null</c> for embedded templates.</param>
/// <seealso cref="IPromptTemplateRepository.GetTemplateInfo"/>
/// <seealso cref="TemplateSource"/>
/// <seealso cref="IPromptTemplate"/>
public record TemplateInfo(
    string TemplateId,
    string Name,
    string? Category,
    IReadOnlyList<string> Tags,
    TemplateSource Source,
    DateTimeOffset LoadedAt,
    string? FilePath)
{
    /// <summary>
    /// Gets a value indicating whether this template is a built-in template from embedded resources.
    /// </summary>
    /// <value>
    /// <c>true</c> if the template was loaded from embedded resources; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// Built-in templates are always available regardless of license tier and provide
    /// baseline functionality. They can be overridden by user or global templates with
    /// the same template ID.
    /// </remarks>
    public bool IsBuiltIn => Source == TemplateSource.Embedded;

    /// <summary>
    /// Gets a value indicating whether this template is a custom template loaded from disk.
    /// </summary>
    /// <value>
    /// <c>true</c> if the template was loaded from the global or user templates directory;
    /// otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// Custom templates require <see cref="LicenseTier.WriterPro"/> or higher to load.
    /// </remarks>
    public bool IsCustom => Source != TemplateSource.Embedded;

    /// <summary>
    /// Gets a value indicating whether this template has an associated file path.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="FilePath"/> is not <c>null</c> or empty; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// Embedded templates do not have file paths. Global and user templates loaded from
    /// disk will have file paths, which can be useful for editing or debugging purposes.
    /// </remarks>
    public bool HasFilePath => !string.IsNullOrEmpty(FilePath);

    /// <summary>
    /// Gets a value indicating whether this template has a category assigned.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Category"/> is not <c>null</c> or empty; otherwise, <c>false</c>.
    /// </value>
    public bool HasCategory => !string.IsNullOrEmpty(Category);

    /// <summary>
    /// Gets a value indicating whether this template has any tags.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Tags"/> contains at least one tag; otherwise, <c>false</c>.
    /// </value>
    public bool HasTags => Tags.Count > 0;

    /// <summary>
    /// Gets the total number of tags associated with this template.
    /// </summary>
    /// <value>The count of tags in the <see cref="Tags"/> collection.</value>
    public int TagCount => Tags.Count;

    /// <summary>
    /// Returns a string representation of this template info for debugging purposes.
    /// </summary>
    /// <returns>A string containing the template ID, source, and category.</returns>
    public override string ToString()
    {
        var categoryPart = HasCategory ? $", Category={Category}" : string.Empty;
        return $"TemplateInfo: {TemplateId} ({Source}{categoryPart})";
    }
}
