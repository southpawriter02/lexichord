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
}
