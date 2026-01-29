using Lexichord.Abstractions.Layout;
using Lexichord.Abstractions.Messaging;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Layout;

/// <summary>
/// Implementation of <see cref="ITabService"/> for managing document tabs.
/// </summary>
/// <remarks>
/// LOGIC: Coordinates tab operations between IDocumentTab instances and IRegionManager:
/// - Maintains ordered collection of registered documents
/// - Executes close operations with CanCloseAsync checks
/// - Manages pin state and tab ordering
/// - Publishes lifecycle notifications via MediatR
/// </remarks>
public sealed class TabService : ITabService
{
    private readonly IRegionManager _regionManager;
    private readonly IMediator _mediator;
    private readonly ILogger<TabService> _logger;
    
    private readonly Dictionary<string, IDocumentTab> _documents = new();
    private readonly List<string> _tabOrder = new();
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TabService"/> class.
    /// </summary>
    public TabService(
        IRegionManager regionManager,
        IMediator mediator,
        ILogger<TabService> logger)
    {
        _regionManager = regionManager;
        _mediator = mediator;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> CloseDocumentAsync(string documentId, bool force = false, CancellationToken cancellationToken = default)
    {
        IDocumentTab? document;
        lock (_lock)
        {
            if (!_documents.TryGetValue(documentId, out document))
            {
                _logger.LogWarning("Attempted to close unknown document: {DocumentId}", documentId);
                return false;
            }
        }

        // LOGIC: Check if document allows close (unless forced)
        if (!force && !document.CanClose)
        {
            _logger.LogDebug("Document {DocumentId} does not allow close", documentId);
            return false;
        }

        // LOGIC: Check CanCloseAsync for dirty document handling
        if (!force)
        {
            var canClose = await document.CanCloseAsync();
            if (!canClose)
            {
                _logger.LogDebug("Document {DocumentId} close was cancelled", documentId);
                return false;
            }
        }

        // LOGIC: Publish closing notification
        await _mediator.Publish(new DocumentClosingNotification(documentId, force), cancellationToken);

        // LOGIC: Close via region manager
        var closed = await _regionManager.CloseAsync(documentId, force);
        if (closed)
        {
            UnregisterDocument(documentId);
            await _mediator.Publish(new DocumentClosedNotification(documentId), cancellationToken);
            _logger.LogInformation("Document closed: {DocumentId}", documentId);
        }

        return closed;
    }

    /// <inheritdoc />
    public async Task<bool> CloseAllDocumentsAsync(bool force = false, bool skipPinned = true, CancellationToken cancellationToken = default)
    {
        List<string> documentsToClose;
        lock (_lock)
        {
            documentsToClose = _tabOrder
                .Where(id => !skipPinned || !(_documents.TryGetValue(id, out var doc) && doc.IsPinned))
                .ToList();
        }

        _logger.LogDebug("Closing {Count} documents (force={Force}, skipPinned={SkipPinned})", 
            documentsToClose.Count, force, skipPinned);

        foreach (var documentId in documentsToClose)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            var closed = await CloseDocumentAsync(documentId, force, cancellationToken);
            if (!closed && !force)
            {
                _logger.LogDebug("Close all cancelled at document: {DocumentId}", documentId);
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> CloseAllButThisAsync(string exceptDocumentId, bool force = false, bool skipPinned = true, CancellationToken cancellationToken = default)
    {
        List<string> documentsToClose;
        lock (_lock)
        {
            documentsToClose = _tabOrder
                .Where(id => id != exceptDocumentId)
                .Where(id => !skipPinned || !(_documents.TryGetValue(id, out var doc) && doc.IsPinned))
                .ToList();
        }

        _logger.LogDebug("Closing {Count} documents except {ExceptId}", documentsToClose.Count, exceptDocumentId);

        foreach (var documentId in documentsToClose)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            var closed = await CloseDocumentAsync(documentId, force, cancellationToken);
            if (!closed && !force)
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> CloseToTheRightAsync(string documentId, bool force = false, CancellationToken cancellationToken = default)
    {
        List<string> documentsToClose;
        lock (_lock)
        {
            var index = _tabOrder.IndexOf(documentId);
            if (index < 0)
            {
                _logger.LogWarning("Document not found for close-to-right: {DocumentId}", documentId);
                return false;
            }

            documentsToClose = _tabOrder.Skip(index + 1).ToList();
        }

        _logger.LogDebug("Closing {Count} documents to the right of {DocumentId}", documentsToClose.Count, documentId);

        foreach (var id in documentsToClose)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            var closed = await CloseDocumentAsync(id, force, cancellationToken);
            if (!closed && !force)
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public async Task PinDocumentAsync(string documentId, bool pin)
    {
        IDocumentTab? document;
        lock (_lock)
        {
            if (!_documents.TryGetValue(documentId, out document))
            {
                _logger.LogWarning("Attempted to pin unknown document: {DocumentId}", documentId);
                return;
            }
        }

        document.IsPinned = pin;
        ReorderTabs();

        await _mediator.Publish(new DocumentPinnedNotification(documentId, pin));
        _logger.LogDebug("Document {DocumentId} pinned={Pin}", documentId, pin);
    }

    /// <inheritdoc />
    public Task<bool> ActivateDocumentAsync(string documentId)
    {
        return _regionManager.NavigateToAsync(documentId);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetDirtyDocumentIds()
    {
        lock (_lock)
        {
            return _documents
                .Where(kvp => kvp.Value.IsDirty)
                .Select(kvp => kvp.Key)
                .ToList();
        }
    }

    /// <inheritdoc />
    public bool HasUnsavedChanges()
    {
        lock (_lock)
        {
            return _documents.Values.Any(doc => doc.IsDirty);
        }
    }

    /// <inheritdoc />
    public void RegisterDocument(IDocumentTab document)
    {
        lock (_lock)
        {
            if (_documents.ContainsKey(document.DocumentId))
            {
                _logger.LogWarning("Document already registered: {DocumentId}", document.DocumentId);
                return;
            }

            _documents[document.DocumentId] = document;
            _tabOrder.Add(document.DocumentId);
            
            // Subscribe to state changes
            document.StateChanged += OnDocumentStateChanged;
            
            ReorderTabs();
            _logger.LogDebug("Document registered: {DocumentId}", document.DocumentId);
        }
    }

    /// <inheritdoc />
    public void UnregisterDocument(string documentId)
    {
        lock (_lock)
        {
            if (_documents.TryGetValue(documentId, out var document))
            {
                document.StateChanged -= OnDocumentStateChanged;
                _documents.Remove(documentId);
                _tabOrder.Remove(documentId);
                _logger.LogDebug("Document unregistered: {DocumentId}", documentId);
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetTabOrder()
    {
        lock (_lock)
        {
            return _tabOrder.ToList();
        }
    }

    /// <inheritdoc />
    public IDocumentTab? GetDocument(string documentId)
    {
        lock (_lock)
        {
            return _documents.GetValueOrDefault(documentId);
        }
    }

    private void OnDocumentStateChanged(object? sender, DocumentStateChangedEventArgs e)
    {
        if (sender is IDocumentTab document)
        {
            // LOGIC: Reorder tabs when pinned state changes
            if (e.PropertyName == nameof(IDocumentTab.IsPinned))
            {
                ReorderTabs();
            }

            // LOGIC: Publish dirty state notification
            if (e.PropertyName == nameof(IDocumentTab.IsDirty))
            {
                _ = _mediator.Publish(new DocumentDirtyNotification(document.DocumentId, document.IsDirty));
            }
        }
    }

    private void ReorderTabs()
    {
        // LOGIC: Sort tabs with pinned documents first, maintaining relative order within each group
        lock (_lock)
        {
            var pinned = _tabOrder.Where(id => _documents.TryGetValue(id, out var doc) && doc.IsPinned).ToList();
            var unpinned = _tabOrder.Where(id => !_documents.TryGetValue(id, out var doc) || !doc.IsPinned).ToList();
            
            _tabOrder.Clear();
            _tabOrder.AddRange(pinned);
            _tabOrder.AddRange(unpinned);
        }
    }
}
