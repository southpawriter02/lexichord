# LCS-DES-046-KG-g: Axiom Loader

## 1. Metadata & Categorization

| Field | Value |
| :--- | :--- |
| **Document ID** | LCS-DES-046-KG-g |
| **Feature ID** | KG-046g |
| **Feature Name** | Axiom Loader |
| **Target Version** | v0.4.6g |
| **Module Scope** | `Lexichord.Modules.Knowledge` |
| **Swimlane** | Memory |
| **License Tier** | WriterPro (built-in), Teams (custom) |
| **Feature Gate Key** | `knowledge.axioms.enabled` |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 2. Executive Summary

### 2.1 The Requirement

Axioms must be loadable from YAML files in the workspace `.lexichord/knowledge/axioms/` directory and from built-in application axioms. The loader must validate YAML syntax, axiom structure, and schema compatibility before persisting.

### 2.2 The Proposed Solution

Implement `IAxiomLoader` with:

- **YAML Parsing**: Parse axiom definitions from YAML files.
- **Schema Validation**: Validate axiom structure against data model.
- **Type Verification**: Ensure target types exist in Schema Registry.
- **File Watching**: Reload axioms when files change.
- **Built-in Axioms**: Ship default axioms for technical documentation.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

**Upstream Modules:**
- v0.4.6e: `Axiom`, `AxiomRule` — Data model
- v0.4.6f: `IAxiomRepository` — Persistence
- v0.4.5f: `ISchemaRegistry` — Type validation
- v0.0.3d: `IConfiguration` — Paths

**NuGet Packages:**
- `YamlDotNet` (13.x) — YAML parsing
- `System.IO.FileSystemWatcher` — File watching

### 3.2 Module Placement

```
Lexichord.Modules.Knowledge/
├── Axioms/
│   ├── IAxiomLoader.cs
│   ├── AxiomLoader.cs
│   ├── AxiomYamlParser.cs
│   ├── AxiomSchemaValidator.cs
│   └── AxiomFileWatcher.cs
├── Resources/
│   └── BuiltInAxioms/
│       ├── api-documentation.yaml
│       └── technical-writing.yaml
```

### 3.3 Licensing Behavior

- **Load Behavior:** [x] Conditional
- **WriterPro:** Can load built-in axioms only
- **Teams+:** Can load custom workspace axioms

---

## 4. Data Contract (The API)

### 4.1 Loader Interface

