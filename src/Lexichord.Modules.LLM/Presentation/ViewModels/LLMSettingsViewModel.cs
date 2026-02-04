// -----------------------------------------------------------------------
// <copyright file="LLMSettingsViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Abstractions.Contracts.Security;
using Lexichord.Modules.LLM.Logging;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.LLM.Presentation.ViewModels;

/// <summary>
/// ViewModel for the LLM Settings page in the application settings dialog.
/// </summary>
/// <remarks>
/// <para>
/// This ViewModel manages the overall LLM provider settings, including:
/// </para>
/// <list type="bullet">
///   <item><description>Display of available providers</description></item>
///   <item><description>Provider selection and navigation</description></item>
///   <item><description>API key management via <see cref="ProviderConfigViewModel"/></description></item>
///   <item><description>Connection testing</description></item>
///   <item><description>Default provider selection</description></item>
/// </list>
/// <para>
/// <b>License Gating:</b> The settings page is visible to all users (Core tier),
/// but API key configuration requires WriterPro or higher.
/// </para>
/// <para>
/// <b>Version:</b> v0.6.1d
/// </para>
/// </remarks>
public sealed partial class LLMSettingsViewModel : ObservableObject, IDisposable
{
    /// <summary>
    /// The minimum license tier required to configure LLM providers.
    /// </summary>
    /// <remarks>
    /// Users below this tier can view the settings page but cannot save API keys
    /// or set default providers.
    /// </remarks>
    public const LicenseTier RequiredTierForConfiguration = LicenseTier.WriterPro;

    private readonly ILLMProviderRegistry _registry;
    private readonly ISecureVault _vault;
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<LLMSettingsViewModel> _logger;
    private readonly ILogger<ProviderConfigViewModel> _providerConfigLogger;
    private bool _isDisposed;

    // =========================================================================
    // Observable Properties
    // =========================================================================

    /// <summary>
    /// Gets the collection of provider configuration ViewModels.
    /// </summary>
    /// <remarks>
    /// Each item represents a registered LLM provider with its configuration state.
    /// The collection is populated during <see cref="LoadProvidersAsync"/>.
    /// </remarks>
    [ObservableProperty]
    private ObservableCollection<ProviderConfigViewModel> _providers = [];

    /// <summary>
    /// Gets or sets the currently selected provider for configuration.
    /// </summary>
    /// <remarks>
    /// The right panel displays the configuration UI for this provider.
    /// Changing selection updates the detail view.
    /// </remarks>
    [ObservableProperty]
    private ProviderConfigViewModel? _selectedProvider;

    /// <summary>
    /// Gets a value indicating whether the ViewModel is performing an async operation.
    /// </summary>
    /// <remarks>
    /// When true, UI controls that trigger operations should be disabled.
    /// </remarks>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>
    /// Gets an error message to display in the UI, if any.
    /// </summary>
    /// <remarks>
    /// This is set when an operation fails. Clear by setting to null.
    /// </remarks>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Gets the name of the default provider.
    /// </summary>
    /// <remarks>
    /// This is updated after <see cref="SetAsDefaultCommand"/> executes
    /// and during initial load.
    /// </remarks>
    [ObservableProperty]
    private string? _defaultProviderName;

    // =========================================================================
    // Constructor
    // =========================================================================

