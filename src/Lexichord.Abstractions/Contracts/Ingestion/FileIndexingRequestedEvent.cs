// =============================================================================
// File: FileIndexingRequestedEvent.cs
// Project: Lexichord.Abstractions
// Description: Event published when a file change requires re-indexing.
// =============================================================================
// LOGIC: Published by FileWatcherIngestionHandler when file watcher detects
//   a file that needs to be added/updated in the RAG index.
//   - Only published for Created, Changed, or Renamed events (not Deleted).
//   - Deleted files are handled separately (document removal flow).
//   - Consumers can process this event to trigger ingestion pipeline.
// =============================================================================

using MediatR;

namespace Lexichord.Abstractions.Contracts.Ingestion;

/// <summary>
/// Specifies the type of file change that triggered an indexing request.
/// </summary>
/// <remarks>
/// <para>
/// This enum mirrors a subset of <see cref="FileSystemChangeType"/> but only
/// includes change types that require re-indexing. <see cref="FileSystemChangeType.Deleted"/>
/// is not included as deleted files are handled through the document removal flow
/// rather than re-indexing.
/// </para>
/// </remarks>
public enum FileIndexingChangeType
{
    /// <summary>
    /// A new file was created and needs initial indexing.
    /// </summary>
    Created,

    /// <summary>
    /// An existing file was modified and needs re-indexing.
    /// </summary>
    Changed,

    /// <summary>
    /// A file was renamed and needs index metadata update.
    /// </summary>
    Renamed
}

/// <summary>
/// Event published when a file system change requires RAG indexing.
/// </summary>
/// <param name="FilePath">
/// The absolute path to the file that requires indexing.
/// For rename operations, this is the new path.
/// </param>
/// <param name="ChangeType">
/// The type of change that triggered this indexing request.
/// </param>
/// <param name="OldPath">
/// For rename operations, the previous file path. Null for other change types.
/// </param>
/// <param name="Timestamp">
/// UTC timestamp when the change was detected.
/// </param>
/// <remarks>
/// <para>
/// This notification is published by the <c>FileWatcherIngestionHandler</c>
/// when the file watcher detects changes to files matching the configured
/// supported extensions (e.g., .md, .txt, .json, .yaml).
/// </para>
/// <para>
/// <b>Change Types:</b>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="FileIndexingChangeType.Created"/>: New file needs initial indexing.</description></item>
///   <item><description><see cref="FileIndexingChangeType.Changed"/>: Existing file content was modified.</description></item>
///   <item><description><see cref="FileIndexingChangeType.Renamed"/>: File was renamed; index metadata update needed.</description></item>
/// </list>
/// <para>
/// <b>Threading:</b> This event may be published from a background thread.
/// Handlers should marshal to the appropriate context if needed.
/// </para>
/// </remarks>
public sealed record FileIndexingRequestedEvent(
    string FilePath,
    FileIndexingChangeType ChangeType,
    string? OldPath,
    DateTimeOffset Timestamp
) : INotification
{
    /// <summary>
    /// Creates a new <see cref="FileIndexingRequestedEvent"/> for a file creation.
    /// </summary>
    /// <param name="filePath">The absolute path to the created file.</param>
    /// <returns>A new event instance with <see cref="FileIndexingChangeType.Created"/>.</returns>
    public static FileIndexingRequestedEvent ForCreated(string filePath)
    {
        return new FileIndexingRequestedEvent(
            filePath,
            FileIndexingChangeType.Created,
            OldPath: null,
            DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Creates a new <see cref="FileIndexingRequestedEvent"/> for a file modification.
    /// </summary>
    /// <param name="filePath">The absolute path to the modified file.</param>
    /// <returns>A new event instance with <see cref="FileIndexingChangeType.Changed"/>.</returns>
    public static FileIndexingRequestedEvent ForChanged(string filePath)
    {
        return new FileIndexingRequestedEvent(
            filePath,
            FileIndexingChangeType.Changed,
            OldPath: null,
            DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Creates a new <see cref="FileIndexingRequestedEvent"/> for a file rename.
    /// </summary>
    /// <param name="newPath">The new absolute path to the renamed file.</param>
    /// <param name="oldPath">The previous absolute path before rename.</param>
    /// <returns>A new event instance with <see cref="FileIndexingChangeType.Renamed"/>.</returns>
    public static FileIndexingRequestedEvent ForRenamed(string newPath, string oldPath)
    {
        return new FileIndexingRequestedEvent(
            newPath,
            FileIndexingChangeType.Renamed,
            oldPath,
            DateTimeOffset.UtcNow);
    }
}
