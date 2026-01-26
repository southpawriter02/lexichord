# LDS-01: Feature Design Specification — v0.3.2d Bulk Import/Export

## 1. Metadata & Categorization

| Field              | Value                     |
| :----------------- | :------------------------ |
| **Feature ID**     | `STY-032d`                |
| **Feature Name**   | Bulk Import/Export        |
| **Target Version** | `v0.3.2d`                 |
| **Module Scope**   | `Lexichord.Modules.Style` |
| **License Tier**   | `Writer Pro`              |
| **Depends On**     | `v0.3.2a`, `v0.3.2c`      |
| **Status**         | **Draft**                 |
| **Last Updated**   | 2026-01-26                |

---

## 2. Executive Summary

Enable batch terminology operations via CSV import with column mapping and JSON export with versioned schema. Users can migrate dictionaries from external tools or share configurations across teams.

**Key Features:**

- 3-step Import Wizard (File → Mapping → Preview)
- Auto-detection of common column names
- Duplicate handling (skip or overwrite)
- JSON export with schema version and metadata

---

## 3. Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    IMPORT/EXPORT FLOW                           │
│                                                                  │
│  ┌────────────────┐    ┌────────────────┐    ┌──────────────┐   │
│  │ ImportWizard   │───►│ CsvImporter    │───►│ Repository   │   │
│  │ View/ViewModel │    │ (CsvHelper)    │    │ (Bulk Add)   │   │
│  └────────────────┘    └────────────────┘    └──────────────┘   │
│                                                                  │
│  ┌────────────────┐    ┌────────────────┐    ┌──────────────┐   │
│  │ LexiconView    │───►│ JsonExporter   │───►│ File Save    │   │
│  │ Export Button  │    │ (STJ)          │    │ Dialog       │   │
│  └────────────────┘    └────────────────┘    └──────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

### Dependencies

| Package     | Version | Purpose     |
| :---------- | :------ | :---------- |
| `CsvHelper` | 31.x    | CSV parsing |

---

## 4. Data Contract

### 4.1 New Interfaces

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>CSV import contract.</summary>
public interface ITerminologyImporter
{
    Task<ImportResult> ImportAsync(
        Stream csvStream,
        IReadOnlyList<ImportMapping> mappings,
        ImportOptions options,
        CancellationToken ct);

    Task<IReadOnlyList<string>> DetectColumnsAsync(
        Stream csvStream,
        CancellationToken ct);
}

/// <summary>JSON export contract.</summary>
public interface ITerminologyExporter
{
    Task ExportAsync(
        IEnumerable<StyleTerm> terms,
        Stream outputStream,
        CancellationToken ct);
}
```

### 4.2 New Records

```csharp
namespace Lexichord.Abstractions.Models;

/// <summary>Maps CSV column to entity field.</summary>
public record ImportMapping(string CsvColumn, string DbField);

/// <summary>Import operation result.</summary>
public record ImportResult(
    int Imported,
    int Skipped,
    IReadOnlyList<ImportError> Errors);

/// <summary>Row-level import failure.</summary>
public record ImportError(int RowNumber, string Column, string Message);

