// -----------------------------------------------------------------------
// <copyright file="SummaryExportedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: MediatR notification published when a summary export operation
//   completes successfully (v0.7.6c). Published by SummaryExporter after
//   the content has been written to the destination.
//
//   Consumers:
//     - UI components (success notifications)
//     - Analytics/telemetry handlers
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.SummaryExport;
using MediatR;

namespace Lexichord.Modules.Agents.SummaryExport.Events;

/// <summary>
/// Published when a summary export operation completes successfully.
/// </summary>
/// <remarks>
/// <para>
/// Published by the SummaryExporter after the content has been written to
/// the destination. Carries metrics about the export for observability.
/// </para>
/// <para><b>Introduced in:</b> v0.7.6c</para>
/// </remarks>
/// <param name="Destination">The target export destination.</param>
/// <param name="DocumentPath">Path to the source document, if applicable.</param>
/// <param name="OutputPath">Path to the output file, if applicable.</param>
/// <param name="BytesWritten">Number of bytes written (file exports).</param>
/// <param name="CharactersWritten">Number of characters written (clipboard/inline).</param>
/// <param name="DidOverwrite">Whether existing content was overwritten.</param>
/// <param name="Duration">Total time elapsed for the export operation.</param>
/// <param name="Timestamp">UTC timestamp when the event was created.</param>
public record SummaryExportedEvent(
    ExportDestination Destination,
    string? DocumentPath,
    string? OutputPath,
    long? BytesWritten,
    int? CharactersWritten,
    bool DidOverwrite,
    TimeSpan Duration,
    DateTime Timestamp) : INotification
{
    /// <summary>
    /// Creates a new <see cref="SummaryExportedEvent"/> with the current UTC timestamp.
    /// </summary>
    /// <param name="destination">The target export destination.</param>
    /// <param name="documentPath">Path to the source document.</param>
    /// <param name="outputPath">Path to the output file.</param>
    /// <param name="bytesWritten">Number of bytes written.</param>
    /// <param name="charactersWritten">Number of characters written.</param>
    /// <param name="didOverwrite">Whether existing content was overwritten.</param>
    /// <param name="duration">Total time elapsed for the export.</param>
    /// <returns>A new <see cref="SummaryExportedEvent"/>.</returns>
    public static SummaryExportedEvent Create(
        ExportDestination destination,
        string? documentPath,
        string? outputPath = null,
        long? bytesWritten = null,
        int? charactersWritten = null,
        bool didOverwrite = false,
        TimeSpan? duration = null) =>
        new(destination, documentPath, outputPath, bytesWritten, charactersWritten,
            didOverwrite, duration ?? TimeSpan.Zero, DateTime.UtcNow);
}
