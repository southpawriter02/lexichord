// -----------------------------------------------------------------------
// <copyright file="SummaryPanelOpenedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: MediatR notification published when the Summary Panel is opened
//   (v0.7.6c). Published by SummaryExporter when ShowInPanelAsync is called.
//
//   Consumers:
//     - UI layout managers (panel tracking)
//     - Analytics/telemetry handlers
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.Summarizer;
using MediatR;

namespace Lexichord.Modules.Agents.SummaryExport.Events;

/// <summary>
/// Published when the Summary Panel is opened.
/// </summary>
/// <remarks>
/// <para>
/// Published by the SummaryExporter when ShowInPanelAsync is called.
/// Carries information about the summary being displayed for analytics.
/// </para>
/// <para><b>Introduced in:</b> v0.7.6c</para>
/// </remarks>
/// <param name="DocumentPath">Path to the source document.</param>
/// <param name="Mode">The summarization mode of the displayed summary.</param>
/// <param name="HasMetadata">Whether metadata is included with the summary.</param>
/// <param name="WasCached">Whether the summary was retrieved from cache.</param>
/// <param name="Timestamp">UTC timestamp when the event was created.</param>
public record SummaryPanelOpenedEvent(
    string DocumentPath,
    SummarizationMode Mode,
    bool HasMetadata,
    bool WasCached,
    DateTime Timestamp) : INotification
{
    /// <summary>
    /// Creates a new <see cref="SummaryPanelOpenedEvent"/> with the current UTC timestamp.
    /// </summary>
    /// <param name="documentPath">Path to the source document.</param>
    /// <param name="mode">The summarization mode of the displayed summary.</param>
    /// <param name="hasMetadata">Whether metadata is included with the summary.</param>
    /// <param name="wasCached">Whether the summary was retrieved from cache.</param>
    /// <returns>A new <see cref="SummaryPanelOpenedEvent"/>.</returns>
    public static SummaryPanelOpenedEvent Create(
        string documentPath,
        SummarizationMode mode,
        bool hasMetadata,
        bool wasCached = false) =>
        new(documentPath, mode, hasMetadata, wasCached, DateTime.UtcNow);
}
