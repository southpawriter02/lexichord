using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Modules.StatusBar.Services;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.StatusBar.ViewModels;

/// <summary>
/// ViewModel for the API key entry dialog.
/// </summary>
/// <remarks>
/// LOGIC: This dialog allows users to enter an API key when the vault is empty.
/// The key is securely stored in the vault and encrypted at rest.
///
/// Implementation Note: This is a placeholder for v0.0.8a.
/// Full vault integration comes in v0.0.8c.
/// </remarks>
public partial class ApiKeyDialogViewModel : ObservableObject
{
    private readonly IVaultStatusService _vaultStatusService;
    private readonly ILogger _logger;

    public ApiKeyDialogViewModel(
        IVaultStatusService vaultStatusService,
        ILogger logger)
    {
        _vaultStatusService = vaultStatusService;
        _logger = logger;
    }

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _isSaving;

    /// <summary>
    /// Event raised when the dialog should close with a result.
    /// </summary>
    public event EventHandler<bool>? CloseRequested;

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            ErrorMessage = "Please enter an API key";
            HasError = true;
            return;
        }

        try
        {
            IsSaving = true;
            HasError = false;

            await _vaultStatusService.StoreKeyAsync("default-api-key", ApiKey);

            _logger.LogInformation("API key stored successfully");
            CloseRequested?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store API key");
            ErrorMessage = $"Failed to save key: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, false);
    }
}
