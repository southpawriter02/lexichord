using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Service for managing file ignore patterns from .lexichordignore files.
/// </summary>
/// <remarks>
/// LOGIC: IgnorePatternService provides glob-based file exclusion from style analysis.
///
/// Implementation Details:
/// - Uses Microsoft.Extensions.FileSystemGlobbing for pattern matching
/// - ReaderWriterLockSlim for thread-safe pattern access
/// - FileSystemWatcher for hot-reload (debounced at 100ms)
/// - License gating: Core=5 patterns, Writer Pro=unlimited
///
/// File Format:
/// - Located at {workspace}/.lexichord/.lexichordignore
/// - Standard glob patterns: *.log, build/, **/temp/*
/// - Negation patterns: !important.log
/// - Comments: Lines starting with # are ignored
/// - Empty lines are ignored
///
/// Thread Safety:
/// - IsIgnored: Read lock (concurrent reads allowed)
/// - Pattern loading: Write lock (exclusive access)
///
/// Performance:
/// - O(n) where n is pattern count
/// - Target: &lt;10ms for 100 path checks
///
/// Version: v0.3.6d
/// </remarks>
public sealed class IgnorePatternService : IIgnorePatternService
{
    #region Constants

    /// <summary>
    /// Relative path to the ignore file within the workspace.
    /// </summary>
    private const string IgnoreFilePath = ".lexichord/.lexichordignore";

    /// <summary>
    /// Maximum allowed file size for .lexichordignore (100KB).
    /// </summary>
    private const int MaxIgnoreFileSize = 100 * 1024;

    /// <summary>
    /// Maximum patterns for Core tier license.
    /// </summary>
    private const int CoreTierMaxPatterns = 5;

    /// <summary>
    /// Debounce delay for file watcher in milliseconds.
    /// </summary>
    private const int FileWatcherDebounceMs = 100;

    #endregion

