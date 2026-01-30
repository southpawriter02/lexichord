using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Lexichord.Abstractions.Validation;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Service for importing terminology from CSV and Excel files.
/// </summary>
/// <remarks>
/// LOGIC: Implements ITermImportService with:
/// - CsvHelper for CSV parsing with flexible column mapping
/// - ClosedXML for Excel parsing (safe, no macro execution)
/// - Pattern validation using TermPatternValidator
/// - Duplicate detection against existing terms
///
/// Version: v0.2.5d
/// </remarks>
public class TermImportService : ITermImportService
{
    private readonly ITerminologyRepository _repository;
    private readonly ITerminologyService _terminologyService;
    private readonly ILogger<TermImportService> _logger;

    /// <summary>
    /// Standard CSV headers for auto-detection.
    /// </summary>
    private static readonly HashSet<string> StandardPatternHeaders =
        new(StringComparer.OrdinalIgnoreCase) { "pattern", "term", "find", "match" };

    private static readonly HashSet<string> StandardRecommendationHeaders =
        new(StringComparer.OrdinalIgnoreCase) { "recommendation", "replacement", "suggestion", "replace" };

    private static readonly HashSet<string> StandardCategoryHeaders =
        new(StringComparer.OrdinalIgnoreCase) { "category", "type", "group" };

    private static readonly HashSet<string> StandardSeverityHeaders =
        new(StringComparer.OrdinalIgnoreCase) { "severity", "level", "priority" };

    /// <summary>
    /// Creates a new TermImportService instance.
    /// </summary>
    public TermImportService(
        ITerminologyRepository repository,
        ITerminologyService terminologyService,
        ILogger<TermImportService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _terminologyService = terminologyService ?? throw new ArgumentNullException(nameof(terminologyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ImportParseResult> ParseCsvAsync(
        Stream stream,
        ImportOptions? options = null,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        options ??= new ImportOptions();

        ValidateFileSize(stream.Length);

        _logger.LogDebug("Parsing CSV file with HasHeaderRow={HasHeader}", options.HasHeaderRow);

        var rows = new List<ImportRow>();
        var existingTerms = await GetExistingTermsLookupAsync(ct);

        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = options.HasHeaderRow,
            MissingFieldFound = null,
            BadDataFound = null
        });

        // Read header to determine column mapping
        string[] headers = Array.Empty<string>();
        if (options.HasHeaderRow)
        {
            await csv.ReadAsync();
            csv.ReadHeader();
            headers = csv.HeaderRecord ?? Array.Empty<string>();
        }

        var mapping = options.ColumnMapping ?? DetectColumnMapping(headers);
        var rowNumber = options.HasHeaderRow ? 1 : 0;
        var totalRows = 0;

        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();
            rowNumber++;

            if (++totalRows > ITermImportService.MaxRowCount)
            {
                throw new ImportException($"File exceeds maximum of {ITermImportService.MaxRowCount} rows.");
            }

            var row = ParseCsvRow(csv, rowNumber, mapping, existingTerms);
            rows.Add(row);

            // Report progress every 100 rows
            if (totalRows % 100 == 0)
            {
                progress?.Report(Math.Min(99, totalRows / 100));
            }
        }

        progress?.Report(100);

        _logger.LogInformation("Parsed {Count} rows from CSV file", rows.Count);

        return CreateParseResult(rows);
    }

