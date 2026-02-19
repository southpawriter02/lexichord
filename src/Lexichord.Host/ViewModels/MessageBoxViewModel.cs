using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Lexichord.Host.ViewModels;

/// <summary>
/// ViewModel for a simple message box dialog.
/// </summary>
public partial class MessageBoxViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _message = string.Empty;

    /// <summary>
    /// Action to request closing the dialog.
    /// </summary>
    public Action? CloseAction { get; set; }

    /// <summary>
    /// Command executed when the OK button is clicked.
    /// </summary>
    [RelayCommand]
    private void Ok()
    {
        CloseAction?.Invoke();
    }
}
