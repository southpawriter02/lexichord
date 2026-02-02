// =============================================================================
// File: AxiomLoader.cs
// Project: Lexichord.Modules.Knowledge
// Description: Loads axioms from YAML files and built-in resources.
// =============================================================================
// LOGIC: Main implementation of IAxiomLoader:
//   - Loads built-in axioms from embedded resources (WriterPro+ tier)
//   - Loads workspace axioms from .lexichord/knowledge/axioms/ (Teams+ tier)
//   - Validates axioms against schema registry
//   - Persists valid axioms via IAxiomRepository
//   - Watches workspace axiom files for changes with 500ms debounce
//
// v0.4.6g: Axiom Loader (CKVS Phase 1d)
// Dependencies: IAxiomRepository (v0.4.6f), ISchemaRegistry (v0.4.5f),
//               ILicenseContext (v0.0.4c), IWorkspaceService (v0.1.2a)
// =============================================================================

using System.Diagnostics;
using System.Reflection;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Axioms;

/// <summary>
/// Loads axioms from YAML files and built-in resources.
/// </summary>
/// <remarks>
/// <para>
/// The loader orchestrates axiom loading from two sources:
/// </para>
/// <para>
/// <b>Built-in Axioms (WriterPro+):</b> Embedded in the application assembly
/// as resources under <c>Lexichord.Modules.Knowledge.Resources.BuiltInAxioms</c>.
/// These provide default validation rules for technical documentation.
/// </para>
/// <para>
/// <b>Workspace Axioms (Teams+):</b> Custom axioms from the user's workspace
/// directory at <c>.lexichord/knowledge/axioms/</c>. These allow teams to
/// define project-specific validation rules.
/// </para>
/// <para>
/// The loader validates all axioms against the schema registry before
/// persisting them via <see cref="IAxiomRepository"/>.
/// </para>
/// </remarks>
public sealed class AxiomLoader : IAxiomLoader, IDisposable
{
    private readonly IAxiomRepository _repository;
    private readonly ISchemaRegistry _schemaRegistry;
    private readonly ILicenseContext _licenseContext;
    private readonly IWorkspaceService _workspaceService;
    private readonly ILogger<AxiomLoader> _logger;
    private readonly AxiomYamlParser _parser;
    private readonly AxiomSchemaValidator _validator;

    private FileSystemWatcher? _watcher;
    private CancellationTokenSource? _debounceCts;
    private readonly object _watcherLock = new();
    private bool _disposed;

    /// <summary>
    /// Debounce delay for file watcher (500ms as per specification).
    /// </summary>
    private const int DebounceDelayMs = 500;

    /// <inheritdoc/>
    public event EventHandler<AxiomLoadResult>? AxiomsReloaded;

    /// <summary>
    /// Initializes a new instance of the <see cref="AxiomLoader"/> class.
    /// </summary>
    /// <param name="repository">The axiom repository for persistence.</param>
    /// <param name="schemaRegistry">The schema registry for type validation.</param>
    /// <param name="licenseContext">The license context for tier checks.</param>
    /// <param name="workspaceService">The workspace service for path resolution.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public AxiomLoader(
        IAxiomRepository repository,
        ISchemaRegistry schemaRegistry,
        ILicenseContext licenseContext,
        IWorkspaceService workspaceService,
        ILogger<AxiomLoader> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _schemaRegistry = schemaRegistry ?? throw new ArgumentNullException(nameof(schemaRegistry));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _workspaceService = workspaceService ?? throw new ArgumentNullException(nameof(workspaceService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _parser = new AxiomYamlParser();
        _validator = new AxiomSchemaValidator(schemaRegistry);

        _logger.LogDebug("AxiomLoader initialized");
    }

    /// <inheritdoc/>
    public async Task<AxiomLoadResult> LoadAllAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Loading all axioms...");

        var allAxioms = new List<Axiom>();
        var allFiles = new List<string>();
        var allErrors = new List<AxiomLoadError>();

        // LOGIC: Load built-in axioms (WriterPro+ required).
        if (HasWriterProLicense())
        {
            var builtInResult = await LoadBuiltInAsync(ct).ConfigureAwait(false);
            allAxioms.AddRange(builtInResult.Axioms);
            allFiles.AddRange(builtInResult.FilesProcessed);
            allErrors.AddRange(builtInResult.Errors);
        }
        else
        {
            _logger.LogDebug("Skipping built-in axioms (WriterPro+ required)");
        }

        // LOGIC: Load workspace axioms (Teams+ required).
        if (HasTeamsLicense() && _workspaceService.IsWorkspaceOpen)
        {
            var workspacePath = _workspaceService.CurrentWorkspace?.RootPath;
            if (!string.IsNullOrEmpty(workspacePath))
            {
                var workspaceResult = await LoadWorkspaceAsync(workspacePath, ct).ConfigureAwait(false);
                allAxioms.AddRange(workspaceResult.Axioms);
                allFiles.AddRange(workspaceResult.FilesProcessed);
                allErrors.AddRange(workspaceResult.Errors);
            }
        }
        else
        {
            _logger.LogDebug("Skipping workspace axioms (Teams+ required or no workspace open)");
        }

        sw.Stop();
        _logger.LogInformation(
            "Loaded {AxiomCount} axioms from {FileCount} files in {Duration}ms with {ErrorCount} errors",
            allAxioms.Count, allFiles.Count, sw.ElapsedMilliseconds, allErrors.Count);

        return new AxiomLoadResult
        {
            Axioms = allAxioms,
            FilesProcessed = allFiles,
            Errors = allErrors,
            Duration = sw.Elapsed,
            SourceType = AxiomSourceType.BuiltIn // Primary source
        };
    }

