using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// MediatR notification raised when the update channel is changed.
/// </summary>
/// <remarks>
/// LOGIC: Published by UpdateService when the user switches between
/// Stable and Insider channels. Enables other components to react
/// to channel changes (e.g., analytics, logging).
///
/// Version: v0.1.6d
/// </remarks>
/// <param name="OldChannel">The previous channel.</param>
/// <param name="NewChannel">The new channel.</param>
public sealed record UpdateChannelChangedEvent(
    Contracts.UpdateChannel OldChannel,
    Contracts.UpdateChannel NewChannel
) : INotification;
