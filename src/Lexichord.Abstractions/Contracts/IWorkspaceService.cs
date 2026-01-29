namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for managing the current workspace state.
/// </summary>
/// <remarks>
/// LOGIC: IWorkspaceService is the single source of truth for which folder
/// is currently open in the application. It provides:
///
/// - CurrentWorkspace: The currently open workspace info (or null)
/// - OpenWorkspaceAsync: Opens a folder as the workspace
/// - CloseWorkspaceAsync: Closes the current workspace
/// - GetRecentWorkspaces: Lists recently opened workspaces
/// - WorkspaceChanged: Event for state change notifications
///
/// Design decisions:
/// - Only one workspace can be open at a time
/// - Opening a new workspace closes the previous one
/// - Recent workspaces are persisted for quick access
/// - MediatR events are published for cross-module notification
/// </remarks>
public interface IWorkspaceService
{
    /// <summary>
    /// Gets the currently open workspace, or null if no workspace is open.
    /// </summary>
    WorkspaceInfo? CurrentWorkspace { get; }

    /// <summary>
    /// Gets whether a workspace is currently open.
    /// </summary>
    bool IsWorkspaceOpen { get; }

    /// <summary>
    /// Opens a folder as the current workspace.
    /// </summary>
    /// <param name="folderPath">Absolute path to the folder to open.</param>
    /// <returns>True if the workspace was opened successfully; false if validation failed.</returns>
    /// <remarks>
    /// LOGIC: Opening a new workspace when one is already open will:
    /// 1. Stop the file watcher on the old workspace
    /// 2. Publish WorkspaceClosedEvent
    /// 3. Set the new workspace
    /// 4. Start the file watcher on the new workspace
    /// 5. Add to recent workspaces
    /// 6. Publish WorkspaceOpenedEvent
    ///
    /// Returns false if:
    /// - Path is null or empty
    /// - Path does not exist
    /// - Path is not a directory
    /// </remarks>
    Task<bool> OpenWorkspaceAsync(string folderPath);

    /// <summary>
    /// Closes the current workspace.
    /// </summary>
    /// <remarks>
    /// LOGIC: Closing the workspace will:
    /// 1. Stop the file watcher
    /// 2. Set CurrentWorkspace to null
    /// 3. Publish WorkspaceClosedEvent
    ///
    /// Idempotent - calling when no workspace is open is a no-op.
    /// </remarks>
    Task CloseWorkspaceAsync();

    /// <summary>
    /// Gets the list of recently opened workspaces.
    /// </summary>
    /// <returns>A read-only list of workspace paths, most recent first.</returns>
    /// <remarks>
    /// LOGIC: Returns up to 10 most recently opened workspace paths.
    /// Paths may no longer exist on disk - consumers should verify before using.
    /// UI should check existence before offering "Open Recent."
    /// </remarks>
    IReadOnlyList<string> GetRecentWorkspaces();

    /// <summary>
    /// Clears the recent workspaces list.
    /// </summary>
    /// <remarks>
    /// LOGIC: Removes all entries from the recent workspaces list.
    /// Persists the empty list immediately.
    /// </remarks>
    Task ClearRecentWorkspacesAsync();

    /// <summary>
    /// Event raised when the workspace state changes.
    /// </summary>
    /// <remarks>
    /// LOGIC: Local event for components that need synchronous notification.
    /// For cross-module communication, use the MediatR events instead.
    /// </remarks>
    event EventHandler<WorkspaceChangedEventArgs>? WorkspaceChanged;
}
