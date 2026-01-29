namespace Lexichord.Abstractions.Layout;

/// <summary>
/// Represents the result of a save confirmation dialog.
/// </summary>
/// <remarks>
/// LOGIC: Used by <see cref="IDocumentTab.CanCloseAsync"/> to determine
/// how to proceed when closing a dirty document.
/// </remarks>
public enum SaveDialogResult
{
    /// <summary>
    /// Save the document before closing.
    /// </summary>
    Save,
    
    /// <summary>
    /// Discard changes and close without saving.
    /// </summary>
    DontSave,
    
    /// <summary>
    /// Cancel the close operation.
    /// </summary>
    Cancel
}
