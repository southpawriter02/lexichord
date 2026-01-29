namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for managing application updates and version information.
/// </summary>
/// <remarks>
/// LOGIC: Provides channel switching between Stable and Insider,
/// version information display, and update management functionality.
/// In v0.1.7a, implements real Velopack integration for update delivery.
///
/// Thread Safety:
/// - Property access is thread-safe
/// - Async methods should be called from UI thread
///
/// Version: v0.1.7a
/// </remarks>
/// <example>
/// <code>
/// var updateService = services.GetRequiredService&lt;IUpdateService&gt;();
/// var version = updateService.GetVersionInfo();
/// await updateService.SetChannelAsync(UpdateChannel.Insider);
/// var update = await updateService.CheckForUpdatesAsync();
/// if (update is not null)
/// {
///     await updateService.DownloadUpdatesAsync(update);
///     updateService.ApplyUpdatesAndRestart();
/// }
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
    /// Gets a value indicating whether an update is downloaded and ready to install.
    /// </summary>
    /// <remarks>
    /// LOGIC: Returns true after DownloadUpdatesAsync completes successfully.
    /// Reset to false after ApplyUpdatesAndRestart or when a new check finds no update.
    /// </remarks>
    bool IsUpdateReady { get; }

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
    /// LOGIC: Queries the Velopack update feed for the current channel.
    /// Returns null when running in development mode (not installed via Velopack).
    /// </remarks>
    Task<UpdateInfo?> CheckForUpdatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the specified update package.
    /// </summary>
    /// <param name="update">The update to download.</param>
    /// <param name="progress">Optional progress reporter (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the download operation.</returns>
    /// <remarks>
    /// LOGIC: Downloads delta or full update package depending on availability.
    /// Sets IsUpdateReady to true on successful completion.
    /// Raises UpdateProgress events during download.
    /// </remarks>
    Task DownloadUpdatesAsync(
        UpdateInfo update,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies downloaded updates and restarts the application.
    /// </summary>
    /// <remarks>
    /// LOGIC: Applies the previously downloaded update and initiates application restart.
    /// This method does not return—the application will exit and restart.
    /// Throws InvalidOperationException if no update is ready.
    /// </remarks>
    /// <exception cref="InvalidOperationException">No update is ready to apply.</exception>
    void ApplyUpdatesAndRestart();

    /// <summary>
    /// Raised when an update is available.
    /// </summary>
    event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;

    /// <summary>
    /// Raised when the update channel is changed.
    /// </summary>
    event EventHandler<UpdateChannelChangedEventArgs>? ChannelChanged;

    /// <summary>
    /// Raised during update download to report progress.
    /// </summary>
    event EventHandler<DownloadProgressEventArgs>? UpdateProgress;
}