    /// <summary>
    /// Initializes a new instance of the <see cref="LLMSettingsViewModel"/> class.
    /// </summary>
    /// <param name="registry">The LLM provider registry.</param>
    /// <param name="vault">The secure vault for API key storage.</param>
    /// <param name="licenseContext">The license context for tier checking.</param>
    /// <param name="logger">The logger for this ViewModel.</param>
    /// <param name="providerConfigLogger">The logger for child ProviderConfigViewModels.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public LLMSettingsViewModel(
        ILLMProviderRegistry registry,
        ISecureVault vault,
        ILicenseContext licenseContext,
        ILogger<LLMSettingsViewModel> logger,
        ILogger<ProviderConfigViewModel> providerConfigLogger)
    {
        ArgumentNullException.ThrowIfNull(registry, nameof(registry));
        ArgumentNullException.ThrowIfNull(vault, nameof(vault));
        ArgumentNullException.ThrowIfNull(licenseContext, nameof(licenseContext));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        ArgumentNullException.ThrowIfNull(providerConfigLogger, nameof(providerConfigLogger));

        _registry = registry;
        _vault = vault;
        _licenseContext = licenseContext;
        _logger = logger;
        _providerConfigLogger = providerConfigLogger;
    }

    // =========================================================================
    // Computed Properties
    // =========================================================================

    /// <summary>
    /// Gets the current license tier.
    /// </summary>
    /// <remarks>
    /// Exposed for binding to show tier-specific UI elements.
    /// </remarks>
    public LicenseTier CurrentTier => _licenseContext.GetCurrentTier();

    /// <summary>
    /// Gets a value indicating whether the user can configure providers.
    /// </summary>
    /// <remarks>
    /// Returns true if the user has WriterPro tier or higher.
    /// When false, configuration controls should be disabled.
    /// </remarks>
    public bool CanConfigure => CurrentTier >= RequiredTierForConfiguration;

    /// <summary>
    /// Gets a value indicating whether a connection test can be performed.
    /// </summary>
    /// <remarks>
    /// Returns true when a provider is selected, configured, and not busy.
    /// </remarks>
    public bool CanTestConnection =>
        SelectedProvider?.CanTestConnection == true && !IsBusy;

    /// <summary>
    /// Gets a value indicating whether the selected provider can be set as default.
    /// </summary>
    /// <remarks>
    /// Returns true when a provider is selected that is not already the default,
    /// is configured, and not busy.
    /// </remarks>
    public bool CanSetAsDefault =>
        SelectedProvider != null &&
        !SelectedProvider.IsDefault &&
        SelectedProvider.IsConfigured &&
        CanConfigure &&
        !IsBusy;

    /// <summary>
    /// Gets a value indicating whether the API key can be saved.
    /// </summary>
    /// <remarks>
    /// Returns true when the selected provider has input and the user can configure.
    /// </remarks>
    public bool CanSaveApiKey =>
        SelectedProvider?.CanSaveApiKey == true &&
        CanConfigure &&
        !IsBusy;

    /// <summary>
    /// Gets a value indicating whether the API key can be deleted.
    /// </summary>
    /// <remarks>
    /// Returns true when the selected provider is configured and the user can configure.
    /// </remarks>
    public bool CanDeleteApiKey =>
        SelectedProvider?.CanDeleteApiKey == true &&
        CanConfigure &&
        !IsBusy;

    // =========================================================================
    // Public Methods
    // =========================================================================

    /// <summary>
    /// Loads all available providers from the registry.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// <para>
    /// This method should be called when the settings page is displayed.
    /// It creates <see cref="ProviderConfigViewModel"/> instances for each
    /// registered provider and loads their API key status.
    /// </para>
    /// <para>
    /// The first provider is automatically selected after loading.
    /// </para>
    /// </remarks>
    public async Task LoadProvidersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var providerInfos = _registry.AvailableProviders;
            var viewModels = new List<ProviderConfigViewModel>();

            // Find the current default provider name
            try
            {
                var defaultProvider = _registry.GetDefaultProvider();
                DefaultProviderName = defaultProvider.ProviderName;
            }
            catch
            {
                // No default provider configured
                DefaultProviderName = null;
            }

            // Create ViewModels for each provider
            foreach (var info in providerInfos)
            {
                var vm = new ProviderConfigViewModel(info, _vault, _providerConfigLogger);
                vm.IsDefault = string.Equals(vm.Name, DefaultProviderName, StringComparison.OrdinalIgnoreCase);

                // Load the API key display
                await vm.LoadApiKeyAsync(cancellationToken);

                LLMLogEvents.ProviderConfigurationLoaded(_logger, vm.Name, vm.IsConfigured, vm.IsDefault);
                viewModels.Add(vm);
            }

            Providers = new ObservableCollection<ProviderConfigViewModel>(viewModels);

            // Select the first provider if available
            if (Providers.Count > 0)
            {
                SelectedProvider = Providers[0];
            }

            LLMLogEvents.SettingsPageLoaded(_logger, Providers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load LLM providers");
            ErrorMessage = "Failed to load provider list.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // =========================================================================
    // Commands
    // =========================================================================

    /// <summary>
    /// Tests the connection to the selected provider.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// <para>
    /// This command sends a minimal chat completion request to verify that
    /// the API key is valid and the provider is reachable.
    /// </para>
    /// <para>
    /// The result is displayed via <see cref="ProviderConfigViewModel.Status"/>
    /// and <see cref="ProviderConfigViewModel.StatusMessage"/>.
    /// </para>
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanTestConnection))]
    private async Task TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedProvider == null)
        {
            return;
        }

        var providerName = SelectedProvider.Name;

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            SelectedProvider.UpdateConnectionStatus(ConnectionStatus.Checking, "Testing connection...");

            LLMLogEvents.ConnectionTestStarted(_logger, providerName);

            // Get the provider service
            var provider = _registry.GetProvider(providerName);

            // Build a minimal test request
            var testRequest = new ChatRequest(
                Messages: [new ChatMessage(ChatRole.User, "Hello")],
                Options: ChatOptions.Default with { MaxTokens = 1 }
            );

            // Execute with timing
            var stopwatch = Stopwatch.StartNew();

            await provider.CompleteAsync(testRequest, cancellationToken);

            stopwatch.Stop();
            var latencyMs = stopwatch.ElapsedMilliseconds;

            // Success
            SelectedProvider.UpdateConnectionStatus(
                ConnectionStatus.Connected,
                $"Connected ({latencyMs}ms)");

            LLMLogEvents.ConnectionTestSucceeded(_logger, providerName, latencyMs);
        }
        catch (AuthenticationException)
        {
            SelectedProvider.UpdateConnectionStatus(
                ConnectionStatus.Failed,
                "Invalid API key");

            LLMLogEvents.ConnectionTestFailed(_logger, providerName, "Invalid API key");
        }
        catch (RateLimitException)
        {
            SelectedProvider.UpdateConnectionStatus(
                ConnectionStatus.Failed,
                "Rate limit exceeded");

            LLMLogEvents.ConnectionTestFailed(_logger, providerName, "Rate limit exceeded");
        }
        catch (ProviderNotFoundException)
        {
            SelectedProvider.UpdateConnectionStatus(
                ConnectionStatus.Failed,
                "Provider not found");

            LLMLogEvents.ConnectionTestFailed(_logger, providerName, "Provider not found");
        }
        catch (ProviderNotConfiguredException)
        {
            SelectedProvider.UpdateConnectionStatus(
                ConnectionStatus.Failed,
                "API key not configured");

            LLMLogEvents.ConnectionTestFailed(_logger, providerName, "API key not configured");
        }
        catch (ChatCompletionException ex)
        {
            SelectedProvider.UpdateConnectionStatus(
                ConnectionStatus.Failed,
                ex.Message);

            LLMLogEvents.ConnectionTestFailed(_logger, providerName, ex.Message);
        }
        catch (TaskCanceledException)
        {
            SelectedProvider.UpdateConnectionStatus(
                ConnectionStatus.Unknown,
                "Test cancelled");
        }
        catch (Exception ex)
        {
            SelectedProvider.UpdateConnectionStatus(
                ConnectionStatus.Failed,
                "Connection failed");

            LLMLogEvents.ConnectionTestFailed(_logger, providerName, ex.Message);
            _logger.LogError(ex, "Connection test failed for provider {ProviderName}", providerName);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Saves the API key for the selected provider.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    [RelayCommand(CanExecute = nameof(CanSaveApiKey))]
    private async Task SaveApiKeyAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedProvider == null)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var success = await SelectedProvider.SaveApiKeyAsync(cancellationToken);

            if (!success)
            {
                ErrorMessage = "Failed to save API key.";
            }

            // Refresh command states
            RefreshCommandStates();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Deletes the API key for the selected provider.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    [RelayCommand(CanExecute = nameof(CanDeleteApiKey))]
    private async Task DeleteApiKeyAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedProvider == null)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var success = await SelectedProvider.DeleteApiKeyAsync(cancellationToken);

            if (!success)
            {
                ErrorMessage = "Failed to delete API key.";
            }

            // Refresh command states
            RefreshCommandStates();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Sets the selected provider as the default.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSetAsDefault))]
    private void SetAsDefault()
    {
        if (SelectedProvider == null)
        {
            return;
        }

        try
        {
            ErrorMessage = null;

            // Update the registry
            _registry.SetDefaultProvider(SelectedProvider.Name);

            // Update UI state - clear old default, set new
            foreach (var provider in Providers)
            {
                provider.IsDefault = string.Equals(
                    provider.Name,
                    SelectedProvider.Name,
                    StringComparison.OrdinalIgnoreCase);
            }

            DefaultProviderName = SelectedProvider.Name;

            LLMLogEvents.DefaultProviderChangedViaSettings(_logger, SelectedProvider.Name);

            // Refresh command states
            RefreshCommandStates();
        }
        catch (ProviderNotFoundException)
        {
            ErrorMessage = "Provider not found.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set default provider");
            ErrorMessage = "Failed to set default provider.";
        }
    }

    // =========================================================================
    // Property Changed Handlers
    // =========================================================================

    /// <summary>
    /// Called when the <see cref="SelectedProvider"/> property changes.
    /// </summary>
    /// <param name="value">The new selected provider.</param>
    partial void OnSelectedProviderChanged(ProviderConfigViewModel? value)
    {
        if (value != null)
        {
            LLMLogEvents.ProviderSelected(_logger, value.Name);
        }

        // Refresh all command CanExecute states
        RefreshCommandStates();
    }

    /// <summary>
    /// Called when the <see cref="IsBusy"/> property changes.
    /// </summary>
    /// <param name="value">The new busy state.</param>
    partial void OnIsBusyChanged(bool value)
    {
        RefreshCommandStates();
    }

    // =========================================================================
    // Private Methods
    // =========================================================================

    /// <summary>
    /// Refreshes the CanExecute state of all commands.
    /// </summary>
    private void RefreshCommandStates()
    {
        OnPropertyChanged(nameof(CanTestConnection));
        OnPropertyChanged(nameof(CanSetAsDefault));
        OnPropertyChanged(nameof(CanSaveApiKey));
        OnPropertyChanged(nameof(CanDeleteApiKey));

        TestConnectionCommand.NotifyCanExecuteChanged();
        SaveApiKeyCommand.NotifyCanExecuteChanged();
        DeleteApiKeyCommand.NotifyCanExecuteChanged();
        SetAsDefaultCommand.NotifyCanExecuteChanged();
    }

    // =========================================================================
    // IDisposable Implementation
    // =========================================================================

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        // Clear provider ViewModels
        Providers.Clear();
        SelectedProvider = null;

        _isDisposed = true;
    }
}
