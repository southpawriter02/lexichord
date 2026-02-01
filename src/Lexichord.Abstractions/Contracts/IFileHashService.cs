// =============================================================================
// File: IFileHashService.cs
// Project: Lexichord.Abstractions
// Description: Interface for computing and comparing file content hashes.
// =============================================================================
// LOGIC: Central abstraction for hash-based change detection in the RAG pipeline.
//   - ComputeHashAsync: SHA-256 streaming computation for large files.
//   - HasChangedAsync: Tiered detection (size → timestamp → hash).
//   - GetMetadata: Quick file info without hash computation.
//   - GetMetadataWithHashAsync: Combined metadata + hash in single operation.
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for computing and comparing file content hashes.
/// </summary>
/// <remarks>
/// <para>
/// This service provides SHA-256 hash computation for file content verification
/// and change detection. It is a core component of the ingestion pipeline,
/// enabling efficient re-indexing by identifying unchanged files.
/// </para>
/// <para>
/// <b>Tiered Detection Strategy:</b> The <see cref="HasChangedAsync"/> method
/// uses a three-tier approach to minimize expensive hash computation:
/// </para>
/// <list type="number">
///   <item>
///     <description>
///       <b>Size check:</b> If the file size differs from the stored value,
///       the file has definitely changed (no hash needed).
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Timestamp check:</b> If the modification timestamp is unchanged
///       (within ~1 second tolerance), the file is assumed unchanged (no hash needed).
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Hash check:</b> Only if size matches and timestamp differs,
///       compute the full SHA-256 hash for definitive comparison.
///     </description>
///   </item>
/// </list>
/// <para>
/// <b>Memory Efficiency:</b> Hash computation uses streaming to support large
/// files without loading entire content into memory. The default buffer size
/// is 80KB, optimized for modern file system block sizes.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe for use with
/// concurrent file watchers and parallel ingestion operations.
/// </para>
/// </remarks>
public interface IFileHashService
{
    /// <summary>
    /// Computes the SHA-256 hash of a file's content.
    /// </summary>
    /// <param name="filePath">
    /// The absolute path to the file to hash. Must be a valid, accessible file.
    /// </param>
    /// <param name="cancellationToken">
    /// Token to cancel the operation. Useful for aborting hash computation
    /// of large files during application shutdown.
    /// </param>
    /// <returns>
    /// A lowercase hexadecimal string of exactly 64 characters representing
    /// the SHA-256 digest of the file content.
    /// </returns>
    /// <exception cref="FileNotFoundException">
    /// Thrown when the specified file does not exist at <paramref name="filePath"/>.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown when the file cannot be read (e.g., locked by another process,
    /// insufficient permissions, or I/O error).
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken"/> is cancelled during computation.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The hash is computed using streaming to avoid loading the entire file
    /// into memory. This makes it safe for use with files of any size.
    /// </para>
    /// <para>
    /// The file is opened with <see cref="FileShare.Read"/> to allow concurrent
    /// read access by other processes.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var hash = await hashService.ComputeHashAsync("/path/to/file.md");
    /// // hash: "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
    /// </code>
    /// </example>
    Task<string> ComputeHashAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if a file has changed since it was last indexed.
    /// </summary>
    /// <param name="filePath">
    /// The absolute path to the file to check.
    /// </param>
    /// <param name="storedHash">
    /// The SHA-256 hash that was computed when the file was last indexed.
    /// </param>
    /// <param name="storedSize">
    /// The file size in bytes when it was last indexed.
    /// </param>
    /// <param name="storedModified">
    /// The modification timestamp when the file was last indexed.
    /// May be <c>null</c> if not previously recorded.
    /// </param>
    /// <param name="cancellationToken">
    /// Token to cancel the operation.
    /// </param>
    /// <returns>
    /// <c>true</c> if the file content has changed (or file was deleted);
    /// <c>false</c> if the file content is unchanged.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method uses a tiered detection strategy to minimize hash computation:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       If the file no longer exists, returns <c>true</c> (deleted = changed).
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       If the file size differs from <paramref name="storedSize"/>,
    ///       returns <c>true</c> without computing hash.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       If the modification timestamp matches <paramref name="storedModified"/>
    ///       (within 1 second), returns <c>false</c> without computing hash.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Otherwise, computes the full hash and compares with <paramref name="storedHash"/>.
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    Task<bool> HasChangedAsync(
        string filePath,
        string storedHash,
        long storedSize,
        DateTimeOffset? storedModified,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets file metadata without computing the hash.
    /// </summary>
    /// <param name="filePath">
    /// The absolute path to the file.
    /// </param>
    /// <returns>
    /// A <see cref="FileMetadata"/> instance with the file's existence status,
    /// size, and modification timestamp. Returns <see cref="FileMetadata.NotFound"/>
    /// if the file does not exist.
    /// </returns>
    /// <remarks>
    /// This is a fast, synchronous operation that only queries the file system
    /// for basic attributes. No file content is read.
    /// </remarks>
    FileMetadata GetMetadata(string filePath);

    /// <summary>
    /// Gets file metadata with the computed SHA-256 hash in a single operation.
    /// </summary>
    /// <param name="filePath">
    /// The absolute path to the file.
    /// </param>
    /// <param name="cancellationToken">
    /// Token to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="FileMetadataWithHash"/> instance containing the file's
    /// existence status, size, modification timestamp, and SHA-256 hash.
    /// If the file does not exist, <see cref="FileMetadataWithHash.Hash"/> is <c>null</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method combines <see cref="GetMetadata"/> and <see cref="ComputeHashAsync"/>
    /// into a single operation, ensuring atomicity of the metadata snapshot.
    /// </para>
    /// <para>
    /// Use this method when you need both metadata and hash, as it avoids
    /// the race condition of calling the methods separately.
    /// </para>
    /// </remarks>
    Task<FileMetadataWithHash> GetMetadataWithHashAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}
