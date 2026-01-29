using Lexichord.Abstractions.Contracts;

namespace Lexichord.Abstractions.Layout;

/// <summary>
/// Specifies the type of change that occurred in a dock region.
/// </summary>
public enum RegionChangeType
{
    /// <summary>
    /// A dockable was added to the region.
    /// </summary>
    Added,

    /// <summary>
    /// A dockable was removed from the region.
    /// </summary>
    Removed,

    /// <summary>
    /// A dockable was activated (made visible/focused) in the region.
    /// </summary>
    Activated
}

/// <summary>
/// Event arguments for region change notifications.
/// </summary>
/// <param name="Region">The shell region where the change occurred.</param>
/// <param name="DockableId">The identifier of the affected dockable.</param>
/// <param name="ChangeType">The type of change that occurred.</param>
/// <remarks>
/// LOGIC: Provides details about dock region changes for event handlers.
/// Modules can subscribe to these events to react to layout changes
/// without coupling to Dock.Avalonia.
/// </remarks>
public record RegionChangedEventArgs(
    ShellRegion Region,
    string DockableId,
    RegionChangeType ChangeType
);
