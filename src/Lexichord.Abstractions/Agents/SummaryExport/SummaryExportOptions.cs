// -----------------------------------------------------------------------
// <copyright file="SummarySummaryExportOptions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Configuration record for summary export operations (v0.7.6c).
//   Contains all settings that control export behavior across destinations:
//   destination selection, output paths, field filtering, formatting options.
//
//   Destination-Specific Options:
//     - File: OutputPath, IncludeMetadata, IncludeSourceReference, ExportTemplate
//     - Frontmatter: Fields, Overwrite
//     - Clipboard: ClipboardAsMarkdown
//     - InlineInsert: UseCalloutBlock, CalloutType
//
//   Introduced in: v0.7.6c
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.SummaryExport;

/// <summary>
/// Configuration options for summary export operations.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="SummaryExportOptions"/> record provides comprehensive configuration
/// for <see cref="ISummaryExporter.ExportAsync"/>. Options are destination-specific; unused
/// options for a given destination are ignored.
/// </para>
/// <para>
/// <b>Destination-Specific Options:</b>
/// <list type="table">
/// <listheader>
/// <term>Destination</term>
/// <description>Relevant Options</description>
/// </listheader>
/// <item>
/// <term><see cref="ExportDestination.Panel"/></term>
/// <description>No additional options required</description>
/// </item>
/// <item>
/// <term><see cref="ExportDestination.Frontmatter"/></term>
/// <description><see cref="Fields"/>, <see cref="Overwrite"/></description>
/// </item>
/// <item>
/// <term><see cref="ExportDestination.File"/></term>
/// <description><see cref="OutputPath"/>, <see cref="IncludeMetadata"/>,
/// <see cref="IncludeSourceReference"/>, <see cref="ExportTemplate"/></description>
/// </item>
/// <item>
/// <term><see cref="ExportDestination.Clipboard"/></term>
/// <description><see cref="ClipboardAsMarkdown"/></description>
/// </item>
/// <item>
/// <term><see cref="ExportDestination.InlineInsert"/></term>
/// <description><see cref="UseCalloutBlock"/>, <see cref="CalloutType"/></description>
/// </item>
/// </list>
/// </para>
/// <para>
/// <b>Validation:</b>
/// Use <see cref="Validate"/> to check for invalid option combinations before export.
/// Invalid options throw <see cref="ArgumentException"/>.
/// </para>
/// <para>
/// <b>Thread safety:</b> This is an immutable record and is inherently thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6c as part of the Summarizer Agent Export Formats.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Export summary to a custom file path
/// var fileOptions = new SummaryExportOptions
/// {
///     Destination = ExportDestination.File,
///     OutputPath = "/docs/summaries/project-summary.md",
///     IncludeMetadata = true,
///     IncludeSourceReference = true
/// };
///
/// // Export to frontmatter with selected fields
/// var frontmatterOptions = new SummaryExportOptions
/// {
///     Destination = ExportDestination.Frontmatter,
///     Fields = FrontmatterFields.Abstract | FrontmatterFields.Tags,
///     Overwrite = false // Merge with existing
/// };
///
/// // Copy plain text to clipboard
/// var clipboardOptions = new SummaryExportOptions
/// {
///     Destination = ExportDestination.Clipboard,
///     ClipboardAsMarkdown = false
/// };
/// </code>
/// </example>
/// <seealso cref="ExportDestination"/>
/// <seealso cref="FrontmatterFields"/>
/// <seealso cref="ExportResult"/>
/// <seealso cref="ISummaryExporter"/>
public record SummaryExportOptions
{
    /// <summary>
    /// Gets where to export the summary.
    /// </summary>
    /// <value>
    /// The target destination for the export operation.
    /// Default: <see cref="ExportDestination.Panel"/>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Determines which export handler is invoked. Each destination
    /// has different requirements and produces different output formats.
    /// </remarks>
    public ExportDestination Destination { get; init; } = ExportDestination.Panel;

    /// <summary>
    /// Gets the output file path for <see cref="ExportDestination.File"/> exports.
    /// </summary>
    /// <value>
    /// The full path where the summary file should be created.
    /// If <c>null</c>, auto-generates as "{document-name}.summary.md".
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Only used when <see cref="Destination"/> is <see cref="ExportDestination.File"/>.
    /// When <c>null</c>, the exporter generates a path based on the source document:
    /// <code>
    /// /path/to/document.md → /path/to/document.summary.md
    /// </code>
    /// </remarks>
    public string? OutputPath { get; init; }

    /// <summary>
    /// Gets which frontmatter fields to include.
    /// </summary>
    /// <value>
    /// Flags indicating which summary and metadata fields to export.
    /// Default: <see cref="FrontmatterFields.All"/>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Only used when <see cref="Destination"/> is <see cref="ExportDestination.Frontmatter"/>.
    /// Allows fine-grained control over which fields appear in the YAML frontmatter block.
    /// </remarks>
    public FrontmatterFields Fields { get; init; } = FrontmatterFields.All;

    /// <summary>
    /// Gets whether to overwrite existing summary data.
    /// </summary>
    /// <value>
    /// <c>true</c> to replace existing summary fields; <c>false</c> to merge.
    /// Default: <c>true</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Used with <see cref="ExportDestination.Frontmatter"/> and
    /// <see cref="ExportDestination.File"/>. When <c>false</c>:
    /// <list type="bullet">
    /// <item><description>Frontmatter: Existing user fields are preserved; only summary/metadata fields are updated</description></item>
    /// <item><description>File: Export fails if output file already exists</description></item>
    /// </list>
    /// </remarks>
    public bool Overwrite { get; init; } = true;

