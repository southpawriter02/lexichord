using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.ViewModels;

namespace Lexichord.Host.ViewModels;

/// <summary>
/// Result from the SaveChangesDialog.
/// </summary>
public record SaveChangesDialogResult
{
    /// <summary>
    /// The action chosen by the user.
    /// </summary>
    public required SaveChangesAction Action { get; init; }

    /// <summary>
    /// Documents that were successfully saved (if Save was chosen).
    /// </summary>
    public IReadOnlyList<DocumentViewModelBase> SavedDocuments { get; init; } = [];

    /// <summary>
    /// Documents that failed to save.
    /// </summary>
    public IReadOnlyList<SaveFailure> FailedDocuments { get; init; } = [];

    /// <summary>
    /// Whether all documents were handled successfully.
    /// </summary>
    public bool AllSucceeded => FailedDocuments.Count == 0;
}

/// <summary>
/// User action in save changes dialog.
/// </summary>
public enum SaveChangesAction
{
    /// <summary>Save all dirty documents.</summary>
    SaveAll,

    /// <summary>Discard all changes and close.</summary>
    DiscardAll,

    /// <summary>Cancel the close operation.</summary>
    Cancel
}

/// <summary>
/// Information about a failed save.
/// </summary>
/// <param name="Document">The document that failed to save.</param>
/// <param name="Error">The error details.</param>
public record SaveFailure(
    DocumentViewModelBase Document,
    SaveError Error
);
