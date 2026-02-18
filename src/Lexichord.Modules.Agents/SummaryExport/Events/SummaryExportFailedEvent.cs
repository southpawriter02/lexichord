// -----------------------------------------------------------------------
// <copyright file="SummaryExportFailedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: MediatR notification published when a summary export operation
//   fails (v0.7.6c). Published by SummaryExporter when an error occurs
//   during the export process.
//
//   Consumers:
//     - UI components (error notifications)
//     - Analytics/telemetry handlers (error tracking)
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.SummaryExport;
using MediatR;

namespace Lexichord.Modules.Agents.SummaryExport.Events;

/// <summary>
/// Published when a summary export operation fails.
/// </summary>
/// <remarks>
/// <para>
/// Published by the SummaryExporter when an error occurs during the export process.
/// Carries the error message and context for error tracking and display.
/// </para>
/// <para><b>Introduced in:</b> v0.7.6c</para>
/// </remarks>
/// <param name="Destination">The target export destination that was attempted.</param>
/// <param name="DocumentPath">Path to the source document, if applicable.</param>
/// <param name="ErrorMessage">A user-facing error message describing the failure.</param>
/// <param name="Timestamp">UTC timestamp when the event was created.</param>
public record SummaryExportFailedEvent(
    ExportDestination Destination,
    string? DocumentPath,
    string ErrorMessage,
    DateTime Timestamp) : INotification
{
    /// <summary>
    /// Creates a new <see cref="SummaryExportFailedEvent"/> with the current UTC timestamp.
    /// </summary>
    /// <param name="destination">The target export destination that was attempted.</param>
    /// <param name="documentPath">Path to the source document.</param>
    /// <param name="errorMessage">A user-facing error message describing the failure.</param>
    /// <returns>A new <see cref="SummaryExportFailedEvent"/>.</returns>
    public static SummaryExportFailedEvent Create(
        ExportDestination destination,
        string? documentPath,
        string errorMessage) =>
        new(destination, documentPath, errorMessage, DateTime.UtcNow);
}
