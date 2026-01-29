namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Describes an available application update.
/// </summary>
/// <remarks>
/// LOGIC: Returned by IUpdateService.CheckForUpdatesAsync when an update
/// is available. Contains all information needed to display and download the update.
///
/// Version: v0.1.6d
/// </remarks>
/// <param name="Version">Version string of the available update.</param>
/// <param name="ReleaseNotes">Release notes or changelog summary.</param>
/// <param name="DownloadUrl">URL to download the update package.</param>
/// <param name="ReleaseDate">Date when the update was released.</param>
/// <param name="IsCritical">True if this is a critical security or stability update.</param>
/// <param name="DownloadSize">Size of the download in bytes, if known.</param>
public record UpdateInfo(
    string Version,
    string ReleaseNotes,
    string DownloadUrl,
    DateTime ReleaseDate,
    bool IsCritical = false,
    long? DownloadSize = null
)
{
    /// <summary>
    /// Gets a human-readable formatted download size.
    /// </summary>
    /// <remarks>
    /// LOGIC: Converts bytes to appropriate unit (B, KB, MB, GB)
    /// for user-friendly display.
    /// </remarks>
    public string? FormattedDownloadSize => DownloadSize switch
    {
        null => null,
        < 1024 => $"{DownloadSize} B",
        < 1024 * 1024 => $"{DownloadSize / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{DownloadSize / 1024.0 / 1024.0:F1} MB",
        _ => $"{DownloadSize / 1024.0 / 1024.0 / 1024.0:F2} GB"
    };
}