/// <summary>Import behavior options.</summary>
public record ImportOptions(bool SkipDuplicates, bool OverwriteExisting)
{
    public static ImportOptions Default => new(true, false);
}
```

### 4.3 Export Schema

```json
{
    "version": "1.0",
    "exportedAt": "2026-01-26T12:00:00Z",
    "exportedBy": "Lexichord v0.3.2",
    "terms": [
        {
            "pattern": "whitelist",
            "isRegex": false,
            "recommendation": "Use 'allowlist' instead",
            "category": "Terminology",
            "severity": "Error",
            "tags": "inclusive",
            "fuzzyEnabled": true,
            "fuzzyThreshold": 0.8
        }
    ]
}
```

---

## 5. Implementation

### 5.1 CsvTerminologyImporter

```csharp
public sealed class CsvTerminologyImporter(
    ITerminologyRepository repository,
    IValidator<StyleTermDto> validator,
    ILogger<CsvTerminologyImporter> logger) : ITerminologyImporter
{
    private static readonly Dictionary<string, string> ColumnAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["term"] = "Pattern",
        ["pattern"] = "Pattern",
        ["word"] = "Pattern",
        ["suggestion"] = "Recommendation",
        ["replacement"] = "Recommendation",
        ["recommendation"] = "Recommendation"
    };

    public async Task<IReadOnlyList<string>> DetectColumnsAsync(
        Stream csvStream, CancellationToken ct)
    {
        using var reader = new StreamReader(csvStream, leaveOpen: true);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        await csv.ReadAsync();
        csv.ReadHeader();

        return csv.HeaderRecord?.ToList() ?? [];
    }

    public async Task<ImportResult> ImportAsync(
        Stream csvStream,
        IReadOnlyList<ImportMapping> mappings,
        ImportOptions options,
        CancellationToken ct)
    {
        logger.LogInformation("Starting CSV import with {MappingCount} mappings", mappings.Count);

        var imported = 0;
        var skipped = 0;
        var errors = new List<ImportError>();

        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        await csv.ReadAsync();
        csv.ReadHeader();

        var rowNumber = 1;
        while (await csv.ReadAsync())
        {
            rowNumber++;
            ct.ThrowIfCancellationRequested();

            try
            {
                var term = MapRowToTerm(csv, mappings);
                var validationResult = validator.Validate(term);

                if (!validationResult.IsValid)
                {
                    var error = validationResult.Errors.First();
                    errors.Add(new ImportError(rowNumber, error.PropertyName, error.ErrorMessage));
                    continue;
                }

                var exists = await repository.ExistsByPatternAsync(term.Pattern, ct);
                if (exists)
                {
                    if (options.SkipDuplicates)
                    {
                        skipped++;
                        continue;
                    }
                    if (options.OverwriteExisting)
                    {
                        await repository.UpdateByPatternAsync(term.ToEntity(), ct);
                        imported++;
                        continue;
                    }
                }

                await repository.CreateAsync(term.ToEntity(), ct);
                imported++;
            }
            catch (Exception ex)
            {
                errors.Add(new ImportError(rowNumber, "Row", ex.Message));
            }
        }

        logger.LogInformation(
            "Import complete: {Imported} imported, {Skipped} skipped, {Errors} errors",
            imported, skipped, errors.Count);

        return new ImportResult(imported, skipped, errors);
    }

    private static StyleTermDto MapRowToTerm(CsvReader csv, IReadOnlyList<ImportMapping> mappings)
    {
        var dto = new StyleTermDto { Id = Guid.NewGuid() };

        foreach (var mapping in mappings)
        {
            var value = csv.GetField(mapping.CsvColumn);
            // Apply value to corresponding property via reflection or switch
        }

        return dto;
    }

    public static IReadOnlyList<ImportMapping> AutoDetectMappings(IReadOnlyList<string> columns)
    {
        var mappings = new List<ImportMapping>();

        foreach (var column in columns)
        {
            if (ColumnAliases.TryGetValue(column, out var dbField))
            {
                mappings.Add(new ImportMapping(column, dbField));
            }
        }

        return mappings;
    }
}
```

### 5.2 JsonTerminologyExporter

```csharp
public sealed class JsonTerminologyExporter(
    ILogger<JsonTerminologyExporter> logger) : ITerminologyExporter
{
    public async Task ExportAsync(
        IEnumerable<StyleTerm> terms,
        Stream outputStream,
        CancellationToken ct)
    {
        var termList = terms.ToList();
        logger.LogInformation("Exporting {TermCount} terms to JSON", termList.Count);

        var document = new ExportDocument
        {
            Version = "1.0",
            ExportedAt = DateTime.UtcNow,
            ExportedBy = "Lexichord v0.3.2",
            Terms = termList.Select(t => new ExportedTerm
            {
                Pattern = t.Pattern,
                IsRegex = t.IsRegex,
                Recommendation = t.Recommendation,
                Category = t.Category.ToString(),
                Severity = t.Severity.ToString(),
                Tags = t.Tags,
                FuzzyEnabled = t.FuzzyEnabled,
                FuzzyThreshold = t.FuzzyThreshold
            }).ToList()
        };

        await JsonSerializer.SerializeAsync(outputStream, document,
            new JsonSerializerOptions { WriteIndented = true }, ct);

        logger.LogInformation("Export complete: {TermCount} terms", termList.Count);
    }
}

public record ExportDocument
{
    public required string Version { get; init; }
    public DateTime ExportedAt { get; init; }
    public required string ExportedBy { get; init; }
    public required List<ExportedTerm> Terms { get; init; }
}

