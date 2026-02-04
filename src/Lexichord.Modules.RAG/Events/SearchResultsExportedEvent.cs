// =============================================================================
// File: SearchResultsExportedEvent.cs
// Project: Lexichord.Modules.RAG
// Description: MediatR notification published when search results are exported.
// =============================================================================
// LOGIC: Telemetry event for export operations (v0.5.7d).
//   - Format: The export format used (JSON, CSV, Markdown, BibTeX).
//   - ResultCount: Number of results exported.
//   - OutputPath: File path where results were written.
//   - Duration: Time taken for the export operation.
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.5.7d: SearchActionExportFormat, SearchExportResult (this version).
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;
using MediatR;

namespace Lexichord.Modules.RAG.Events;

/// <summary>
/// MediatR notification published when search results are exported to a file.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SearchResultsExportedEvent"/> is published by
/// <see cref="ISearchActionsService.ExportResultsAsync"/> upon completion of
/// an export operation, whether successful or failed. This enables telemetry,
/// activity logging, and UI notifications.
/// </para>
/// <para>
/// <b>Success vs Failure:</b> Check <see cref="Success"/> to determine whether
/// the export completed successfully. For failures, <see cref="ResultCount"/>
/// may reflect partial progress before the error occurred.
/// </para>
/// <para>
/// <b>Privacy:</b> The <see cref="Query"/> field is included for analytics but
/// should be handled according to the user's telemetry opt-in preferences.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.7d as part of the Search Actions feature.
/// </para>
/// </remarks>
/// <param name="Format">
/// The export format used for the operation.
/// </param>
/// <param name="ResultCount">
/// Number of results included in the export. May be partial on failure.
/// </param>
/// <param name="OutputPath">
/// File path where results were exported, or intended path on failure.
/// </param>
/// <param name="Duration">
/// Time taken for the export operation.
/// </param>
/// <param name="Success">
/// Whether the export completed successfully.
/// </param>
/// <param name="Query">
/// The search query that produced the exported results. May be null
/// if the query is not available or telemetry is disabled.
/// </param>
/// <param name="ErrorMessage">
/// Description of the error if <see cref="Success"/> is false; otherwise null.
/// </param>
public record SearchResultsExportedEvent(
    SearchActionExportFormat Format,
    int ResultCount,
    string OutputPath,
    TimeSpan Duration,
    bool Success,
    string? Query = null,
    string? ErrorMessage = null) : INotification
{
    /// <summary>
    /// Creates an event for a successful export operation.
    /// </summary>
    /// <param name="result">The export result containing operation details.</param>
    /// <param name="format">The export format used.</param>
    /// <param name="query">The search query that produced the results.</param>
    /// <returns>A <see cref="SearchResultsExportedEvent"/> for the successful export.</returns>
    /// <remarks>
    /// LOGIC: Factory method that extracts relevant fields from a SearchExportResult
    /// to simplify event creation in the service layer.
    /// </remarks>
    public static SearchResultsExportedEvent FromResult(SearchExportResult result, SearchActionExportFormat format, string? query = null) =>
        new(
            Format: format,
            ResultCount: result.ItemCount,
            OutputPath: result.OutputPath,
            Duration: result.ElapsedTime,
            Success: result.Success,
            Query: query,
            ErrorMessage: result.ErrorMessage);
}
