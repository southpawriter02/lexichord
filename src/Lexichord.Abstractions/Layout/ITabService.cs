namespace Lexichord.Abstractions.Layout;

/// <summary>
/// Service for managing document tab operations.
/// </summary>
/// <remarks>
/// LOGIC: Acts as a coordinator between individual <see cref="IDocumentTab"/> instances
/// and the <see cref="IRegionManager"/>. Key responsibilities:
/// - Tab lifecycle (register, unregister, close)
/// - Batch close operations (close all, close all but this)
/// - Pin state management
/// - Tab ordering (pinned tabs first)
/// - MediatR event publishing for lifecycle events
/// </remarks>
public interface ITabService
{
    /// <summary>
    /// Closes the specified document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="force">If true, bypasses CanCloseAsync check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the document was closed; false otherwise.</returns>
    /// <remarks>
    /// LOGIC: When force is false:
    /// 1. Calls IDocumentTab.CanCloseAsync()
    /// 2. If false, aborts close
    /// 3. If true, removes from IRegionManager
    /// 
    /// Publishes DocumentClosingNotification before and DocumentClosedNotification after.
    /// </remarks>
    Task<bool> CloseDocumentAsync(string documentId, bool force = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes all documents.
    /// </summary>
    /// <param name="force">If true, bypasses CanCloseAsync for all documents.</param>
    /// <param name="skipPinned">If true, skips pinned documents.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if all targeted documents were closed; false if any refused.</returns>
    /// <remarks>
    /// LOGIC: Iterates through all registered documents. If any document's CanCloseAsync
    /// returns false (and not forced), stops and returns false.
    /// </remarks>
    Task<bool> CloseAllDocumentsAsync(bool force = false, bool skipPinned = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes all documents except the specified one.
    /// </summary>
    /// <param name="exceptDocumentId">The document to keep open.</param>
    /// <param name="force">If true, bypasses CanCloseAsync for all documents.</param>
    /// <param name="skipPinned">If true, skips pinned documents.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if all targeted documents were closed; false if any refused.</returns>
    Task<bool> CloseAllButThisAsync(string exceptDocumentId, bool force = false, bool skipPinned = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes all documents to the right of the specified document.
    /// </summary>
    /// <param name="documentId">The reference document.</param>
    /// <param name="force">If true, bypasses CanCloseAsync check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if all targeted documents were closed; false if any refused.</returns>
    Task<bool> CloseToTheRightAsync(string documentId, bool force = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pins or unpins a document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="pin">True to pin; false to unpin.</param>
    /// <returns>A task representing the operation.</returns>
    /// <remarks>
    /// LOGIC: Updates IDocumentTab.IsPinned and reorders tabs.
    /// Publishes DocumentPinnedNotification.
    /// </remarks>
    Task PinDocumentAsync(string documentId, bool pin);

    /// <summary>
    /// Activates the specified document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <returns>True if the document was activated; false if not found.</returns>
    Task<bool> ActivateDocumentAsync(string documentId);

    /// <summary>
    /// Gets the IDs of all dirty documents.
    /// </summary>
    /// <returns>The document IDs with unsaved changes.</returns>
    IReadOnlyList<string> GetDirtyDocumentIds();

    /// <summary>
    /// Checks if there are any documents with unsaved changes.
    /// </summary>
    /// <returns>True if any document is dirty; false otherwise.</returns>
    bool HasUnsavedChanges();

    /// <summary>
    /// Registers a document with the tab service.
    /// </summary>
    /// <param name="document">The document to register.</param>
    /// <remarks>
    /// LOGIC: Called by IRegionManager when a document is opened.
    /// Subscribes to StateChanged events for tracking.
    /// </remarks>
    void RegisterDocument(IDocumentTab document);

    /// <summary>
    /// Unregisters a document from the tab service.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <remarks>
    /// LOGIC: Called by IRegionManager when a document is closed.
    /// Unsubscribes from StateChanged events.
    /// </remarks>
    void UnregisterDocument(string documentId);

    /// <summary>
    /// Gets the ordered list of document IDs.
    /// </summary>
    /// <remarks>
    /// LOGIC: Returns documents in display order (pinned first, then by open order).
    /// </remarks>
    IReadOnlyList<string> GetTabOrder();

    /// <summary>
    /// Gets a registered document by ID.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <returns>The document if found; null otherwise.</returns>
    IDocumentTab? GetDocument(string documentId);
}
