using System.Globalization;
using System.Text;
using System.Text.Json;
using CsvHelper;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Service for exporting terminology to JSON and CSV files.
/// </summary>
/// <remarks>
/// LOGIC: Implements ITermExportService with:
/// - JSON export with metadata for traceability
/// - CSV export with standard headers
/// - Category filtering and inactive term inclusion
///
/// Version: v0.2.5d
/// </remarks>
public class TermExportService : ITermExportService
{
    private readonly ITerminologyService _terminologyService;
    private readonly ILogger<TermExportService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Creates a new TermExportService instance.
    /// </summary>
    public TermExportService(
        ITerminologyService terminologyService,
        ILogger<TermExportService> logger)
    {
        _terminologyService = terminologyService ?? throw new ArgumentNullException(nameof(terminologyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ExportResult> ExportToJsonAsync(
        ExportOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new ExportOptions();

        try
        {
            var terms = await GetFilteredTermsAsync(options, ct);

            _logger.LogDebug("Exporting {Count} terms to JSON", terms.Count);

            var exportedTerms = terms.Select(t => new ExportedTerm(
                t.Term,
                t.Replacement,
                t.Category,
                t.Severity,
                t.MatchCase,
                t.IsActive)).ToList();

            string content;
            if (options.IncludeMetadata)
            {
                var document = new TerminologyExportDocument(
                    new ExportMetadata(
                        Version: "1.0",
                        ExportedAt: DateTimeOffset.UtcNow,
                        TermCount: exportedTerms.Count),
                    exportedTerms);

                content = JsonSerializer.Serialize(document, JsonOptions);
            }
            else
            {
                content = JsonSerializer.Serialize(exportedTerms, JsonOptions);
            }

            _logger.LogInformation("Exported {Count} terms to JSON format", terms.Count);

            return ExportResult.Succeeded(content, terms.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export terms to JSON");
            return ExportResult.Failed($"Export failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ExportResult> ExportToCsvAsync(
        ExportOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new ExportOptions();

        try
        {
            var terms = await GetFilteredTermsAsync(options, ct);

            _logger.LogDebug("Exporting {Count} terms to CSV", terms.Count);

            using var writer = new StringWriter();
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            // Write headers
            csv.WriteField("pattern");
            csv.WriteField("recommendation");
            csv.WriteField("category");
            csv.WriteField("severity");
            csv.WriteField("match_case");
            csv.WriteField("is_active");
            await csv.NextRecordAsync();

            // Write data rows
            foreach (var term in terms)
            {
                csv.WriteField(term.Term);
                csv.WriteField(term.Replacement);
                csv.WriteField(term.Category);
                csv.WriteField(term.Severity);
                csv.WriteField(term.MatchCase.ToString().ToLowerInvariant());
                csv.WriteField(term.IsActive.ToString().ToLowerInvariant());
                await csv.NextRecordAsync();
            }

            var content = writer.ToString();

            _logger.LogInformation("Exported {Count} terms to CSV format", terms.Count);

            return ExportResult.Succeeded(content, terms.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export terms to CSV");
            return ExportResult.Failed($"Export failed: {ex.Message}");
        }
    }

    #region Private Methods

    private async Task<IReadOnlyList<Abstractions.Entities.StyleTerm>> GetFilteredTermsAsync(
        ExportOptions options,
        CancellationToken ct)
    {
        var allTerms = await _terminologyService.GetAllAsync(ct);
        var filtered = allTerms.AsEnumerable();

        // Filter by active status
        if (!options.IncludeInactive)
        {
            filtered = filtered.Where(t => t.IsActive);
        }

        // Filter by categories
        if (options.Categories is { Count: > 0 })
        {
            var categorySet = new HashSet<string>(options.Categories, StringComparer.OrdinalIgnoreCase);
            filtered = filtered.Where(t => t.Category != null && categorySet.Contains(t.Category));
        }

        return filtered.ToList();
    }

    #endregion
}