```csharp
namespace Lexichord.Modules.Knowledge.Axioms;

/// <summary>
/// Loads axioms from YAML files and built-in resources.
/// </summary>
public interface IAxiomLoader
{
    /// <summary>
    /// Loads all axioms from workspace and built-in sources.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Load result with statistics and errors.</returns>
    Task<AxiomLoadResult> LoadAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Loads axioms from a specific YAML file.
    /// </summary>
    /// <param name="filePath">Path to YAML file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Load result for this file.</returns>
    Task<AxiomLoadResult> LoadFileAsync(
        string filePath,
        CancellationToken ct = default);

    /// <summary>
    /// Loads built-in axioms from application resources.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Load result for built-in axioms.</returns>
    Task<AxiomLoadResult> LoadBuiltInAsync(CancellationToken ct = default);

    /// <summary>
    /// Loads axioms from workspace directory.
    /// </summary>
    /// <param name="workspacePath">Workspace root path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Load result for workspace axioms.</returns>
    Task<AxiomLoadResult> LoadWorkspaceAsync(
        string workspacePath,
        CancellationToken ct = default);

    /// <summary>
    /// Validates a YAML file without loading.
    /// </summary>
    /// <param name="filePath">Path to YAML file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validation result.</returns>
    Task<AxiomValidationReport> ValidateFileAsync(
        string filePath,
        CancellationToken ct = default);

    /// <summary>
    /// Starts watching workspace for axiom file changes.
    /// </summary>
    /// <param name="workspacePath">Workspace root path.</param>
    void StartWatching(string workspacePath);

    /// <summary>
    /// Stops watching for file changes.
    /// </summary>
    void StopWatching();

    /// <summary>
    /// Event raised when axioms are reloaded due to file changes.
    /// </summary>
    event EventHandler<AxiomLoadResult>? AxiomsReloaded;
}

/// <summary>
/// Result of loading axioms.
/// </summary>
public record AxiomLoadResult
{
    /// <summary>Whether loading succeeded overall.</summary>
    public bool Success => !Errors.Any(e => e.Severity == LoadErrorSeverity.Error);

    /// <summary>Axioms successfully loaded.</summary>
    public required IReadOnlyList<Axiom> Axioms { get; init; }

    /// <summary>Files processed.</summary>
    public required IReadOnlyList<string> FilesProcessed { get; init; }

    /// <summary>Errors and warnings encountered.</summary>
    public IReadOnlyList<AxiomLoadError> Errors { get; init; } = Array.Empty<AxiomLoadError>();

    /// <summary>Load duration.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Source type (BuiltIn, Workspace, File).</summary>
    public AxiomSourceType SourceType { get; init; }
}

/// <summary>
/// Error encountered during axiom loading.
/// </summary>
public record AxiomLoadError
{
    /// <summary>File where error occurred.</summary>
    public string? FilePath { get; init; }

    /// <summary>Line number (if applicable).</summary>
    public int? Line { get; init; }

    /// <summary>Column number (if applicable).</summary>
    public int? Column { get; init; }

    /// <summary>Error code.</summary>
    public required string Code { get; init; }

    /// <summary>Error message.</summary>
    public required string Message { get; init; }

    /// <summary>Error severity.</summary>
    public LoadErrorSeverity Severity { get; init; } = LoadErrorSeverity.Error;

    /// <summary>Axiom ID (if error is axiom-specific).</summary>
    public string? AxiomId { get; init; }
}

public enum LoadErrorSeverity { Error, Warning, Info }
public enum AxiomSourceType { BuiltIn, Workspace, File }

/// <summary>
/// Validation report for a YAML file.
/// </summary>
public record AxiomValidationReport
{
    /// <summary>File path validated.</summary>
    public required string FilePath { get; init; }

    /// <summary>Whether file is valid.</summary>
    public bool IsValid => !Errors.Any(e => e.Severity == LoadErrorSeverity.Error);

    /// <summary>Validation errors.</summary>
    public required IReadOnlyList<AxiomLoadError> Errors { get; init; }

    /// <summary>Axiom count if valid.</summary>
    public int AxiomCount { get; init; }

    /// <summary>Unrecognized target types.</summary>
    public IReadOnlyList<string> UnknownTargetTypes { get; init; } = Array.Empty<string>();
}
```

### 4.2 YAML Model

