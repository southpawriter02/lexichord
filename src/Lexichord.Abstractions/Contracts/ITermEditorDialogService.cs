using Lexichord.Abstractions.Entities;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service interface for displaying and managing the Term Editor Dialog.
/// </summary>
/// <remarks>
/// LOGIC: Provides a clean abstraction for modal dialog operations.
/// 
/// Design decisions:
/// - License enforcement (WriterPro required) is handled internally
/// - Returns bool indicating if the operation was completed (true = saved)
/// - Async operations support cancellation
/// 
/// Version: v0.2.5c
/// </remarks>
public interface ITermEditorDialogService
{
    /// <summary>
    /// Shows the dialog in "Add" mode for creating a new term.
    /// </summary>
    /// <returns>True if a term was created, false if cancelled or license check failed.</returns>
    /// <remarks>
    /// LOGIC: Checks license tier before showing dialog.
    /// If not WriterPro, returns false immediately.
    /// </remarks>
    Task<bool> ShowAddDialogAsync();

    /// <summary>
    /// Shows the dialog in "Edit" mode for modifying an existing term.
    /// </summary>
    /// <param name="term">The term to edit.</param>
    /// <returns>True if the term was updated, false if cancelled or license check failed.</returns>
    /// <remarks>
    /// LOGIC: Pre-populates form fields with existing term data.
    /// Checks license tier before showing dialog.
    /// </remarks>
    Task<bool> ShowEditDialogAsync(StyleTerm term);

    /// <summary>
    /// Shows a confirmation dialog before deleting a term.
    /// </summary>
    /// <param name="termPattern">The term pattern being deleted (for display).</param>
    /// <returns>True if user confirmed deletion, false if cancelled.</returns>
    Task<bool> ShowDeleteConfirmationAsync(string termPattern);
}
