using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Layout;

namespace Lexichord.Host.ViewModels;

/// <summary>
/// ViewModel for the save confirmation dialog.
/// </summary>
/// <remarks>
/// LOGIC: Provides commands for Save, Don't Save, and Cancel actions.
/// The Result property is set based on user selection.
/// </remarks>
public sealed partial class SaveConfirmationViewModel : ObservableObject
{
    [ObservableProperty]
    private string _documentTitle = string.Empty;

    [ObservableProperty]
    private SaveDialogResult _result = SaveDialogResult.Cancel;

    [ObservableProperty]
    private bool _isComplete;

    /// <summary>
    /// Gets or sets the action to close the dialog.
    /// </summary>
    public Action? CloseAction { get; set; }

    /// <summary>
    /// Command to save the document and close.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        Result = SaveDialogResult.Save;
        IsComplete = true;
        CloseAction?.Invoke();
    }

    /// <summary>
    /// Command to discard changes and close.
    /// </summary>
    [RelayCommand]
    private void DontSave()
    {
        Result = SaveDialogResult.DontSave;
        IsComplete = true;
        CloseAction?.Invoke();
    }

    /// <summary>
    /// Command to cancel the close operation.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        Result = SaveDialogResult.Cancel;
        IsComplete = true;
        CloseAction?.Invoke();
    }
}
