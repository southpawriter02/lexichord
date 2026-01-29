using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.ViewModels;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Services;

/// <summary>
/// Service for managing application shutdown with dirty document handling.
/// </summary>
/// <remarks>
/// LOGIC: Maintains a registry of open documents and intercepts shutdown
/// requests to ensure users can save their work.
///
/// Thread Safety:
/// - Document list is protected by lock
/// - All public methods are safe to call from any thread
/// </remarks>
public sealed class ShutdownService(
    ILogger<ShutdownService> logger) : IShutdownService
{
    private readonly List<DocumentViewModelBase> _documents = [];
    private readonly object _lock = new();
    private bool _isShuttingDown;

    /// <inheritdoc/>
    public event EventHandler<ShutdownRequestedEventArgs>? ShutdownRequested;

    /// <inheritdoc/>
    public event EventHandler<ShutdownProceedingEventArgs>? ShutdownProceeding;

    /// <inheritdoc/>
    public bool HasDirtyDocuments => GetDirtyDocuments().Count > 0;

    /// <inheritdoc/>
    public bool IsShuttingDown => _isShuttingDown;

    /// <inheritdoc/>
    public async Task<bool> RequestShutdownAsync()
    {
        if (_isShuttingDown)
        {
            logger.LogWarning("Shutdown already in progress");
            return false;
        }

        _isShuttingDown = true;
        logger.LogInformation("Shutdown requested");

        try
        {
            var dirtyDocuments = GetDirtyDocuments();

            // Raise ShutdownRequested event
            var args = new ShutdownRequestedEventArgs
            {
                DirtyDocuments = dirtyDocuments,
                Reason = ShutdownReason.UserRequested
            };

            ShutdownRequested?.Invoke(this, args);

            if (args.Cancel)
            {
                logger.LogInformation("Shutdown cancelled by event handler");
                return false;
            }

            // Re-check dirty documents after event (handlers may have changed state)
            dirtyDocuments = GetDirtyDocuments();

            if (dirtyDocuments.Count == 0)
            {
                logger.LogDebug("No dirty documents, proceeding with shutdown");
                OnShutdownProceeding([], []);
                return true;
            }

            logger.LogInformation(
                "Found {Count} dirty documents, dialog will be shown",
                dirtyDocuments.Count);

            // Return true to indicate there are dirty documents that need handling
            // The UI layer will show the dialog based on HasDirtyDocuments
            return await Task.FromResult(true);
        }
        finally
        {
            _isShuttingDown = false;
        }
    }

    /// <inheritdoc/>
    public void RegisterDocument(DocumentViewModelBase document)
    {
        ArgumentNullException.ThrowIfNull(document);

        lock (_lock)
        {
            if (!_documents.Contains(document))
            {
                _documents.Add(document);
                logger.LogDebug(
                    "Registered document: {Title} ({Id})",
                    document.Title, document.DocumentId);
            }
        }
    }

    /// <inheritdoc/>
    public void UnregisterDocument(DocumentViewModelBase document)
    {
        ArgumentNullException.ThrowIfNull(document);

        lock (_lock)
        {
            if (_documents.Remove(document))
            {
                logger.LogDebug(
                    "Unregistered document: {Title} ({Id})",
                    document.Title, document.DocumentId);
            }
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<DocumentViewModelBase> GetRegisteredDocuments()
    {
        lock (_lock)
        {
            return _documents.ToList();
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<DocumentViewModelBase> GetDirtyDocuments()
    {
        lock (_lock)
        {
            return _documents.Where(d => d.IsDirty).ToList();
        }
    }

    /// <summary>
    /// Notifies subscribers that shutdown is proceeding.
    /// </summary>
    /// <param name="saved">Documents that were saved.</param>
    /// <param name="discarded">Documents that were discarded.</param>
    internal void OnShutdownProceeding(
        IReadOnlyList<DocumentViewModelBase> saved,
        IReadOnlyList<DocumentViewModelBase> discarded)
    {
        ShutdownProceeding?.Invoke(this, new ShutdownProceedingEventArgs
        {
            SavedDocuments = saved,
            DiscardedDocuments = discarded
        });
    }
}
