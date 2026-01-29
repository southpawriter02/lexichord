namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Configuration options for the update service.
/// </summary>
/// <remarks>
/// LOGIC: Provides channel-specific update feed URLs and auto-update behavior settings.
/// URLs point to Velopack-compatible update feeds (GitHub Releases, Azure Blob, etc.).
///
/// Version: v0.1.7a
/// </remarks>
/// <param name="StableUpdateUrl">URL for stable channel update feed.</param>
/// <param name="InsiderUpdateUrl">URL for insider/preview channel update feed.</param>
/// <param name="AutoCheckOnStartup">Whether to check for updates on application startup.</param>
/// <param name="AutoDownload">Whether to automatically download updates when found.</param>
public sealed record UpdateOptions(
    string StableUpdateUrl = "",
    string InsiderUpdateUrl = "",
    bool AutoCheckOnStartup = true,
    bool AutoDownload = false)
{
    /// <summary>
    /// Gets the update URL for the specified channel.
    /// </summary>
    /// <param name="channel">The update channel.</param>
    /// <returns>The URL for the channel's update feed.</returns>
    public string GetUrlForChannel(UpdateChannel channel) =>
        channel == UpdateChannel.Insider ? InsiderUpdateUrl : StableUpdateUrl;
}
