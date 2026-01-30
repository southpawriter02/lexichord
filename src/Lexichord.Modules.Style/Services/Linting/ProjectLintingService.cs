using System.Collections.Concurrent;
using System.Diagnostics;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Linting;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services.Linting;

/// <summary>
/// Implements project-wide and multi-document linting with caching.
/// </summary>
/// <remarks>
/// LOGIC: ProjectLintingService orchestrates background linting for:
/// - Open Files scope: Scans all documents from IEditorService
/// - Project scope: Enumerates files matching extensions, respects .lexichordignore
///
/// Caching Strategy:
/// - Thread-safe ConcurrentDictionary stores (violations, lastModified)
/// - Cache hit if file hasn't been modified since last scan
/// - LRU eviction when cache exceeds MaxCacheSize
/// - InvalidateFile() called on document save
///
/// Threading:
/// - All public methods are thread-safe
/// - LintProjectAsync runs on thread pool via Task.Run
/// - Progress events may fire from any thread
///
/// Version: v0.2.6d
/// </remarks>
public class ProjectLintingService : IProjectLintingService
{
    /// <summary>
    /// Maximum number of files to cache violations for.
    /// </summary>
    internal const int MaxCacheSize = 500;

    private readonly IStyleEngine _styleEngine;
    private readonly IEditorService _editorService;
    private readonly ILintingConfiguration _configuration;
    private readonly IFileSystemAccess _fileSystem;
    private readonly ILogger<ProjectLintingService> _logger;

    private readonly ConcurrentDictionary<string, CachedViolations> _cache = new();

    /// <inheritdoc/>
    public event EventHandler<ProjectLintProgress>? ProgressChanged;

    /// <summary>
    /// Initializes a new instance of ProjectLintingService.
    /// </summary>
    public ProjectLintingService(
        IStyleEngine styleEngine,
        IEditorService editorService,
        ILintingConfiguration configuration,
        IFileSystemAccess fileSystem,
        ILogger<ProjectLintingService> logger)
    {
        _styleEngine = styleEngine ?? throw new ArgumentNullException(nameof(styleEngine));
        _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<MultiLintResult> LintOpenDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var openDocs = _editorService.GetOpenDocuments();
        var results = new Dictionary<string, IReadOnlyList<StyleViolation>>();
        var totalProblems = 0;

        _logger.LogInformation("Linting {Count} open documents", openDocs.Count);

        foreach (var doc in openDocs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var violations = await _styleEngine.AnalyzeAsync(
                    doc.Content,
                    cancellationToken);

                results[doc.DocumentId] = violations;
                totalProblems += violations.Count;

                _logger.LogDebug(
                    "Linted document {DocumentId}: {Count} violations",
                    doc.DocumentId,
                    violations.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Error linting document {DocumentId}", doc.DocumentId);
                results[doc.DocumentId] = Array.Empty<StyleViolation>();
            }
        }

        stopwatch.Stop();
        _logger.LogInformation(
            "Open documents lint completed: {ProblemCount} problems in {Duration}ms",
            totalProblems,
            stopwatch.ElapsedMilliseconds);

        return new MultiLintResult(
            results,
            openDocs.Count,
            totalProblems,
            stopwatch.Elapsed);
    }

