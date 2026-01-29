using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// File system-based implementation of <see cref="IStyleConfigurationWatcher"/>.
/// </summary>
/// <remarks>
/// LOGIC: Monitors the .lexichord/ directory for style.yaml changes.
/// 
/// Implementation Details:
/// - Uses FileSystemWatcher for OS-level file notifications
/// - Debounces rapid changes using System.Threading.Timer (300ms default)
/// - License-gated to WriterPro tier
/// - Graceful fallback: invalid YAML keeps previous valid rules
/// - Thread-safe disposal of watcher and timer resources
///
/// Version: v0.2.1d
/// </remarks>
public sealed class FileSystemStyleWatcher : IStyleConfigurationWatcher
{
    private const string StyleFileName = "style.yaml";
    private const string LexichordDirectory = ".lexichord";
    private const int DefaultDebounceDelayMs = 300;

    private readonly IStyleSheetLoader _loader;
    private readonly IStyleEngine _engine;
    private readonly IMediator _mediator;
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<FileSystemStyleWatcher> _logger;

    private readonly object _syncLock = new();
    private FileSystemWatcher? _watcher;
    private Timer? _debounceTimer;
    private string? _pendingFilePath;
    private WatcherChangeTypes _pendingChangeType;
    private bool _disposed;

    /// <inheritdoc/>
    public bool IsWatching { get; private set; }

    /// <inheritdoc/>
    public string? WatchedPath { get; private set; }

    /// <inheritdoc/>
    public int DebounceDelayMs { get; set; } = DefaultDebounceDelayMs;

    /// <inheritdoc/>
    public event EventHandler<StyleFileChangedEventArgs>? FileChanged;

    /// <inheritdoc/>
    public event EventHandler<StyleWatcherErrorEventArgs>? WatcherError;

    /// <summary>
    /// Initializes a new instance of <see cref="FileSystemStyleWatcher"/>.
    /// </summary>
    /// <param name="loader">Style sheet loader for YAML parsing.</param>
    /// <param name="engine">Style engine to update on reload.</param>
    /// <param name="mediator">MediatR instance for event publishing.</param>
    /// <param name="licenseContext">License context for tier checking.</param>
    /// <param name="logger">Logger instance.</param>
    public FileSystemStyleWatcher(
        IStyleSheetLoader loader,
        IStyleEngine engine,
        IMediator mediator,
        ILicenseContext licenseContext,
        ILogger<FileSystemStyleWatcher> logger)
    {
        _loader = loader;
        _engine = engine;
        _mediator = mediator;
        _licenseContext = licenseContext;
        _logger = logger;

        _logger.LogDebug("FileSystemStyleWatcher initialized");
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Starts watching for style.yaml changes in the project root.
    /// 
    /// Pre-conditions:
    /// - Requires WriterPro license tier or higher
    /// - Directory must exist
    /// 
    /// If already watching, stops the previous watcher first.
    /// </remarks>
    public void StartWatching(string projectRoot)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);

        // LOGIC: License tier gate - feature requires WriterPro
        if (_licenseContext.GetCurrentTier() < LicenseTier.WriterPro)
        {
            _logger.LogDebug(
                "Configuration watching requires WriterPro tier. Current tier: {Tier}",
                _licenseContext.GetCurrentTier());
            return;
        }

        if (IsWatching)
        {
            StopWatching();
        }

        var lexichordDir = Path.Combine(projectRoot, LexichordDirectory);

        // LOGIC: Create .lexichord directory if it doesn't exist
        if (!Directory.Exists(lexichordDir))
        {
            _logger.LogDebug("Creating .lexichord directory at '{Path}'", lexichordDir);
            Directory.CreateDirectory(lexichordDir);
        }

