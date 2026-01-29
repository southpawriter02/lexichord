using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Services;

/// <summary>
/// Handles WorkspaceOpenedEvent to trigger file index rebuild.
/// </summary>
/// <remarks>
/// LOGIC (v0.1.5c): When a workspace is opened, rebuild the file index
/// to enable fast file search via the Command Palette.
/// </remarks>
internal sealed class FileIndexWorkspaceOpenedHandler : INotificationHandler<WorkspaceOpenedEvent>
{
    private readonly IFileIndexService _fileIndexService;
    private readonly ILogger<FileIndexWorkspaceOpenedHandler> _logger;

    public FileIndexWorkspaceOpenedHandler(
        IFileIndexService fileIndexService,
        ILogger<FileIndexWorkspaceOpenedHandler> logger)
    {
        _fileIndexService = fileIndexService;
        _logger = logger;
    }

    public async Task Handle(WorkspaceOpenedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Workspace opened, rebuilding file index: {Path}", notification.WorkspaceRootPath);

        try
        {
            await _fileIndexService.RebuildIndexAsync(notification.WorkspaceRootPath, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to rebuild file index for workspace: {Path}", notification.WorkspaceRootPath);
        }
    }
}

/// <summary>
/// Handles WorkspaceClosedEvent to clear the file index.
/// </summary>
/// <remarks>
/// LOGIC (v0.1.5c): When a workspace is closed, clear the file index
/// to free memory and reset state for the next workspace.
/// </remarks>
internal sealed class FileIndexWorkspaceClosedHandler : INotificationHandler<WorkspaceClosedEvent>
{
    private readonly IFileIndexService _fileIndexService;
    private readonly ILogger<FileIndexWorkspaceClosedHandler> _logger;

    public FileIndexWorkspaceClosedHandler(
        IFileIndexService fileIndexService,
        ILogger<FileIndexWorkspaceClosedHandler> logger)
    {
        _fileIndexService = fileIndexService;
        _logger = logger;
    }

    public Task Handle(WorkspaceClosedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Workspace closed, clearing file index: {Path}", notification.WorkspaceRootPath);
        _fileIndexService.Clear();
        return Task.CompletedTask;
    }
}

/// <summary>
/// Handles ExternalFileChangesEvent for incremental index updates.
/// </summary>
/// <remarks>
/// LOGIC (v0.1.5c): Maps file system changes to file index updates:
/// - Created → UpdateFile(Created)
/// - Changed → UpdateFile(Modified)
/// - Deleted → UpdateFile(Deleted)
/// - Renamed → UpdateFileRenamed()
/// 
/// Only processes file changes (directories are ignored).
/// </remarks>
internal sealed class FileIndexExternalChangesHandler : INotificationHandler<ExternalFileChangesEvent>
{
    private readonly IFileIndexService _fileIndexService;
    private readonly ILogger<FileIndexExternalChangesHandler> _logger;

    public FileIndexExternalChangesHandler(
        IFileIndexService fileIndexService,
        ILogger<FileIndexExternalChangesHandler> logger)
    {
        _fileIndexService = fileIndexService;
        _logger = logger;
    }

    public Task Handle(ExternalFileChangesEvent notification, CancellationToken cancellationToken)
    {
        foreach (var change in notification.Changes)
        {
            // Skip directory changes
            if (change.IsDirectory)
                continue;

            switch (change.ChangeType)
            {
                case FileSystemChangeType.Created:
                    _fileIndexService.UpdateFile(change.FullPath, FileIndexAction.Created);
                    break;

                case FileSystemChangeType.Changed:
                    _fileIndexService.UpdateFile(change.FullPath, FileIndexAction.Modified);
                    break;

                case FileSystemChangeType.Deleted:
                    _fileIndexService.UpdateFile(change.FullPath, FileIndexAction.Deleted);
                    break;

                case FileSystemChangeType.Renamed:
                    if (change.OldPath != null)
                    {
                        _fileIndexService.UpdateFileRenamed(change.OldPath, change.FullPath);
                    }
                    else
                    {
                        // Fall back to treating as create if old path unknown
                        _fileIndexService.UpdateFile(change.FullPath, FileIndexAction.Created);
                    }
                    break;
            }
        }

        _logger.LogDebug("Processed {Count} file changes for index update", 
            notification.Changes.Count(c => !c.IsDirectory));

        return Task.CompletedTask;
    }
}