    /// <inheritdoc/>
    public async Task<AxiomLoadResult> LoadBuiltInAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("Loading built-in axioms from embedded resources...");

        var axioms = new List<Axiom>();
        var files = new List<string>();
        var errors = new List<AxiomLoadError>();

        var assembly = typeof(AxiomLoader).Assembly;
        var resourcePrefix = "Lexichord.Modules.Knowledge.Resources.BuiltInAxioms.";

        // LOGIC: Find all embedded YAML resources.
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(resourcePrefix, StringComparison.Ordinal) &&
                       (n.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
                        n.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        _logger.LogDebug("Found {Count} built-in axiom resource(s)", resourceNames.Count);

        foreach (var resourceName in resourceNames)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    errors.Add(new AxiomLoadError
                    {
                        FilePath = resourceName,
                        Code = "BUILTIN_LOAD_ERROR",
                        Message = $"Failed to open resource stream for {resourceName}",
                        Severity = LoadErrorSeverity.Error
                    });
                    continue;
                }

                using var reader = new StreamReader(stream);
                var yaml = await reader.ReadToEndAsync(ct).ConfigureAwait(false);

                var sourcePath = $"builtin:{resourceName}";
                var parseResult = _parser.Parse(yaml, sourcePath);

                // LOGIC: Set source file for all parsed axioms.
                var axiomsWithSource = parseResult.Axioms
                    .Select(a => a with { SourceFile = sourcePath })
                    .ToList();

                // LOGIC: Validate against schema registry.
                var validationResult = _validator.Validate(axiomsWithSource);
                errors.AddRange(parseResult.Errors);
                errors.AddRange(validationResult.Errors);

                // LOGIC: Filter out invalid axioms.
                var validAxioms = axiomsWithSource
                    .Where(a => !validationResult.InvalidAxiomIds.Contains(a.Id))
                    .ToList();

                axioms.AddRange(validAxioms);
                files.Add(resourceName);

