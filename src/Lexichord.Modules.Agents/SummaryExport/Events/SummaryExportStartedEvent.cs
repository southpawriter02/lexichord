// -----------------------------------------------------------------------
// <copyright file="SummaryExportStartedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: MediatR notification published when a summary export operation
//   starts (v0.7.6c). Published by SummaryExporter before beginning the
//   export to a destination.
//
//   Consumers:
//     - UI components (loading indicators)
//     - Analytics/telemetry handlers
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.SummaryExport;
using MediatR;

namespace Lexichord.Modules.Agents.SummaryExport.Events;

/// <summary>
/// Published when a summary export operation starts.
/// </summary>
/// <remarks>
/// <para>
/// Published by the SummaryExporter before beginning the export operation.
/// Carries destination and document path information for observability.
/// </para>
/// <para><b>Introduced in:</b> v0.7.6c</para>
/// </remarks>
/// <param name="Destination">The target export destination.</param>
/// <param name="DocumentPath">Path to the source document being exported.</param>
/// <param name="Timestamp">UTC timestamp when the event was created.</param>
public record SummaryExportStartedEvent(
    ExportDestination Destination,
    string? DocumentPath,
    DateTime Timestamp) : INotification
{
    /// <summary>
    /// Creates a new <see cref="SummaryExportStartedEvent"/> with the current UTC timestamp.
    /// </summary>
    /// <param name="destination">The target export destination.</param>
    /// <param name="documentPath">Path to the source document being exported.</param>
    /// <returns>A new <see cref="SummaryExportStartedEvent"/>.</returns>
    public static SummaryExportStartedEvent Create(
        ExportDestination destination,
        string? documentPath) =>
        new(destination, documentPath, DateTime.UtcNow);
}
