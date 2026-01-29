using MediatR;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Layout;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// MediatR notification published when a dock region changes.
/// </summary>
/// <param name="Region">The shell region where the change occurred.</param>
/// <param name="DockableId">The identifier of the affected dockable.</param>
/// <param name="ChangeType">The type of change that occurred.</param>
/// <remarks>
/// LOGIC: Enables loose coupling between the DockRegionManager and modules
/// that need to react to layout changes. Modules can implement INotificationHandler
/// to receive these notifications without direct reference to the region manager.
/// </remarks>
public record RegionChangedNotification(
    ShellRegion Region,
    string DockableId,
    RegionChangeType ChangeType
) : INotification;