    /// <summary>
    /// Gets whether to include document metadata in file exports.
    /// </summary>
    /// <value>
    /// <c>true</c> to include reading time, complexity, key terms, etc.;
    /// <c>false</c> for summary only. Default: <c>true</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Only used when <see cref="Destination"/> is <see cref="ExportDestination.File"/>.
    /// When <c>true</c>, the export includes a Metadata section with document metrics.
    /// </remarks>
    public bool IncludeMetadata { get; init; } = true;

    /// <summary>
    /// Gets whether to include a source document reference.
    /// </summary>
    /// <value>
    /// <c>true</c> to include source path, generation time, and model;
    /// <c>false</c> to omit. Default: <c>true</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Only used when <see cref="Destination"/> is <see cref="ExportDestination.File"/>.
    /// When <c>true</c>, the export includes a header with source document link and generation info.
    /// </remarks>
    public bool IncludeSourceReference { get; init; } = true;

    /// <summary>
    /// Gets a custom template for file export format.
    /// </summary>
    /// <value>
    /// A Mustache-style template with placeholders like <c>{{summary}}</c>, <c>{{metadata}}</c>.
    /// If <c>null</c>, uses the default template.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Only used when <see cref="Destination"/> is <see cref="ExportDestination.File"/>.
    /// Supported placeholders:
    /// <list type="bullet">
    /// <item><description><c>{{document_title}}</c> - Source document name</description></item>
    /// <item><description><c>{{source_name}}</c> - Source file name</description></item>
    /// <item><description><c>{{source_path}}</c> - Full path to source</description></item>
    /// <item><description><c>{{generated_at}}</c> - Generation timestamp</description></item>
    /// <item><description><c>{{model}}</c> - LLM model used</description></item>
    /// <item><description><c>{{summary}}</c> - The summary text</description></item>
    /// <item><description><c>{{reading_time}}</c> - Reading time in minutes</description></item>
    /// <item><description><c>{{complexity}}</c> - Complexity score</description></item>
    /// <item><description><c>{{document_type}}</c> - Detected type</description></item>
    /// <item><description><c>{{target_audience}}</c> - Inferred audience</description></item>
    /// <item><description><c>{{category}}</c> - Primary category</description></item>
    /// </list>
    /// </remarks>
    public string? ExportTemplate { get; init; }

    /// <summary>
    /// Gets whether to format clipboard content as Markdown.
    /// </summary>
    /// <value>
    /// <c>true</c> to copy Markdown-formatted text; <c>false</c> for plain text.
    /// Default: <c>true</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Only used when <see cref="Destination"/> is <see cref="ExportDestination.Clipboard"/>.
    /// When <c>false</c>, Markdown formatting is stripped (bullets converted to dashes,
    /// bold/italic markers removed).
    /// </remarks>
    public bool ClipboardAsMarkdown { get; init; } = true;

    /// <summary>
    /// Gets whether to wrap inline insertions in a callout block.
    /// </summary>
    /// <value>
    /// <c>true</c> to wrap in a callout/admonition block; <c>false</c> for raw text.
    /// Default: <c>true</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Only used when <see cref="Destination"/> is <see cref="ExportDestination.InlineInsert"/>.
    /// Callout format follows the GitHub/Obsidian syntax:
    /// <code>
    /// > [!info] Summary
    /// > • First key point
    /// > • Second key point
    /// </code>
    /// </remarks>
    public bool UseCalloutBlock { get; init; } = true;

    /// <summary>
    /// Gets the callout type for inline insertions.
    /// </summary>
    /// <value>
    /// The callout type (info, note, tip, warning). Default: "info".
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Only used when <see cref="Destination"/> is <see cref="ExportDestination.InlineInsert"/>
    /// and <see cref="UseCalloutBlock"/> is <c>true</c>. Common types:
    /// <list type="bullet">
    /// <item><description><c>info</c> - Neutral information</description></item>
    /// <item><description><c>note</c> - Important note</description></item>
    /// <item><description><c>tip</c> - Helpful tip</description></item>
    /// <item><description><c>warning</c> - Warning or caution</description></item>
    /// </list>
    /// </remarks>
    public string CalloutType { get; init; } = "info";

    /// <summary>
    /// Validates the options for the specified destination.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when options are invalid for the configured destination.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Validates destination-specific option combinations:
    /// <list type="bullet">
    /// <item><description>File + non-null OutputPath: Validates path format</description></item>
    /// <item><description>InlineInsert + UseCalloutBlock: Validates CalloutType is not empty</description></item>
    /// </list>
    /// </remarks>
    public void Validate()
    {
        if (Destination == ExportDestination.File && OutputPath is not null)
        {
            if (string.IsNullOrWhiteSpace(OutputPath))
            {
                throw new ArgumentException(
                    "OutputPath cannot be empty or whitespace when specified.",
                    nameof(OutputPath));
            }
        }

        if (Destination == ExportDestination.InlineInsert && UseCalloutBlock)
        {
            if (string.IsNullOrWhiteSpace(CalloutType))
            {
                throw new ArgumentException(
                    "CalloutType cannot be empty when UseCalloutBlock is true.",
                    nameof(CalloutType));
            }
        }
    }

    /// <summary>
    /// Gets the default options for a given destination.
    /// </summary>
    /// <param name="destination">The target export destination.</param>
    /// <returns>A new <see cref="SummaryExportOptions"/> configured for the destination.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for creating destination-specific default options.
    /// Useful for quick exports without manual configuration.
    /// </remarks>
    public static SummaryExportOptions ForDestination(ExportDestination destination) =>
        new() { Destination = destination };
}
