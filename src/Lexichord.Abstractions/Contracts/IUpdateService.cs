namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for managing application updates and version information.
/// </summary>
/// <remarks>
/// LOGIC: Provides channel switching between Stable and Insider,
/// version information display, and update checking functionality.
/// In v0.1.6d, CheckForUpdatesAsync is a stub that always returns null.
///
/// Thread Safety:
/// - Property access is thread-safe
/// - SetChannelAsync and CheckForUpdatesAsync should be called from UI thread
///
/// Version: v0.1.6d
/// </remarks>
/// <example>
/// <code>
/// var updateService = services.GetRequiredService&lt;IUpdateService&gt;();
/// var version = updateService.GetVersionInfo();
/// await updateService.SetChannelAsync(UpdateChannel.Insider);
/// var update = await updateService.CheckForUpdatesAsync();
/// </code>
/// </example>
public interface IUpdateService
{
    /// <summary>
    /// Gets the currently selected update channel.
    /// </summary>
    UpdateChannel CurrentChannel { get; }

    /// <summary>
    /// Gets the current application version string.
    /// </summary>
    string CurrentVersion { get; }

    /// <summary>
    /// Gets the last time an update check was performed.
    /// </summary>
    /// <remarks>
    /// LOGIC: Returns null if no check has been performed yet.
    /// Time is stored in UTC.
    /// </remarks>
    DateTime? LastCheckTime { get; }

    /// <summary>
    /// Gets detailed version information.
    /// </summary>
    /// <returns>A <see cref="VersionInfo"/> record with complete version details.</returns>
    VersionInfo GetVersionInfo();

    /// <summary>
    /// Changes the update channel.
    /// </summary>
    /// <param name="channel">The new channel to use.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// LOGIC: Persists the channel selection and raises ChannelChanged event.
    /// No-op if channel is already set to the specified value.
    /// </remarks>
    Task SetChannelAsync(UpdateChannel channel);

    /// <summary>
    /// Checks for available updates on the current channel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Update info if available, null if up to date.</returns>
    /// <remarks>
    /// LOGIC: In v0.1.6d, this is a stub that simulates a network delay
    /// and always returns null (no update available).
    /// </remarks>
    Task<UpdateInfo?> CheckForUpdatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Raised when an update is available.
    /// </summary>
    event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;

    /// <summary>
    /// Raised when the update channel is changed.
    /// </summary>
    event EventHandler<UpdateChannelChangedEventArgs>? ChannelChanged;
}