    #region Fields

    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<IgnorePatternService> _logger;
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);
    private readonly List<IgnorePattern> _patterns = new();

    private FileSystemWatcher? _fileWatcher;
    private CancellationTokenSource? _debounceTokenSource;
    private string? _workspacePath;
    private bool _disposed;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of <see cref="IgnorePatternService"/>.
    /// </summary>
    /// <param name="licenseContext">License context for tier checks.</param>
    /// <param name="logger">Logger instance.</param>
    /// <remarks>
    /// LOGIC: Dependencies injected via DI.
    /// - ILicenseContext: Required for enforcing pattern limits
    /// - ILogger: Required for operational logging
    /// </remarks>
    public IgnorePatternService(
        ILicenseContext licenseContext,
        ILogger<IgnorePatternService> logger)
    {
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("IgnorePatternService initialized");
    }

    #endregion

    #region Properties

    /// <inheritdoc />
    public int PatternCount
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _patterns.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    /// <inheritdoc />
    public int TruncatedPatternCount { get; private set; }

    /// <inheritdoc />
    public bool IsWatching => _fileWatcher?.EnableRaisingEvents == true;

    #endregion

    #region Events

    /// <inheritdoc />
    public event EventHandler<PatternsReloadedEventArgs>? PatternsReloaded;

    #endregion

    #region Public Methods

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Pattern matching algorithm:
    /// 1. Return false for null/empty paths (never ignored)
    /// 2. Normalize path separators to forward slashes
    /// 3. Convert absolute paths to workspace-relative
    /// 4. Apply patterns in order; last match wins
    /// 5. Negation patterns (prefix !) un-ignore files
    /// </remarks>
    public bool IsIgnored(string? filePath)
    {
        // LOGIC: Never ignore null or empty paths
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.LogTrace("IsIgnored called with null/empty path, returning false");
            return false;
        }

        _lock.EnterReadLock();
        try
        {
            // LOGIC: No patterns = nothing ignored
            if (_patterns.Count == 0)
            {
                return false;
            }

            // LOGIC: Normalize path separators
            var normalizedPath = NormalizePath(filePath);

            // LOGIC: Convert to workspace-relative if absolute
            if (_workspacePath != null && Path.IsPathRooted(normalizedPath))
            {
                var workspaceNormalized = NormalizePath(_workspacePath);
                if (normalizedPath.StartsWith(workspaceNormalized, StringComparison.OrdinalIgnoreCase))
                {
                    normalizedPath = normalizedPath[(workspaceNormalized.Length + 1)..];
                }
            }

            // LOGIC: Apply patterns in order; last match wins
            var isIgnored = false;
            foreach (var pattern in _patterns)
            {
                if (pattern.Matches(normalizedPath))
                {
                    isIgnored = !pattern.IsNegation;
                }
            }

            if (isIgnored)
            {
                _logger.LogTrace("File is ignored: {FilePath}", filePath);
            }

            return isIgnored;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Pattern registration flow:
    /// 1. Clear existing patterns
    /// 2. Parse and validate new patterns
    /// 3. Apply license tier limits
    /// 4. Store patterns and notify subscribers
    /// </remarks>
    public void RegisterPatterns(IEnumerable<string> patterns)
    {
        ArgumentNullException.ThrowIfNull(patterns);

        var patternList = patterns.ToList();

        _logger.LogDebug("Registering {Count} patterns manually", patternList.Count);

        _lock.EnterWriteLock();
        try
        {
            _patterns.Clear();
            TruncatedPatternCount = 0;

            var parsedPatterns = ParsePatterns(patternList);
            var (limited, truncated) = ApplyLicenseLimit(parsedPatterns);

            _patterns.AddRange(limited);
            TruncatedPatternCount = truncated;

            _logger.LogInformation(
                "Patterns registered: {Active} active, {Truncated} truncated",
                _patterns.Count,
                TruncatedPatternCount);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        // LOGIC: Notify subscribers (outside lock)
        RaisePatternsReloaded(null);
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Workspace loading flow:
    /// 1. Build full path to .lexichordignore
    /// 2. Check if file exists
    /// 3. Validate file size (max 100KB)
    /// 4. Parse patterns from file
    /// 5. Apply license limits
    /// 6. Start file watcher for hot-reload
    /// </remarks>
    public async Task<bool> LoadFromWorkspaceAsync(
        string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);

        _workspacePath = workspacePath;
        var ignoreFilePath = Path.Combine(workspacePath, IgnoreFilePath);

        _logger.LogDebug("Loading ignore patterns from: {Path}", ignoreFilePath);

        // LOGIC: Check if file exists
        if (!File.Exists(ignoreFilePath))
        {
            _logger.LogDebug("No .lexichordignore file found at: {Path}", ignoreFilePath);
            Clear();
            return false;
        }

        // LOGIC: Validate file size
        var fileInfo = new FileInfo(ignoreFilePath);
        if (fileInfo.Length > MaxIgnoreFileSize)
        {
            _logger.LogWarning(
                "Ignore file too large: {Path} ({Size} bytes, max {Max} bytes)",
                ignoreFilePath,
                fileInfo.Length,
                MaxIgnoreFileSize);
            Clear();
            return false;
        }

        try
        {
            // LOGIC: Read and parse file
            var lines = await File.ReadAllLinesAsync(ignoreFilePath, cancellationToken);

            _lock.EnterWriteLock();
            try
            {
                _patterns.Clear();
                TruncatedPatternCount = 0;

                var parsedPatterns = ParsePatterns(lines);
                var (limited, truncated) = ApplyLicenseLimit(parsedPatterns);

                _patterns.AddRange(limited);
                TruncatedPatternCount = truncated;

                _logger.LogInformation(
                    "Loaded {Active} patterns from {Path} ({Truncated} truncated)",
                    _patterns.Count,
                    ignoreFilePath,
                    TruncatedPatternCount);
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            // LOGIC: Start file watcher
            StartFileWatcher(workspacePath);

            // LOGIC: Notify subscribers
            RaisePatternsReloaded(ignoreFilePath);

            return true;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Failed to read ignore file: {Path}", ignoreFilePath);
            Clear();
            return false;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Clear flow:
    /// 1. Stop file watcher
    /// 2. Clear all patterns
    /// 3. Notify subscribers with count 0
    /// </remarks>
    public void Clear()
    {
        _logger.LogDebug("Clearing all ignore patterns");

        StopFileWatcher();

        _lock.EnterWriteLock();
        try
        {
            _patterns.Clear();
            TruncatedPatternCount = 0;
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        RaisePatternsReloaded(null);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Parses pattern strings into IgnorePattern objects.
    /// </summary>
    /// <param name="lines">Raw pattern lines from file or manual registration.</param>
    /// <returns>List of parsed patterns.</returns>
    /// <remarks>
    /// LOGIC: Pattern parsing rules:
    /// - Skip empty lines
    /// - Skip comments (lines starting with #)
    /// - Trim whitespace
    /// - Detect negation prefix (!)
    /// </remarks>
    private List<IgnorePattern> ParsePatterns(IEnumerable<string> lines)
    {
        var patterns = new List<IgnorePattern>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // LOGIC: Skip empty lines and comments
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            // LOGIC: Detect negation prefix
            var isNegation = trimmed.StartsWith('!');
            var pattern = isNegation ? trimmed[1..] : trimmed;

            if (string.IsNullOrWhiteSpace(pattern))
            {
                continue;
            }

            try
            {
                patterns.Add(new IgnorePattern(pattern, isNegation));
                _logger.LogTrace("Parsed pattern: {Pattern} (negation: {IsNegation})", pattern, isNegation);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid pattern ignored: {Pattern}", pattern);
            }
        }

        return patterns;
    }

    /// <summary>
    /// Applies license tier limits to patterns.
    /// </summary>
    /// <param name="patterns">All parsed patterns.</param>
    /// <returns>Tuple of (limited patterns, truncated count).</returns>
    /// <remarks>
    /// LOGIC: License enforcement:
    /// - Core tier: Maximum 5 patterns
    /// - Writer Pro: Unlimited patterns
    /// </remarks>
    private (List<IgnorePattern> Limited, int Truncated) ApplyLicenseLimit(List<IgnorePattern> patterns)
    {
        var isWriterPro = _licenseContext.GetCurrentTier() >= LicenseTier.WriterPro;

        if (isWriterPro)
        {
            _logger.LogTrace("Writer Pro tier: all {Count} patterns allowed", patterns.Count);
            return (patterns, 0);
        }

        // LOGIC: Core tier limit
        if (patterns.Count <= CoreTierMaxPatterns)
        {
            return (patterns, 0);
        }

        var limited = patterns.Take(CoreTierMaxPatterns).ToList();
        var truncated = patterns.Count - CoreTierMaxPatterns;

        _logger.LogInformation(
            "Core tier: truncated {Truncated} patterns (max {Max})",
            truncated,
            CoreTierMaxPatterns);

        return (limited, truncated);
    }

    /// <summary>
    /// Normalizes path separators to forward slashes.
    /// </summary>
    /// <param name="path">Path to normalize.</param>
    /// <returns>Normalized path with forward slashes.</returns>
    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimEnd('/');
    }

    /// <summary>
    /// Starts the file watcher for hot-reload.
    /// </summary>
    /// <param name="workspacePath">Workspace root path.</param>
    /// <remarks>
    /// LOGIC: File watcher setup:
    /// - Watches the .lexichord directory
    /// - Filters for .lexichordignore file
    /// - Debounces at 100ms to prevent rapid reloads
    /// </remarks>
    private void StartFileWatcher(string workspacePath)
    {
        StopFileWatcher();

        var watchDir = Path.Combine(workspacePath, ".lexichord");

        // LOGIC: Ensure directory exists for watcher
        if (!Directory.Exists(watchDir))
        {
            _logger.LogDebug("Watch directory does not exist: {Path}", watchDir);
            return;
        }

        try
        {
            _fileWatcher = new FileSystemWatcher(watchDir, ".lexichordignore")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            _fileWatcher.Changed += OnIgnoreFileChanged;
            _fileWatcher.Created += OnIgnoreFileChanged;
            _fileWatcher.Deleted += OnIgnoreFileChanged;
            _fileWatcher.Renamed += OnIgnoreFileRenamed;

            _logger.LogDebug("File watcher started for: {Path}", watchDir);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start file watcher for: {Path}", watchDir);
        }
    }

    /// <summary>
    /// Stops the file watcher.
    /// </summary>
    private void StopFileWatcher()
    {
        if (_fileWatcher == null) return;

        _fileWatcher.EnableRaisingEvents = false;
        _fileWatcher.Changed -= OnIgnoreFileChanged;
        _fileWatcher.Created -= OnIgnoreFileChanged;
        _fileWatcher.Deleted -= OnIgnoreFileChanged;
        _fileWatcher.Renamed -= OnIgnoreFileRenamed;
        _fileWatcher.Dispose();
        _fileWatcher = null;

        _debounceTokenSource?.Cancel();
        _debounceTokenSource?.Dispose();
        _debounceTokenSource = null;

        _logger.LogDebug("File watcher stopped");
    }

    /// <summary>
    /// Handles ignore file changes with debouncing.
    /// </summary>
    private void OnIgnoreFileChanged(object sender, FileSystemEventArgs e)
    {
        _logger.LogDebug("Ignore file changed: {ChangeType}", e.ChangeType);
        ScheduleReload();
    }

    /// <summary>
    /// Handles ignore file rename events.
    /// </summary>
    private void OnIgnoreFileRenamed(object sender, RenamedEventArgs e)
    {
        _logger.LogDebug("Ignore file renamed: {OldPath} -> {NewPath}", e.OldFullPath, e.FullPath);
        ScheduleReload();
    }

    /// <summary>
    /// Schedules a debounced reload of patterns.
    /// </summary>
    /// <remarks>
    /// LOGIC: Debounce prevents rapid reloads during save operations.
    /// - Cancel any pending reload
    /// - Wait 100ms for additional changes
    /// - Execute reload after debounce period
    /// </remarks>
    private void ScheduleReload()
    {
        _debounceTokenSource?.Cancel();
        _debounceTokenSource?.Dispose();
        _debounceTokenSource = new CancellationTokenSource();

        var token = _debounceTokenSource.Token;

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(FileWatcherDebounceMs, token);

                if (_workspacePath != null)
                {
                    await LoadFromWorkspaceAsync(_workspacePath, token);
                }
            }
            catch (OperationCanceledException)
            {
                // LOGIC: Expected when debounce is reset
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during hot-reload of ignore patterns");
            }
        }, token);
    }

    /// <summary>
    /// Raises the PatternsReloaded event.
    /// </summary>
    /// <param name="source">Path to the source file, or null for manual registration.</param>
    private void RaisePatternsReloaded(string? source)
    {
        var args = new PatternsReloadedEventArgs(
            PatternCount: PatternCount,
            TruncatedCount: TruncatedPatternCount,
            Source: source);

        PatternsReloaded?.Invoke(this, args);
    }

    #endregion

    #region IDisposable

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _logger.LogDebug("Disposing IgnorePatternService");

        StopFileWatcher();

        _lock.Dispose();
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// Represents a single ignore pattern with its matcher.
    /// </summary>
    /// <remarks>
    /// LOGIC: Encapsulates pattern string and compiled matcher.
    /// - Pattern: Original pattern string
    /// - IsNegation: True if pattern starts with !
    /// - Matcher: Compiled glob matcher
    /// </remarks>
    private sealed class IgnorePattern
    {
        private readonly Matcher _matcher;

        /// <summary>
        /// Gets the original pattern string.
        /// </summary>
        public string Pattern { get; }

        /// <summary>
        /// Gets whether this is a negation pattern.
        /// </summary>
        public bool IsNegation { get; }

        /// <summary>
        /// Initializes a new IgnorePattern.
        /// </summary>
        /// <param name="pattern">The glob pattern.</param>
        /// <param name="isNegation">Whether this is a negation pattern.</param>
        public IgnorePattern(string pattern, bool isNegation)
        {
            Pattern = pattern;
            IsNegation = isNegation;

            // LOGIC: Build matcher with the pattern
            _matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
            _matcher.AddInclude(pattern);
        }

        /// <summary>
        /// Checks if the pattern matches a file path.
        /// </summary>
        /// <param name="filePath">Normalized file path to check.</param>
        /// <returns>True if the pattern matches.</returns>
        public bool Matches(string filePath)
        {
            // LOGIC: Use InMemoryDirectoryInfo for path-based matching
            var result = _matcher.Match(filePath);
            return result.HasMatches;
        }
    }

    #endregion
}
