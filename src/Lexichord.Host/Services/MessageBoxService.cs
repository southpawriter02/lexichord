using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Lexichord.Abstractions.Services;
using Lexichord.Host.ViewModels;
using Lexichord.Host.Views;

namespace Lexichord.Host.Services;

/// <summary>
/// Implementation of <see cref="IMessageBoxService"/> using Avalonia dialogs.
/// </summary>
public class MessageBoxService : IMessageBoxService
{
    /// <inheritdoc />
    public async Task ShowMessageAsync(string title, string message)
    {
        var viewModel = new MessageBoxViewModel
        {
            Title = title,
            Message = message
        };

        // UI operations must happen on the UI thread
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var dialog = new MessageBoxDialog(viewModel);

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
                    // In case we don't have a main window, we just show it.
                    // Ideally we should wait, but without ShowDialog returning a Task, it's tricky.
                    // However, ShowDialog(Window) returns a Task.
                }
            }
        });
    }
}