        lock (_syncLock)
        {
            try
            {
                _watcher = new FileSystemWatcher(lexichordDir)
                {
                    Filter = StyleFileName,
                    NotifyFilter = NotifyFilters.LastWrite
                                 | NotifyFilters.FileName
                                 | NotifyFilters.CreationTime,
                    EnableRaisingEvents = true
                };

                _watcher.Changed += OnFileSystemEvent;
                _watcher.Created += OnFileSystemEvent;
                _watcher.Deleted += OnFileSystemEvent;
                _watcher.Renamed += OnFileSystemRenamed;
                _watcher.Error += OnWatcherError;

                WatchedPath = lexichordDir;
                IsWatching = true;

                _logger.LogInformation(
                    "Started watching for style configuration changes at '{Path}'",
                    lexichordDir);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start file system watcher for '{Path}'", lexichordDir);
                RaiseWatcherError(ex);
                throw;
            }
        }
    }

    /// <inheritdoc/>
    public void StopWatching()
    {
        if (!IsWatching)
        {
            return;
        }

        lock (_syncLock)
        {
            DisposeWatcherResources();
            IsWatching = false;

            _logger.LogDebug("Stopped watching for style configuration changes at '{Path}'", WatchedPath);
            WatchedPath = null;
        }
    }

    /// <inheritdoc/>
    public async Task ForceReloadAsync()
    {
        if (!IsWatching || string.IsNullOrEmpty(WatchedPath))
        {
            _logger.LogDebug("ForceReloadAsync called but not currently watching");
            return;
        }

        var filePath = Path.Combine(WatchedPath, StyleFileName);
        if (File.Exists(filePath))
        {
            await ReloadStyleFileAsync(filePath, StyleReloadSource.ManualReload);
        }
        else
        {
            _logger.LogDebug("ForceReloadAsync: style.yaml not found at '{Path}'", filePath);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        StopWatching();
        _disposed = true;

        _logger.LogDebug("FileSystemStyleWatcher disposed");
    }

    private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
    {
        // LOGIC: Delegate to common handler
        ScheduleReload(e.FullPath, e.ChangeType);
    }

    private void OnFileSystemRenamed(object sender, RenamedEventArgs e)
    {
        // LOGIC: Handle rename as a modification to the new file
        ScheduleReload(e.FullPath, e.ChangeType);
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        var exception = e.GetException();
        _logger.LogError(exception, "FileSystemWatcher encountered an error");

        RaiseWatcherError(exception);

        // LOGIC: Attempt to restart the watcher
        if (IsWatching && WatchedPath is not null)
        {
            var projectRoot = Path.GetDirectoryName(WatchedPath);
            if (projectRoot is not null)
            {
                _logger.LogInformation("Attempting to restart file watcher after error");
                try
                {
                    StopWatching();
                    StartWatching(projectRoot);
                }
                catch (Exception restartEx)
                {
                    _logger.LogError(restartEx, "Failed to restart file watcher");
                }
            }
        }
    }

    /// <summary>
    /// Schedules a debounced reload of the style file.
    /// </summary>
    private void ScheduleReload(string filePath, WatcherChangeTypes changeType)
    {
        lock (_syncLock)
        {
            _pendingFilePath = filePath;
            _pendingChangeType = changeType;

            // LOGIC: Reset or create the debounce timer
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(
                callback: OnDebounceTimerElapsed,
                state: null,
                dueTime: DebounceDelayMs,
                period: Timeout.Infinite);

            _logger.LogDebug(
                "Scheduled debounced reload for '{Path}' (change type: {ChangeType})",
                filePath, changeType);
        }
    }

    /// <summary>
    /// Called when the debounce timer elapses.
    /// </summary>
    private void OnDebounceTimerElapsed(object? state)
    {
        string? filePath;
        WatcherChangeTypes changeType;

        lock (_syncLock)
        {
            filePath = _pendingFilePath;
            changeType = _pendingChangeType;
            _pendingFilePath = null;

            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }

        if (filePath is null)
        {
            return;
        }

        // LOGIC: Handle deletion - revert to embedded defaults
        if (changeType == WatcherChangeTypes.Deleted)
        {
            _ = HandleFileDeletionAsync();
            return;
        }

        // LOGIC: Fire-and-forget the async reload
        var reloadSource = changeType switch
        {
            WatcherChangeTypes.Created => StyleReloadSource.FileCreated,
            WatcherChangeTypes.Renamed => StyleReloadSource.FileRenamed,
            _ => StyleReloadSource.FileModified
        };

        _ = ReloadStyleFileAsync(filePath, reloadSource);
    }

    /// <summary>
    /// Handles file deletion by reverting to default rules.
    /// </summary>
    private async Task HandleFileDeletionAsync()
    {
        try
        {
            _logger.LogInformation("Style configuration file deleted, reverting to embedded defaults");

            var previousSheet = _engine.GetActiveStyleSheet();
            var defaultSheet = await _loader.LoadEmbeddedDefaultAsync();

            _engine.SetActiveStyleSheet(defaultSheet);

            // LOGIC: Raise local event
            FileChanged?.Invoke(this, new StyleFileChangedEventArgs
            {
                FilePath = WatchedPath ?? string.Empty,
                ChangeType = WatcherChangeTypes.Deleted
            });

            // LOGIC: Publish MediatR event
            await _mediator.Publish(new StyleSheetReloadedEvent(
                FilePath: WatchedPath ?? string.Empty,
                NewStyleSheet: defaultSheet,
                PreviousStyleSheet: previousSheet,
                ReloadSource: StyleReloadSource.FileModified));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revert to default rules after file deletion");
            RaiseWatcherError(ex);
        }
    }

    /// <summary>
    /// Reloads the style file and updates the engine.
    /// </summary>
    private async Task ReloadStyleFileAsync(string filePath, StyleReloadSource reloadSource)
    {
        try
        {
            _logger.LogDebug("Reloading style configuration from '{Path}'", filePath);

            var previousSheet = _engine.GetActiveStyleSheet();
            var newSheet = await _loader.LoadFromFileAsync(filePath);

            _engine.SetActiveStyleSheet(newSheet);

            _logger.LogInformation(
                "Style configuration reloaded: {RuleCount} rules from '{SheetName}'",
                newSheet.Rules.Count, newSheet.Name);

            // LOGIC: Raise local event
            FileChanged?.Invoke(this, new StyleFileChangedEventArgs
            {
                FilePath = filePath,
                ChangeType = WatcherChangeTypes.Changed
            });

            // LOGIC: Publish MediatR event
            await _mediator.Publish(new StyleSheetReloadedEvent(
                FilePath: filePath,
                NewStyleSheet: newSheet,
                PreviousStyleSheet: previousSheet,
                ReloadSource: reloadSource));
        }
        catch (Exception ex)
        {
            // LOGIC: Graceful fallback - keep previous valid rules
            _logger.LogWarning(
                ex,
                "Failed to reload style configuration from '{Path}'. Keeping previous rules.",
                filePath);

            var errorType = ex switch
            {
                FileNotFoundException => StyleWatcherErrorType.FileAccessError,
                UnauthorizedAccessException => StyleWatcherErrorType.FileAccessError,
                InvalidOperationException when ex.Message.Contains("YAML") => StyleWatcherErrorType.YamlParseError,
                InvalidOperationException => StyleWatcherErrorType.SchemaValidationError,
                _ => StyleWatcherErrorType.Unknown
            };

            RaiseWatcherError(ex);

            // LOGIC: Publish error event
            await _mediator.Publish(new StyleWatcherErrorEvent(
                FilePath: filePath,
                ErrorMessage: ex.Message,
                Exception: ex,
                ErrorType: errorType));
        }
    }

    /// <summary>
    /// Raises the WatcherError event.
    /// </summary>
    private void RaiseWatcherError(Exception exception)
    {
        WatcherError?.Invoke(this, new StyleWatcherErrorEventArgs
        {
            Exception = exception
        });
    }

    /// <summary>
    /// Disposes watcher and timer resources.
    /// </summary>
    private void DisposeWatcherResources()
    {
        if (_watcher is not null)
        {
            _watcher.Changed -= OnFileSystemEvent;
            _watcher.Created -= OnFileSystemEvent;
            _watcher.Deleted -= OnFileSystemEvent;
            _watcher.Renamed -= OnFileSystemRenamed;
            _watcher.Error -= OnWatcherError;
            _watcher.Dispose();
            _watcher = null;
        }

        _debounceTimer?.Dispose();
        _debounceTimer = null;
    }
}
