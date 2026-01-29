namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Event arguments for when the update channel is changed.
/// </summary>
/// <remarks>
/// LOGIC: Raised by IUpdateService.ChannelChanged event when
/// the user switches between Stable and Insider channels.
///
/// Version: v0.1.6d
/// </remarks>
public sealed class UpdateChannelChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous update channel.
    /// </summary>
    public required UpdateChannel OldChannel { get; init; }

    /// <summary>
    /// Gets the new update channel.
    /// </summary>
    public required UpdateChannel NewChannel { get; init; }
}
