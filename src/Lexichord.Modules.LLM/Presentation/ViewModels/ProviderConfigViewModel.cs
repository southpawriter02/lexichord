// -----------------------------------------------------------------------
// <copyright file="ProviderConfigViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Abstractions.Contracts.Security;
using Lexichord.Modules.LLM.Logging;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.LLM.Presentation.ViewModels;

/// <summary>
/// ViewModel for individual LLM provider configuration within the settings page.
/// </summary>
/// <remarks>
/// <para>
/// This ViewModel manages the configuration state for a single LLM provider,
/// including API key input/display, connection testing, and default provider selection.
/// </para>
/// <para>
/// <b>Security:</b> API keys are never stored in memory as plaintext after loading.
/// The <see cref="ApiKeyDisplay"/> property shows a masked representation (e.g., "sk-••••abcd").
/// The actual key is only retrieved when needed for saving or testing.
/// </para>
/// <para>
/// <b>Version:</b> v0.6.1d
/// </para>
/// </remarks>
public sealed partial class ProviderConfigViewModel : ObservableObject
{
    /// <summary>
    /// The minimum number of characters required to show both prefix and suffix in masked display.
    /// </summary>
    /// <remarks>
    /// Keys shorter than this will show only the prefix portion.
    /// </remarks>
    private const int MinLengthForFullMask = 8;

    /// <summary>
    /// The number of characters to show at the start of a masked API key.
    /// </summary>
    private const int MaskPrefixLength = 4;

    /// <summary>
    /// The number of characters to show at the end of a masked API key.
    /// </summary>
    private const int MaskSuffixLength = 4;

    /// <summary>
    /// The masking character used to hide the middle portion of API keys.
    /// </summary>
    private const char MaskCharacter = '•';

    /// <summary>
    /// The number of mask characters to display in the middle portion.
    /// </summary>
    private const int MaskLength = 12;

    private readonly ISecureVault _secureVault;
    private readonly ILogger _logger;

    /// <summary>
    /// The vault key pattern for this provider's API key.
    /// Format: "{providerName}:api-key" (e.g., "openai:api-key").
    /// </summary>
    private readonly string _vaultKey;

    // =========================================================================
    // Observable Properties
    // =========================================================================

    /// <summary>
    /// Gets the unique provider identifier.
    /// </summary>
    /// <remarks>
    /// This is the lowercase, URL-safe identifier used for service resolution
    /// (e.g., "openai", "anthropic", "ollama").
    /// </remarks>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// Gets the human-readable display name for the provider.
    /// </summary>
    /// <remarks>
    /// This is shown in the UI (e.g., "OpenAI", "Anthropic", "Ollama").
    /// </remarks>
    [ObservableProperty]
    private string _displayName = string.Empty;

    /// <summary>
    /// Gets or sets the user's input for a new API key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property is bound to the password input field in the UI.
    /// The value is cleared after a successful save operation.
    /// </para>
    /// <para>
    /// <b>Security:</b> This value should be cleared from memory after use.
    /// The UI should use a password-masked input field.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private string _apiKeyInput = string.Empty;

    /// <summary>
    /// Gets the masked display of the currently stored API key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Shows "Not configured" if no key is stored, or a masked format
    /// like "sk-••••••••••••abcd" if a key exists.
    /// </para>
    /// <para>
    /// <b>Security:</b> The full API key is never exposed through this property.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private string _apiKeyDisplay = "Not configured";

    /// <summary>
    /// Gets a value indicating whether the provider has a valid API key configured.
    /// </summary>
    /// <remarks>
    /// This is determined by checking the secure vault for the provider's API key.
    /// A configured provider can be used for chat completions.
    /// </remarks>
    [ObservableProperty]
    private bool _isConfigured;

    /// <summary>
    /// Gets or sets a value indicating whether this provider is the default.
    /// </summary>
    /// <remarks>
    /// Only one provider can be the default at a time. The default provider
    /// is used when no specific provider is requested for a chat completion.
    /// </remarks>
    [ObservableProperty]
    private bool _isDefault;

    /// <summary>
    /// Gets the current connection status of the provider.
    /// </summary>
    /// <remarks>
    /// This is updated after connection tests. The UI should display
    /// appropriate visual indicators based on this status.
    /// </remarks>
    [ObservableProperty]
    private ConnectionStatus _status = ConnectionStatus.Unknown;

    /// <summary>
    /// Gets the status message providing details about the current connection status.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For successful connections, this typically shows the latency (e.g., "Connected (150ms)").
    /// For failures, this contains the error message (e.g., "Invalid API key").
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>
    /// Gets the list of model identifiers supported by this provider.
    /// </summary>
    /// <remarks>
    /// This list is populated from the <see cref="LLMProviderInfo"/> when the
    /// ViewModel is initialized.
    /// </remarks>
    [ObservableProperty]
    private IReadOnlyList<string> _supportedModels = [];

