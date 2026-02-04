// -----------------------------------------------------------------------
// <copyright file="ProviderConfigViewModelTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Abstractions.Contracts.Security;
using Lexichord.Modules.LLM.Presentation;
using Lexichord.Modules.LLM.Presentation.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.LLM.Presentation;

/// <summary>
/// Unit tests for <see cref="ProviderConfigViewModel"/>.
/// </summary>
/// <remarks>
/// Tests cover:
/// <list type="bullet">
///   <item><description>Constructor validation</description></item>
///   <item><description>Property initialization from provider info</description></item>
///   <item><description>API key masking logic</description></item>
///   <item><description>Async operations (load, save, delete)</description></item>
///   <item><description>Computed properties (CanSaveApiKey, CanDeleteApiKey, etc.)</description></item>
///   <item><description>Connection status updates</description></item>
/// </list>
/// <para>
/// <b>Version:</b> v0.6.1d
/// </para>
/// </remarks>
public class ProviderConfigViewModelTests
{
    private readonly ISecureVault _vault;
    private readonly ILogger<ProviderConfigViewModel> _logger;
    private readonly LLMProviderInfo _defaultProviderInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderConfigViewModelTests"/> class.
    /// </summary>
    public ProviderConfigViewModelTests()
    {
        _vault = Substitute.For<ISecureVault>();
        _logger = NullLogger<ProviderConfigViewModel>.Instance;
        _defaultProviderInfo = LLMProviderInfo.Create(
            "openai",
            "OpenAI",
            new[] { "gpt-4o", "gpt-4o-mini" },
            supportsStreaming: true);
    }

    /// <summary>
    /// Creates a new ViewModel instance with default test dependencies.
    /// </summary>
    /// <param name="providerInfo">Optional provider info override.</param>
    /// <returns>A new <see cref="ProviderConfigViewModel"/> instance.</returns>
    private ProviderConfigViewModel CreateViewModel(LLMProviderInfo? providerInfo = null)
    {
        return new ProviderConfigViewModel(
            providerInfo ?? _defaultProviderInfo,
            _vault,
            _logger);
    }

    #region Constructor Tests