```csharp
namespace Lexichord.Modules.Knowledge.Axioms;

/// <summary>
/// YAML file structure for axiom definitions.
/// </summary>
internal record AxiomYamlFile
{
    [YamlMember(Alias = "axiom_version")]
    public string AxiomVersion { get; init; } = "1.0";

    [YamlMember(Alias = "name")]
    public string? Name { get; init; }

    [YamlMember(Alias = "description")]
    public string? Description { get; init; }

    [YamlMember(Alias = "axioms")]
    public List<AxiomYamlEntry> Axioms { get; init; } = new();
}

/// <summary>
/// Single axiom entry in YAML.
/// </summary>
internal record AxiomYamlEntry
{
    [YamlMember(Alias = "id")]
    public string Id { get; init; } = "";

    [YamlMember(Alias = "name")]
    public string Name { get; init; } = "";

    [YamlMember(Alias = "description")]
    public string? Description { get; init; }

    [YamlMember(Alias = "target_type")]
    public string TargetType { get; init; } = "";

    [YamlMember(Alias = "target_kind")]
    public string TargetKind { get; init; } = "Entity";

    [YamlMember(Alias = "severity")]
    public string Severity { get; init; } = "error";

    [YamlMember(Alias = "category")]
    public string? Category { get; init; }

    [YamlMember(Alias = "tags")]
    public List<string> Tags { get; init; } = new();

    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; init; } = true;

    [YamlMember(Alias = "rules")]
    public List<AxiomRuleYamlEntry> Rules { get; init; } = new();
}

/// <summary>
/// Single rule entry in YAML.
/// </summary>
internal record AxiomRuleYamlEntry
{
    [YamlMember(Alias = "property")]
    public string? Property { get; init; }

    [YamlMember(Alias = "properties")]
    public List<string>? Properties { get; init; }

    [YamlMember(Alias = "constraint")]
    public string Constraint { get; init; } = "";

    [YamlMember(Alias = "values")]
    public List<object>? Values { get; init; }

    [YamlMember(Alias = "min")]
    public object? Min { get; init; }

    [YamlMember(Alias = "max")]
    public object? Max { get; init; }

    [YamlMember(Alias = "pattern")]
    public string? Pattern { get; init; }

    [YamlMember(Alias = "min_count")]
    public int? MinCount { get; init; }

    [YamlMember(Alias = "max_count")]
    public int? MaxCount { get; init; }

    [YamlMember(Alias = "when")]
    public AxiomConditionYamlEntry? When { get; init; }

    [YamlMember(Alias = "error_message")]
    public string? ErrorMessage { get; init; }

    [YamlMember(Alias = "reference_type")]
    public string? ReferenceType { get; init; }
}

/// <summary>
/// Condition entry in YAML.
/// </summary>
internal record AxiomConditionYamlEntry
{
    [YamlMember(Alias = "property")]
    public string Property { get; init; } = "";

    [YamlMember(Alias = "operator")]
    public string Operator { get; init; } = "equals";

    [YamlMember(Alias = "value")]
    public object Value { get; init; } = "";
}
```

---

## 5. Implementation Logic

### 5.1 AxiomLoader