                _logger.LogDebug(
                    "Loaded {Count} axioms from {Resource}",
                    validAxioms.Count, resourceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load built-in axiom resource {Resource}", resourceName);
                errors.Add(new AxiomLoadError
                {
                    FilePath = resourceName,
                    Code = "BUILTIN_LOAD_ERROR",
                    Message = $"Failed to load resource: {ex.Message}",
                    Severity = LoadErrorSeverity.Error
                });
            }
        }

        // LOGIC: Persist valid built-in axioms to repository.
        if (axioms.Count > 0)
        {
            try
            {
                await _repository.SaveBatchAsync(axioms, ct).ConfigureAwait(false);
                _logger.LogDebug("Persisted {Count} built-in axioms to repository", axioms.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist built-in axioms");
                errors.Add(new AxiomLoadError
                {
                    Code = "PERSISTENCE_ERROR",
                    Message = $"Failed to persist axioms: {ex.Message}",
                    Severity = LoadErrorSeverity.Error
                });
            }
        }

        sw.Stop();
        return new AxiomLoadResult
        {
            Axioms = axioms,
            FilesProcessed = files,
            Errors = errors,
            Duration = sw.Elapsed,
            SourceType = AxiomSourceType.BuiltIn
        };
    }

    /// <inheritdoc/>
    public async Task<AxiomLoadResult> LoadWorkspaceAsync(string workspacePath, CancellationToken ct = default)
    {
        // LOGIC: Enforce Teams+ license for workspace axioms.
        if (!HasTeamsLicense())
        {
            _logger.LogWarning("Workspace axiom loading requires Teams+ license");
            throw new FeatureNotLicensedException(
                "Workspace axioms require Teams+ license",
                LicenseTier.Teams);
        }

        var sw = Stopwatch.StartNew();
        var axiomDir = Path.Combine(workspacePath, ".lexichord", "knowledge", "axioms");

        _logger.LogDebug("Loading workspace axioms from {Directory}", axiomDir);

        var axioms = new List<Axiom>();
        var files = new List<string>();
        var errors = new List<AxiomLoadError>();

        if (!Directory.Exists(axiomDir))
        {
            _logger.LogDebug("Axiom directory does not exist: {Directory}", axiomDir);
            sw.Stop();
            return new AxiomLoadResult
            {
                Axioms = axioms,
                FilesProcessed = files,
                Errors = errors,
                Duration = sw.Elapsed,
                SourceType = AxiomSourceType.Workspace
            };
        }

        // LOGIC: Scan for all YAML files recursively.
        var yamlFiles = Directory.EnumerateFiles(axiomDir, "*.yaml", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(axiomDir, "*.yml", SearchOption.AllDirectories))
            .ToList();

        _logger.LogDebug("Found {Count} axiom file(s) in workspace", yamlFiles.Count);

        foreach (var filePath in yamlFiles)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var yaml = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
                var parseResult = _parser.Parse(yaml, filePath);

                // LOGIC: Set source file for all parsed axioms.
                var axiomsWithSource = parseResult.Axioms
                    .Select(a => a with { SourceFile = filePath })
                    .ToList();

                // LOGIC: Validate against schema registry.
                var validationResult = _validator.Validate(axiomsWithSource);
                errors.AddRange(parseResult.Errors);
                errors.AddRange(validationResult.Errors);

                // LOGIC: Filter out invalid axioms.
                var validAxioms = axiomsWithSource
                    .Where(a => !validationResult.InvalidAxiomIds.Contains(a.Id))
                    .ToList();

                axioms.AddRange(validAxioms);
                files.Add(filePath);

                _logger.LogDebug("Loaded {Count} axioms from {File}", validAxioms.Count, filePath);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Failed to read axiom file {File}", filePath);
                errors.Add(new AxiomLoadError
                {
                    FilePath = filePath,
                    Code = "FILE_LOAD_ERROR",
                    Message = $"Failed to read file: {ex.Message}",
                    Severity = LoadErrorSeverity.Error
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load axiom file {File}", filePath);
                errors.Add(new AxiomLoadError
                {
                    FilePath = filePath,
                    Code = "FILE_LOAD_ERROR",
                    Message = $"Failed to load file: {ex.Message}",
                    Severity = LoadErrorSeverity.Error
                });
            }
        }

        // LOGIC: Persist valid workspace axioms to repository.
        if (axioms.Count > 0)
        {
            try
            {
                await _repository.SaveBatchAsync(axioms, ct).ConfigureAwait(false);
                _logger.LogDebug("Persisted {Count} workspace axioms to repository", axioms.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist workspace axioms");
                errors.Add(new AxiomLoadError
                {
                    Code = "PERSISTENCE_ERROR",
                    Message = $"Failed to persist axioms: {ex.Message}",
                    Severity = LoadErrorSeverity.Error
                });
            }
        }

        sw.Stop();
        _logger.LogInformation(
            "Loaded {AxiomCount} axioms from {FileCount} workspace files in {Duration}ms",
            axioms.Count, files.Count, sw.ElapsedMilliseconds);

        return new AxiomLoadResult
        {
            Axioms = axioms,
            FilesProcessed = files,
            Errors = errors,
            Duration = sw.Elapsed,
            SourceType = AxiomSourceType.Workspace
        };
    }

    /// <inheritdoc/>
    public async Task<AxiomLoadResult> LoadFileAsync(string filePath, CancellationToken ct = default)
    {
        // LOGIC: Enforce Teams+ license for custom file loading.
        if (!HasTeamsLicense())
        {
            _logger.LogWarning("Custom axiom file loading requires Teams+ license");
            throw new FeatureNotLicensedException(
                "Custom axioms require Teams+ license",
                LicenseTier.Teams);
        }

        var sw = Stopwatch.StartNew();
        _logger.LogDebug("Loading axiom file: {File}", filePath);

        var errors = new List<AxiomLoadError>();

        if (!File.Exists(filePath))
        {
            errors.Add(new AxiomLoadError
            {
                FilePath = filePath,
                Code = "FILE_NOT_FOUND",
                Message = $"File not found: {filePath}",
                Severity = LoadErrorSeverity.Error
            });

            sw.Stop();
            return new AxiomLoadResult
            {
                Axioms = Array.Empty<Axiom>(),
                FilesProcessed = new[] { filePath },
                Errors = errors,
                Duration = sw.Elapsed,
                SourceType = AxiomSourceType.File
            };
        }

        try
        {
            // LOGIC: Delete existing axioms from this source before reloading.
            await _repository.DeleteBySourceAsync(filePath, ct).ConfigureAwait(false);

            var yaml = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
            var parseResult = _parser.Parse(yaml, filePath);

            // LOGIC: Set source file for all parsed axioms.
            var axiomsWithSource = parseResult.Axioms
                .Select(a => a with { SourceFile = filePath })
                .ToList();

            // LOGIC: Validate against schema registry.
            var validationResult = _validator.Validate(axiomsWithSource);
            errors.AddRange(parseResult.Errors);
            errors.AddRange(validationResult.Errors);

            // LOGIC: Filter out invalid axioms.
            var validAxioms = axiomsWithSource
                .Where(a => !validationResult.InvalidAxiomIds.Contains(a.Id))
                .ToList();

            // LOGIC: Persist valid axioms.
            if (validAxioms.Count > 0)
            {
                await _repository.SaveBatchAsync(validAxioms, ct).ConfigureAwait(false);
            }

            sw.Stop();
            _logger.LogInformation(
                "Loaded {Count} axioms from {File} in {Duration}ms",
                validAxioms.Count, filePath, sw.ElapsedMilliseconds);

            return new AxiomLoadResult
            {
                Axioms = validAxioms,
                FilesProcessed = new[] { filePath },
                Errors = errors,
                Duration = sw.Elapsed,
                SourceType = AxiomSourceType.File
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load axiom file {File}", filePath);
            errors.Add(new AxiomLoadError
            {
                FilePath = filePath,
                Code = "FILE_LOAD_ERROR",
                Message = $"Failed to load file: {ex.Message}",
                Severity = LoadErrorSeverity.Error
            });

            sw.Stop();
            return new AxiomLoadResult
            {
                Axioms = Array.Empty<Axiom>(),
                FilesProcessed = new[] { filePath },
                Errors = errors,
                Duration = sw.Elapsed,
                SourceType = AxiomSourceType.File
            };
        }
    }

    /// <inheritdoc/>
    public async Task<AxiomValidationReport> ValidateFileAsync(string filePath, CancellationToken ct = default)
    {
        _logger.LogDebug("Validating axiom file: {File}", filePath);

        var errors = new List<AxiomLoadError>();

        if (!File.Exists(filePath))
        {
            errors.Add(new AxiomLoadError
            {
                FilePath = filePath,
                Code = "FILE_NOT_FOUND",
                Message = $"File not found: {filePath}",
                Severity = LoadErrorSeverity.Error
            });

            return new AxiomValidationReport
            {
                FilePath = filePath,
                Errors = errors,
                AxiomCount = 0
            };
        }

        try
        {
            var yaml = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
            var parseResult = _parser.Parse(yaml, filePath);

            errors.AddRange(parseResult.Errors);

            // LOGIC: Validate against schema registry.
            var validationResult = _validator.Validate(parseResult.Axioms);
            errors.AddRange(validationResult.Errors);

            return new AxiomValidationReport
            {
                FilePath = filePath,
                Errors = errors,
                AxiomCount = parseResult.Axioms.Count,
                UnknownTargetTypes = validationResult.UnknownTargetTypes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate axiom file {File}", filePath);
            errors.Add(new AxiomLoadError
            {
                FilePath = filePath,
                Code = "VALIDATION_ERROR",
                Message = $"Validation failed: {ex.Message}",
                Severity = LoadErrorSeverity.Error
            });

            return new AxiomValidationReport
            {
                FilePath = filePath,
                Errors = errors,
                AxiomCount = 0
            };
        }
    }

    /// <inheritdoc/>
    public void StartWatching(string workspacePath)
    {
        // LOGIC: File watching requires Teams+ license.
        if (!HasTeamsLicense())
        {
            _logger.LogDebug("Skipping file watcher (Teams+ required)");
            return;
        }

        lock (_watcherLock)
        {
            StopWatchingInternal();

            var axiomDir = Path.Combine(workspacePath, ".lexichord", "knowledge", "axioms");
            if (!Directory.Exists(axiomDir))
            {
                _logger.LogDebug("Axiom directory does not exist, not watching: {Directory}", axiomDir);
                return;
            }

            _logger.LogInformation("Starting file watcher for {Directory}", axiomDir);

            _watcher = new FileSystemWatcher(axiomDir)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
            };

            // LOGIC: Watch for YAML file changes.
            _watcher.Changed += OnAxiomFileChanged;
            _watcher.Created += OnAxiomFileChanged;
            _watcher.Deleted += OnAxiomFileDeleted;
            _watcher.Renamed += OnAxiomFileRenamed;
            _watcher.Error += OnWatcherError;
        }
    }

    /// <inheritdoc/>
    public void StopWatching()
    {
        lock (_watcherLock)
        {
            StopWatchingInternal();
        }
    }

    /// <summary>
    /// Stops the file watcher (internal, assumes lock is held).
    /// </summary>
    private void StopWatchingInternal()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = null;

        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnAxiomFileChanged;
            _watcher.Created -= OnAxiomFileChanged;
            _watcher.Deleted -= OnAxiomFileDeleted;
            _watcher.Renamed -= OnAxiomFileRenamed;
            _watcher.Error -= OnWatcherError;
            _watcher.Dispose();
            _watcher = null;

            _logger.LogDebug("File watcher stopped");
        }
    }

    /// <summary>
    /// Handles axiom file change/create events with debouncing.
    /// </summary>
    private async void OnAxiomFileChanged(object sender, FileSystemEventArgs e)
    {
        if (!IsYamlFile(e.FullPath))
            return;

        _logger.LogDebug("Axiom file changed: {File}", e.FullPath);

        // LOGIC: Cancel any pending debounced reload.
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var ct = _debounceCts.Token;

        try
        {
            // LOGIC: Debounce with 500ms delay per specification.
            await Task.Delay(DebounceDelayMs, ct).ConfigureAwait(false);

            var result = await LoadFileAsync(e.FullPath, ct).ConfigureAwait(false);
            AxiomsReloaded?.Invoke(this, result);
        }
        catch (OperationCanceledException)
        {
            // Debounce cancelled by newer change
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading axiom file {File}", e.FullPath);
        }
    }

    /// <summary>
    /// Handles axiom file deletion events.
    /// </summary>
    private async void OnAxiomFileDeleted(object sender, FileSystemEventArgs e)
    {
        if (!IsYamlFile(e.FullPath))
            return;

        _logger.LogDebug("Axiom file deleted: {File}", e.FullPath);

        try
        {
            // LOGIC: Remove axioms from the deleted file.
            var deletedCount = await _repository.DeleteBySourceAsync(e.FullPath).ConfigureAwait(false);

            _logger.LogInformation("Removed {Count} axioms from deleted file {File}",
                deletedCount, e.FullPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling deleted axiom file {File}", e.FullPath);
        }
    }

    /// <summary>
    /// Handles axiom file rename events.
    /// </summary>
    private async void OnAxiomFileRenamed(object sender, RenamedEventArgs e)
    {
        _logger.LogDebug("Axiom file renamed: {OldPath} -> {NewPath}", e.OldFullPath, e.FullPath);

        try
        {
            // LOGIC: Handle as delete of old file + load of new file.
            if (IsYamlFile(e.OldFullPath))
            {
                await _repository.DeleteBySourceAsync(e.OldFullPath).ConfigureAwait(false);
            }

            if (IsYamlFile(e.FullPath))
            {
                var result = await LoadFileAsync(e.FullPath).ConfigureAwait(false);
                AxiomsReloaded?.Invoke(this, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling renamed axiom file {OldPath} -> {NewPath}",
                e.OldFullPath, e.FullPath);
        }
    }

    /// <summary>
    /// Handles file watcher errors.
    /// </summary>
    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        _logger.LogError(e.GetException(), "File watcher error");
    }

    /// <summary>
    /// Checks if a file path is a YAML file.
    /// </summary>
    private static bool IsYamlFile(string path)
    {
        return path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the current license is WriterPro+.
    /// </summary>
    private bool HasWriterProLicense()
    {
        return _licenseContext.GetCurrentTier() >= LicenseTier.WriterPro;
    }

    /// <summary>
    /// Checks if the current license is Teams+.
    /// </summary>
    private bool HasTeamsLicense()
    {
        return _licenseContext.GetCurrentTier() >= LicenseTier.Teams;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        StopWatching();

        _logger.LogDebug("AxiomLoader disposed");
    }
}
