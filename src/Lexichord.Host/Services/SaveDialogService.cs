using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Lexichord.Abstractions.Layout;
using Lexichord.Abstractions.Services;
using Lexichord.Host.ViewModels;
using Lexichord.Host.Views;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Services;

/// <summary>
/// Implementation of <see cref="ISaveDialogService"/> using Avalonia dialogs.
/// </summary>
public sealed class SaveDialogService : ISaveDialogService
{
    private readonly ILogger<SaveDialogService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaveDialogService"/> class.
    /// </summary>
    public SaveDialogService(ILogger<SaveDialogService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SaveDialogResult> ShowSaveDialogAsync(string documentTitle)
    {
        _logger.LogDebug("Showing save dialog for: {DocumentTitle}", documentTitle);

        var viewModel = new SaveConfirmationViewModel
        {
            DocumentTitle = documentTitle
        };

        var dialog = new SaveConfirmationDialog(viewModel);

        // Get the main window as the owner
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow;
            if (mainWindow != null)
            {
                await dialog.ShowDialog(mainWindow);
            }
            else
            {
                dialog.Show();
                await WaitForDialogClose(viewModel);
            }
        }
        else
        {
            // Fallback for non-desktop scenarios
            dialog.Show();
            await WaitForDialogClose(viewModel);
        }

        _logger.LogDebug("Save dialog result for {DocumentTitle}: {Result}", documentTitle, viewModel.Result);
        return viewModel.Result;
    }

    private static async Task WaitForDialogClose(SaveConfirmationViewModel viewModel)
    {
        while (!viewModel.IsComplete)
        {
            await Task.Delay(50);
        }
    }
}