    /// <summary>
    /// Gets or sets the currently selected model for this provider.
    /// </summary>
    /// <remarks>
    /// This is the model that will be used by default when making requests
    /// to this provider. If null, the provider's default model is used.
    /// </remarks>
    [ObservableProperty]
    private string? _selectedModel;

    /// <summary>
    /// Gets a value indicating whether the provider supports streaming responses.
    /// </summary>
    /// <remarks>
    /// Providers that support streaming can return tokens incrementally
    /// via Server-Sent Events (SSE).
    /// </remarks>
    [ObservableProperty]
    private bool _supportsStreaming;

    /// <summary>
    /// Gets a value indicating whether an async operation is in progress.
    /// </summary>
    /// <remarks>
    /// When true, UI controls that trigger async operations should be disabled
    /// to prevent concurrent operations.
    /// </remarks>
    [ObservableProperty]
    private bool _isBusy;

    // =========================================================================
    // Constructor
    // =========================================================================

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderConfigViewModel"/> class.
    /// </summary>
    /// <param name="providerInfo">The provider metadata from the registry.</param>
    /// <param name="secureVault">The secure vault for API key storage.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="providerInfo"/>, <paramref name="secureVault"/>,
    /// or <paramref name="logger"/> is null.
    /// </exception>
    public ProviderConfigViewModel(
        LLMProviderInfo providerInfo,
        ISecureVault secureVault,
        ILogger<ProviderConfigViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(providerInfo, nameof(providerInfo));
        ArgumentNullException.ThrowIfNull(secureVault, nameof(secureVault));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        _secureVault = secureVault;
        _logger = logger;

        // Initialize from provider info
        Name = providerInfo.Name;
        DisplayName = providerInfo.DisplayName;
        IsConfigured = providerInfo.IsConfigured;
        SupportedModels = providerInfo.SupportedModels;
        SupportsStreaming = providerInfo.SupportsStreaming;

        // Build the vault key for this provider
        _vaultKey = $"{providerInfo.Name}:api-key";

        // Set initial selected model to first in list if available
        if (SupportedModels.Count > 0)
        {
            SelectedModel = SupportedModels[0];
        }
    }

    // =========================================================================
    // Computed Properties
    // =========================================================================

    /// <summary>
    /// Gets a value indicating whether a new API key can be saved.
    /// </summary>
    /// <remarks>
    /// Returns true when there is input text and no operation is in progress.
    /// </remarks>
    public bool CanSaveApiKey => !string.IsNullOrWhiteSpace(ApiKeyInput) && !IsBusy;

    /// <summary>
    /// Gets a value indicating whether the API key can be deleted.
    /// </summary>
    /// <remarks>
    /// Returns true when the provider is configured and no operation is in progress.
    /// </remarks>
    public bool CanDeleteApiKey => IsConfigured && !IsBusy;

    /// <summary>
    /// Gets a value indicating whether a connection test can be performed.
    /// </summary>
    /// <remarks>
    /// Returns true when the provider is configured and no operation is in progress.
    /// </remarks>
    public bool CanTestConnection => IsConfigured && !IsBusy;

    // =========================================================================
    // Public Methods
    // =========================================================================

    /// <summary>
    /// Loads the API key display from the secure vault.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// <para>
    /// This method retrieves the API key from the vault and sets the
    /// <see cref="ApiKeyDisplay"/> property to a masked representation.
    /// </para>
    /// <para>
    /// <b>Security:</b> The full API key is briefly held in memory during masking,
    /// then discarded. Only the masked version is retained.
    /// </para>
    /// </remarks>
    public async Task LoadApiKeyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IsBusy = true;

            // Check if key exists without retrieving it
            var exists = await _secureVault.SecretExistsAsync(_vaultKey, cancellationToken);

            if (!exists)
            {
                ApiKeyDisplay = "Not configured";
                IsConfigured = false;
                return;
            }

            // Retrieve and mask the key
            var apiKey = await _secureVault.GetSecretAsync(_vaultKey, cancellationToken);
            ApiKeyDisplay = MaskApiKey(apiKey);
            IsConfigured = true;

