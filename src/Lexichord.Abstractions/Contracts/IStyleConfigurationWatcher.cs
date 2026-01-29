namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Defines the contract for monitoring style configuration files for changes.
/// </summary>
/// <remarks>
/// LOGIC: IStyleConfigurationWatcher enables live reload of style sheets.
/// It monitors the project root for lexichord.yaml changes and notifies subscribers.
///
/// Full implementation in v0.2.1d will use FileSystemWatcher with debouncing.
/// This interface is IDisposable because watchers hold OS resources.
///
/// Design Decisions:
/// - Single-path watching: one watcher per project root
/// - Debounced events: rapid saves don't spam reload
/// - Error events: watcher failures don't crash the module
/// </remarks>
public interface IStyleConfigurationWatcher : IDisposable
{
    /// <summary>
    /// Starts watching the specified project root for style configuration changes.
    /// </summary>
    /// <param name="projectRoot">The project root directory to monitor.</param>
    /// <remarks>
    /// LOGIC: Watches for lexichord.yaml in the project root.
    /// If already watching, stops the previous watcher first.
    /// Raises FileChanged when the configuration file is modified.
    /// </remarks>
    void StartWatching(string projectRoot);

    /// <summary>
    /// Stops watching for configuration changes.
    /// </summary>
    /// <remarks>
    /// LOGIC: Releases file system watcher resources.
    /// Safe to call multiple times or when not watching.
    /// </remarks>
    void StopWatching();

    /// <summary>
    /// Gets whether the watcher is currently active.
    /// </summary>
    bool IsWatching { get; }

    /// <summary>
    /// Gets the path currently being watched, if any.
    /// </summary>
    string? WatchedPath { get; }

    /// <summary>
    /// Occurs when a style configuration file changes.
    /// </summary>
    /// <remarks>
    /// LOGIC: Raised after debounce period when lexichord.yaml is modified.
    /// Consumers should reload the style sheet on this event.
    /// </remarks>
    event EventHandler<StyleFileChangedEventArgs>? FileChanged;

    /// <summary>
    /// Occurs when the watcher encounters an error.
    /// </summary>
    /// <remarks>
    /// LOGIC: Raised on file system errors (permissions, buffer overflow).
    /// Watcher may auto-restart; consumers should log but not crash.
    /// </remarks>
    event EventHandler<StyleWatcherErrorEventArgs>? WatcherError;

    /// <summary>
    /// Gets or sets the debounce delay in milliseconds.
    /// </summary>
    /// <remarks>
    /// LOGIC: Debouncing prevents multiple reloads during rapid saves.
    /// Typical editor save operations may trigger multiple file events.
    /// Default is 300ms which handles most save scenarios.
    /// </remarks>
    int DebounceDelayMs { get; set; }

    /// <summary>
    /// Forces an immediate reload of the style configuration file.
    /// </summary>
    /// <returns>A task representing the async reload operation.</returns>
    /// <remarks>
    /// LOGIC: Allows manual reload trigger independent of file changes.
    /// Useful for:
    /// - Initial load when watcher starts
    /// - User-initiated refresh command
    /// - Recovery after error conditions
    ///
    /// Does nothing if not currently watching.
    /// </remarks>
    Task ForceReloadAsync();
}