```csharp
namespace Lexichord.Modules.Knowledge.Axioms;

/// <summary>
/// Loads axioms from YAML files and built-in resources.
/// </summary>
public sealed class AxiomLoader : IAxiomLoader, IDisposable
{
    private readonly IAxiomRepository _repository;
    private readonly ISchemaRegistry _schemaRegistry;
    private readonly ILicenseContext _licenseContext;
    private readonly AxiomYamlParser _parser;
    private readonly AxiomSchemaValidator _validator;
    private readonly ILogger<AxiomLoader> _logger;
    private FileSystemWatcher? _watcher;

    private const string AxiomDirectory = ".lexichord/knowledge/axioms";
    private const string BuiltInResourcePrefix = "Lexichord.Modules.Knowledge.Resources.BuiltInAxioms";

    public event EventHandler<AxiomLoadResult>? AxiomsReloaded;

    public AxiomLoader(
        IAxiomRepository repository,
        ISchemaRegistry schemaRegistry,
        ILicenseContext licenseContext,
        ILogger<AxiomLoader> logger)
    {
        _repository = repository;
        _schemaRegistry = schemaRegistry;
        _licenseContext = licenseContext;
        _logger = logger;
        _parser = new AxiomYamlParser();
        _validator = new AxiomSchemaValidator(schemaRegistry);
    }

    public async Task<AxiomLoadResult> LoadAllAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var allAxioms = new List<Axiom>();
        var allFiles = new List<string>();
        var allErrors = new List<AxiomLoadError>();

        // 1. Load built-in axioms (always)
        var builtInResult = await LoadBuiltInAsync(ct);
        allAxioms.AddRange(builtInResult.Axioms);
        allFiles.AddRange(builtInResult.FilesProcessed);
        allErrors.AddRange(builtInResult.Errors);

        // 2. Load workspace axioms (Teams+ only)
        if (_licenseContext.Tier >= LicenseTier.Teams)
        {
            var workspacePath = GetWorkspacePath();
            if (workspacePath != null)
            {
                var workspaceResult = await LoadWorkspaceAsync(workspacePath, ct);
                allAxioms.AddRange(workspaceResult.Axioms);
                allFiles.AddRange(workspaceResult.FilesProcessed);
                allErrors.AddRange(workspaceResult.Errors);
            }
        }

        // 3. Persist to repository
        if (allAxioms.Any())
        {
            await _repository.SaveBatchAsync(allAxioms, ct);
        }

        stopwatch.Stop();

        _logger.LogInformation(
            "Loaded {Count} axioms from {Files} files in {Duration}ms",
            allAxioms.Count, allFiles.Count, stopwatch.ElapsedMilliseconds);

        return new AxiomLoadResult
        {
            Axioms = allAxioms,
            FilesProcessed = allFiles,
            Errors = allErrors,
            Duration = stopwatch.Elapsed,
            SourceType = AxiomSourceType.BuiltIn
        };
    }

    public async Task<AxiomLoadResult> LoadBuiltInAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var axioms = new List<Axiom>();
        var files = new List<string>();
        var errors = new List<AxiomLoadError>();

        var assembly = typeof(AxiomLoader).Assembly;
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(BuiltInResourcePrefix) && n.EndsWith(".yaml"));

        foreach (var resourceName in resourceNames)
        {
            try
            {
                await using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) continue;

                using var reader = new StreamReader(stream);
                var yaml = await reader.ReadToEndAsync(ct);

                var parseResult = _parser.Parse(yaml, resourceName);
                if (parseResult.Errors.Any())
                {
                    errors.AddRange(parseResult.Errors);
                    continue;
                }

                var validationResult = _validator.Validate(parseResult.Axioms);
                errors.AddRange(validationResult.Errors);

                var validAxioms = parseResult.Axioms
                    .Where(a => !validationResult.InvalidAxiomIds.Contains(a.Id))
                    .Select(a => a with { SourceFile = $"builtin:{resourceName}" });

                axioms.AddRange(validAxioms);
                files.Add(resourceName);

                _logger.LogDebug("Loaded {Count} axioms from built-in {Resource}",
                    validAxioms.Count(), resourceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load built-in axiom resource {Resource}", resourceName);
                errors.Add(new AxiomLoadError
                {
                    FilePath = resourceName,
                    Code = "BUILTIN_LOAD_ERROR",
                    Message = $"Failed to load built-in axioms: {ex.Message}"
                });
            }
        }

        return new AxiomLoadResult
        {
            Axioms = axioms,
            FilesProcessed = files,
            Errors = errors,
            Duration = stopwatch.Elapsed,
            SourceType = AxiomSourceType.BuiltIn
        };
    }

    public async Task<AxiomLoadResult> LoadWorkspaceAsync(
        string workspacePath,
        CancellationToken ct = default)
    {
        if (_licenseContext.Tier < LicenseTier.Teams)
        {
            throw new FeatureNotLicensedException("Custom Axioms", LicenseTier.Teams);
        }

        var stopwatch = Stopwatch.StartNew();
        var axioms = new List<Axiom>();
        var files = new List<string>();
        var errors = new List<AxiomLoadError>();

        var axiomDir = Path.Combine(workspacePath, AxiomDirectory);
        if (!Directory.Exists(axiomDir))
        {
            return new AxiomLoadResult
            {
                Axioms = Array.Empty<Axiom>(),
                FilesProcessed = Array.Empty<string>(),
                Duration = stopwatch.Elapsed,
                SourceType = AxiomSourceType.Workspace
            };
        }

        var yamlFiles = Directory.GetFiles(axiomDir, "*.yaml", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(axiomDir, "*.yml", SearchOption.AllDirectories));

        foreach (var filePath in yamlFiles)
        {
            var fileResult = await LoadFileInternalAsync(filePath, ct);
            axioms.AddRange(fileResult.Axioms);
            files.Add(filePath);
            errors.AddRange(fileResult.Errors);
        }

        return new AxiomLoadResult
        {
            Axioms = axioms,
            FilesProcessed = files,
            Errors = errors,
            Duration = stopwatch.Elapsed,
            SourceType = AxiomSourceType.Workspace
        };
    }

    public async Task<AxiomLoadResult> LoadFileAsync(
        string filePath,
        CancellationToken ct = default)
    {
        if (_licenseContext.Tier < LicenseTier.Teams)
        {
            throw new FeatureNotLicensedException("Custom Axioms", LicenseTier.Teams);
        }

        var result = await LoadFileInternalAsync(filePath, ct);

        // Persist to repository
        if (result.Axioms.Any())
        {
            // Delete existing axioms from this source first
            await _repository.DeleteBySourceAsync(filePath, ct);
            await _repository.SaveBatchAsync(result.Axioms, ct);
        }

        return result;
    }

    private async Task<AxiomLoadResult> LoadFileInternalAsync(
        string filePath,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var errors = new List<AxiomLoadError>();

        try
        {
            var yaml = await File.ReadAllTextAsync(filePath, ct);

            var parseResult = _parser.Parse(yaml, filePath);
            if (parseResult.Errors.Any())
            {
                return new AxiomLoadResult
                {
                    Axioms = Array.Empty<Axiom>(),
                    FilesProcessed = new[] { filePath },
                    Errors = parseResult.Errors,
                    Duration = stopwatch.Elapsed,
                    SourceType = AxiomSourceType.File
                };
            }

            var validationResult = _validator.Validate(parseResult.Axioms);
            errors.AddRange(validationResult.Errors);

            var validAxioms = parseResult.Axioms
                .Where(a => !validationResult.InvalidAxiomIds.Contains(a.Id))
                .Select(a => a with { SourceFile = filePath })
                .ToList();

            _logger.LogInformation("Loaded {Count} axioms from {File}",
                validAxioms.Count, filePath);

            return new AxiomLoadResult
            {
                Axioms = validAxioms,
                FilesProcessed = new[] { filePath },
                Errors = errors,
                Duration = stopwatch.Elapsed,
                SourceType = AxiomSourceType.File
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load axiom file {File}", filePath);

            return new AxiomLoadResult
            {
                Axioms = Array.Empty<Axiom>(),
                FilesProcessed = new[] { filePath },
                Errors = new[]
                {
                    new AxiomLoadError
                    {
                        FilePath = filePath,
                        Code = "FILE_LOAD_ERROR",
                        Message = $"Failed to load file: {ex.Message}"
                    }
                },
                Duration = stopwatch.Elapsed,
                SourceType = AxiomSourceType.File
            };
        }
    }

    public async Task<AxiomValidationReport> ValidateFileAsync(
        string filePath,
        CancellationToken ct = default)
    {
        var errors = new List<AxiomLoadError>();
        var unknownTypes = new List<string>();

        try
        {
            var yaml = await File.ReadAllTextAsync(filePath, ct);

            var parseResult = _parser.Parse(yaml, filePath);
            errors.AddRange(parseResult.Errors);

            if (!parseResult.Errors.Any(e => e.Severity == LoadErrorSeverity.Error))
            {
                var validationResult = _validator.Validate(parseResult.Axioms);
                errors.AddRange(validationResult.Errors);
                unknownTypes.AddRange(validationResult.UnknownTargetTypes);
            }

            return new AxiomValidationReport
            {
                FilePath = filePath,
                Errors = errors,
                AxiomCount = parseResult.Axioms.Count,
                UnknownTargetTypes = unknownTypes
            };
        }
        catch (Exception ex)
        {
            return new AxiomValidationReport
            {
                FilePath = filePath,
                Errors = new[]
                {
                    new AxiomLoadError
                    {
                        FilePath = filePath,
                        Code = "VALIDATION_ERROR",
                        Message = ex.Message
                    }
                }
            };
        }
    }

    public void StartWatching(string workspacePath)
    {
        if (_licenseContext.Tier < LicenseTier.Teams)
        {
            return;
        }

        StopWatching();

        var axiomDir = Path.Combine(workspacePath, AxiomDirectory);
        if (!Directory.Exists(axiomDir))
        {
            return;
        }

        _watcher = new FileSystemWatcher(axiomDir)
        {
            Filter = "*.yaml",
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
        };

        _watcher.Changed += OnAxiomFileChanged;
        _watcher.Created += OnAxiomFileChanged;
        _watcher.Deleted += OnAxiomFileChanged;
        _watcher.Renamed += OnAxiomFileRenamed;

        _watcher.EnableRaisingEvents = true;

        _logger.LogInformation("Started watching axiom directory: {Dir}", axiomDir);
    }

    public void StopWatching()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;

            _logger.LogInformation("Stopped watching axiom directory");
        }
    }

    private async void OnAxiomFileChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce rapid changes
        await Task.Delay(500);

        try
        {
            var result = await LoadFileAsync(e.FullPath);
            AxiomsReloaded?.Invoke(this, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading axiom file {File}", e.FullPath);
        }
    }

    private async void OnAxiomFileRenamed(object sender, RenamedEventArgs e)
    {
        try
        {
            // Delete axioms from old file
            await _repository.DeleteBySourceAsync(e.OldFullPath);

            // Load from new file
            var result = await LoadFileAsync(e.FullPath);
            AxiomsReloaded?.Invoke(this, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling axiom file rename {File}", e.FullPath);
        }
    }

    private string? GetWorkspacePath()
    {
        // This would integrate with workspace service
        return null;
    }

    public void Dispose()
    {
        StopWatching();
    }
}
```

