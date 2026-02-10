namespace Lexichord.Abstractions.Contracts.Editor;

/// <summary>
/// Service for managing document lifecycle in the editor.
/// </summary>
/// <remarks>
/// LOGIC: Central service for:
/// - Opening files from disk into editor tabs
/// - Creating new untitled documents
/// - Saving documents back to disk
/// - Tracking open documents
/// 
/// Integrates with <see cref="Layout.IRegionManager"/> for tab creation
/// and <see cref="MediatR.IMediator"/> for cross-module notifications.
/// </remarks>
public interface IEditorService
{
    /// <summary>
    /// Opens a file from disk as a new document tab.
    /// </summary>
    /// <param name="filePath">Absolute path to the file.</param>
    /// <returns>The created manuscript ViewModel.</returns>
    /// <exception cref="FileNotFoundException">File does not exist.</exception>
    /// <exception cref="IOException">Error reading file.</exception>
    Task<IManuscriptViewModel> OpenDocumentAsync(string filePath);

    /// <summary>
    /// Creates a new untitled document.
    /// </summary>
    /// <param name="title">Optional title (defaults to "Untitled-N").</param>
    /// <returns>The created manuscript ViewModel.</returns>
    Task<IManuscriptViewModel> CreateDocumentAsync(string? title = null);

    /// <summary>
    /// Saves a document to disk.
    /// </summary>
    /// <param name="document">The document to save.</param>
    /// <returns>True if save succeeded; false otherwise.</returns>
    /// <remarks>
    /// For untitled documents, this should trigger a Save As dialog.
    /// </remarks>
    Task<bool> SaveDocumentAsync(IManuscriptViewModel document);

    /// <summary>
    /// Gets all currently open documents.
    /// </summary>
    /// <returns>Read-only list of open documents.</returns>
    IReadOnlyList<IManuscriptViewModel> GetOpenDocuments();

    /// <summary>
    /// Gets a document by its file path.
    /// </summary>
    /// <param name="filePath">The file path to search for.</param>
    /// <returns>The document if found; null otherwise.</returns>
    IManuscriptViewModel? GetDocumentByPath(string filePath);

    /// <summary>
    /// Closes a document.
    /// </summary>
    /// <param name="document">The document to close.</param>
    /// <returns>True if closed; false if cancelled.</returns>
    Task<bool> CloseDocumentAsync(IManuscriptViewModel document);

    #region v0.2.6b Navigation Support

    /// <summary>
    /// Gets a document by its unique identifier.
    /// </summary>
    /// <param name="documentId">The document ID to search for.</param>
    /// <returns>The document if found; null otherwise.</returns>
    /// <remarks>
    /// LOGIC: Supports navigation from Problems Panel where only
    /// the document ID is available (not the file path).
    ///
    /// Version: v0.2.6b
    /// </remarks>
    IManuscriptViewModel? GetDocumentById(string documentId);

    /// <summary>
    /// Activates (brings to front) an open document tab.
    /// </summary>
    /// <param name="document">The document to activate.</param>
    /// <returns>True if activation succeeded; false otherwise.</returns>
    /// <remarks>
    /// LOGIC: Used during navigation to ensure the target document
    /// is visible before scrolling/highlighting.
    ///
    /// Version: v0.2.6b
    /// </remarks>
    Task<bool> ActivateDocumentAsync(IManuscriptViewModel document);

    #endregion

    #region v0.6.7a Selection Context

    /// <summary>
    /// Gets the currently selected text in the active document.
    /// </summary>
    /// <returns>The selected text, or null if no text is selected.</returns>
    /// <remarks>
    /// LOGIC: Returns the text currently highlighted by the user in the
    /// active editor tab. Used by <c>SelectionContextService</c> to
    /// retrieve selection for Co-pilot context.
    ///
    /// Version: v0.6.7a
    /// </remarks>
    string? GetSelectedText();

    /// <summary>
    /// Event raised when the text selection changes in the active document.
    /// </summary>
    /// <remarks>
    /// LOGIC: Fired whenever the user changes their text selection
    /// (including clearing the selection). Used by <c>SelectionContextService</c>
    /// to detect stale selection context.
    ///
    /// Version: v0.6.7a
    /// </remarks>
    event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    /// <summary>
    /// Registers a context menu item in the editor's right-click menu.
    /// </summary>
    /// <param name="item">The context menu item to register.</param>
    /// <remarks>
    /// LOGIC: Menu items are grouped by <see cref="ContextMenuItem.Group"/>
    /// and ordered by <see cref="ContextMenuItem.Order"/> within each group.
    ///
    /// Version: v0.6.7a
    /// </remarks>
    void RegisterContextMenuItem(ContextMenuItem item);

    #endregion
}
