using Lexichord.Abstractions.Entities;

namespace Lexichord.Abstractions.Contracts;

#region Import Types

/// <summary>
/// Options for parsing import files.
/// </summary>
/// <param name="HasHeaderRow">Whether the file has a header row.</param>
/// <param name="SheetName">Excel sheet name (null = first sheet).</param>
/// <param name="ColumnMapping">Custom column mapping (null = auto-detect).</param>
public record ImportOptions(
    bool HasHeaderRow = true,
    string? SheetName = null,
    ImportColumnMapping? ColumnMapping = null);

/// <summary>
/// Custom column mapping for import files.
/// </summary>
/// <param name="PatternColumn">Column index or name for pattern.</param>
/// <param name="RecommendationColumn">Column index or name for recommendation.</param>
/// <param name="CategoryColumn">Column index or name for category.</param>
/// <param name="SeverityColumn">Column index or name for severity.</param>
/// <param name="MatchCaseColumn">Column index or name for case sensitivity.</param>
/// <param name="IsActiveColumn">Column index or name for active status.</param>
public record ImportColumnMapping(
    string PatternColumn = "pattern",
    string RecommendationColumn = "recommendation",
    string CategoryColumn = "category",
    string SeverityColumn = "severity",
    string? MatchCaseColumn = "match_case",
    string? IsActiveColumn = "is_active");

/// <summary>
/// Result of parsing an import file (before commit).
/// </summary>
/// <param name="Rows">Parsed rows with validation status.</param>
/// <param name="ValidCount">Number of valid rows.</param>
/// <param name="InvalidCount">Number of invalid rows.</param>
/// <param name="DuplicateCount">Number of duplicate rows.</param>
public record ImportParseResult(
    IReadOnlyList<ImportRow> Rows,
    int ValidCount,
    int InvalidCount,
    int DuplicateCount)
{
    /// <summary>
    /// Total number of rows parsed.
    /// </summary>
    public int TotalCount => Rows.Count;

    /// <summary>
    /// Whether the import file is empty.
    /// </summary>
    public bool IsEmpty => Rows.Count == 0;
}

/// <summary>
/// A single row from an import file.
/// </summary>
/// <param name="RowNumber">1-based row number in source file.</param>
/// <param name="Pattern">The term pattern.</param>
/// <param name="Recommendation">Suggested replacement.</param>
/// <param name="Category">Term category.</param>
/// <param name="Severity">Rule severity.</param>
/// <param name="MatchCase">Case-sensitive matching.</param>
/// <param name="IsActive">Whether term is active.</param>
/// <param name="Status">Validation status.</param>
/// <param name="ErrorMessage">Error message if invalid.</param>
/// <param name="ExistingTermId">ID of existing term if duplicate.</param>
public record ImportRow(
    int RowNumber,
    string Pattern,
    string? Recommendation,
    string? Category,
    string? Severity,
    bool MatchCase,
    bool IsActive,
    ImportRowStatus Status,
    string? ErrorMessage = null,
    Guid? ExistingTermId = null)
{
    /// <summary>
    /// Whether this row should be selected for import by default.
    /// </summary>
    public bool IsSelected { get; set; } = Status == ImportRowStatus.Valid;
}

/// <summary>
/// Validation status for an import row.
/// </summary>
public enum ImportRowStatus
{
    /// <summary>Row is valid and ready for import.</summary>
    Valid,

    /// <summary>Row has validation errors.</summary>
    Invalid,

    /// <summary>Row matches an existing term.</summary>
    Duplicate
}

/// <summary>
/// How to handle duplicate terms during import.
/// </summary>
public enum DuplicateHandling
{
    /// <summary>Skip duplicate rows.</summary>
    Skip,

    /// <summary>Overwrite existing terms with imported data.</summary>
    Overwrite
}

/// <summary>
/// Result of committing an import.
/// </summary>
/// <param name="Success">Whether the import succeeded.</param>
/// <param name="ImportedCount">Number of terms imported.</param>
/// <param name="SkippedCount">Number of terms skipped.</param>
/// <param name="UpdatedCount">Number of terms updated (overwrite mode).</param>
/// <param name="ErrorCount">Number of terms that failed.</param>
/// <param name="ErrorMessage">Error message if failed.</param>
public record ImportResult(
    bool Success,
    int ImportedCount,
    int SkippedCount,
    int UpdatedCount,
    int ErrorCount,
    string? ErrorMessage = null)
{
    /// <summary>
    /// Total terms processed.
    /// </summary>
    public int TotalProcessed => ImportedCount + SkippedCount + UpdatedCount + ErrorCount;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ImportResult Succeeded(int imported, int skipped = 0, int updated = 0) =>
        new(true, imported, skipped, updated, 0);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static ImportResult Failed(string error) =>
        new(false, 0, 0, 0, 0, error);
}

#endregion

#region Export Types

/// <summary>
/// Options for exporting terms.
/// </summary>
/// <param name="Categories">Filter by categories (null = all).</param>
/// <param name="IncludeInactive">Include inactive terms.</param>
/// <param name="IncludeMetadata">Include export metadata (JSON only).</param>
public record ExportOptions(
    IReadOnlyList<string>? Categories = null,
    bool IncludeInactive = false,
    bool IncludeMetadata = true);

/// <summary>
/// Result of an export operation.
/// </summary>
/// <param name="Success">Whether export succeeded.</param>
/// <param name="Content">Exported content as string.</param>
/// <param name="ExportedCount">Number of terms exported.</param>
/// <param name="ErrorMessage">Error message if failed.</param>
public record ExportResult(
    bool Success,
    string? Content,
    int ExportedCount,
    string? ErrorMessage = null)
{
    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ExportResult Succeeded(string content, int count) =>
        new(true, content, count);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static ExportResult Failed(string error) =>
        new(false, null, 0, error);
}

/// <summary>
/// JSON export format with metadata.
/// </summary>
/// <param name="Metadata">Export metadata.</param>
/// <param name="Terms">Exported terms.</param>
public record TerminologyExportDocument(
    ExportMetadata Metadata,
    IReadOnlyList<ExportedTerm> Terms);

/// <summary>
/// Metadata for terminology export.
/// </summary>
/// <param name="Version">Export format version.</param>
/// <param name="ExportedAt">Export timestamp.</param>
/// <param name="TermCount">Number of terms.</param>
/// <param name="Source">Source application.</param>
public record ExportMetadata(
    string Version,
    DateTimeOffset ExportedAt,
    int TermCount,
    string Source = "Lexichord");

/// <summary>
/// A term in export format.
/// </summary>
/// <param name="Pattern">The term pattern.</param>
/// <param name="Recommendation">Suggested replacement.</param>
/// <param name="Category">Term category.</param>
/// <param name="Severity">Rule severity.</param>
/// <param name="MatchCase">Case-sensitive matching.</param>
/// <param name="IsActive">Whether term is active.</param>
public record ExportedTerm(
    string Pattern,
    string? Recommendation,
    string? Category,
    string? Severity,
    bool MatchCase,
    bool IsActive);

#endregion
