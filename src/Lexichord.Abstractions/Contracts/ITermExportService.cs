namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service interface for exporting terminology to JSON and CSV files.
/// </summary>
/// <remarks>
/// LOGIC: Export returns content as a string, allowing the caller to save
/// to a file or display in the UI. The export format includes metadata
/// for traceability and version tracking.
///
/// Version: v0.2.5d
/// </remarks>
public interface ITermExportService
{
    /// <summary>
    /// Exports terms to JSON format.
    /// </summary>
    /// <param name="options">Export options (filtering, metadata).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Export result with JSON content.</returns>
    Task<ExportResult> ExportToJsonAsync(
        ExportOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Exports terms to CSV format.
    /// </summary>
    /// <param name="options">Export options (filtering).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Export result with CSV content.</returns>
    Task<ExportResult> ExportToCsvAsync(
        ExportOptions? options = null,
        CancellationToken ct = default);
}
