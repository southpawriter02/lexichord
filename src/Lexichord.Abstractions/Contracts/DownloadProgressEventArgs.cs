namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Event arguments for update download progress.
/// </summary>
/// <remarks>
/// LOGIC: Provides progress information during update downloads.
/// Progress value is normalized to 0.0-1.0 range for easy UI binding.
///
/// Version: v0.1.7a
/// </remarks>
/// <param name="Progress">Download progress from 0.0 to 1.0.</param>
/// <param name="BytesDownloaded">Number of bytes downloaded so far.</param>
/// <param name="TotalBytes">Total bytes to download (0 if unknown).</param>
public sealed record DownloadProgressEventArgs(
    float Progress,
    long BytesDownloaded,
    long TotalBytes)
{
    /// <summary>
    /// Gets the progress as a percentage (0-100).
    /// </summary>
    public int ProgressPercent => (int)(Progress * 100);
}
