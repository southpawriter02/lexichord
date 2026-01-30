namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service interface for importing terminology from CSV and Excel files.
/// </summary>
/// <remarks>
/// LOGIC: Import is a two-phase operation:
/// 1. Parse: Read file, validate rows, detect duplicates
/// 2. Commit: Insert/update selected rows into database
///
/// This separation allows the UI to show a preview before committing.
///
/// Version: v0.2.5d
/// </remarks>
public interface ITermImportService
{
    /// <summary>
    /// Maximum file size allowed for import (10MB).
    /// </summary>
    const int MaxFileSizeBytes = 10 * 1024 * 1024;

    /// <summary>
    /// Maximum number of rows allowed per import.
    /// </summary>
    const int MaxRowCount = 10_000;

    /// <summary>
    /// Parses a CSV file and returns rows with validation status.
    /// </summary>
    /// <param name="stream">CSV file stream.</param>
    /// <param name="options">Import options.</param>
    /// <param name="progress">Optional progress callback (0-100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Parse result with validated rows.</returns>
    /// <exception cref="ImportException">If file exceeds size or row limits.</exception>
    Task<ImportParseResult> ParseCsvAsync(
        Stream stream,
        ImportOptions? options = null,
        IProgress<int>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Parses an Excel file and returns rows with validation status.
    /// </summary>
    /// <param name="stream">Excel file stream (.xlsx).</param>
    /// <param name="options">Import options (SheetName selects which sheet).</param>
    /// <param name="progress">Optional progress callback (0-100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Parse result with validated rows.</returns>
    /// <exception cref="ImportException">If file exceeds size or row limits.</exception>
    Task<ImportParseResult> ParseExcelAsync(
        Stream stream,
        ImportOptions? options = null,
        IProgress<int>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the sheet names from an Excel file.
    /// </summary>
    /// <param name="stream">Excel file stream (.xlsx).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of sheet names.</returns>
    Task<IReadOnlyList<string>> GetExcelSheetNamesAsync(
        Stream stream,
        CancellationToken ct = default);

    /// <summary>
    /// Commits selected rows from a parse result to the database.
    /// </summary>
    /// <param name="parseResult">Result from ParseCsvAsync or ParseExcelAsync.</param>
    /// <param name="duplicateHandling">How to handle duplicate terms.</param>
    /// <param name="progress">Optional progress callback (0-100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Import result with counts.</returns>
    Task<ImportResult> CommitAsync(
        ImportParseResult parseResult,
        DuplicateHandling duplicateHandling = DuplicateHandling.Skip,
        IProgress<int>? progress = null,
        CancellationToken ct = default);
}

/// <summary>
/// Exception thrown during import operations.
/// </summary>
public class ImportException : Exception
{
    /// <summary>
    /// Creates a new import exception.
    /// </summary>
    public ImportException(string message) : base(message) { }

    /// <summary>
    /// Creates a new import exception with inner exception.
    /// </summary>
    public ImportException(string message, Exception inner) : base(message, inner) { }
}