### 5.2 YAML Parser

```csharp
namespace Lexichord.Modules.Knowledge.Axioms;

/// <summary>
/// Parses axiom YAML files into Axiom records.
/// </summary>
internal sealed class AxiomYamlParser
{
    private readonly IDeserializer _deserializer;

    public AxiomYamlParser()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public AxiomParseResult Parse(string yaml, string sourcePath)
    {
        var errors = new List<AxiomLoadError>();
        var axioms = new List<Axiom>();

        try
        {
            var file = _deserializer.Deserialize<AxiomYamlFile>(yaml);

            if (file?.Axioms == null)
            {
                errors.Add(new AxiomLoadError
                {
                    FilePath = sourcePath,
                    Code = "EMPTY_FILE",
                    Message = "No axioms found in file"
                });
                return new AxiomParseResult(axioms, errors);
            }

            foreach (var entry in file.Axioms)
            {
                try
                {
                    var axiom = MapToAxiom(entry, file.Name);
                    axioms.Add(axiom);
                }
                catch (Exception ex)
                {
                    errors.Add(new AxiomLoadError
                    {
                        FilePath = sourcePath,
                        Code = "AXIOM_PARSE_ERROR",
                        Message = $"Failed to parse axiom '{entry.Id}': {ex.Message}",
                        AxiomId = entry.Id,
                        Severity = LoadErrorSeverity.Error
                    });
                }
            }
        }
        catch (YamlException ex)
        {
            errors.Add(new AxiomLoadError
            {
                FilePath = sourcePath,
                Line = ex.Start.Line,
                Column = ex.Start.Column,
                Code = "YAML_SYNTAX_ERROR",
                Message = ex.Message,
                Severity = LoadErrorSeverity.Error
            });
        }

        return new AxiomParseResult(axioms, errors);
    }

    private Axiom MapToAxiom(AxiomYamlEntry entry, string? fileCategory)
    {
        return new Axiom
        {
            Id = entry.Id,
            Name = entry.Name,
            Description = entry.Description,
            TargetType = entry.TargetType,
            TargetKind = ParseTargetKind(entry.TargetKind),
            Severity = ParseSeverity(entry.Severity),
            Category = entry.Category ?? fileCategory,
            Tags = entry.Tags,
            IsEnabled = entry.Enabled,
            Rules = entry.Rules.Select(MapToRule).ToList()
        };
    }

    private AxiomRule MapToRule(AxiomRuleYamlEntry entry)
    {
        return new AxiomRule
        {
            Property = entry.Property,
            Properties = entry.Properties,
            Constraint = ParseConstraintType(entry.Constraint),
            Values = entry.Values,
            Min = entry.Min,
            Max = entry.Max,
            Pattern = entry.Pattern,
            MinCount = entry.MinCount,
            MaxCount = entry.MaxCount,
            When = entry.When != null ? MapToCondition(entry.When) : null,
            ErrorMessage = entry.ErrorMessage,
            ReferenceType = entry.ReferenceType
        };
    }

    private AxiomCondition MapToCondition(AxiomConditionYamlEntry entry)
    {
        return new AxiomCondition
        {
            Property = entry.Property,
            Operator = ParseConditionOperator(entry.Operator),
            Value = entry.Value
        };
    }

    private static AxiomTargetKind ParseTargetKind(string value) =>
        Enum.TryParse<AxiomTargetKind>(value, ignoreCase: true, out var result)
            ? result
            : AxiomTargetKind.Entity;

    private static AxiomSeverity ParseSeverity(string value) =>
        Enum.TryParse<AxiomSeverity>(value, ignoreCase: true, out var result)
            ? result
            : AxiomSeverity.Error;

    private static AxiomConstraintType ParseConstraintType(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "required" => AxiomConstraintType.Required,
            "one_of" or "oneof" => AxiomConstraintType.OneOf,
            "not_one_of" or "notoneof" => AxiomConstraintType.NotOneOf,
            "range" => AxiomConstraintType.Range,
            "pattern" => AxiomConstraintType.Pattern,
            "cardinality" => AxiomConstraintType.Cardinality,
            "not_both" or "notboth" => AxiomConstraintType.NotBoth,
            "requires_together" => AxiomConstraintType.RequiresTogether,
            "equals" => AxiomConstraintType.Equals,
            "not_equals" or "notequals" => AxiomConstraintType.NotEquals,
            "unique" => AxiomConstraintType.Unique,
            "reference_exists" => AxiomConstraintType.ReferenceExists,
            "type_valid" => AxiomConstraintType.TypeValid,
            "custom" => AxiomConstraintType.Custom,
            _ => throw new ArgumentException($"Unknown constraint type: {value}")
        };
    }

    private static ConditionOperator ParseConditionOperator(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "equals" or "=" or "==" => ConditionOperator.Equals,
            "not_equals" or "!=" or "<>" => ConditionOperator.NotEquals,
            "contains" => ConditionOperator.Contains,
            "starts_with" or "startswith" => ConditionOperator.StartsWith,
            "ends_with" or "endswith" => ConditionOperator.EndsWith,
            "greater_than" or ">" or "gt" => ConditionOperator.GreaterThan,
            "less_than" or "<" or "lt" => ConditionOperator.LessThan,
            "is_null" or "isnull" => ConditionOperator.IsNull,
            "is_not_null" or "isnotnull" => ConditionOperator.IsNotNull,
            _ => ConditionOperator.Equals
        };
    }
}

internal record AxiomParseResult(
    IReadOnlyList<Axiom> Axioms,
    IReadOnlyList<AxiomLoadError> Errors);
```

