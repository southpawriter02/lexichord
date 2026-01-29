namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Event arguments for settings page registration and unregistration events.
/// </summary>
/// <remarks>
/// LOGIC: Provides the page information when a settings page is registered or unregistered
/// from the registry. This enables consumers to react to dynamic changes in available
/// settings pages (e.g., when modules are loaded or unloaded).
///
/// Thread Safety:
/// - This class is immutable and thread-safe.
///
/// Version: v0.1.6a
/// </remarks>
public sealed class SettingsPageEventArgs : EventArgs
{
    /// <summary>
    /// Gets the settings page that was registered or unregistered.
    /// </summary>
    public ISettingsPage Page { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsPageEventArgs"/> class.
    /// </summary>
    /// <param name="page">The settings page involved in the event.</param>
    /// <exception cref="ArgumentNullException">Thrown when page is null.</exception>
    public SettingsPageEventArgs(ISettingsPage page)
    {
        Page = page ?? throw new ArgumentNullException(nameof(page));
    }
}
