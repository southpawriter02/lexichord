using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// MediatR notification raised when an update check completes.
/// </summary>
/// <remarks>
/// LOGIC: Published by UpdateService after CheckForUpdatesAsync
/// completes, regardless of whether an update was found. Useful
/// for updating UI state and logging.
///
/// Version: v0.1.6d
/// </remarks>
/// <param name="UpdateFound">True if an update was found.</param>
/// <param name="CurrentVersion">The current application version.</param>
public sealed record UpdateCheckCompletedEvent(
    bool UpdateFound,
    string CurrentVersion
) : INotification;
