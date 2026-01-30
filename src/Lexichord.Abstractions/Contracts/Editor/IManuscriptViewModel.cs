using System.Text;
using Lexichord.Abstractions.Layout;

namespace Lexichord.Abstractions.Contracts.Editor;

/// <summary>
/// Contract for manuscript document ViewModels.
/// </summary>
/// <remarks>
/// LOGIC: Extends <see cref="IDocumentTab"/> with manuscript-specific properties:
/// - File system integration (FilePath, FileExtension, Encoding)
/// - Text content and editing operations
/// - Document statistics (line/word/character counts)
/// - Caret and selection tracking
/// 
/// Implementations should extend <see cref="ViewModels.DocumentViewModelBase"/>
/// for dirty state and save dialog integration.
/// </remarks>
public interface IManuscriptViewModel : IDocumentTab
{
    /// <summary>
    /// Gets or sets the file path for this document.
    /// </summary>
    /// <remarks>
    /// Null for untitled documents that haven't been saved yet.
    /// </remarks>
    string? FilePath { get; set; }

    /// <summary>
    /// Gets the file extension including the leading dot.
    /// </summary>
    /// <remarks>
    /// Returns empty string for untitled documents.
    /// </remarks>
    string FileExtension { get; }

    /// <summary>
    /// Gets or sets the document text content.
    /// </summary>
    string Content { get; set; }

    /// <summary>
    /// Gets the file encoding.
    /// </summary>
    Encoding Encoding { get; }

    /// <summary>
    /// Gets the current caret (cursor) position.
    /// </summary>
    CaretPosition CaretPosition { get; }

    /// <summary>
    /// Gets the current text selection.
    /// </summary>
    TextSelection Selection { get; }

    /// <summary>
    /// Gets the number of lines in the document.
    /// </summary>
    int LineCount { get; }

    /// <summary>
    /// Gets the number of words in the document.
    /// </summary>
    int WordCount { get; }

    /// <summary>
    /// Gets the number of characters in the document.
    /// </summary>
    int CharacterCount { get; }

    /// <summary>
    /// Selects text at the specified range.
    /// </summary>
    /// <param name="startOffset">0-based start offset.</param>
    /// <param name="length">Number of characters to select.</param>
    void Select(int startOffset, int length);

    /// <summary>
    /// Scrolls the editor to show the specified line.
    /// </summary>
    /// <param name="line">1-based line number.</param>
    void ScrollToLine(int line);

    /// <summary>
    /// Inserts text at the current caret position.
    /// </summary>
    /// <param name="text">Text to insert.</param>
    void InsertText(string text);

    #region v0.2.6b Navigation Support

    /// <summary>
    /// Sets the caret position to the specified line and column.
    /// </summary>
    /// <param name="line">Target line number (1-indexed).</param>
    /// <param name="column">Target column number (1-indexed).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// LOGIC: Scrolls the editor to show the target line and positions
    /// the caret at the specified column. Async to allow UI thread
    /// marshalling in implementations.
    ///
    /// Version: v0.2.6b
    /// </remarks>
    Task SetCaretPositionAsync(int line, int column);

    /// <summary>
    /// Temporarily highlights a text span with a fade animation.
    /// </summary>
    /// <param name="startOffset">Starting character offset (0-indexed).</param>
    /// <param name="length">Number of characters to highlight.</param>
    /// <param name="duration">Duration of the highlight animation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// LOGIC: Draws a temporary highlight over the specified text span
    /// that fades out over the specified duration. Used to visually
    /// indicate the navigation target to the user.
    ///
    /// Typical usage: 2 second duration with yellow/gold background.
    ///
    /// Version: v0.2.6b
    /// </remarks>
    Task HighlightSpanAsync(int startOffset, int length, TimeSpan duration);

    #endregion
}

