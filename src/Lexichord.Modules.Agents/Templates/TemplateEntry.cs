// -----------------------------------------------------------------------
// <copyright file="TemplateEntry.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.LLM;

namespace Lexichord.Modules.Agents.Templates;

/// <summary>
/// Internal cache entry representing a loaded prompt template with associated metadata.
/// </summary>
/// <remarks>
/// <para>
/// This record is used internally by <see cref="PromptTemplateRepository"/> to track
/// loaded templates along with their source information and load timing.
/// </para>
/// <para>
/// The entry includes:
/// </para>
/// <list type="bullet">
///   <item><description>The template itself (<see cref="IPromptTemplate"/>)</description></item>
///   <item><description>Extended metadata (category, tags) not in the base interface</description></item>
///   <item><description>Source tracking for priority resolution</description></item>
///   <item><description>File path for templates loaded from disk</description></item>
/// </list>
/// </remarks>
/// <param name="Template">The loaded prompt template instance.</param>
/// <param name="Category">The category for organizing templates, or <c>null</c> if uncategorized.</param>
/// <param name="Tags">A collection of tags for filtering and searching.</param>
/// <param name="Source">The source from which the template was loaded.</param>
/// <param name="LoadedAt">The UTC timestamp when the template was loaded.</param>
/// <param name="FilePath">The file path for disk-based templates, or <c>null</c> for embedded templates.</param>
internal record TemplateEntry(
    IPromptTemplate Template,
    string? Category,
    IReadOnlyList<string> Tags,
    TemplateSource Source,
    DateTimeOffset LoadedAt,
    string? FilePath)
{
    /// <summary>
    /// Gets a value indicating whether this template was loaded from an embedded resource.
    /// </summary>
    public bool IsEmbedded => Source == TemplateSource.Embedded;

    /// <summary>
    /// Gets a value indicating whether this template has an associated file path.
    /// </summary>
    public bool HasFilePath => !string.IsNullOrEmpty(FilePath);

    /// <summary>
    /// Converts this entry to a <see cref="TemplateInfo"/> instance for external consumption.
    /// </summary>
    /// <returns>A new <see cref="TemplateInfo"/> containing the metadata from this entry.</returns>
    public TemplateInfo ToTemplateInfo()
    {
        return new TemplateInfo(
            TemplateId: Template.TemplateId,
            Name: Template.Name,
            Category: Category,
            Tags: Tags,
            Source: Source,
            LoadedAt: LoadedAt,
            FilePath: FilePath);
    }
}
