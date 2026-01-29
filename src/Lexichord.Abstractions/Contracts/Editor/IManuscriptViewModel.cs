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
}
