// -----------------------------------------------------------------------
// <copyright file="ExportDestination.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Enumeration for summary export destinations (v0.7.6c).
//   Defines the possible output targets for summary and metadata export:
//   Panel (UI display), Frontmatter (YAML), File (Markdown), Clipboard, InlineInsert.
//
//   Each destination has different requirements and behavior:
//     - Panel: Opens the Summary Panel UI with the content
//     - Frontmatter: Injects into document's YAML frontmatter block
//     - File: Creates a standalone .summary.md file
//     - Clipboard: Copies formatted text to system clipboard
//     - InlineInsert: Inserts at current cursor position in editor
//
//   Introduced in: v0.7.6c
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.SummaryExport;

/// <summary>
/// Destination options for summary and metadata export operations.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="ExportDestination"/> enumeration defines the supported
/// output targets for <see cref="ISummaryExporter"/>. Each destination has different
/// requirements and produces different output formats.
/// </para>
/// <para>
/// <b>Destination Behaviors:</b>
/// <list type="bullet">
/// <item>
/// <term><see cref="Panel"/></term>
/// <description>Opens the Summary Panel UI with the summary content displayed.
/// Content persists until the document changes or the user clears it.</description>
/// </item>
/// <item>
/// <term><see cref="Frontmatter"/></term>
/// <description>Injects summary data into the document's YAML frontmatter block.
/// Creates the block if it doesn't exist, merges with existing fields.</description>
/// </item>
/// <item>
/// <term><see cref="File"/></term>
/// <description>Creates a standalone Markdown file containing the summary.
/// Default path is "{document-name}.summary.md" alongside the source.</description>
/// </item>
/// <item>
/// <term><see cref="Clipboard"/></term>
/// <description>Copies formatted summary text to the system clipboard.
/// Format controlled by <see cref="ExportOptions.ClipboardAsMarkdown"/>.</description>
/// </item>
/// <item>
/// <term><see cref="InlineInsert"/></term>
/// <description>Inserts the summary at the current cursor position in the editor.
/// Can be wrapped in a callout block via <see cref="ExportOptions.UseCalloutBlock"/>.</description>
/// </item>
/// </list>
/// </para>
/// <para>
/// <b>Thread safety:</b> This is a value type enumeration and is inherently thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6c as part of the Summarizer Agent Export Formats.
/// </para>
/// </remarks>
/// <seealso cref="ExportOptions"/>
/// <seealso cref="ExportResult"/>
/// <seealso cref="ISummaryExporter"/>
public enum ExportDestination
{
    /// <summary>
    /// Display in the dedicated Summary Panel.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Opens the Summary Panel UI with the summary and metadata displayed.
    /// Summary persists in the panel until the document changes or the user clears it.
    /// This is the default destination when users want to view the summary without
    /// modifying the document.
    /// </remarks>
    Panel = 0,

    /// <summary>
    /// Inject into document's YAML frontmatter.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Creates or updates the frontmatter block at the document start.
    /// The exporter parses existing frontmatter (if any) and merges summary fields
    /// while preserving other user-defined fields. Fields to include are controlled
    /// by <see cref="ExportOptions.Fields"/>.
    /// </remarks>
    Frontmatter = 1,

    /// <summary>
    /// Create a standalone summary file.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Creates a Markdown file alongside the source document or at the
    /// path specified by <see cref="ExportOptions.OutputPath"/>. Default filename is
    /// "{source-document-name}.summary.md". File includes summary, metadata (optional),
    /// and source reference (optional).
    /// </remarks>
    File = 2,

    /// <summary>
    /// Copy formatted summary to system clipboard.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Copies the summary text to the system clipboard. Format is
    /// controlled by <see cref="ExportOptions.ClipboardAsMarkdown"/>:
    /// <list type="bullet">
    /// <item><description><c>true</c>: Copies Markdown-formatted text</description></item>
    /// <item><description><c>false</c>: Copies plain text (bullets converted to dashes)</description></item>
    /// </list>
    /// Ready for pasting into other applications.
    /// </remarks>
    Clipboard = 3,

    /// <summary>
    /// Insert at current cursor position in editor.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Inserts the formatted summary at the current cursor position.
    /// When <see cref="ExportOptions.UseCalloutBlock"/> is true, wraps the summary
    /// in a callout/admonition block of type <see cref="ExportOptions.CalloutType"/>.
    /// Example output with callout:
    /// <code>
    /// > [!info] Summary
    /// > • First key point
    /// > • Second key point
    /// </code>
    /// </remarks>
    InlineInsert = 4
}