    /// <inheritdoc/>
    public async Task<ProjectLintResult> LintProjectAsync(
        string projectPath,
        IProgress<ProjectLintProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(projectPath);

        // Run on background thread to avoid blocking UI
        return await Task.Run(async () =>
        {
            var stopwatch = Stopwatch.StartNew();
            var results = new Dictionary<string, IReadOnlyList<StyleViolation>>();
            var errors = new List<string>();
            var scannedCount = 0;
            var totalProblems = 0;
            var wasCancelled = false;

            try
            {
                // Enumerate files
                var files = await EnumerateProjectFilesAsync(projectPath, cancellationToken);
                var totalFiles = files.Count;

                _logger.LogInformation(
                    "Linting project {Path}: {Count} files found",
                    projectPath,
                    totalFiles);

                for (var i = 0; i < files.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var filePath = files[i];
                    var progressInfo = new ProjectLintProgress(i + 1, totalFiles, filePath);

                    progress?.Report(progressInfo);
                    ProgressChanged?.Invoke(this, progressInfo);

                    try
                    {
                        var violations = await GetOrScanFileAsync(filePath, cancellationToken);
                        results[filePath] = violations;
                        totalProblems += violations.Count;
                        scannedCount++;

                        _logger.LogDebug(
                            "Linted {Index}/{Total}: {FilePath} ({Count} violations)",
                            i + 1,
                            totalFiles,
                            filePath,
                            violations.Count);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        var errorMsg = $"{filePath}: {ex.Message}";
                        errors.Add(errorMsg);
                        results[filePath] = Array.Empty<StyleViolation>();
                        _logger.LogWarning(ex, "Error scanning file {FilePath}", filePath);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                wasCancelled = true;
                _logger.LogInformation("Project linting cancelled");
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "Project lint {Status}: {Scanned}/{Total} files, {Problems} problems in {Duration}ms",
                wasCancelled ? "cancelled" : "completed",
                scannedCount,
                results.Count,
                totalProblems,
                stopwatch.ElapsedMilliseconds);

            return new ProjectLintResult(
                results,
                results.Count,
                scannedCount,
                totalProblems,
                stopwatch.Elapsed,
                wasCancelled,
                errors);
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public void InvalidateFile(string filePath)
    {
        if (_cache.TryRemove(filePath, out _))
        {
            _logger.LogDebug("Invalidated cache for {FilePath}", filePath);
        }
    }

    /// <inheritdoc/>
    public void InvalidateAll()
    {
        var count = _cache.Count;
        _cache.Clear();
        _logger.LogInformation("Invalidated all cached violations ({Count} entries)", count);
    }

    /// <summary>
    /// Gets violations from cache or scans the file.
    /// </summary>
    private async Task<IReadOnlyList<StyleViolation>> GetOrScanFileAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        DateTime lastModified;
        try
        {
            lastModified = File.GetLastWriteTimeUtc(filePath);
        }
        catch
        {
            lastModified = DateTime.MinValue;
        }

        // Check cache
        if (_cache.TryGetValue(filePath, out var cached) &&
            cached.LastModified >= lastModified)
        {
            _logger.LogDebug("Cache hit for {FilePath}", filePath);
            return cached.Violations;
        }

        // Read and scan file
        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        var violations = await _styleEngine.AnalyzeAsync(content, cancellationToken);

        // Update cache
        var entry = new CachedViolations(violations, lastModified);
        _cache.AddOrUpdate(filePath, entry, (_, _) => entry);

        // Enforce cache size limit
        EnforceCacheSize();

        return violations;
    }

    /// <summary>
    /// Enumerates project files respecting .lexichordignore.
    /// </summary>
    private async Task<List<string>> EnumerateProjectFilesAsync(
        string projectPath,
        CancellationToken cancellationToken)
    {
        var matcher = await LoadIgnoreMatcherAsync(projectPath);
        var files = new List<string>();
        var targetExtensions = _configuration.TargetExtensions;

        await EnumerateDirectoryAsync(
            projectPath,
            projectPath,
            matcher,
            targetExtensions,
            files,
            cancellationToken);

        return files;
    }

    /// <summary>
    /// Recursively enumerates a directory for matching files.
    /// </summary>
    private async Task EnumerateDirectoryAsync(
        string rootPath,
        string currentPath,
        IgnorePatternMatcher matcher,
        IReadOnlyList<string> extensions,
        List<string> results,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entries = await _fileSystem.GetDirectoryContentsAsync(currentPath);

        foreach (var entry in entries)
        {
            var relativePath = Path.GetRelativePath(rootPath, entry.FullPath)
                .Replace('\\', '/');

            if (matcher.IsIgnored(relativePath))
            {
                _logger.LogDebug("Ignoring {Path}", relativePath);
                continue;
            }

            if (entry.IsDirectory)
            {
                await EnumerateDirectoryAsync(
                    rootPath,
                    entry.FullPath,
                    matcher,
                    extensions,
                    results,
                    cancellationToken);
            }
            else
            {
                var ext = Path.GetExtension(entry.Name);
                if (extensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                {
                    results.Add(entry.FullPath);
                }
            }
        }
    }

    /// <summary>
    /// Loads .lexichordignore patterns from project root.
    /// </summary>
    private async Task<IgnorePatternMatcher> LoadIgnoreMatcherAsync(string projectPath)
    {
        var ignorePath = Path.Combine(projectPath, ".lexichordignore");

        if (!File.Exists(ignorePath))
        {
            return new IgnorePatternMatcher();
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(ignorePath);
            _logger.LogDebug("Loaded .lexichordignore with {Count} lines", lines.Length);
            return new IgnorePatternMatcher(lines);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading .lexichordignore");
            return new IgnorePatternMatcher();
        }
    }

    /// <summary>
    /// Removes oldest entries when cache exceeds max size.
    /// </summary>
    private void EnforceCacheSize()
    {
        if (_cache.Count <= MaxCacheSize)
        {
            return;
        }

        // Remove oldest entries (LRU by last modified time)
        var toRemove = _cache
            .OrderBy(kvp => kvp.Value.LastModified)
            .Take(_cache.Count - MaxCacheSize)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in toRemove)
        {
            _cache.TryRemove(key, out _);
        }

        _logger.LogDebug("Evicted {Count} cache entries", toRemove.Count);
    }

    /// <summary>
    /// Caches violations for a specific file (for testing).
    /// </summary>
    internal void CacheViolations(string filePath, IReadOnlyList<StyleViolation> violations)
    {
        _cache[filePath] = new CachedViolations(violations, DateTime.UtcNow);
    }

    /// <summary>
    /// Gets the current cache count (for testing).
    /// </summary>
    internal int CacheCount => _cache.Count;

    /// <summary>
    /// Cached violation data with timestamp.
    /// </summary>
    private sealed record CachedViolations(
        IReadOnlyList<StyleViolation> Violations,
        DateTime LastModified);
}