    /// <summary>
    /// Tests that constructor throws on null provider info.
    /// </summary>
    [Fact]
    public void Constructor_WithNullProviderInfo_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new ProviderConfigViewModel(null!, _vault, _logger);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("providerInfo");
    }

    /// <summary>
    /// Tests that constructor throws on null vault.
    /// </summary>
    [Fact]
    public void Constructor_WithNullVault_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new ProviderConfigViewModel(_defaultProviderInfo, null!, _logger);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("secureVault");
    }

    /// <summary>
    /// Tests that constructor throws on null logger.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new ProviderConfigViewModel(_defaultProviderInfo, _vault, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    /// <summary>
    /// Tests that constructor initializes properties from provider info.
    /// </summary>
    [Fact]
    public void Constructor_WithValidProviderInfo_ShouldInitializeProperties()
    {
        // Arrange
        var providerInfo = new LLMProviderInfo(
            "anthropic",
            "Anthropic",
            new[] { "claude-3-opus", "claude-3-sonnet" },
            IsConfigured: true,
            SupportsStreaming: true);

        // Act
        var vm = new ProviderConfigViewModel(providerInfo, _vault, _logger);

        // Assert
        vm.Name.Should().Be("anthropic");
        vm.DisplayName.Should().Be("Anthropic");
        vm.IsConfigured.Should().BeTrue();
        vm.SupportsStreaming.Should().BeTrue();
        vm.SupportedModels.Should().HaveCount(2);
        vm.SelectedModel.Should().Be("claude-3-opus");
    }

    /// <summary>
    /// Tests that constructor sets SelectedModel to first model in list.
    /// </summary>
    [Fact]
    public void Constructor_WithModels_ShouldSetSelectedModelToFirst()
    {
        // Arrange
        var providerInfo = LLMProviderInfo.Create(
            "openai",
            "OpenAI",
            new[] { "gpt-4o", "gpt-4o-mini", "gpt-4-turbo" });

        // Act
        var vm = new ProviderConfigViewModel(providerInfo, _vault, _logger);

        // Assert
        vm.SelectedModel.Should().Be("gpt-4o");
    }

    /// <summary>
    /// Tests that constructor handles empty models list.
    /// </summary>
    [Fact]
    public void Constructor_WithNoModels_ShouldLeaveSelectedModelNull()
    {
        // Arrange
        var providerInfo = LLMProviderInfo.Unconfigured("custom", "Custom Provider");

        // Act
        var vm = new ProviderConfigViewModel(providerInfo, _vault, _logger);

        // Assert
        vm.SelectedModel.Should().BeNull();
    }

    /// <summary>
    /// Tests that constructor sets initial connection status to Unknown.
    /// </summary>
    [Fact]
    public void Constructor_ShouldSetInitialStatusToUnknown()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        vm.Status.Should().Be(ConnectionStatus.Unknown);
        vm.StatusMessage.Should().BeNull();
    }

    /// <summary>
    /// Tests that constructor sets initial IsBusy to false.
    /// </summary>
    [Fact]
    public void Constructor_ShouldSetInitialIsBusyToFalse()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        vm.IsBusy.Should().BeFalse();
    }

    /// <summary>
    /// Tests that constructor sets initial ApiKeyDisplay to "Not configured".
    /// </summary>
    [Fact]
    public void Constructor_ShouldSetInitialApiKeyDisplay()
    {
        // Arrange
        var providerInfo = LLMProviderInfo.Unconfigured("openai", "OpenAI");

        // Act
        var vm = new ProviderConfigViewModel(providerInfo, _vault, _logger);

        // Assert
        vm.ApiKeyDisplay.Should().Be("Not configured");
    }

    #endregion

    #region MaskApiKey Tests

    /// <summary>
    /// Tests that MaskApiKey returns "Invalid key" for null input.
    /// </summary>
    [Fact]
    public void MaskApiKey_WithNull_ShouldReturnInvalidKey()
    {
        // Act
        var result = ProviderConfigViewModel.MaskApiKey(null);

        // Assert
        result.Should().Be("Invalid key");
    }

    /// <summary>
    /// Tests that MaskApiKey returns "Invalid key" for empty string.
    /// </summary>
    [Fact]
    public void MaskApiKey_WithEmptyString_ShouldReturnInvalidKey()
    {
        // Act
        var result = ProviderConfigViewModel.MaskApiKey(string.Empty);

        // Assert
        result.Should().Be("Invalid key");
    }

    /// <summary>
    /// Tests that MaskApiKey handles short keys (less than 8 chars).
    /// </summary>
    [Fact]
    public void MaskApiKey_WithShortKey_ShouldShowPrefixAndMask()
    {
        // Act
        var result = ProviderConfigViewModel.MaskApiKey("short");

        // Assert
        result.Should().StartWith("shor");
        result.Should().Contain("••••");
        result.Length.Should().BeGreaterThan(5);
    }

    /// <summary>
    /// Tests that MaskApiKey handles very short keys (less than 4 chars).
    /// </summary>
    [Fact]
    public void MaskApiKey_WithVeryShortKey_ShouldUseEntireKeyAsPrefix()
    {
        // Act
        var result = ProviderConfigViewModel.MaskApiKey("abc");

        // Assert
        result.Should().StartWith("abc");
        result.Should().Contain("••••");
    }

    /// <summary>
    /// Tests that MaskApiKey handles standard API keys.
    /// </summary>
    [Fact]
    public void MaskApiKey_WithStandardKey_ShouldShowPrefixAndSuffix()
    {
        // Arrange
        const string key = "sk-1234567890abcdefghij";

        // Act
        var result = ProviderConfigViewModel.MaskApiKey(key);

        // Assert
        result.Should().StartWith("sk-1");
        result.Should().EndWith("ghij");
        result.Should().Contain("••••");
    }

    /// <summary>
    /// Tests that MaskApiKey shows exactly 12 mask characters.
    /// </summary>
    [Fact]
    public void MaskApiKey_ShouldShowTwelveMaskCharacters()
    {
        // Arrange
        const string key = "sk-1234567890abcdefghij";

        // Act
        var result = ProviderConfigViewModel.MaskApiKey(key);

        // Assert
        var maskCount = result.Count(c => c == '•');
        maskCount.Should().Be(12);
    }

    /// <summary>
    /// Tests that MaskApiKey handles exactly 8-character keys.
    /// </summary>
    [Fact]
    public void MaskApiKey_WithExactly8Chars_ShouldShowFullMask()
    {
        // Arrange
        const string key = "12345678";

        // Act
        var result = ProviderConfigViewModel.MaskApiKey(key);

        // Assert
        result.Should().StartWith("1234");
        result.Should().EndWith("5678");
    }

    #endregion

    #region LoadApiKeyAsync Tests

    /// <summary>
    /// Tests that LoadApiKeyAsync sets "Not configured" when key doesn't exist.
    /// </summary>
    [Fact]
    public async Task LoadApiKeyAsync_WhenKeyDoesNotExist_ShouldSetNotConfigured()
    {
        // Arrange
        var vm = CreateViewModel();
        _vault.SecretExistsAsync("openai:api-key", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        // Act
        await vm.LoadApiKeyAsync();

        // Assert
        vm.ApiKeyDisplay.Should().Be("Not configured");
        vm.IsConfigured.Should().BeFalse();
    }

    /// <summary>
    /// Tests that LoadApiKeyAsync loads and masks existing key.
    /// </summary>
    [Fact]
    public async Task LoadApiKeyAsync_WhenKeyExists_ShouldLoadAndMask()
    {
        // Arrange
        var vm = CreateViewModel();
        const string storedKey = "sk-test1234567890abcdef";

        _vault.SecretExistsAsync("openai:api-key", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        _vault.GetSecretAsync("openai:api-key", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(storedKey));

        // Act
        await vm.LoadApiKeyAsync();

        // Assert
        vm.ApiKeyDisplay.Should().StartWith("sk-t");
        vm.ApiKeyDisplay.Should().Contain("••••");
        vm.IsConfigured.Should().BeTrue();
    }

    /// <summary>
    /// Tests that LoadApiKeyAsync sets IsBusy during operation.
    /// </summary>
    [Fact]
    public async Task LoadApiKeyAsync_ShouldSetIsBusyDuringOperation()
    {
        // Arrange
        var vm = CreateViewModel();
        var tcs = new TaskCompletionSource<bool>();
        bool wasBusyDuringExecution = false;

        _vault.SecretExistsAsync("openai:api-key", Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                wasBusyDuringExecution = vm.IsBusy;
                tcs.SetResult(true);
                return Task.FromResult(false);
            });

        // Act
        var loadTask = vm.LoadApiKeyAsync();
        await tcs.Task;
        await loadTask;

        // Assert
        wasBusyDuringExecution.Should().BeTrue();
        vm.IsBusy.Should().BeFalse(); // Should be false after completion
    }

    /// <summary>
    /// Tests that LoadApiKeyAsync handles vault errors gracefully.
    /// </summary>
    [Fact]
    public async Task LoadApiKeyAsync_WithVaultError_ShouldHandleGracefully()
    {
        // Arrange
        var vm = CreateViewModel();
        _vault.SecretExistsAsync("openai:api-key", Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Vault error"));

        // Act
        await vm.LoadApiKeyAsync();

        // Assert
        vm.ApiKeyDisplay.Should().Be("Error loading key");
        vm.IsConfigured.Should().BeFalse();
    }

    #endregion

    #region SaveApiKeyAsync Tests

    /// <summary>
    /// Tests that SaveApiKeyAsync returns false for empty input.
    /// </summary>
    [Fact]
    public async Task SaveApiKeyAsync_WithEmptyInput_ShouldReturnFalse()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ApiKeyInput = string.Empty;

        // Act
        var result = await vm.SaveApiKeyAsync();

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that SaveApiKeyAsync returns false for whitespace-only input.
    /// </summary>
    [Fact]
    public async Task SaveApiKeyAsync_WithWhitespaceOnly_ShouldReturnFalse()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ApiKeyInput = "   \t\n  ";

        // Act
        var result = await vm.SaveApiKeyAsync();

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that SaveApiKeyAsync stores trimmed key in vault.
    /// </summary>
    [Fact]
    public async Task SaveApiKeyAsync_WithValidInput_ShouldStoreTrimmedKey()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ApiKeyInput = "  sk-test123  ";

        _vault.StoreSecretAsync("openai:api-key", "sk-test123", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await vm.SaveApiKeyAsync();

        // Assert
        result.Should().BeTrue();
        await _vault.Received(1).StoreSecretAsync(
            "openai:api-key",
            "sk-test123",
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that SaveApiKeyAsync clears input after successful save.
    /// </summary>
    [Fact]
    public async Task SaveApiKeyAsync_OnSuccess_ShouldClearInput()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ApiKeyInput = "sk-test123";

        _vault.StoreSecretAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await vm.SaveApiKeyAsync();

        // Assert
        vm.ApiKeyInput.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that SaveApiKeyAsync updates display with masked key.
    /// </summary>
    [Fact]
    public async Task SaveApiKeyAsync_OnSuccess_ShouldUpdateDisplay()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ApiKeyInput = "sk-1234567890abcdef";

        _vault.StoreSecretAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await vm.SaveApiKeyAsync();

        // Assert
        vm.ApiKeyDisplay.Should().StartWith("sk-1");
        vm.ApiKeyDisplay.Should().Contain("••••");
    }

    /// <summary>
    /// Tests that SaveApiKeyAsync sets IsConfigured to true on success.
    /// </summary>
    [Fact]
    public async Task SaveApiKeyAsync_OnSuccess_ShouldSetIsConfiguredTrue()
    {
        // Arrange
        var providerInfo = LLMProviderInfo.Unconfigured("openai", "OpenAI");
        var vm = new ProviderConfigViewModel(providerInfo, _vault, _logger);
        vm.ApiKeyInput = "sk-test123";

        _vault.StoreSecretAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await vm.SaveApiKeyAsync();

        // Assert
        vm.IsConfigured.Should().BeTrue();
    }

    /// <summary>
    /// Tests that SaveApiKeyAsync resets connection status to Unknown.
    /// </summary>
    [Fact]
    public async Task SaveApiKeyAsync_OnSuccess_ShouldResetConnectionStatus()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ApiKeyInput = "sk-test123";
        vm.UpdateConnectionStatus(ConnectionStatus.Connected, "Connected (150ms)");

        _vault.StoreSecretAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await vm.SaveApiKeyAsync();

        // Assert
        vm.Status.Should().Be(ConnectionStatus.Unknown);
        vm.StatusMessage.Should().BeNull();
    }

    /// <summary>
    /// Tests that SaveApiKeyAsync returns false on vault error.
    /// </summary>
    [Fact]
    public async Task SaveApiKeyAsync_WithVaultError_ShouldReturnFalse()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ApiKeyInput = "sk-test123";

        _vault.StoreSecretAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Vault error"));

        // Act
        var result = await vm.SaveApiKeyAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region DeleteApiKeyAsync Tests

    /// <summary>
    /// Tests that DeleteApiKeyAsync deletes key from vault.
    /// </summary>
    [Fact]
    public async Task DeleteApiKeyAsync_ShouldDeleteFromVault()
    {
        // Arrange
        var vm = CreateViewModel();
        _vault.DeleteSecretAsync("openai:api-key", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act
        await vm.DeleteApiKeyAsync();

        // Assert
        await _vault.Received(1).DeleteSecretAsync(
            "openai:api-key",
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that DeleteApiKeyAsync updates state on success.
    /// </summary>
    [Fact]
    public async Task DeleteApiKeyAsync_OnSuccess_ShouldUpdateState()
    {
        // Arrange
        var providerInfo = new LLMProviderInfo("openai", "OpenAI", new[] { "gpt-4o" }, IsConfigured: true, SupportsStreaming: true);
        var vm = new ProviderConfigViewModel(providerInfo, _vault, _logger);
        vm.UpdateConnectionStatus(ConnectionStatus.Connected, "Connected");

        _vault.DeleteSecretAsync("openai:api-key", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act
        var result = await vm.DeleteApiKeyAsync();

        // Assert
        result.Should().BeTrue();
        vm.ApiKeyDisplay.Should().Be("Not configured");
        vm.IsConfigured.Should().BeFalse();
        vm.Status.Should().Be(ConnectionStatus.Unknown);
        vm.StatusMessage.Should().BeNull();
    }

    /// <summary>
    /// Tests that DeleteApiKeyAsync returns false when key doesn't exist.
    /// </summary>
    [Fact]
    public async Task DeleteApiKeyAsync_WhenKeyDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var vm = CreateViewModel();
        _vault.DeleteSecretAsync("openai:api-key", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        // Act
        var result = await vm.DeleteApiKeyAsync();

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that DeleteApiKeyAsync returns false on vault error.
    /// </summary>
    [Fact]
    public async Task DeleteApiKeyAsync_WithVaultError_ShouldReturnFalse()
    {
        // Arrange
        var vm = CreateViewModel();
        _vault.DeleteSecretAsync("openai:api-key", Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Vault error"));

        // Act
        var result = await vm.DeleteApiKeyAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Computed Properties Tests

    /// <summary>
    /// Tests that CanSaveApiKey is false when ApiKeyInput is empty.
    /// </summary>
    [Fact]
    public void CanSaveApiKey_WhenInputEmpty_ShouldBeFalse()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ApiKeyInput = string.Empty;

        // Assert
        vm.CanSaveApiKey.Should().BeFalse();
    }

    /// <summary>
    /// Tests that CanSaveApiKey is true when ApiKeyInput has content.
    /// </summary>
    [Fact]
    public void CanSaveApiKey_WhenInputHasContent_ShouldBeTrue()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ApiKeyInput = "sk-test123";

        // Assert
        vm.CanSaveApiKey.Should().BeTrue();
    }

    /// <summary>
    /// Tests that CanSaveApiKey is false when IsBusy.
    /// </summary>
    [Fact]
    public async Task CanSaveApiKey_WhenBusy_ShouldBeFalse()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ApiKeyInput = "sk-test123";

        var tcs = new TaskCompletionSource<bool>();
        _vault.SecretExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => tcs.Task);

        // Act - Start an operation that will set IsBusy
        var loadTask = vm.LoadApiKeyAsync();

        // Assert - Should be busy now
        vm.IsBusy.Should().BeTrue();
        vm.CanSaveApiKey.Should().BeFalse();

        // Cleanup
        tcs.SetResult(false);
        await loadTask;
    }

    /// <summary>
    /// Tests that CanDeleteApiKey is false when not configured.
    /// </summary>
    [Fact]
    public void CanDeleteApiKey_WhenNotConfigured_ShouldBeFalse()
    {
        // Arrange
        var providerInfo = LLMProviderInfo.Unconfigured("openai", "OpenAI");
        var vm = new ProviderConfigViewModel(providerInfo, _vault, _logger);

        // Assert
        vm.CanDeleteApiKey.Should().BeFalse();
    }

    /// <summary>
    /// Tests that CanDeleteApiKey is true when configured.
    /// </summary>
    [Fact]
    public void CanDeleteApiKey_WhenConfigured_ShouldBeTrue()
    {
        // Arrange
        var providerInfo = new LLMProviderInfo("openai", "OpenAI", new[] { "gpt-4o" }, IsConfigured: true, SupportsStreaming: true);
        var vm = new ProviderConfigViewModel(providerInfo, _vault, _logger);

        // Assert
        vm.CanDeleteApiKey.Should().BeTrue();
    }

    /// <summary>
    /// Tests that CanTestConnection is false when not configured.
    /// </summary>
    [Fact]
    public void CanTestConnection_WhenNotConfigured_ShouldBeFalse()
    {
        // Arrange
        var providerInfo = LLMProviderInfo.Unconfigured("openai", "OpenAI");
        var vm = new ProviderConfigViewModel(providerInfo, _vault, _logger);

        // Assert
        vm.CanTestConnection.Should().BeFalse();
    }

    /// <summary>
    /// Tests that CanTestConnection is true when configured.
    /// </summary>
    [Fact]
    public void CanTestConnection_WhenConfigured_ShouldBeTrue()
    {
        // Arrange
        var providerInfo = new LLMProviderInfo("openai", "OpenAI", new[] { "gpt-4o" }, IsConfigured: true, SupportsStreaming: true);
        var vm = new ProviderConfigViewModel(providerInfo, _vault, _logger);

        // Assert
        vm.CanTestConnection.Should().BeTrue();
    }

    #endregion

    #region UpdateConnectionStatus Tests

    /// <summary>
    /// Tests that UpdateConnectionStatus sets status and message.
    /// </summary>
    [Fact]
    public void UpdateConnectionStatus_ShouldSetStatusAndMessage()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.UpdateConnectionStatus(ConnectionStatus.Connected, "Connected (150ms)");

        // Assert
        vm.Status.Should().Be(ConnectionStatus.Connected);
        vm.StatusMessage.Should().Be("Connected (150ms)");
    }

    /// <summary>
    /// Tests that UpdateConnectionStatus handles null message.
    /// </summary>
    [Fact]
    public void UpdateConnectionStatus_WithNullMessage_ShouldSetStatusOnly()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.UpdateConnectionStatus(ConnectionStatus.Connected, "Previous message");

        // Act
        vm.UpdateConnectionStatus(ConnectionStatus.Checking, null);

        // Assert
        vm.Status.Should().Be(ConnectionStatus.Checking);
        vm.StatusMessage.Should().BeNull();
    }

    /// <summary>
    /// Tests that UpdateConnectionStatus can set Failed status.
    /// </summary>
    [Fact]
    public void UpdateConnectionStatus_WithFailedStatus_ShouldSetCorrectly()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.UpdateConnectionStatus(ConnectionStatus.Failed, "Invalid API key");

        // Assert
        vm.Status.Should().Be(ConnectionStatus.Failed);
        vm.StatusMessage.Should().Be("Invalid API key");
    }

    #endregion

    #region Property Change Notification Tests

    /// <summary>
    /// Tests that changing ApiKeyInput triggers property changed for CanSaveApiKey.
    /// </summary>
    [Fact]
    public void ApiKeyInput_WhenChanged_ShouldNotifyCanSaveApiKey()
    {
        // Arrange
        var vm = CreateViewModel();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != null)
            {
                changedProperties.Add(e.PropertyName);
            }
        };

        // Act
        vm.ApiKeyInput = "new-key";

        // Assert
        changedProperties.Should().Contain(nameof(ProviderConfigViewModel.CanSaveApiKey));
    }

    /// <summary>
    /// Tests that changing IsConfigured triggers property changed for related properties.
    /// </summary>
    [Fact]
    public async Task IsConfigured_WhenChanged_ShouldNotifyRelatedProperties()
    {
        // Arrange
        var providerInfo = LLMProviderInfo.Unconfigured("openai", "OpenAI");
        var vm = new ProviderConfigViewModel(providerInfo, _vault, _logger);
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != null)
            {
                changedProperties.Add(e.PropertyName);
            }
        };

        _vault.SecretExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        _vault.GetSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("sk-test123"));

        // Act
        await vm.LoadApiKeyAsync();

        // Assert
        changedProperties.Should().Contain(nameof(ProviderConfigViewModel.CanDeleteApiKey));
        changedProperties.Should().Contain(nameof(ProviderConfigViewModel.CanTestConnection));
    }

    #endregion

    #region Vault Key Pattern Tests

    /// <summary>
    /// Tests that vault key uses correct pattern for provider name.
    /// </summary>
    [Fact]
    public async Task VaultKey_ShouldUseCorrectPattern()
    {
        // Arrange
        var providerInfo = LLMProviderInfo.Create("anthropic", "Anthropic", new[] { "claude-3" });
        var vm = new ProviderConfigViewModel(providerInfo, _vault, _logger);

        _vault.SecretExistsAsync("anthropic:api-key", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        // Act
        await vm.LoadApiKeyAsync();

        // Assert
        await _vault.Received(1).SecretExistsAsync(
            "anthropic:api-key",
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that vault key uses provider name verbatim (no transformation).
    /// </summary>
    [Fact]
    public async Task VaultKey_ShouldUseProviderNameVerbatim()
    {
        // Arrange - Provider name with mixed case
        var providerInfo = LLMProviderInfo.Create("MyProvider", "My Provider", new[] { "model-1" });
        var vm = new ProviderConfigViewModel(providerInfo, _vault, _logger);
        vm.ApiKeyInput = "test-key";

        _vault.StoreSecretAsync("MyProvider:api-key", "test-key", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await vm.SaveApiKeyAsync();

        // Assert
        await _vault.Received(1).StoreSecretAsync(
            "MyProvider:api-key",
            "test-key",
            Arg.Any<CancellationToken>());
    }

    #endregion
}