    /// <inheritdoc />
    public async Task<ImportParseResult> ParseExcelAsync(
        Stream stream,
        ImportOptions? options = null,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        options ??= new ImportOptions();

        ValidateFileSize(stream.Length);

        _logger.LogDebug("Parsing Excel file with SheetName={SheetName}", options.SheetName ?? "(first)");

        var rows = new List<ImportRow>();
        var existingTerms = await GetExistingTermsLookupAsync(ct);

        using var workbook = new XLWorkbook(stream);
        var worksheet = string.IsNullOrEmpty(options.SheetName)
            ? workbook.Worksheets.First()
            : workbook.Worksheet(options.SheetName);

        var usedRange = worksheet.RangeUsed();
        if (usedRange == null)
        {
            return new ImportParseResult(Array.Empty<ImportRow>(), 0, 0, 0);
        }

        var firstRow = usedRange.FirstRow().RowNumber();
        var lastRow = usedRange.LastRow().RowNumber();
        var firstCol = usedRange.FirstColumn().ColumnNumber();
        var lastCol = usedRange.LastColumn().ColumnNumber();

        // Read headers
        string[] headers = Array.Empty<string>();
        if (options.HasHeaderRow)
        {
            headers = Enumerable.Range(firstCol, lastCol - firstCol + 1)
                .Select(col => worksheet.Cell(firstRow, col).GetString().Trim())
                .ToArray();
            firstRow++;
        }

        var mapping = options.ColumnMapping ?? DetectColumnMapping(headers);
        var totalRows = lastRow - firstRow + 1;

        if (totalRows > ITermImportService.MaxRowCount)
        {
            throw new ImportException($"File exceeds maximum of {ITermImportService.MaxRowCount} rows.");
        }

        for (var rowNum = firstRow; rowNum <= lastRow; rowNum++)
        {
            ct.ThrowIfCancellationRequested();

            var row = ParseExcelRow(worksheet, rowNum, firstCol, mapping, headers, existingTerms);
            rows.Add(row);

            // Report progress
            var progressPct = (rowNum - firstRow + 1) * 100 / totalRows;
            progress?.Report(Math.Min(99, progressPct));
        }

        progress?.Report(100);

        _logger.LogInformation("Parsed {Count} rows from Excel file", rows.Count);

        return CreateParseResult(rows);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetExcelSheetNamesAsync(
        Stream stream,
        CancellationToken ct = default)
    {
        using var workbook = new XLWorkbook(stream);
        var names = workbook.Worksheets.Select(ws => ws.Name).ToList();
        return Task.FromResult<IReadOnlyList<string>>(names);
    }

    /// <inheritdoc />
    public async Task<ImportResult> CommitAsync(
        ImportParseResult parseResult,
        DuplicateHandling duplicateHandling = DuplicateHandling.Skip,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        var selectedRows = parseResult.Rows
            .Where(r => r.IsSelected && (r.Status == ImportRowStatus.Valid ||
                (r.Status == ImportRowStatus.Duplicate && duplicateHandling == DuplicateHandling.Overwrite)))
            .ToList();

        _logger.LogInformation("Committing {Count} rows with DuplicateHandling={Handling}",
            selectedRows.Count, duplicateHandling);

        var imported = 0;
        var updated = 0;
        var skipped = 0;
        var errors = 0;

        for (var i = 0; i < selectedRows.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var row = selectedRows[i];

            try
            {
                if (row.Status == ImportRowStatus.Duplicate && row.ExistingTermId.HasValue)
                {
                    if (duplicateHandling == DuplicateHandling.Overwrite)
                    {
                        var updateCommand = new UpdateTermCommand(
                            row.ExistingTermId.Value,
                            row.Pattern,
                            row.Recommendation,
                            row.Category ?? "General",
                            row.Severity ?? "Suggestion",
                            Notes: null,
                            MatchCase: row.MatchCase);

                        var result = await _terminologyService.UpdateAsync(updateCommand, ct);
                        if (result.IsSuccess) updated++;
                        else errors++;
                    }
                    else
                    {
                        skipped++;
                    }
                }
                else
                {
                    var createCommand = new CreateTermCommand(
                        row.Pattern,
                        row.Recommendation,
                        row.Category ?? "General",
                        row.Severity ?? "Suggestion",
                        Notes: null,
                        MatchCase: row.MatchCase);

                    var result = await _terminologyService.CreateAsync(createCommand, ct);
                    if (result.IsSuccess) imported++;
                    else errors++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to import row {RowNumber}", row.RowNumber);
                errors++;
            }

            // Report progress
            var progressPct = (i + 1) * 100 / selectedRows.Count;
            progress?.Report(Math.Min(99, progressPct));
        }

        progress?.Report(100);

        _logger.LogInformation("Import complete: Imported={Imported}, Updated={Updated}, Skipped={Skipped}, Errors={Errors}",
            imported, updated, skipped, errors);

        return ImportResult.Succeeded(imported, skipped, updated);
    }

    #region Private Methods

    private void ValidateFileSize(long length)
    {
        if (length > ITermImportService.MaxFileSizeBytes)
        {
            throw new ImportException(
                $"File exceeds maximum size of {ITermImportService.MaxFileSizeBytes / 1024 / 1024}MB.");
        }
    }

    private async Task<Dictionary<string, Guid>> GetExistingTermsLookupAsync(CancellationToken ct)
    {
        var terms = await _repository.GetAllAsync(ct);
        return terms.ToDictionary(t => t.Term.ToLowerInvariant(), t => t.Id);
    }

    private static ImportColumnMapping DetectColumnMapping(string[] headers)
    {
        var pattern = FindMatchingHeader(headers, StandardPatternHeaders) ?? "pattern";
        var recommendation = FindMatchingHeader(headers, StandardRecommendationHeaders) ?? "recommendation";
        var category = FindMatchingHeader(headers, StandardCategoryHeaders) ?? "category";
        var severity = FindMatchingHeader(headers, StandardSeverityHeaders) ?? "severity";

        return new ImportColumnMapping(pattern, recommendation, category, severity);
    }

    private static string? FindMatchingHeader(string[] headers, HashSet<string> candidates)
    {
        return headers.FirstOrDefault(h => candidates.Contains(h));
    }

    private ImportRow ParseCsvRow(
        CsvReader csv,
        int rowNumber,
        ImportColumnMapping mapping,
        Dictionary<string, Guid> existingTerms)
    {
        var pattern = GetCsvField(csv, mapping.PatternColumn);
        var recommendation = GetCsvField(csv, mapping.RecommendationColumn);
        var category = GetCsvField(csv, mapping.CategoryColumn);
        var severity = GetCsvField(csv, mapping.SeverityColumn);
        var matchCase = ParseBool(GetCsvField(csv, mapping.MatchCaseColumn));
        var isActive = ParseBool(GetCsvField(csv, mapping.IsActiveColumn), defaultValue: true);

        return CreateImportRow(rowNumber, pattern, recommendation, category, severity, matchCase, isActive, existingTerms);
    }

    private static string? GetCsvField(CsvReader csv, string? columnName)
    {
        if (string.IsNullOrEmpty(columnName)) return null;

        try
        {
            return csv.GetField<string>(columnName)?.Trim();
        }
        catch
        {
            return null;
        }
    }

    private ImportRow ParseExcelRow(
        IXLWorksheet worksheet,
        int rowNum,
        int firstCol,
        ImportColumnMapping mapping,
        string[] headers,
        Dictionary<string, Guid> existingTerms)
    {
        var pattern = GetExcelCell(worksheet, rowNum, firstCol, mapping.PatternColumn, headers);
        var recommendation = GetExcelCell(worksheet, rowNum, firstCol, mapping.RecommendationColumn, headers);
        var category = GetExcelCell(worksheet, rowNum, firstCol, mapping.CategoryColumn, headers);
        var severity = GetExcelCell(worksheet, rowNum, firstCol, mapping.SeverityColumn, headers);
        var matchCase = ParseBool(GetExcelCell(worksheet, rowNum, firstCol, mapping.MatchCaseColumn, headers));
        var isActive = ParseBool(GetExcelCell(worksheet, rowNum, firstCol, mapping.IsActiveColumn, headers), defaultValue: true);

        return CreateImportRow(rowNum, pattern, recommendation, category, severity, matchCase, isActive, existingTerms);
    }

    private static string? GetExcelCell(
        IXLWorksheet worksheet,
        int rowNum,
        int firstCol,
        string? columnName,
        string[] headers)
    {
        if (string.IsNullOrEmpty(columnName)) return null;

        var colIndex = Array.FindIndex(headers, h =>
            h.Equals(columnName, StringComparison.OrdinalIgnoreCase));

        if (colIndex < 0) return null;

        return worksheet.Cell(rowNum, firstCol + colIndex).GetString().Trim();
    }

    private ImportRow CreateImportRow(
        int rowNumber,
        string? pattern,
        string? recommendation,
        string? category,
        string? severity,
        bool matchCase,
        bool isActive,
        Dictionary<string, Guid> existingTerms)
    {
        // Validate pattern
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return new ImportRow(rowNumber, "", recommendation, category, severity, matchCase, isActive,
                ImportRowStatus.Invalid, "Pattern is required");
        }

        var validationResult = TermPatternValidator.Validate(pattern);
        if (validationResult.IsFailure)
        {
            return new ImportRow(rowNumber, pattern, recommendation, category, severity, matchCase, isActive,
                ImportRowStatus.Invalid, validationResult.Error);
        }

        // Check for duplicates
        if (existingTerms.TryGetValue(pattern.ToLowerInvariant(), out var existingId))
        {
            return new ImportRow(rowNumber, pattern, recommendation, category, severity, matchCase, isActive,
                ImportRowStatus.Duplicate, ExistingTermId: existingId);
        }

        return new ImportRow(rowNumber, pattern, recommendation, category, severity, matchCase, isActive,
            ImportRowStatus.Valid);
    }

    private static bool ParseBool(string? value, bool defaultValue = false)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;

        return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("1", StringComparison.Ordinal);
    }

    private static ImportParseResult CreateParseResult(List<ImportRow> rows)
    {
        var valid = rows.Count(r => r.Status == ImportRowStatus.Valid);
        var invalid = rows.Count(r => r.Status == ImportRowStatus.Invalid);
        var duplicate = rows.Count(r => r.Status == ImportRowStatus.Duplicate);

        return new ImportParseResult(rows, valid, invalid, duplicate);
    }

    #endregion
}
