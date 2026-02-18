// -----------------------------------------------------------------------
// <copyright file="SummarySummaryExportResult.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Result record for export operations (v0.7.6c).
//   Contains the outcome of an export operation including success status,
//   destination, output path, bytes/characters written, and error messages.
//
//   Success vs. Failure:
//     - Success: Created via Succeeded() factory — Success is true, error is null
//     - Failure: Created via Failed() factory — Success is false, ErrorMessage set
//
//   Introduced in: v0.7.6c
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.SummaryExport;

/// <summary>
/// Result of an export operation.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="SummaryExportResult"/> record represents the outcome of
/// <see cref="ISummaryExporter.ExportAsync"/> and related export methods. It provides
/// detailed information about what was exported, where, and whether it succeeded.
/// </para>
/// <para>
/// <b>Success vs. Failure:</b>
/// <list type="bullet">
/// <item><description>Use <see cref="Succeeded"/> factory for successful exports</description></item>
/// <item><description>Use <see cref="Failed"/> factory for failed exports</description></item>
/// </list>
/// Always check <see cref="Success"/> before processing the result.
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
/// var result = await exporter.ExportAsync(summary, documentPath, options, ct);
///
/// if (result.Success)
/// {
///     if (result.Destination == ExportDestination.File)
///     {
///         Console.WriteLine($"Summary saved to {result.OutputPath} ({result.BytesWritten} bytes)");
///     }
///     else if (result.Destination == ExportDestination.Clipboard)
///     {
///         Console.WriteLine($"Copied {result.CharactersWritten} characters to clipboard");
///     }
/// }
/// else
/// {
///     Console.WriteLine($"Export failed: {result.ErrorMessage}");
/// }
/// </code>
/// </example>
/// <seealso cref="ExportOptions"/>
/// <seealso cref="ExportDestination"/>
/// <seealso cref="ISummaryExporter"/>
public record SummaryExportResult
{
    /// <summary>
    /// Gets whether the export operation succeeded.
    /// </summary>
    /// <value>
    /// <c>true</c> if the export completed successfully; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Always check this property before using other result properties.
    /// When <c>false</c>, <see cref="ErrorMessage"/> contains failure details.
    /// </remarks>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the destination where content was exported.
    /// </summary>
    /// <value>
    /// The <see cref="ExportDestination"/> that was targeted by the operation.
    /// </value>
    public ExportDestination Destination { get; init; }

    /// <summary>
    /// Gets the output file path for file exports.
    /// </summary>
    /// <value>
    /// The full path to the created file when <see cref="Destination"/> is
    /// <see cref="ExportDestination.File"/>; otherwise <c>null</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Only populated for file exports. Contains the actual path
    /// where the file was written (may differ from requested path if auto-generated).
    /// </remarks>
    public string? OutputPath { get; init; }

    /// <summary>
    /// Gets the error message if the export failed.
    /// </summary>
    /// <value>
    /// A user-facing error message describing what went wrong.
    /// <c>null</c> if <see cref="Success"/> is <c>true</c>.
    /// </value>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the number of bytes written for file exports.
    /// </summary>
    /// <value>
    /// The size in bytes of the exported file. <c>null</c> for non-file exports.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Only populated for <see cref="ExportDestination.File"/> and
    /// <see cref="ExportDestination.Frontmatter"/> destinations.
    /// </remarks>
    public long? BytesWritten { get; init; }

    /// <summary>
    /// Gets the number of characters written for clipboard/inline exports.
    /// </summary>
    /// <value>
    /// The character count of the exported content. <c>null</c> for file exports.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Only populated for <see cref="ExportDestination.Clipboard"/>
    /// and <see cref="ExportDestination.InlineInsert"/> destinations.
    /// </remarks>
    public int? CharactersWritten { get; init; }

    /// <summary>
    /// Gets whether existing content was overwritten.
    /// </summary>
    /// <value>
    /// <c>true</c> if existing content (file or frontmatter fields) was replaced;
    /// otherwise <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Relevant for <see cref="ExportDestination.File"/> and
    /// <see cref="ExportDestination.Frontmatter"/> destinations. Helps users
    /// understand if their existing data was modified.
    /// </remarks>
    public bool DidOverwrite { get; init; }

    /// <summary>
    /// Gets the timestamp when the export was completed.
    /// </summary>
    /// <value>
    /// UTC timestamp of when the export operation finished.
    /// </value>
    public DateTimeOffset ExportedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Creates a successful export result.
    /// </summary>
    /// <param name="destination">The export destination that was used.</param>
    /// <param name="outputPath">The output file path, if applicable.</param>
    /// <returns>A successful <see cref="SummaryExportResult"/>.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for creating successful results. Use the
    /// <c>with</c> expression to add additional properties like <see cref="BytesWritten"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = SummaryExportResult.Succeeded(ExportDestination.File, outputPath)
    ///     with { BytesWritten = fileInfo.Length, DidOverwrite = fileExisted };
    /// </code>
    /// </example>
    public static SummaryExportResult Succeeded(ExportDestination destination, string? outputPath = null) =>
        new()
        {
            Success = true,
            Destination = destination,
            OutputPath = outputPath,
            ExportedAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Creates a failed export result.
    /// </summary>
    /// <param name="destination">The export destination that was attempted.</param>
    /// <param name="errorMessage">A user-facing error message describing the failure.</param>
    /// <returns>A failed <see cref="SummaryExportResult"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="errorMessage"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for creating failed results with consistent structure.
    /// Called from catch blocks following the 3-catch error handling pattern.
    /// </remarks>
    public static SummaryExportResult Failed(ExportDestination destination, string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(errorMessage);

        return new SummaryExportResult
        {
            Success = false,
            Destination = destination,
            ErrorMessage = errorMessage,
            ExportedAt = DateTimeOffset.UtcNow
        };
    }
}
