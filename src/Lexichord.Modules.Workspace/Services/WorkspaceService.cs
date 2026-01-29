using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Lexichord.Modules.Workspace.Services;

/// <summary>
/// Implementation of IWorkspaceService managing workspace state.
/// </summary>
/// <remarks>
/// LOGIC: WorkspaceService is the central coordinator for workspace state.
/// It owns:
/// - CurrentWorkspace property
/// - File watcher lifecycle (start/stop)
/// - Recent workspaces persistence
/// - Event publishing
///
/// Thread safety: All state changes are atomic. Events are published
/// after state changes complete.
/// </remarks>
public sealed class WorkspaceService : IWorkspaceService, IDisposable
{
    /// <summary>
    /// Maximum number of recent workspaces to remember.
    /// </summary>
    private const int MaxRecentWorkspaces = 10;

    /// <summary>
    /// Configuration key for recent workspaces storage.
    /// </summary>
    private const string RecentWorkspacesConfigKey = "workspace:recent";

    private readonly IFileSystemWatcher _fileWatcher;
    private readonly ISystemSettingsRepository _settingsRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<WorkspaceService> _logger;
    private readonly object _stateLock = new();

    private WorkspaceInfo? _currentWorkspace;

    /// <summary>
    /// Initializes a new instance of WorkspaceService.
    /// </summary>
    public WorkspaceService(
        IFileSystemWatcher fileWatcher,
        ISystemSettingsRepository settingsRepository,
        IMediator mediator,
        ILogger<WorkspaceService> logger)
    {
        _fileWatcher = fileWatcher ?? throw new ArgumentNullException(nameof(fileWatcher));
        _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Wire up file watcher error handling
        _fileWatcher.Error += OnFileWatcherError;
    }

    /// <inheritdoc/>
    public WorkspaceInfo? CurrentWorkspace
    {
        get
        {
            lock (_stateLock)
            {
                return _currentWorkspace;
            }
        }
    }

    /// <inheritdoc/>
    public bool IsWorkspaceOpen => CurrentWorkspace is not null;

    /// <inheritdoc/>
    public event EventHandler<WorkspaceChangedEventArgs>? WorkspaceChanged;

    /// <inheritdoc/>
    public async Task<bool> OpenWorkspaceAsync(string folderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath, nameof(folderPath));

        _logger.LogInformation("Opening workspace: {Path}", folderPath);

        // LOGIC: Validate path exists and is a directory
        if (!Directory.Exists(folderPath))
        {
            _logger.LogWarning("Cannot open workspace: path does not exist: {Path}", folderPath);
            return false;
        }

        WorkspaceInfo? previousWorkspace;
        WorkspaceInfo newWorkspace;

        // LOGIC: Perform state change atomically
        lock (_stateLock)
        {
            previousWorkspace = _currentWorkspace;

            // Create new workspace info with normalized path
            var normalizedPath = Path.GetFullPath(folderPath);
            var folderName = Path.GetFileName(normalizedPath);

            // Handle root paths (C:\ or /)
            if (string.IsNullOrEmpty(folderName))
            {
                folderName = normalizedPath;
            }

            newWorkspace = new WorkspaceInfo(
                RootPath: normalizedPath,
                Name: folderName,
                OpenedAt: DateTimeOffset.UtcNow
            );

            _currentWorkspace = newWorkspace;
        }

        // LOGIC: Close previous workspace if open (outside lock to avoid deadlock)
        if (previousWorkspace is not null)
        {
            _fileWatcher.StopWatching();

            await _mediator.Publish(new WorkspaceClosedEvent(previousWorkspace.RootPath));

            _logger.LogDebug("Previous workspace closed: {Path}", previousWorkspace.RootPath);
        }

        // LOGIC: Start file watcher on new workspace
        try
        {
            _fileWatcher.StartWatching(newWorkspace.RootPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start file watcher for workspace: {Path}", newWorkspace.RootPath);
            // Continue - workspace is still open, just without file watching
        }

        // LOGIC: Add to recent workspaces
        await AddToRecentWorkspacesAsync(newWorkspace.RootPath);

        // LOGIC: Publish MediatR event for cross-module notification
        await _mediator.Publish(new WorkspaceOpenedEvent(
            newWorkspace.RootPath,
            newWorkspace.Name
        ));

        // LOGIC: Raise local event for synchronous listeners
        WorkspaceChanged?.Invoke(this, new WorkspaceChangedEventArgs
        {
            ChangeType = WorkspaceChangeType.Opened,
            PreviousWorkspace = previousWorkspace,
            NewWorkspace = newWorkspace
        });

        _logger.LogInformation(
            "Workspace opened successfully: {Name} ({Path})",
            newWorkspace.Name,
            newWorkspace.RootPath);

        return true;
    }