---

## 6. Built-in Axioms Example

```yaml
# Resources/BuiltInAxioms/api-documentation.yaml
axiom_version: "1.0"
name: "API Documentation"
description: "Standard axioms for API documentation"

axioms:
  - id: "endpoint-must-have-method"
    name: "Endpoint Method Required"
    description: "Every API endpoint must specify exactly one HTTP method"
    target_type: Endpoint
    severity: error
    category: "API Documentation"
    tags: [api, endpoint, required]
    rules:
      - property: method
        constraint: required
        error_message: "Endpoint must specify an HTTP method"
      - property: method
        constraint: one_of
        values: [GET, POST, PUT, PATCH, DELETE, HEAD, OPTIONS]
        error_message: "Method must be a valid HTTP method"

  - id: "endpoint-must-have-path"
    name: "Endpoint Path Required"
    description: "Every endpoint must have a valid path"
    target_type: Endpoint
    severity: error
    rules:
      - property: path
        constraint: required
      - property: path
        constraint: pattern
        pattern: "^/.*"
        error_message: "Path must start with /"

  - id: "parameter-required-no-default"
    name: "Required Parameter Cannot Have Default"
    description: "A required parameter should not specify a default value"
    target_type: Parameter
    severity: warning
    rules:
      - constraint: not_both
        properties: [required, default_value]
        when:
          property: required
          operator: equals
          value: true
        error_message: "Required parameters should not have default values"

  - id: "response-status-range"
    name: "Valid HTTP Status Code"
    description: "Response status codes must be in valid range"
    target_type: Response
    severity: error
    rules:
      - property: status_code
        constraint: range
        min: 100
        max: 599
        error_message: "Status code must be between 100 and 599"
```

