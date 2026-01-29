namespace Lexichord.Abstractions.Services;

/// <summary>
/// Service for showing save confirmation dialogs.
/// </summary>
/// <remarks>
/// LOGIC: Abstracts the UI dialog from document logic, enabling:
/// - Testability via mock implementations
/// - Platform-specific dialog implementations
/// - Consistent save prompts across the application
/// </remarks>
public interface ISaveDialogService
{
    /// <summary>
    /// Shows a save confirmation dialog for the specified document.
    /// </summary>
    /// <param name="documentTitle">The title of the document to display.</param>
    /// <returns>The user's choice: Save, DontSave, or Cancel.</returns>
    /// <remarks>
    /// LOGIC: The dialog should present three options:
    /// - Save: User wants to save changes before closing
    /// - Don't Save: User wants to discard changes
    /// - Cancel: User wants to abort the close operation
    /// </remarks>
    Task<Layout.SaveDialogResult> ShowSaveDialogAsync(string documentTitle);
}
