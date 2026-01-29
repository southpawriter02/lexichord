namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Event args for dirty state changes.
/// </summary>
/// <remarks>
/// LOGIC: Raised when a document's dirty state transitions between clean and dirty.
/// Subscribers can use this for local UI updates, status bar changes, or logging.
/// </remarks>
public class DirtyStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the document ID.
    /// </summary>
    public required string DocumentId { get; init; }

    /// <summary>
    /// Gets the file path, if the document has been saved.
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Gets the new dirty state.
    /// </summary>
    public required bool IsDirty { get; init; }

    /// <summary>
    /// Gets the timestamp of the state change.
    /// </summary>
    public DateTimeOffset ChangedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Tracks dirty (unsaved changes) state for a document.
/// </summary>
/// <remarks>
/// LOGIC: This interface is implemented by document view models to track
/// whether the document has unsaved changes. The dirty state affects:
/// - Tab title display (adds "*" suffix)
/// - Save command enabled state
/// - Close confirmation workflow
///
/// Implementation Guidelines:
/// - Use debouncing to avoid excessive state changes on rapid input
/// - Clear dirty state only after confirmed successful save
/// - Publish events for cross-module awareness
/// </remarks>
public interface IDirtyStateTracker
{
    /// <summary>
    /// Gets whether the document has unsaved changes.
    /// </summary>
    /// <remarks>
    /// LOGIC: True when content differs from last saved state.
    /// UI binds to this property to show visual indicators.
    /// </remarks>
    bool IsDirty { get; }

    /// <summary>
    /// Gets the hash of the last saved content.
    /// </summary>
    /// <remarks>
    /// LOGIC: Used to detect if undo operations return to saved state.
    /// Stored as SHA256 hash for memory efficiency.
    /// </remarks>
    string? LastSavedContentHash { get; }

    /// <summary>
    /// Marks the document as having unsaved changes.
    /// </summary>
    /// <remarks>
    /// LOGIC: Called when document content changes.
    /// Implementation should debounce rapid calls.
    /// Raises DirtyStateChanged event.
    /// </remarks>
    void MarkDirty();

    /// <summary>
    /// Clears the dirty state after successful save.
    /// </summary>
    /// <remarks>
    /// LOGIC: Called after FileService.SaveAsync returns success.
    /// Updates LastSavedContentHash with current content hash.
    /// Raises DirtyStateChanged event.
    /// </remarks>
    void ClearDirty();

    /// <summary>
    /// Clears the dirty state and updates the saved content hash.
    /// </summary>
    /// <param name="contentHash">Hash of the saved content.</param>
    /// <remarks>
    /// LOGIC: Used when saving to update the comparison baseline.
    /// </remarks>
    void ClearDirty(string contentHash);

    /// <summary>
    /// Checks if current content matches the last saved content.
    /// </summary>
    /// <returns>True if content matches last saved state.</returns>
    /// <remarks>
    /// LOGIC: Used to potentially clear dirty state after undo operations.
    /// Compares content hashes for efficiency.
    /// </remarks>
    bool ContentMatchesLastSaved();

    /// <summary>
    /// Event raised when dirty state changes.
    /// </summary>
    event EventHandler<DirtyStateChangedEventArgs>? DirtyStateChanged;
}
