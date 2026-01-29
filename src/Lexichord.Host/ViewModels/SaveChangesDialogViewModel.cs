using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.ViewModels;

namespace Lexichord.Host.ViewModels;

/// <summary>
/// View model for the SaveChangesDialog.
/// </summary>
/// <remarks>
/// LOGIC: Displays a list of dirty documents and offers three choices:
/// - Save All: Save each document, then close
/// - Discard All: Close without saving
/// - Cancel: Abort close, return to editing
///
/// During save, shows progress and handles errors per-document.
/// </remarks>
public partial class SaveChangesDialogViewModel : ObservableObject
{
    private readonly IFileService _fileService;
    private TaskCompletionSource<SaveChangesDialogResult>? _resultSource;

    /// <summary>
    /// Creates a new SaveChangesDialogViewModel.
    /// </summary>
    public SaveChangesDialogViewModel(IFileService fileService)
    {
        _fileService = fileService;
    }

    /// <summary>
    /// Gets or sets the dirty documents to display.
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<DocumentViewModelBase> _dirtyDocuments = [];

    /// <summary>
    /// Gets whether a save operation is in progress.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(DiscardAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isSaving;

    /// <summary>
    /// Gets the current save progress (0-100).
    /// </summary>
    [ObservableProperty]
    private int _saveProgress;

    /// <summary>
    /// Gets the current status message.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "The following documents have unsaved changes:";

    /// <summary>
    /// Gets the currently saving document name.
    /// </summary>
    [ObservableProperty]
    private string? _currentlySaving;

    /// <summary>
    /// Gets any error message to display.
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Gets whether the dialog is complete and result is ready.
    /// </summary>
    public bool IsComplete => _resultSource?.Task.IsCompleted ?? false;

    /// <summary>
    /// Gets the dialog result when complete.
    /// </summary>
    public SaveChangesDialogResult? Result => _resultSource?.Task.IsCompleted == true
        ? _resultSource.Task.Result
        : null;

    /// <summary>
    /// Shows the dialog and returns the result.
    /// </summary>
    /// <param name="dirtyDocuments">The dirty documents.</param>
    /// <returns>The dialog result.</returns>
    public Task<SaveChangesDialogResult> ShowAsync(IReadOnlyList<DocumentViewModelBase> dirtyDocuments)
    {
        DirtyDocuments = dirtyDocuments;
        IsSaving = false;
        SaveProgress = 0;
        ErrorMessage = null;
        StatusMessage = dirtyDocuments.Count == 1
            ? "The following document has unsaved changes:"
            : "The following documents have unsaved changes:";

        _resultSource = new TaskCompletionSource<SaveChangesDialogResult>();
        return _resultSource.Task;
    }

    /// <summary>
    /// Command to save all documents.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteActions))]
    private async Task SaveAllAsync()
    {
        IsSaving = true;
        StatusMessage = "Saving documents...";

        var saved = new List<DocumentViewModelBase>();
        var failed = new List<SaveFailure>();

        for (int i = 0; i < DirtyDocuments.Count; i++)
        {
            var doc = DirtyDocuments[i];
            CurrentlySaving = doc.Title;
            SaveProgress = (int)((i / (float)DirtyDocuments.Count) * 100);

            // Check if document has a file path (using reflection since FilePath is on derived type)
            var filePathProperty = doc.GetType().GetProperty("FilePath");
            var filePath = filePathProperty?.GetValue(doc) as string;

            if (string.IsNullOrEmpty(filePath))
            {
                // Document has not been saved before
                failed.Add(new SaveFailure(doc, new SaveError(
                    SaveErrorCode.InvalidPath,
                    "Document has not been saved before. Use Save As.",
                    null,
                    "Click Cancel and use File > Save As")));
                continue;
            }

            // Get content (using reflection since Content is on derived type)
            var contentProperty = doc.GetType().GetProperty("Content");
            var content = contentProperty?.GetValue(doc) as string ?? string.Empty;

            var result = await _fileService.SaveAsync(
                filePath,
                content,
                Encoding.UTF8);

            if (result.Success)
            {
                // Clear dirty state via the document's method
                await doc.SaveAsync();
                saved.Add(doc);
            }
            else
            {
                failed.Add(new SaveFailure(doc, result.Error!));

                // Show error
                ErrorMessage = $"Failed to save {doc.Title}: {result.Error!.Message}";
            }
        }

        SaveProgress = 100;
        IsSaving = false;

        if (failed.Count == 0)
        {
            _resultSource?.SetResult(new SaveChangesDialogResult
            {
                Action = SaveChangesAction.SaveAll,
                SavedDocuments = saved
            });
        }
        else if (saved.Count > 0)
        {
            // Partial success - some saved, some failed
            StatusMessage = $"Saved {saved.Count} of {DirtyDocuments.Count} documents.";
            ErrorMessage = $"{failed.Count} document(s) could not be saved.";

            _resultSource?.SetResult(new SaveChangesDialogResult
            {
                Action = SaveChangesAction.SaveAll,
                SavedDocuments = saved,
                FailedDocuments = failed
            });
        }
        else
        {
            // All failed - don't complete, let user try again or cancel
            StatusMessage = "Could not save documents.";
            ErrorMessage = failed[0].Error.Message;
        }
    }

    /// <summary>
    /// Command to discard all changes.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteActions))]
    private void DiscardAll()
    {
        _resultSource?.SetResult(new SaveChangesDialogResult
        {
            Action = SaveChangesAction.DiscardAll
        });
    }

    /// <summary>
    /// Command to cancel the close operation.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteActions))]
    private void Cancel()
    {
        _resultSource?.SetResult(new SaveChangesDialogResult
        {
            Action = SaveChangesAction.Cancel
        });
    }

    private bool CanExecuteActions() => !IsSaving;
}
