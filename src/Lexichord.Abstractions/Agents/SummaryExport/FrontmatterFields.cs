// -----------------------------------------------------------------------
// <copyright file="FrontmatterFields.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Flags enumeration for frontmatter field selection (v0.7.6c).
//   Defines which summary and metadata fields to include when exporting
//   to YAML frontmatter. Allows fine-grained control over exported data.
//
//   Field Categories:
//     - Abstract: The summary text itself
//     - Tags: Suggested tags for categorization
//     - KeyTerms: Extracted key terms with importance
//     - ReadingTime: Estimated reading duration
//     - Category: Primary document category
//     - Audience: Target audience inference
//     - GeneratedAt: Timestamp of generation
//
//   Introduced in: v0.7.6c
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.SummaryExport;

/// <summary>
/// Flags for selecting which fields to include in frontmatter export.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="FrontmatterFields"/> flags enumeration allows users
/// to specify exactly which summary and metadata fields should be included when
/// exporting to YAML frontmatter via <see cref="ExportDestination.Frontmatter"/>.
/// </para>
/// <para>
/// <b>Field Categories:</b>
/// <list type="table">
/// <listheader>
/// <term>Field</term>
/// <description>YAML Output</description>
/// </listheader>
/// <item>
/// <term><see cref="Abstract"/></term>
/// <description><c>summary.text</c> - The generated summary text</description>
/// </item>
/// <item>
/// <term><see cref="Tags"/></term>
/// <description><c>metadata.tags</c> - Suggested tags as a list</description>
/// </item>
/// <item>
/// <term><see cref="KeyTerms"/></term>
/// <description><c>metadata.key_terms</c> - Terms with importance scores</description>
/// </item>
/// <item>
/// <term><see cref="ReadingTime"/></term>
/// <description><c>metadata.reading_time_minutes</c> - Estimated reading time</description>
/// </item>
/// <item>
/// <term><see cref="Category"/></term>
/// <description><c>metadata.category</c> - Primary document category</description>
/// </item>
/// <item>
/// <term><see cref="Audience"/></term>
/// <description><c>metadata.target_audience</c> - Inferred audience</description>
/// </item>
/// <item>
/// <term><see cref="GeneratedAt"/></term>
/// <description><c>summary.generated_at</c> - ISO 8601 timestamp</description>
/// </item>
/// </list>
/// </para>
/// <para>
/// <b>Usage:</b>
/// <code>
/// // Include only summary and tags
/// var options = new ExportOptions
/// {
///     Destination = ExportDestination.Frontmatter,
///     Fields = FrontmatterFields.Abstract | FrontmatterFields.Tags
/// };
///
/// // Include all fields
/// var options = new ExportOptions
/// {
///     Destination = ExportDestination.Frontmatter,
///     Fields = FrontmatterFields.All
/// };
/// </code>
/// </para>
/// <para>
/// <b>Thread safety:</b> This is a value type enumeration and is inherently thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6c as part of the Summarizer Agent Export Formats.
/// </para>
/// </remarks>
/// <seealso cref="ExportOptions"/>
/// <seealso cref="ExportDestination"/>
/// <seealso cref="ISummaryExporter"/>
[Flags]
public enum FrontmatterFields
{
    /// <summary>
    /// No fields (clear summary from frontmatter).
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> When used, no summary or metadata fields are added to frontmatter.
    /// Existing summary fields may be removed if <see cref="ExportOptions.Overwrite"/> is true.
    /// </remarks>
    None = 0,

    /// <summary>
    /// Include abstract/summary text.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Includes the generated summary text in the frontmatter under
    /// <c>summary.text</c>. This is the primary output of summarization.
    /// </remarks>
    Abstract = 1,

    /// <summary>
    /// Include suggested tags.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Includes suggested tags in the frontmatter under <c>metadata.tags</c>
    /// as a YAML list. Tags are lowercase with hyphens (e.g., "api-design").
    /// </remarks>
    Tags = 2,

    /// <summary>
    /// Include key terms.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Includes extracted key terms in the frontmatter under
    /// <c>metadata.key_terms</c> as a list of objects with <c>term</c> and <c>importance</c>.
    /// Limited to top 5 terms by default.
    /// </remarks>
    KeyTerms = 4,

    /// <summary>
    /// Include reading time estimate.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Includes the estimated reading time in minutes under
    /// <c>metadata.reading_time_minutes</c>. Calculated based on word count and complexity.
    /// </remarks>
    ReadingTime = 8,

    /// <summary>
    /// Include primary category.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Includes the inferred primary category under <c>metadata.category</c>.
    /// Examples: "Tutorial", "API Reference", "Architecture", "Report".
    /// </remarks>
    Category = 16,

    /// <summary>
    /// Include target audience.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Includes the inferred target audience under <c>metadata.target_audience</c>.
    /// Examples: "software developers", "data scientists", "executives".
    /// </remarks>
    Audience = 32,

    /// <summary>
    /// Include generation timestamp.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Includes the generation timestamp in ISO 8601 format under
    /// <c>summary.generated_at</c>. Useful for cache invalidation and auditing.
    /// </remarks>
    GeneratedAt = 64,

    /// <summary>
    /// Include all available fields.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience value that includes all defined fields. Equivalent to
    /// <c>Abstract | Tags | KeyTerms | ReadingTime | Category | Audience | GeneratedAt</c>.
    /// </remarks>
    All = Abstract | Tags | KeyTerms | ReadingTime | Category | Audience | GeneratedAt
}
