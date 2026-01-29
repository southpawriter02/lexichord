using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// MediatR notification published when navigation is requested for an unknown dockable ID.
/// </summary>
/// <param name="RequestedId">The identifier that was requested but not found.</param>
/// <remarks>
/// LOGIC: Enables dynamic dockable creation by allowing modules to respond to
/// navigation requests for IDs they manage. A module can implement INotificationHandler
/// to create the requested dockable on demand.
/// </remarks>
public record RegionNavigationRequestNotification(
    string RequestedId
) : INotification;
