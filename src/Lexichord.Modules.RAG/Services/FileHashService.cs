// =============================================================================
// File: FileHashService.cs
// Project: Lexichord.Modules.RAG
// Description: Implementation of IFileHashService using SHA-256 streaming.
// =============================================================================
// LOGIC: Provides efficient file hash computation with tiered change detection.
//   - Streaming SHA-256 computation with 80KB buffer for memory efficiency.
//   - Three-tier change detection: size → timestamp → hash.
//   - FileShare.Read for safe concurrent file access.
//   - Exhaustive logging for debugging and monitoring.
// =============================================================================

using System.Diagnostics;
using System.Security.Cryptography;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Service for computing and comparing file hashes using SHA-256.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses streaming hash computation to efficiently handle
/// files of any size without loading entire content into memory. The buffer
/// size of 80KB is optimized for modern file system block sizes and provides
/// good performance across a variety of storage media.
/// </para>
/// <para>
/// <b>Change Detection Strategy:</b> The <see cref="HasChangedAsync"/> method
/// implements a tiered approach:
/// </para>
/// <list type="number">
///   <item><description>File deleted → changed (immediate return)</description></item>
///   <item><description>Size differs → changed (skip hash computation)</description></item>
///   <item><description>Timestamp same → unchanged (skip hash computation)</description></item>
///   <item><description>Compute hash → compare for definitive answer</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This service is stateless and thread-safe. Multiple
/// concurrent calls can compute hashes for different files without interference.
/// </para>
/// </remarks>
public sealed class FileHashService : IFileHashService
{
    private readonly ILogger<FileHashService> _logger;

    /// <summary>
    /// Buffer size for streaming hash computation.
    /// </summary>
    /// <remarks>
    /// 80KB (81,920 bytes) is chosen to align with typical file system
    /// cluster sizes and provides good I/O performance while keeping
    /// memory usage low.
    /// </remarks>
    private const int BufferSize = 81920;

    /// <summary>
    /// Tolerance in seconds for timestamp comparison.
    /// </summary>
    /// <remarks>
    /// File system timestamps have varying precision (NTFS: 100ns, ext4: 1ns,
    /// FAT32: 2s). Using 1 second tolerance handles most cases while still
    /// being precise enough to detect meaningful changes.
    /// </remarks>
    private const double TimestampToleranceSeconds = 1.0;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileHashService"/> class.
    /// </summary>
    /// <param name="logger">
    /// Logger for diagnostic output. Debug-level messages are emitted for
    /// hash computation timing; Info-level for detected changes.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public FileHashService(ILogger<FileHashService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// The hash is computed using <see cref="SHA256.HashDataAsync"/> with
    /// async streaming I/O. The file is opened with <see cref="FileShare.Read"/>
    /// to allow concurrent read access by other processes.
    /// </para>
    /// <para>
    /// Performance logging includes elapsed time in milliseconds for
    /// monitoring hash computation overhead on large files.
    /// </para>
    /// </remarks>
    public async Task<string> ComputeHashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        // LOGIC: Start timing for performance monitoring.
        var stopwatch = Stopwatch.StartNew();

        // LOGIC: Get file info for logging. This also validates file existence
        // before opening the stream, providing a clearer error message.
        var fileInfo = new FileInfo(filePath);

        if (!fileInfo.Exists)
        {
            _logger.LogWarning("File not found for hash computation: {FilePath}", filePath);
            throw new FileNotFoundException($"Cannot compute hash: file not found.", filePath);
        }

        _logger.LogDebug(
            "Computing hash for: {FilePath} ({FileSize} bytes)",
            filePath,
            fileInfo.Length);