---

## 7. Unit Testing Requirements

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.6g")]
public class AxiomLoaderTests
{
    [Fact]
    public async Task LoadBuiltInAsync_LoadsDefaultAxioms()
    {
        // Arrange
        var loader = CreateLoader();

        // Act
        var result = await loader.LoadBuiltInAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Axioms.Should().NotBeEmpty();
        result.SourceType.Should().Be(AxiomSourceType.BuiltIn);
    }

    [Fact]
    public async Task ValidateFileAsync_WithValidYaml_ReturnsNoErrors()
    {
        // Arrange
        var loader = CreateLoader();
        var tempFile = CreateTempAxiomFile(ValidAxiomYaml);

        // Act
        var result = await loader.ValidateFileAsync(tempFile);

        // Assert
        result.IsValid.Should().BeTrue();
        result.AxiomCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ValidateFileAsync_WithInvalidYaml_ReturnsErrors()
    {
        // Arrange
        var loader = CreateLoader();
        var tempFile = CreateTempAxiomFile("invalid: yaml: content:");

        // Act
        var result = await loader.ValidateFileAsync(tempFile);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "YAML_SYNTAX_ERROR");
    }
}

[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.6g")]
public class AxiomYamlParserTests
{
    [Fact]
    public void Parse_ValidYaml_ReturnsAxioms()
    {
        // Arrange
        var parser = new AxiomYamlParser();
        var yaml = CreateValidAxiomYaml();

        // Act
        var result = parser.Parse(yaml, "test.yaml");

        // Assert
        result.Errors.Should().BeEmpty();
        result.Axioms.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_WithAllConstraintTypes_MapsCorrectly()
    {
        // Arrange
        var parser = new AxiomYamlParser();
        var yaml = CreateYamlWithAllConstraints();

        // Act
        var result = parser.Parse(yaml, "test.yaml");

        // Assert
        result.Axioms.SelectMany(a => a.Rules)
            .Select(r => r.Constraint)
            .Should().Contain(AxiomConstraintType.Required);
    }
}
```

---

## 8. Acceptance Criteria (QA)

| # | Criterion |
| :- | :-------- |
| 1 | Built-in axioms load without errors. |
| 2 | YAML syntax errors reported with line numbers. |
| 3 | Unknown target types flagged in validation. |
| 4 | File watcher triggers reload on changes. |
| 5 | Teams+ can load custom workspace axioms. |
| 6 | WriterPro limited to built-in axioms only. |

---

## 9. Deliverable Checklist

| # | Deliverable | Status |
| :- | :---------- | :----- |
| 1 | `IAxiomLoader` interface | [ ] |
| 2 | `AxiomLoader` implementation | [ ] |
| 3 | `AxiomYamlParser` | [ ] |
| 4 | `AxiomSchemaValidator` | [ ] |
| 5 | `AxiomFileWatcher` integration | [ ] |
| 6 | Built-in axiom YAML files | [ ] |
| 7 | Unit tests | [ ] |

---

## 10. Changelog Entry

```markdown
### Added (v0.4.6g)

- `IAxiomLoader` for loading axioms from files and resources
- `AxiomYamlParser` for YAML axiom parsing
- Built-in axioms for API documentation
- File watcher for automatic axiom reload
- Validation reporting for axiom files
```

---
