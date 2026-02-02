// =============================================================================
// File: IndexedDocumentViewModel.cs
// Project: Lexichord.Modules.RAG
// Description: ViewModel for a single indexed document in the Index Status View.
// Version: v0.4.7a
// =============================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts.RAG;

namespace Lexichord.Modules.RAG.ViewModels;

/// <summary>
/// ViewModel representing a single indexed document in the Index Status View.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IndexedDocumentViewModel"/> wraps an <see cref="IndexedDocumentInfo"/> record
/// and provides display-friendly properties for use in the UI, including status text,
/// status colors, and relative time formatting.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7a as part of the Index Status View.
/// </para>
/// </remarks>
public partial class IndexedDocumentViewModel : ObservableObject
{
    private readonly IndexedDocumentInfo _info;
    private readonly Action<Guid>? _reindexAction;
    private readonly Action<Guid>? _retryAction;
    private readonly Action<Guid>? _removeAction;

    /// <summary>
    /// Initializes a new instance of <see cref="IndexedDocumentViewModel"/>.
    /// </summary>
    /// <param name="info">The document info to wrap.</param>
    /// <param name="reindexAction">Optional action to invoke when reindexing.</param>
    /// <param name="retryAction">Optional action to invoke when retrying a failed document.</param>
    /// <param name="removeAction">Optional action to invoke when removing from index.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="info"/> is null.
    /// </exception>
    public IndexedDocumentViewModel(
        IndexedDocumentInfo info,
        Action<Guid>? reindexAction = null,
        Action<Guid>? retryAction = null,
        Action<Guid>? removeAction = null)
    {
        _info = info ?? throw new ArgumentNullException(nameof(info));
        _reindexAction = reindexAction;
        _retryAction = retryAction;
        _removeAction = removeAction;
    }

    #region Document Properties

    /// <summary>
    /// Gets the unique identifier for the document.
    /// </summary>
    public Guid Id => _info.Id;

    /// <summary>
    /// Gets the file name of the document.
    /// </summary>
    public string FileName => _info.FileName;

    /// <summary>
    /// Gets the full file path of the document.
    /// </summary>
    public string FilePath => _info.FilePath;

    /// <summary>
    /// Gets the current indexing status.
    /// </summary>
    public IndexingStatus Status => _info.Status;

    /// <summary>
    /// Gets the number of chunks generated from this document.
    /// </summary>
    public int ChunkCount => _info.ChunkCount;

    /// <summary>
    /// Gets the timestamp when the document was last indexed.
    /// </summary>
    public DateTimeOffset? IndexedAt => _info.IndexedAt;

    /// <summary>
    /// Gets the error message if indexing failed.
    /// </summary>
    public string? ErrorMessage => _info.ErrorMessage;

    #endregion

    #region Display Properties

    /// <summary>
    /// Gets a human-readable status text.
    /// </summary>
    public string StatusDisplay => Status switch
    {
        IndexingStatus.NotIndexed => "Not Indexed",
        IndexingStatus.Pending => "Pending",
        IndexingStatus.Indexing => "Indexing...",
        IndexingStatus.Indexed => "Indexed",
        IndexingStatus.Stale => "Stale",
        IndexingStatus.Failed => "Failed",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets the color key for the status badge.
    /// </summary>
    /// <remarks>
    /// Returns semantic color names that map to theme resources.
    /// </remarks>
    public string StatusColor => Status switch
    {
        IndexingStatus.Indexed => "StatusSuccess",
        IndexingStatus.Pending => "StatusPending",
        IndexingStatus.Indexing => "StatusActive",
        IndexingStatus.Stale => "StatusWarning",
        IndexingStatus.Failed => "StatusError",
        _ => "StatusNeutral"
    };

    /// <summary>
    /// Gets the formatted indexed timestamp.
    /// </summary>
    public string IndexedAtDisplay => IndexedAt?.ToString("g") ?? "Never";

    /// <summary>
    /// Gets a relative time string (e.g., "5 minutes ago").
    /// </summary>
    public string RelativeTime => GetRelativeTime(IndexedAt);

    /// <summary>
    /// Gets the chunk count display text.
    /// </summary>
    public string ChunkCountDisplay => ChunkCount == 1 ? "1 chunk" : $"{ChunkCount} chunks";

    #endregion

    #region State Flags

    /// <summary>
    /// Gets whether the document has an error.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Gets whether the document can be retried (only for failed documents).
    /// </summary>
    public bool CanRetry => Status == IndexingStatus.Failed && _retryAction != null;

    /// <summary>
    /// Gets whether the document can be reindexed (indexed or stale documents).
    /// </summary>
    public bool CanReindex =>
        (Status == IndexingStatus.Indexed || Status == IndexingStatus.Stale)
        && _reindexAction != null;

    /// <summary>
    /// Gets whether the document can be removed from the index.
    /// </summary>
    public bool CanRemove => _removeAction != null;

    #endregion

    #region Commands

    /// <summary>
    /// Command to reindex this document.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanReindex))]
    private void Reindex()
    {
        _reindexAction?.Invoke(Id);
    }

    /// <summary>
    /// Command to retry indexing a failed document.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRetry))]
    private void Retry()
    {
        _retryAction?.Invoke(Id);
    }

    /// <summary>
    /// Command to remove this document from the index.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void Remove()
    {
        _removeAction?.Invoke(Id);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Formats a timestamp as a relative time string.
    /// </summary>
    private static string GetRelativeTime(DateTimeOffset? timestamp)
    {
        if (timestamp is null)
            return "Never";

        var diff = DateTimeOffset.UtcNow - timestamp.Value;

        return diff.TotalSeconds switch
        {
            < 60 => "Just now",
            < 3600 => $"{(int)diff.TotalMinutes} min ago",
            < 86400 => $"{(int)diff.TotalHours} hours ago",
            < 604800 => $"{(int)diff.TotalDays} days ago",
            _ => timestamp.Value.ToString("MMM d, yyyy")
        };
    }

    #endregion
}