        // LOGIC: Open file with async I/O and shared read access.
        // FileShare.Read allows other processes to read the file concurrently.
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: BufferSize,
            useAsync: true);

        // LOGIC: Compute SHA-256 hash using streaming to avoid loading
        // entire file into memory.
        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);

        // LOGIC: Convert to lowercase hex string (64 characters for 256 bits).
        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        stopwatch.Stop();

        _logger.LogDebug(
            "Hash computed in {ElapsedMs}ms: {Hash}",
            stopwatch.ElapsedMilliseconds,
            hash);

        return hash;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// This method implements the tiered detection strategy documented
    /// in the interface. Each tier is logged at Debug level for troubleshooting.
    /// </para>
    /// <para>
    /// The timestamp tolerance of 1 second handles file system precision
    /// differences while still being accurate enough for practical use.
    /// </para>
    /// </remarks>
    public async Task<bool> HasChangedAsync(
        string filePath,
        string storedHash,
        long storedSize,
        DateTimeOffset? storedModified,
        CancellationToken cancellationToken = default)
    {
        // LOGIC: Get current file metadata for comparison.
        var metadata = GetMetadata(filePath);

        // TIER 0: File deleted = changed.
        if (!metadata.Exists)
        {
            _logger.LogWarning("File no longer exists: {FilePath}", filePath);
            return true;
        }

        // TIER 1: Size changed = definitely changed (skip hash computation).
        if (metadata.Size != storedSize)
        {
            _logger.LogDebug(
                "Quick check: size changed {OldSize} → {NewSize}",
                storedSize,
                metadata.Size);
            return true;
        }

        // TIER 2: Timestamp unchanged = assume unchanged (skip hash computation).
        // Only applies if we have a stored timestamp to compare against.
        if (storedModified.HasValue)
        {
            var timeDiff = Math.Abs((metadata.LastModified - storedModified.Value).TotalSeconds);

            if (timeDiff < TimestampToleranceSeconds)
            {
                _logger.LogDebug(
                    "Quick check: timestamp unchanged, skipping hash for {FilePath}",
                    filePath);
                return false;
            }
        }

        // TIER 3: Full hash comparison required (size same, timestamp differs).
        _logger.LogDebug(
            "Timestamp changed, computing hash for comparison: {FilePath}",
            filePath);

        var currentHash = await ComputeHashAsync(filePath, cancellationToken);

        // LOGIC: Case-insensitive comparison for hash strings.
        if (!string.Equals(currentHash, storedHash, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "File changed (hash mismatch): {FilePath}",
                filePath);
            return true;
        }

        _logger.LogDebug(
            "File unchanged (hash match): {FilePath}",
            filePath);
        return false;
    }

    /// <inheritdoc />
    /// <remarks>
    /// This is a synchronous operation that only queries file system metadata.
    /// No file content is read, making it very fast even for large files.
    /// </remarks>
    public FileMetadata GetMetadata(string filePath)
    {
        var fileInfo = new FileInfo(filePath);

        if (!fileInfo.Exists)
        {
            _logger.LogDebug("File not found: {FilePath}", filePath);
            return FileMetadata.NotFound;
        }

        return new FileMetadata
        {
            Exists = true,
            Size = fileInfo.Length,
            // LOGIC: Use UTC to ensure consistent timestamps across time zones.
            LastModified = new DateTimeOffset(fileInfo.LastWriteTimeUtc, TimeSpan.Zero)
        };
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// This method first retrieves metadata synchronously, then computes
    /// the hash asynchronously. The metadata is captured before hash computation
    /// to ensure consistency (the file could change during hash computation).
    /// </para>
    /// <para>
    /// For non-existent files, the hash is <c>null</c> since there is no
    /// content to hash.
    /// </para>
    /// </remarks>
    public async Task<FileMetadataWithHash> GetMetadataWithHashAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var metadata = GetMetadata(filePath);

        // LOGIC: For non-existent files, return metadata with null hash.
        if (!metadata.Exists)
        {
            return new FileMetadataWithHash
            {
                Exists = false,
                Size = 0,
                LastModified = default,
                Hash = null
            };
        }

        // LOGIC: Compute hash for existing files.
        var hash = await ComputeHashAsync(filePath, cancellationToken);

        return new FileMetadataWithHash
        {
            Exists = true,
            Size = metadata.Size,
            LastModified = metadata.LastModified,
            Hash = hash
        };
    }
}
