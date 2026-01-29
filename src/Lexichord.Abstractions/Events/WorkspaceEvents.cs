using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Event published when a workspace is opened.
/// </summary>
/// <param name="WorkspaceRootPath">Absolute path to the workspace root.</param>
/// <param name="WorkspaceName">Display name of the workspace.</param>
/// <remarks>
/// LOGIC: Published after the workspace is fully opened:
/// - CurrentWorkspace is set
/// - File watcher is started
/// - Recent workspaces list is updated
///
/// Handlers can safely access IWorkspaceService.CurrentWorkspace.
/// </remarks>
public record WorkspaceOpenedEvent(
    string WorkspaceRootPath,
    string WorkspaceName
) : INotification;

/// <summary>
/// Event published when a workspace is closed.
/// </summary>
/// <param name="WorkspaceRootPath">Path of the workspace that was closed.</param>
/// <remarks>
/// LOGIC: Published after the workspace is closed:
/// - CurrentWorkspace is null
/// - File watcher is stopped
///
/// This event includes the path that WAS open (for reference by handlers).
/// After handling this event, IWorkspaceService.CurrentWorkspace is null.
/// </remarks>
public record WorkspaceClosedEvent(
    string WorkspaceRootPath
) : INotification;