    /// <inheritdoc/>
    public async Task CloseWorkspaceAsync()
    {
        WorkspaceInfo? previousWorkspace;

        // LOGIC: Perform state change atomically
        lock (_stateLock)
        {
            if (_currentWorkspace is null)
            {
                _logger.LogDebug("CloseWorkspaceAsync called but no workspace is open");
                return;
            }

            previousWorkspace = _currentWorkspace;
            _currentWorkspace = null;
        }

        _logger.LogInformation("Closing workspace: {Path}", previousWorkspace.RootPath);

        // LOGIC: Stop file watcher
        _fileWatcher.StopWatching();

        // LOGIC: Publish MediatR event
        await _mediator.Publish(new WorkspaceClosedEvent(previousWorkspace.RootPath));

        // LOGIC: Raise local event
        WorkspaceChanged?.Invoke(this, new WorkspaceChangedEventArgs
        {
            ChangeType = WorkspaceChangeType.Closed,
            PreviousWorkspace = previousWorkspace,
            NewWorkspace = null
        });

        _logger.LogInformation("Workspace closed: {Path}", previousWorkspace.RootPath);
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetRecentWorkspaces()
    {
        try
        {
            // LOGIC: Use synchronous path - GetValueAsync with .Result could cause deadlock
            // For a service that may be called from UI thread, we use the sync overload
            var json = _settingsRepository.GetValueAsync(RecentWorkspacesConfigKey, "[]")
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            if (string.IsNullOrWhiteSpace(json))
            {
                return Array.Empty<string>();
            }

            var workspaces = JsonSerializer.Deserialize<List<string>>(json);
            return workspaces ?? new List<string>();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize recent workspaces, returning empty list");
            return Array.Empty<string>();
        }
    }

    /// <inheritdoc/>
    public async Task ClearRecentWorkspacesAsync()
    {
        _logger.LogInformation("Clearing recent workspaces list");

        await _settingsRepository.SetValueAsync(
            RecentWorkspacesConfigKey,
            "[]",
            "Recently opened workspace paths");
    }

    /// <summary>
    /// Adds a path to the recent workspaces list.
    /// </summary>
    private async Task AddToRecentWorkspacesAsync(string path)
    {
        var recent = GetRecentWorkspaces().ToList();

        // LOGIC: Remove existing entry (if any) to move to top
        recent.RemoveAll(p => p.Equals(path, StringComparison.OrdinalIgnoreCase));

        // LOGIC: Insert at top (most recent)
        recent.Insert(0, path);

        // LOGIC: Trim to maximum entries
        if (recent.Count > MaxRecentWorkspaces)
        {
            recent = recent.Take(MaxRecentWorkspaces).ToList();
        }

        // LOGIC: Persist
        var json = JsonSerializer.Serialize(recent);
        await _settingsRepository.SetValueAsync(
            RecentWorkspacesConfigKey,
            json,
            "Recently opened workspace paths");

        _logger.LogDebug("Recent workspaces updated, now {Count} entries", recent.Count);
    }

    /// <summary>
    /// Handles file watcher errors.
    /// </summary>
    private void OnFileWatcherError(object? sender, FileSystemWatcherErrorEventArgs e)
    {
        _logger.LogError(
            e.Exception,
            "File watcher error (recoverable: {IsRecoverable})",
            e.IsRecoverable);

        if (!e.IsRecoverable && _currentWorkspace is not null)
        {
            _logger.LogWarning("Attempting to restart file watcher after unrecoverable error");

            try
            {
                _fileWatcher.StopWatching();
                _fileWatcher.StartWatching(_currentWorkspace.RootPath);
                _logger.LogInformation("File watcher restarted successfully");
            }
            catch (Exception restartEx)
            {
                _logger.LogError(restartEx, "Failed to restart file watcher");
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _fileWatcher.Error -= OnFileWatcherError;
    }
}
