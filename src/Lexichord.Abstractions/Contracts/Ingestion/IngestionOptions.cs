// =============================================================================
// File: IngestionOptions.cs
// Project: Lexichord.Abstractions
// Description: Configuration options for the ingestion service.
// =============================================================================
// LOGIC: Immutable configuration record with sensible defaults.
//   - SupportedExtensions filters which files are processed.
//   - ExcludedDirectories prevents scanning build artifacts and VCS folders.
//   - MaxConcurrency defaults to processor count for optimal parallelism.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Ingestion;

/// <summary>
/// Configuration options for file and directory ingestion operations.
/// </summary>
/// <remarks>
/// <para>
/// This record provides configurable settings for the ingestion pipeline,
/// allowing customization of file filters, size limits, and parallelism.
/// </para>
/// <para>
/// Use <see cref="Default"/> to obtain an instance with sensible defaults
/// suitable for most technical writing projects.
/// </para>
/// </remarks>
/// <param name="SupportedExtensions">
/// The file extensions that should be processed (including the leading dot).
/// Files with extensions not in this list will be skipped.
/// </param>
/// <param name="ExcludedDirectories">
/// Directory names to exclude from scanning. Directory matching is case-insensitive.
/// Common build output and version control directories are excluded by default.
/// </param>
/// <param name="MaxFileSizeBytes">
/// The maximum file size in bytes. Files exceeding this limit will be skipped
/// to prevent memory issues with large binary files.
/// </param>
/// <param name="MaxConcurrency">
/// The maximum number of files to process in parallel. Higher values improve
/// throughput but increase memory usage and API rate consumption.
/// </param>
/// <param name="ThrottleDelayMs">
/// Optional delay (in milliseconds) between file processing operations.
/// Use this to prevent overwhelming external APIs with rapid requests.
/// </param>
public record IngestionOptions(
    IReadOnlyList<string> SupportedExtensions,
    IReadOnlyList<string> ExcludedDirectories,
    long MaxFileSizeBytes,
    int MaxConcurrency,
    int? ThrottleDelayMs)
{
    /// <summary>
    /// The default maximum file size (10 MB).
    /// </summary>
    public const long DefaultMaxFileSizeBytes = 10 * 1024 * 1024;

    /// <summary>
    /// Gets the default ingestion options suitable for technical writing projects.
    /// </summary>
    /// <remarks>
    /// <para><b>Default values:</b></para>
    /// <list type="bullet">
    ///   <item><description>Extensions: .md, .txt, .rst</description></item>
    ///   <item><description>Excluded: bin, obj, .git, node_modules, .vs, .idea</description></item>
    ///   <item><description>Max file size: 10 MB</description></item>
    ///   <item><description>Concurrency: Environment.ProcessorCount</description></item>
    ///   <item><description>Throttle: none</description></item>
    /// </list>
    /// </remarks>
    public static IngestionOptions Default { get; } = new(
        SupportedExtensions: [".md", ".txt", ".rst"],
        ExcludedDirectories: ["bin", "obj", ".git", "node_modules", ".vs", ".idea"],
        MaxFileSizeBytes: DefaultMaxFileSizeBytes,
        MaxConcurrency: Environment.ProcessorCount,
        ThrottleDelayMs: null);

    /// <summary>
    /// Determines whether the specified file extension is supported.
    /// </summary>
    /// <param name="extension">The file extension to check (with leading dot).</param>
    /// <returns>
    /// <c>true</c> if the extension is in <see cref="SupportedExtensions"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool IsExtensionSupported(string extension)
    {
        return SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the specified directory should be excluded.
    /// </summary>
    /// <param name="directoryName">The directory name to check.</param>
    /// <returns>
    /// <c>true</c> if the directory is in <see cref="ExcludedDirectories"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool IsDirectoryExcluded(string directoryName)
    {
        return ExcludedDirectories.Contains(directoryName, StringComparer.OrdinalIgnoreCase);
    }
}
