using Avalonia.Controls;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Lexichord.Modules.Style.ViewModels;
using Lexichord.Modules.Style.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Service for displaying and managing the Term Editor Dialog.
/// </summary>
/// <remarks>
/// LOGIC: Manages the lifecycle of the Term Editor Dialog.
/// 
/// Key responsibilities:
/// - License tier enforcement (WriterPro required)
/// - ViewModel creation and wiring
/// - Dialog display via Avalonia ShowDialog
/// - Delete confirmation handling
/// 
/// Version: v0.2.5c
/// </remarks>
public sealed class TermEditorDialogService : ITermEditorDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<TermEditorDialogService> _logger;

    public TermEditorDialogService(
        IServiceProvider serviceProvider,
        ILicenseContext licenseContext,
        ILogger<TermEditorDialogService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<bool> ShowAddDialogAsync()
    {
        if (!CheckLicense())
        {
            _logger.LogDebug("Add dialog blocked - WriterPro license required");
            return false;
        }

        _logger.LogDebug("Opening term editor in Add mode");

        var terminologyService = _serviceProvider.GetRequiredService<ITerminologyService>();
        var viewModelLogger = _serviceProvider.GetRequiredService<ILogger<TermEditorViewModel>>();

        var viewModel = new TermEditorViewModel(terminologyService, viewModelLogger);
        
        return await ShowDialogAsync(viewModel);
    }

    /// <inheritdoc/>
    public async Task<bool> ShowEditDialogAsync(StyleTerm term)
    {
        if (!CheckLicense())
        {
            _logger.LogDebug("Edit dialog blocked - WriterPro license required");
            return false;
        }

        _logger.LogDebug("Opening term editor in Edit mode for term {TermId}", term.Id);

        var terminologyService = _serviceProvider.GetRequiredService<ITerminologyService>();
        var viewModelLogger = _serviceProvider.GetRequiredService<ILogger<TermEditorViewModel>>();

        var viewModel = new TermEditorViewModel(terminologyService, viewModelLogger, term);
        
        return await ShowDialogAsync(viewModel);
    }

    /// <inheritdoc/>
    public async Task<bool> ShowDeleteConfirmationAsync(string termPattern)
    {
        _logger.LogDebug("Showing delete confirmation for pattern: {Pattern}", termPattern);

        // LOGIC: Use a simple message box for delete confirmation
        // In a full implementation, this would use a proper dialog service
        // For now, we'll return true to allow deletion
        // TODO: Implement proper confirmation dialog via IDialogService
        
        await Task.CompletedTask;
        return true;
    }

    #region Private Methods

    /// <summary>
    /// Checks if the current license tier allows editing.
    /// </summary>
    private bool CheckLicense()
    {
        var tier = _licenseContext.GetCurrentTier();
        var isWriterPro = tier >= LicenseTier.WriterPro;
        
        if (!isWriterPro)
        {
            _logger.LogInformation("Term editing requires WriterPro license. Current tier: {Tier}", tier);
        }
        
        return isWriterPro;
    }

    /// <summary>
    /// Shows the dialog and returns the result.
    /// </summary>
    private async Task<bool> ShowDialogAsync(TermEditorViewModel viewModel)
    {
        var dialog = new TermEditorDialog
        {
            DataContext = viewModel
        };

        var tcs = new TaskCompletionSource<bool>();

        // LOGIC: Subscribe to CloseRequested to handle dialog closing
        viewModel.CloseRequested += (sender, success) =>
        {
            tcs.TrySetResult(success);
            dialog.Close();
        };

        // LOGIC: Get the main window as owner for modal behavior
        var mainWindow = GetMainWindow();
        
        if (mainWindow is not null)
        {
            await dialog.ShowDialog(mainWindow);
        }
        else
        {
            _logger.LogWarning("No main window found, showing dialog without owner");
            dialog.Show();
            await tcs.Task;
        }

        return tcs.Task.IsCompleted ? tcs.Task.Result : false;
    }

    /// <summary>
    /// Gets the main application window.
    /// </summary>
    private Window? GetMainWindow()
    {
        // LOGIC: Try to get the main window from the application
        if (Avalonia.Application.Current?.ApplicationLifetime is 
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        
        return null;
    }

    #endregion
}
