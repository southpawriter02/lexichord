namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Event args for the WorkspaceChanged event.
/// </summary>
public class WorkspaceChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the type of workspace change.
    /// </summary>
    public required WorkspaceChangeType ChangeType { get; init; }

    /// <summary>
    /// Gets the workspace that was open before the change, or null.
    /// </summary>
    public WorkspaceInfo? PreviousWorkspace { get; init; }

    /// <summary>
    /// Gets the workspace that is open after the change, or null.
    /// </summary>
    public WorkspaceInfo? NewWorkspace { get; init; }
}

/// <summary>
/// Types of workspace state changes.
/// </summary>
public enum WorkspaceChangeType
{
    /// <summary>
    /// A workspace was opened (possibly replacing another).
    /// </summary>
    Opened,

    /// <summary>
    /// The workspace was closed (no workspace now open).
    /// </summary>
    Closed
}
