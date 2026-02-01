// =============================================================================
// File: FileMetadata.cs
// Project: Lexichord.Abstractions
// Description: Records for file metadata used in hash-based change detection.
// =============================================================================
// LOGIC: Immutable records for thread-safe file metadata operations.
//   - FileMetadata: Basic file info (existence, size, timestamp).
//   - FileMetadataWithHash: Extends with computed SHA-256 hash.
//   - NotFound static property provides a consistent sentinel value.
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Metadata about a file for change detection.
/// </summary>
/// <remarks>
/// <para>
/// This record captures the essential file attributes needed for efficient
/// change detection in the ingestion pipeline. By comparing size and timestamp
/// before computing hashes, the system can avoid expensive hash computation
/// for obviously changed or unchanged files.
/// </para>
/// <para>
/// <b>Immutability:</b> As a record, this type is immutable and thread-safe,
/// suitable for use in concurrent file watching scenarios.
/// </para>
/// </remarks>
public record FileMetadata
{
    /// <summary>
    /// Gets a value indicating whether the file exists on disk.
    /// </summary>
    /// <remarks>
    /// When <c>false</c>, other properties will have default values:
    /// <see cref="Size"/> will be 0 and <see cref="LastModified"/> will be
    /// <see cref="DateTimeOffset.MinValue"/>.
    /// </remarks>
    public bool Exists { get; init; }

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    /// <remarks>
    /// This is used as the first tier of change detection: if the size differs
    /// from the stored value, the file has definitely changed and hash computation
    /// can be skipped.
    /// </remarks>
    public long Size { get; init; }

    /// <summary>
    /// Gets the last modification timestamp in UTC.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is used as the second tier of change detection: if the timestamp
    /// is unchanged (within a tolerance of ~1 second), the file content is assumed
    /// to be unchanged and hash computation can be skipped.
    /// </para>
    /// <para>
    /// The timestamp is stored as <see cref="DateTimeOffset"/> to preserve
    /// timezone information and ensure consistent comparisons.
    /// </para>
    /// </remarks>
    public DateTimeOffset LastModified { get; init; }

    /// <summary>
    /// Gets a <see cref="FileMetadata"/> instance representing a non-existent file.
    /// </summary>
    /// <remarks>
    /// Use this sentinel value when a file lookup fails rather than returning null.
    /// This follows the Null Object pattern for safer downstream code.
    /// </remarks>
    public static FileMetadata NotFound => new()
    {
        Exists = false,
        Size = 0,
        LastModified = default
    };
}

/// <summary>
/// File metadata with computed SHA-256 hash.
/// </summary>
/// <remarks>
/// <para>
/// This extended record includes the content hash, which is the definitive
/// indicator of file content. Two files with the same hash have identical content,
/// regardless of name, path, or modification time.
/// </para>
/// <para>
/// <b>Hash Format:</b> The hash is a 64-character lowercase hexadecimal string
/// representing the SHA-256 digest of the file content.
/// </para>
/// </remarks>
public record FileMetadataWithHash : FileMetadata
{
    /// <summary>
    /// Gets the SHA-256 hash of the file content.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The hash is represented as a lowercase hexadecimal string of exactly
    /// 64 characters (256 bits / 4 bits per hex digit).
    /// </para>
    /// <para>
    /// This value is <c>null</c> when <see cref="FileMetadata.Exists"/> is <c>false</c>,
    /// as there is no content to hash for non-existent files.
    /// </para>
    /// </remarks>
    /// <example>
    /// A typical hash value: <c>"e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"</c>
    /// (this is the SHA-256 hash of an empty file).
    /// </example>
    public string? Hash { get; init; }
}
