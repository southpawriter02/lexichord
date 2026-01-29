using System;
using Dapper.Contrib.Extensions;

namespace Lexichord.Abstractions.Entities;

/// <summary>
/// Database entity representing a recently opened file.
/// </summary>
/// <remarks>
/// LOGIC: Maps to the "RecentFiles" table created by Migration_20260126001.
///
/// Mapping:
/// - Table: "RecentFiles"
/// - Primary Key: Id (UUID, auto-generated)
/// - Natural Key: FilePath (unique constraint)
///
/// The OpenCount tracks how many times a file has been opened, useful for
/// weighting frequently-accessed files in future smart sorting.
/// </remarks>
[Table("RecentFiles")]
public record RecentFileEntity
{
    /// <summary>
    /// Unique identifier (UUID primary key).
    /// </summary>
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Absolute file path (unique).
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Display name (typically the file name without path).
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// When the file was last opened.
    /// </summary>
    public DateTimeOffset LastOpenedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Number of times the file has been opened.
    /// </summary>
    public int OpenCount { get; init; } = 1;

    /// <summary>
    /// When the entry was created.
    /// </summary>
    [Computed]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the entry was last updated.
    /// </summary>
    [Computed]
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}