public record ExportedTerm
{
    public required string Pattern { get; init; }
    public bool IsRegex { get; init; }
    public required string Recommendation { get; init; }
    public required string Category { get; init; }
    public required string Severity { get; init; }
    public string? Tags { get; init; }
    public bool FuzzyEnabled { get; init; }
    public double FuzzyThreshold { get; init; }
}
```

### 5.3 ImportWizardViewModel

```csharp
public sealed partial class ImportWizardViewModel : ViewModelBase
{
    private readonly ITerminologyImporter _importer;
    private readonly IFileService _fileService;

    [ObservableProperty] private int _currentStep = 0;
    [ObservableProperty] private string? _selectedFilePath;
    [ObservableProperty] private ObservableCollection<string> _detectedColumns = [];
    [ObservableProperty] private ObservableCollection<ImportMapping> _mappings = [];
    [ObservableProperty] private ObservableCollection<StyleTermDto> _previewTerms = [];
    [ObservableProperty] private bool _skipDuplicates = true;
    [ObservableProperty] private bool _overwriteExisting;
    [ObservableProperty] private ImportResult? _result;

    public bool CanGoNext => CurrentStep switch
    {
        0 => !string.IsNullOrEmpty(SelectedFilePath),
        1 => Mappings.Any(m => m.DbField == "Pattern"),
        2 => true,
        _ => false
    };

