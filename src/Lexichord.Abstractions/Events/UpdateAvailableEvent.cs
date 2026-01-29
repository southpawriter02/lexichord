using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// MediatR notification raised when an update is available.
/// </summary>
/// <remarks>
/// LOGIC: Published by UpdateService when CheckForUpdatesAsync
/// detects a new version. Enables other components to display
/// notifications or badges.
///
/// Version: v0.1.6d
/// </remarks>
/// <param name="Update">Information about the available update.</param>
public sealed record UpdateAvailableEvent(
    Contracts.UpdateInfo Update
) : INotification;