            // Clear the plaintext key from memory
            // Note: In .NET, strings are immutable and we cannot truly clear them,
            // but we ensure we don't keep references longer than necessary
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load API key for provider {ProviderName}", Name);
            ApiKeyDisplay = "Error loading key";
            IsConfigured = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Saves the API key from the input field to the secure vault.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// <c>true</c> if the key was saved successfully; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// After successful save, the <see cref="ApiKeyInput"/> is cleared and
    /// <see cref="ApiKeyDisplay"/> is updated with the masked key.
    /// </para>
    /// <para>
    /// <b>Security:</b> The input field is cleared immediately after saving
    /// to minimize the time the plaintext key is in memory.
    /// </para>
    /// </remarks>
    public async Task<bool> SaveApiKeyAsync(CancellationToken cancellationToken = default)
    {
        var keyToSave = ApiKeyInput.Trim();

        if (string.IsNullOrWhiteSpace(keyToSave))
        {
            LLMLogEvents.ApiKeyValidationFailed(_logger, Name, "API key is empty or whitespace");
            return false;
        }

        try
        {
            IsBusy = true;

            // Store the key in the vault
            await _secureVault.StoreSecretAsync(_vaultKey, keyToSave, cancellationToken);

            // Update display with masked version
            ApiKeyDisplay = MaskApiKey(keyToSave);
            IsConfigured = true;

            // Clear the input field
            ApiKeyInput = string.Empty;

            // Reset connection status since key changed
            Status = ConnectionStatus.Unknown;
            StatusMessage = null;

            LLMLogEvents.ApiKeySaved(_logger, Name);

            return true;
        }
        catch (Exception ex)
        {
            LLMLogEvents.ApiKeySaveFailed(_logger, Name, ex.Message);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Deletes the API key from the secure vault.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// <c>true</c> if the key was deleted successfully; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// After successful deletion, <see cref="IsConfigured"/> is set to false
    /// and the connection status is reset to <see cref="ConnectionStatus.Unknown"/>.
    /// </remarks>
    public async Task<bool> DeleteApiKeyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IsBusy = true;

            var deleted = await _secureVault.DeleteSecretAsync(_vaultKey, cancellationToken);

            if (deleted)
            {
                ApiKeyDisplay = "Not configured";
                IsConfigured = false;
                Status = ConnectionStatus.Unknown;
                StatusMessage = null;

                LLMLogEvents.ApiKeyDeleted(_logger, Name);
            }

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete API key for provider {ProviderName}", Name);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Updates the connection status after a test.
    /// </summary>
    /// <param name="status">The new connection status.</param>
    /// <param name="message">An optional status message.</param>
    /// <remarks>
    /// This method is called by the parent <see cref="LLMSettingsViewModel"/>
    /// after performing a connection test.
    /// </remarks>
    public void UpdateConnectionStatus(ConnectionStatus status, string? message = null)
    {
        Status = status;
        StatusMessage = message;
    }

    // =========================================================================
    // Property Changed Handlers
    // =========================================================================

    /// <summary>
    /// Called when the <see cref="ApiKeyInput"/> property changes.
    /// </summary>
    /// <param name="value">The new value.</param>
    partial void OnApiKeyInputChanged(string value)
    {
        // Notify that CanSaveApiKey may have changed
        OnPropertyChanged(nameof(CanSaveApiKey));
    }

    /// <summary>
    /// Called when the <see cref="IsConfigured"/> property changes.
    /// </summary>
    /// <param name="value">The new value.</param>
    partial void OnIsConfiguredChanged(bool value)
    {
        // Notify that CanDeleteApiKey and CanTestConnection may have changed
        OnPropertyChanged(nameof(CanDeleteApiKey));
        OnPropertyChanged(nameof(CanTestConnection));
    }

    /// <summary>
    /// Called when the <see cref="IsBusy"/> property changes.
    /// </summary>
    /// <param name="value">The new value.</param>
    partial void OnIsBusyChanged(bool value)
    {
        // Notify that all "Can*" properties may have changed
        OnPropertyChanged(nameof(CanSaveApiKey));
        OnPropertyChanged(nameof(CanDeleteApiKey));
        OnPropertyChanged(nameof(CanTestConnection));
    }

    // =========================================================================
    // Static Helper Methods
    // =========================================================================

    /// <summary>
    /// Masks an API key for display, showing only the first and last few characters.
    /// </summary>
    /// <param name="apiKey">The API key to mask.</param>
    /// <returns>
    /// A masked representation of the key (e.g., "sk-••••••••••••abcd"),
    /// or "Invalid key" if the input is null or empty.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The masking strategy depends on key length:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Keys shorter than 8 chars: Show first 4 chars + mask</description></item>
    ///   <item><description>Keys 8+ chars: Show first 4 chars + mask + last 4 chars</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// MaskApiKey("sk-1234567890abcdef") // Returns "sk-1••••••••••••cdef"
    /// MaskApiKey("short")               // Returns "shor••••••••••••"
    /// MaskApiKey(null)                  // Returns "Invalid key"
    /// </code>
    /// </example>
    public static string MaskApiKey(string? apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            return "Invalid key";
        }

        // For very short keys, just show the prefix
        if (apiKey.Length < MinLengthForFullMask)
        {
            var shortPrefix = apiKey.Length >= MaskPrefixLength
                ? apiKey[..MaskPrefixLength]
                : apiKey;
            return shortPrefix + new string(MaskCharacter, MaskLength);
        }

        // Show prefix + mask + suffix for longer keys
        var prefix = apiKey[..MaskPrefixLength];
        var suffix = apiKey[^MaskSuffixLength..];
        var mask = new string(MaskCharacter, MaskLength);

        return $"{prefix}{mask}{suffix}";
    }
}
