using Lexichord.Abstractions.ViewModels;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for managing application shutdown with dirty document handling.
/// </summary>
/// <remarks>
/// LOGIC: The shutdown service acts as the central coordinator for application exit.
/// It intercepts close requests, checks for unsaved work, and ensures users have
/// the opportunity to save before closing.
///
/// Integration Points:
/// - MainWindow.Closing event handler
/// - IFileService for save operations
/// - SaveChangesDialog for user interaction
///
/// Thread Safety:
/// - All operations must be safe to call from UI thread
/// - Async methods marshal back to UI thread as needed
/// </remarks>
public interface IShutdownService
{
    /// <summary>
    /// Requests application shutdown, handling dirty documents.
    /// </summary>
    /// <returns>True if shutdown should proceed, false if cancelled by user.</returns>
    /// <remarks>
    /// LOGIC: Call this from Window.Closing event handler.
    /// If dirty documents exist, shows confirmation dialog.
    /// Returns false if user cancels, true otherwise.
    /// </remarks>
    Task<bool> RequestShutdownAsync();

    /// <summary>
    /// Registers a document for shutdown tracking.
    /// </summary>
    /// <param name="document">The document to track.</param>
    /// <remarks>
    /// LOGIC: Called when a document is opened.
    /// Document will be included in dirty checks on shutdown.
    /// </remarks>
    void RegisterDocument(DocumentViewModelBase document);

    /// <summary>
    /// Unregisters a document from shutdown tracking.
    /// </summary>
    /// <param name="document">The document to unregister.</param>
    /// <remarks>
    /// LOGIC: Called when a document is closed.
    /// Document will no longer be checked on shutdown.
    /// </remarks>
    void UnregisterDocument(DocumentViewModelBase document);

    /// <summary>
    /// Gets all documents currently registered for tracking.
    /// </summary>
    /// <returns>All registered documents.</returns>
    IReadOnlyList<DocumentViewModelBase> GetRegisteredDocuments();

    /// <summary>
    /// Gets all documents with unsaved changes.
    /// </summary>
    /// <returns>List of dirty documents.</returns>
    IReadOnlyList<DocumentViewModelBase> GetDirtyDocuments();

    /// <summary>
    /// Gets whether there are any dirty documents.
    /// </summary>
    bool HasDirtyDocuments { get; }

    /// <summary>
    /// Gets whether a shutdown is currently in progress.
    /// </summary>
    bool IsShuttingDown { get; }

    /// <summary>
    /// Event raised when shutdown is requested.
    /// </summary>
    /// <remarks>
    /// LOGIC: Subscribers can use this to perform cleanup
    /// or add additional documents to the dirty list.
    /// Set Cancel = true on args to prevent shutdown.
    /// </remarks>
    event EventHandler<ShutdownRequestedEventArgs>? ShutdownRequested;

    /// <summary>
    /// Event raised when shutdown is about to proceed.
    /// </summary>
    /// <remarks>
    /// LOGIC: Final notification before application exits.
    /// Cannot be cancelled at this point.
    /// </remarks>
    event EventHandler<ShutdownProceedingEventArgs>? ShutdownProceeding;
}

/// <summary>
/// Event args for shutdown request.
/// </summary>
public class ShutdownRequestedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the list of dirty documents at shutdown time.
    /// </summary>
    public required IReadOnlyList<DocumentViewModelBase> DirtyDocuments { get; init; }

    /// <summary>
    /// Gets or sets whether the shutdown should be cancelled.
    /// </summary>
    public bool Cancel { get; set; }

    /// <summary>
    /// Gets the reason for the shutdown request.
    /// </summary>
    public ShutdownReason Reason { get; init; } = ShutdownReason.UserRequested;
}

/// <summary>
/// Event args for shutdown proceeding notification.
/// </summary>
public class ShutdownProceedingEventArgs : EventArgs
{
    /// <summary>
    /// Gets the documents that were saved.
    /// </summary>
    public required IReadOnlyList<DocumentViewModelBase> SavedDocuments { get; init; }

    /// <summary>
    /// Gets the documents that were discarded.
    /// </summary>
    public required IReadOnlyList<DocumentViewModelBase> DiscardedDocuments { get; init; }
}

/// <summary>
/// Reason for shutdown request.
/// </summary>
public enum ShutdownReason
{
    /// <summary>User clicked close or File > Exit.</summary>
    UserRequested,

    /// <summary>System is shutting down or restarting.</summary>
    SystemShutdown,

    /// <summary>Session is ending (logout).</summary>
    SessionEnding,

    /// <summary>Application is restarting (e.g., after update).</summary>
    ApplicationRestart
}