    [RelayCommand]
    private async Task SelectFileAsync(CancellationToken ct)
    {
        var path = await _fileService.OpenFileDialogAsync("*.csv", ct);
        if (path is null) return;

        SelectedFilePath = path;
        await using var stream = File.OpenRead(path);
        var columns = await _importer.DetectColumnsAsync(stream, ct);

        DetectedColumns = new ObservableCollection<string>(columns);
        Mappings = new ObservableCollection<ImportMapping>(
            CsvTerminologyImporter.AutoDetectMappings(columns));
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void NextStep() => CurrentStep++;

    [RelayCommand]
    private void PreviousStep() => CurrentStep = Math.Max(0, CurrentStep - 1);

    [RelayCommand]
    private async Task ImportAsync(CancellationToken ct)
    {
        await using var stream = File.OpenRead(SelectedFilePath!);
        var options = new ImportOptions(SkipDuplicates, OverwriteExisting);

        Result = await _importer.ImportAsync(stream, Mappings.ToList(), options, ct);
        CurrentStep = 3; // Results step
    }
}
```

---

## 6. UI/UX Specifications

### Import Wizard Steps

```
Step 1: File Selection
┌────────────────────────────────────────────────────────┐
│  Import Terminology from CSV                           │
│                                                        │
│  Select a CSV file containing terminology entries.     │
│                                                        │
│  ┌──────────────────────────────────┐  [Browse...]     │
│  │ C:\Users\...\terms.csv           │                  │
│  └──────────────────────────────────┘                  │
│                                                        │
│                           [Cancel]  [Next →]           │
└────────────────────────────────────────────────────────┘

Step 2: Column Mapping
┌────────────────────────────────────────────────────────┐
│  Map CSV Columns to Fields                             │
│                                                        │
│  CSV Column        →  Field                            │
│  ┌─────────────┐      ┌─────────────────┐              │
│  │ Term        │  →   │ Pattern *       ▼│              │
│  │ Suggestion  │  →   │ Recommendation *▼│              │
│  │ Type        │  →   │ Category        ▼│              │
│  └─────────────┘      └─────────────────┘              │
│                                                        │
│  [✓] Skip duplicates  [ ] Overwrite existing           │
│                                                        │
│              [← Back]  [Cancel]  [Next →]              │
└────────────────────────────────────────────────────────┘

Step 3: Preview & Confirm
┌────────────────────────────────────────────────────────┐
│  Preview Import (first 10 rows)                        │
│  ┌──────────────────────────────────────────────────┐  │
│  │ Pattern      │ Recommendation    │ Category      │  │
│  │ whitelist    │ allowlist         │ Terminology   │  │
│  │ blacklist    │ blocklist         │ Terminology   │  │
│  └──────────────────────────────────────────────────┘  │
│                                                        │
│  Ready to import 45 terms.                             │
│                                                        │
│              [← Back]  [Cancel]  [Import]              │
└────────────────────────────────────────────────────────┘
```

---

## 7. Unit Testing

```csharp
[Trait("Category", "Unit")]
public class CsvTerminologyImporterTests
{
    [Fact]
    public async Task Import_WithValidCsv_CreatesTerms()
    {
        var csv = """
            Term,Replacement
            whitelist,allowlist
            blacklist,blocklist
            """;
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var sut = CreateImporter();

        var result = await sut.ImportAsync(stream, DefaultMappings, ImportOptions.Default, CancellationToken.None);

        result.Imported.Should().Be(2);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Import_WithDuplicates_SkipsWhenConfigured()
    {
        // Test skip duplicates behavior
    }

    [Fact]
    public async Task DetectColumns_ReturnsHeaderRow()
    {
        var csv = "Term,Replacement,Category\ntest,test,test";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var sut = CreateImporter();

        var columns = await sut.DetectColumnsAsync(stream, CancellationToken.None);

        columns.Should().Contain("Term", "Replacement", "Category");
    }

    [Fact]
    public async Task AutoDetectMappings_RecognizesAliases()
    {
        var columns = new[] { "term", "suggestion", "type" };

        var mappings = CsvTerminologyImporter.AutoDetectMappings(columns);

        mappings.Should().Contain(m => m.CsvColumn == "term" && m.DbField == "Pattern");
        mappings.Should().Contain(m => m.CsvColumn == "suggestion" && m.DbField == "Recommendation");
    }
}

[Trait("Category", "Unit")]
public class JsonTerminologyExporterTests
{
    [Fact]
    public async Task Export_WithTerms_ProducesValidJson()
    {
        var terms = new[] { new StyleTerm { Pattern = "test", Recommendation = "test" } };
        var sut = new JsonTerminologyExporter(Mock.Of<ILogger<JsonTerminologyExporter>>());
        using var stream = new MemoryStream();

        await sut.ExportAsync(terms, stream, CancellationToken.None);

        stream.Position = 0;
        var json = await JsonSerializer.DeserializeAsync<ExportDocument>(stream);
        json!.Version.Should().Be("1.0");
        json.Terms.Should().HaveCount(1);
    }
}
```

---

## 8. Acceptance Criteria

| #   | Criterion                                                       |
| :-- | :-------------------------------------------------------------- |
| 1   | CSV with valid columns imports successfully.                    |
| 2   | Column mapping auto-detects common names (term, pattern, etc.). |
| 3   | Duplicate handling respects skip/overwrite options.             |
| 4   | Invalid rows logged with row number and error message.          |
| 5   | Export produces valid JSON with version and metadata.           |
| 6   | Import wizard has Back/Next/Cancel navigation.                  |
| 7   | Preview shows first 10 rows before commit.                      |
| 8   | Export button in LexiconView triggers file save dialog.         |

---

## 9. Deliverable Checklist

| Step | Description                                | Status |
| :--- | :----------------------------------------- | :----- |
| 1    | Add `CsvHelper` NuGet package.             | [ ]    |
| 2    | Create `ITerminologyImporter` interface.   | [ ]    |
| 3    | Create `ITerminologyExporter` interface.   | [ ]    |
| 4    | Implement `CsvTerminologyImporter`.        | [ ]    |
| 5    | Implement `JsonTerminologyExporter`.       | [ ]    |
| 6    | Create `ImportWizardView.axaml` (3 steps). | [ ]    |
| 7    | Create `ImportWizardViewModel`.            | [ ]    |
| 8    | Add Export button to `LexiconView`.        | [ ]    |
| 9    | Unit tests for importer and exporter.      | [ ]    |
| 10   | DI registration for services.              | [ ]    |

---

## 10. Changelog Entry

```markdown
### v0.3.2d — Bulk Import/Export

**Added:**

- `ITerminologyImporter` interface for CSV import (STY-032d)
- `ITerminologyExporter` interface for JSON export (STY-032d)
- `CsvTerminologyImporter` with column auto-detection (STY-032d)
- `JsonTerminologyExporter` with versioned schema (STY-032d)
- Import Wizard with 3-step flow: File → Mapping → Preview (STY-032d)
- Export button in LexiconView toolbar (STY-032d)

**Technical:**

- CsvHelper NuGet package (31.x) for robust CSV parsing
- Auto-detection of common column aliases (term, pattern, suggestion)
- Duplicate handling options: skip or overwrite
```
