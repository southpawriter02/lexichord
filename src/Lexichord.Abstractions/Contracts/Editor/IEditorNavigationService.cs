namespace Lexichord.Abstractions.Contracts.Editor;

#region Navigation Result

/// <summary>
/// Represents the result of a navigation operation.
/// </summary>
/// <remarks>
/// LOGIC: Provides structured feedback for navigation attempts.
/// Success indicates the editor scrolled to and highlighted the target.
/// Failure includes an error message explaining why navigation failed.
///
/// Version: v0.2.6b
/// </remarks>
/// <param name="Success">Whether the navigation completed successfully.</param>
/// <param name="ErrorMessage">Optional error message if navigation failed.</param>
/// <param name="DocumentId">The document that was navigated to, if successful.</param>
public record NavigationResult(
    bool Success,
    string? ErrorMessage = null,
    string? DocumentId = null)
{
    /// <summary>
    /// Creates a successful navigation result.
    /// </summary>
    /// <param name="documentId">The document that was navigated to.</param>
    /// <returns>A success result with the document ID.</returns>
    public static NavigationResult Succeeded(string documentId) =>
        new(true, null, documentId);

    /// <summary>
    /// Creates a failed navigation result.
    /// </summary>
    /// <param name="errorMessage">The reason navigation failed.</param>
    /// <returns>A failure result with the error message.</returns>
    public static NavigationResult Failed(string errorMessage) =>
        new(false, errorMessage, null);
}

#endregion

#region Navigation Service Interface

/// <summary>
/// Service for navigating to specific locations in documents.
/// </summary>
/// <remarks>
/// LOGIC: Coordinates navigation from external sources (Problems Panel,
/// search results, bookmarks) to editor locations.
///
/// Navigation Flow:
/// 1. Resolve document by ID
/// 2. Activate document tab if not active
/// 3. Scroll to target line (centered in viewport)
/// 4. Position caret at target column
/// 5. Apply temporary highlight animation
/// 6. Focus editor for immediate editing
///
/// Thread Safety: All operations marshal to UI thread internally.
///
/// Version: v0.2.6b
/// </remarks>
public interface IEditorNavigationService
{
    /// <summary>
    /// Navigates to a specific location in a document.
    /// </summary>
    /// <param name="documentId">The unique identifier of the target document.</param>
    /// <param name="line">Target line number (1-indexed).</param>
    /// <param name="column">Target column number (1-indexed).</param>
    /// <param name="length">Length of text to highlight (for span highlighting).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A <see cref="NavigationResult"/> indicating success or failure.</returns>
    /// <remarks>
    /// LOGIC: Orchestrates the complete navigation flow:
    /// 1. Get document from IEditorService.GetDocumentById
    /// 2. Activate document tab via IEditorService.ActivateDocumentAsync
    /// 3. Scroll editor to target line (centered)
    /// 4. Set caret to target column
    /// 5. Apply temporary highlight animation (2 seconds fade)
    /// 6. Return success/failure result
    ///
    /// Common failure scenarios:
    /// - Document not found (closed or never opened)
    /// - Document activation failed (tab system error)
    /// - Cancellation requested
    ///
    /// Version: v0.2.6b
    /// </remarks>
    Task<NavigationResult> NavigateToViolationAsync(
        string documentId,
        int line,
        int column,
        int length,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Navigates to a document offset with highlighting.
    /// </summary>
    /// <param name="documentId">The unique identifier of the target document.</param>
    /// <param name="startOffset">Starting character offset (0-indexed).</param>
    /// <param name="length">Length of text to highlight.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A <see cref="NavigationResult"/> indicating success or failure.</returns>
    /// <remarks>
    /// LOGIC: Alternative navigation method using absolute offsets.
    /// Useful when line/column are not readily available.
    ///
    /// Version: v0.2.6b
    /// </remarks>
    Task<NavigationResult> NavigateToOffsetAsync(
        string documentId,
        int startOffset,
        int length,
        CancellationToken cancellationToken = default);
}

#endregion
