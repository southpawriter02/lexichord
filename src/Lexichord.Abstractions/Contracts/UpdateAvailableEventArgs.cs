namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Event arguments for when an update becomes available.
/// </summary>
/// <remarks>
/// LOGIC: Raised by IUpdateService.UpdateAvailable event when
/// CheckForUpdatesAsync detects a new version.
///
/// Version: v0.1.6d
/// </remarks>
public sealed class UpdateAvailableEventArgs : EventArgs
{
    /// <summary>
    /// Gets the information about the available update.
    /// </summary>
    public required UpdateInfo Update { get; init; }
}
