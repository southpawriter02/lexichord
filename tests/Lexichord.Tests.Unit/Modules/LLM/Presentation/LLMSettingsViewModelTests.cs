// -----------------------------------------------------------------------
// <copyright file="LLMSettingsViewModelTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
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
/// Unit tests for <see cref="LLMSettingsViewModel"/>.
/// </summary>
/// <remarks>
/// Tests cover:
/// <list type="bullet">
///   <item><description>Constructor validation</description></item>
///   <item><description>Provider loading</description></item>
///   <item><description>Connection testing</description></item>
///   <item><description>API key management commands</description></item>
///   <item><description>Default provider selection</description></item>
///   <item><description>License gating</description></item>
/// </list>
/// <para>
/// <b>Version:</b> v0.6.1d
/// </para>
/// </remarks>
public class LLMSettingsViewModelTests
{
    private readonly ILLMProviderRegistry _registry;
    private readonly ISecureVault _vault;
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<LLMSettingsViewModel> _logger;
    private readonly ILogger<ProviderConfigViewModel> _providerConfigLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LLMSettingsViewModelTests"/> class.
    /// </summary>
    public LLMSettingsViewModelTests()
    {
        _registry = Substitute.For<ILLMProviderRegistry>();
        _vault = Substitute.For<ISecureVault>();
        _licenseContext = Substitute.For<ILicenseContext>();
        _logger = NullLogger<LLMSettingsViewModel>.Instance;
        _providerConfigLogger = NullLogger<ProviderConfigViewModel>.Instance;

        // Default to WriterPro tier (can configure)
        _licenseContext.GetCurrentTier().Returns(LicenseTier.WriterPro);

        // Default to empty provider list
        _registry.AvailableProviders.Returns(new List<LLMProviderInfo>());
    }

    /// <summary>
    /// Creates a new ViewModel instance with default test dependencies.
    /// </summary>
    private LLMSettingsViewModel CreateViewModel()
    {
        return new LLMSettingsViewModel(
            _registry,
            _vault,
            _licenseContext,
            _logger,
            _providerConfigLogger);
    }

    #region Constructor Tests

    /// <summary>
    /// Tests that constructor throws on null registry.
    /// </summary>
    [Fact]
    public void Constructor_WithNullRegistry_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new LLMSettingsViewModel(
            null!,
            _vault,
            _licenseContext,
            _logger,
            _providerConfigLogger);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("registry");
    }

    /// <summary>
    /// Tests that constructor throws on null vault.
    /// </summary>
    [Fact]
    public void Constructor_WithNullVault_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new LLMSettingsViewModel(
            _registry,
            null!,
            _licenseContext,
            _logger,
            _providerConfigLogger);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("vault");
    }

    /// <summary>
    /// Tests that constructor throws on null license context.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLicenseContext_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new LLMSettingsViewModel(
            _registry,
            _vault,
            null!,
            _logger,
            _providerConfigLogger);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("licenseContext");
    }

    /// <summary>
    /// Tests that constructor throws on null logger.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new LLMSettingsViewModel(
            _registry,
            _vault,
            _licenseContext,
            null!,
            _providerConfigLogger);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    /// <summary>
    /// Tests that constructor throws on null provider config logger.
    /// </summary>
    [Fact]
    public void Constructor_WithNullProviderConfigLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new LLMSettingsViewModel(
            _registry,
            _vault,
            _licenseContext,
            _logger,
            null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("providerConfigLogger");
    }

    /// <summary>
    /// Tests that constructor initializes with empty providers.
    /// </summary>
    [Fact]
    public void Constructor_ShouldInitializeWithEmptyProviders()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        vm.Providers.Should().BeEmpty();
        vm.SelectedProvider.Should().BeNull();
        vm.IsBusy.Should().BeFalse();
        vm.ErrorMessage.Should().BeNull();
    }

    #endregion

    #region License Gating Tests

    /// <summary>
    /// Tests that CanConfigure is true for WriterPro tier.
    /// </summary>
    [Fact]
    public void CanConfigure_WithWriterProTier_ShouldBeTrue()
    {
        // Arrange
        _licenseContext.GetCurrentTier().Returns(LicenseTier.WriterPro);
        var vm = CreateViewModel();

        // Assert
        vm.CanConfigure.Should().BeTrue();
    }

    /// <summary>
    /// Tests that CanConfigure is true for higher tiers.
    /// </summary>
    [Fact]
    public void CanConfigure_WithTeamsTier_ShouldBeTrue()
    {
        // Arrange
        _licenseContext.GetCurrentTier().Returns(LicenseTier.Teams);
        var vm = CreateViewModel();

        // Assert
        vm.CanConfigure.Should().BeTrue();
    }

    /// <summary>
    /// Tests that CanConfigure is false for Core tier.
    /// </summary>
    [Fact]
    public void CanConfigure_WithCoreTier_ShouldBeFalse()
    {
        // Arrange
        _licenseContext.GetCurrentTier().Returns(LicenseTier.Core);
        var vm = CreateViewModel();

        // Assert
        vm.CanConfigure.Should().BeFalse();
    }

    /// <summary>
    /// Tests that CurrentTier returns the license context tier.
    /// </summary>
    [Fact]
    public void CurrentTier_ShouldReturnLicenseContextTier()
    {
        // Arrange
        _licenseContext.GetCurrentTier().Returns(LicenseTier.Enterprise);
        var vm = CreateViewModel();

        // Assert
        vm.CurrentTier.Should().Be(LicenseTier.Enterprise);
    }

    #endregion

    #region LoadProvidersAsync Tests

    /// <summary>
    /// Tests that LoadProvidersAsync creates ViewModels for each provider.
    /// </summary>
    [Fact]
    public async Task LoadProvidersAsync_ShouldCreateViewModelsForEachProvider()
    {
        // Arrange
        var providers = new List<LLMProviderInfo>
        {
            LLMProviderInfo.Create("openai", "OpenAI", new[] { "gpt-4o" }),
            LLMProviderInfo.Create("anthropic", "Anthropic", new[] { "claude-3" })
        };

        _registry.AvailableProviders.Returns(providers);
        _registry.GetDefaultProvider().Throws(new ProviderNotConfiguredException("No default"));

        _vault.SecretExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var vm = CreateViewModel();

        // Act
        await vm.LoadProvidersAsync();

        // Assert
        vm.Providers.Should().HaveCount(2);
        vm.Providers[0].Name.Should().Be("openai");
        vm.Providers[1].Name.Should().Be("anthropic");
    }

    /// <summary>
    /// Tests that LoadProvidersAsync selects first provider automatically.
    /// </summary>
    [Fact]
    public async Task LoadProvidersAsync_ShouldSelectFirstProvider()
    {
        // Arrange
        var providers = new List<LLMProviderInfo>
        {
            LLMProviderInfo.Create("openai", "OpenAI", new[] { "gpt-4o" }),
            LLMProviderInfo.Create("anthropic", "Anthropic", new[] { "claude-3" })
        };

        _registry.AvailableProviders.Returns(providers);
        _registry.GetDefaultProvider().Throws(new ProviderNotConfiguredException("No default"));

        _vault.SecretExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var vm = CreateViewModel();

        // Act
        await vm.LoadProvidersAsync();

        // Assert
        vm.SelectedProvider.Should().NotBeNull();
        vm.SelectedProvider!.Name.Should().Be("openai");
    }

    /// <summary>
    /// Tests that LoadProvidersAsync identifies default provider.
    /// </summary>
    [Fact]
    public async Task LoadProvidersAsync_ShouldIdentifyDefaultProvider()
    {
        // Arrange
        var providers = new List<LLMProviderInfo>
        {
            LLMProviderInfo.Create("openai", "OpenAI", new[] { "gpt-4o" }),
            LLMProviderInfo.Create("anthropic", "Anthropic", new[] { "claude-3" })
        };

        _registry.AvailableProviders.Returns(providers);

        var mockService = Substitute.For<IChatCompletionService>();
        mockService.ProviderName.Returns("anthropic");
        _registry.GetDefaultProvider().Returns(mockService);

        _vault.SecretExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var vm = CreateViewModel();

        // Act
        await vm.LoadProvidersAsync();

        // Assert
        vm.DefaultProviderName.Should().Be("anthropic");
        vm.Providers[0].IsDefault.Should().BeFalse();
        vm.Providers[1].IsDefault.Should().BeTrue();
    }

    /// <summary>
    /// Tests that LoadProvidersAsync sets IsBusy during operation.
    /// </summary>
    [Fact]
    public async Task LoadProvidersAsync_ShouldSetIsBusyDuringOperation()
    {
        // Arrange
        var providers = new List<LLMProviderInfo>
        {
            LLMProviderInfo.Create("openai", "OpenAI", new[] { "gpt-4o" })
        };

        _registry.AvailableProviders.Returns(providers);
        _registry.GetDefaultProvider().Throws(new ProviderNotConfiguredException("No default"));

        var tcs = new TaskCompletionSource<bool>();
        bool wasBusyDuringExecution = false;

        var vm = CreateViewModel();

        _vault.SecretExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                wasBusyDuringExecution = vm.IsBusy;
                tcs.SetResult(true);
                return Task.FromResult(false);
            });

        // Act
        var loadTask = vm.LoadProvidersAsync();
        await tcs.Task;
        await loadTask;

        // Assert
        wasBusyDuringExecution.Should().BeTrue();
        vm.IsBusy.Should().BeFalse();
    }

    /// <summary>
    /// Tests that LoadProvidersAsync handles exceptions gracefully.
    /// </summary>
    [Fact]
    public async Task LoadProvidersAsync_WithException_ShouldSetErrorMessage()
    {
        // Arrange
        _registry.AvailableProviders.Throws(new InvalidOperationException("Registry error"));
        var vm = CreateViewModel();

        // Act
        await vm.LoadProvidersAsync();

        // Assert
        vm.ErrorMessage.Should().NotBeNullOrEmpty();
        vm.IsBusy.Should().BeFalse();
    }

    #endregion

    #region SetAsDefault Tests

    /// <summary>
    /// Tests that SetAsDefault updates registry and UI state.
    /// </summary>
    [Fact]
    public async Task SetAsDefault_ShouldUpdateRegistryAndUIState()
    {
        // Arrange
        var providers = new List<LLMProviderInfo>
        {
            new LLMProviderInfo("openai", "OpenAI", new[] { "gpt-4o" }, IsConfigured: true, SupportsStreaming: true),
            new LLMProviderInfo("anthropic", "Anthropic", new[] { "claude-3" }, IsConfigured: true, SupportsStreaming: true)
        };

        _registry.AvailableProviders.Returns(providers);

        var mockService = Substitute.For<IChatCompletionService>();
        mockService.ProviderName.Returns("openai");
        _registry.GetDefaultProvider().Returns(mockService);

        _vault.SecretExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        _vault.GetSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("sk-test123"));

        var vm = CreateViewModel();
        await vm.LoadProvidersAsync();

        // Select the second provider (not current default)
        vm.SelectedProvider = vm.Providers[1];

        // Act
        vm.SetAsDefaultCommand.Execute(null);

        // Assert
        _registry.Received(1).SetDefaultProvider("anthropic");
        vm.DefaultProviderName.Should().Be("anthropic");
        vm.Providers[0].IsDefault.Should().BeFalse();
        vm.Providers[1].IsDefault.Should().BeTrue();
    }

    /// <summary>
    /// Tests that CanSetAsDefault is false when provider is already default.
    /// </summary>
    [Fact]
    public async Task CanSetAsDefault_WhenAlreadyDefault_ShouldBeFalse()
    {
        // Arrange
        var providers = new List<LLMProviderInfo>
        {
            new LLMProviderInfo("openai", "OpenAI", new[] { "gpt-4o" }, IsConfigured: true, SupportsStreaming: true)
        };

        _registry.AvailableProviders.Returns(providers);

        var mockService = Substitute.For<IChatCompletionService>();
        mockService.ProviderName.Returns("openai");
        _registry.GetDefaultProvider().Returns(mockService);

        _vault.SecretExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        _vault.GetSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("sk-test123"));

        var vm = CreateViewModel();
        await vm.LoadProvidersAsync();

        // Assert - provider is already default
        vm.SelectedProvider!.IsDefault.Should().BeTrue();
        vm.CanSetAsDefault.Should().BeFalse();
    }

    /// <summary>
    /// Tests that CanSetAsDefault is false when not configured.
    /// </summary>
    [Fact]
    public async Task CanSetAsDefault_WhenNotConfigured_ShouldBeFalse()
    {
        // Arrange
        var providers = new List<LLMProviderInfo>
        {
            LLMProviderInfo.Unconfigured("openai", "OpenAI")
        };

        _registry.AvailableProviders.Returns(providers);
        _registry.GetDefaultProvider().Throws(new ProviderNotConfiguredException("No default"));

        _vault.SecretExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var vm = CreateViewModel();
        await vm.LoadProvidersAsync();

        // Assert - provider is not configured
        vm.SelectedProvider!.IsConfigured.Should().BeFalse();
        vm.CanSetAsDefault.Should().BeFalse();
    }

    #endregion

    #region TestConnectionAsync Tests

    /// <summary>
    /// Tests that TestConnectionAsync updates status on success.
    /// </summary>
    [Fact]
    public async Task TestConnectionAsync_OnSuccess_ShouldSetConnectedStatus()
    {
        // Arrange
        var providers = new List<LLMProviderInfo>
        {
            new LLMProviderInfo("openai", "OpenAI", new[] { "gpt-4o" }, IsConfigured: true, SupportsStreaming: true)
        };

        _registry.AvailableProviders.Returns(providers);
        _registry.GetDefaultProvider().Throws(new ProviderNotConfiguredException("No default"));

        var mockService = Substitute.For<IChatCompletionService>();
        mockService.ProviderName.Returns("openai");
        mockService.CompleteAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse(
                Content: "Hi",
                PromptTokens: 1,
                CompletionTokens: 1,
                Duration: TimeSpan.FromMilliseconds(100),
                FinishReason: "stop")));

        _registry.GetProvider("openai").Returns(mockService);

        _vault.SecretExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        _vault.GetSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("sk-test123"));

        var vm = CreateViewModel();
        await vm.LoadProvidersAsync();

        // Act
        await vm.TestConnectionCommand.ExecuteAsync(null);

        // Assert
        vm.SelectedProvider!.Status.Should().Be(ConnectionStatus.Connected);
        vm.SelectedProvider.StatusMessage.Should().Contain("ms");
    }

    /// <summary>
    /// Tests that TestConnectionAsync sets Failed status on auth error.
    /// </summary>
    [Fact]
    public async Task TestConnectionAsync_OnAuthError_ShouldSetFailedStatus()
    {
        // Arrange
        var providers = new List<LLMProviderInfo>
        {
            new LLMProviderInfo("openai", "OpenAI", new[] { "gpt-4o" }, IsConfigured: true, SupportsStreaming: true)
        };

        _registry.AvailableProviders.Returns(providers);
        _registry.GetDefaultProvider().Throws(new ProviderNotConfiguredException("No default"));

        var mockService = Substitute.For<IChatCompletionService>();
        mockService.ProviderName.Returns("openai");
        mockService.CompleteAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AuthenticationException("Invalid API key"));

        _registry.GetProvider("openai").Returns(mockService);

        _vault.SecretExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        _vault.GetSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("sk-test123"));

        var vm = CreateViewModel();
        await vm.LoadProvidersAsync();

        // Act
        await vm.TestConnectionCommand.ExecuteAsync(null);

        // Assert
        vm.SelectedProvider!.Status.Should().Be(ConnectionStatus.Failed);
        vm.SelectedProvider.StatusMessage.Should().Be("Invalid API key");
    }

    /// <summary>
    /// Tests that TestConnectionAsync sets Failed status on rate limit.
    /// </summary>
    [Fact]
    public async Task TestConnectionAsync_OnRateLimit_ShouldSetFailedStatus()
    {
        // Arrange
        var providers = new List<LLMProviderInfo>
        {
            new LLMProviderInfo("openai", "OpenAI", new[] { "gpt-4o" }, IsConfigured: true, SupportsStreaming: true)
        };

        _registry.AvailableProviders.Returns(providers);
        _registry.GetDefaultProvider().Throws(new ProviderNotConfiguredException("No default"));

        var mockService = Substitute.For<IChatCompletionService>();
        mockService.ProviderName.Returns("openai");
        mockService.CompleteAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new RateLimitException("Rate limit exceeded"));

        _registry.GetProvider("openai").Returns(mockService);

        _vault.SecretExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        _vault.GetSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("sk-test123"));

        var vm = CreateViewModel();
        await vm.LoadProvidersAsync();

        // Act
        await vm.TestConnectionCommand.ExecuteAsync(null);

        // Assert
        vm.SelectedProvider!.Status.Should().Be(ConnectionStatus.Failed);
        vm.SelectedProvider.StatusMessage.Should().Be("Rate limit exceeded");
    }

    /// <summary>
    /// Tests that CanTestConnection is false when not configured.
    /// </summary>
    [Fact]
    public async Task CanTestConnection_WhenNotConfigured_ShouldBeFalse()
    {
        // Arrange
        var providers = new List<LLMProviderInfo>
        {
            LLMProviderInfo.Unconfigured("openai", "OpenAI")
        };

        _registry.AvailableProviders.Returns(providers);
        _registry.GetDefaultProvider().Throws(new ProviderNotConfiguredException("No default"));

        _vault.SecretExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var vm = CreateViewModel();
        await vm.LoadProvidersAsync();

        // Assert
        vm.CanTestConnection.Should().BeFalse();
    }

    #endregion

    #region SaveApiKeyAsync Tests

    /// <summary>
    /// Tests that SaveApiKeyAsync delegates to ProviderConfigViewModel.
    /// </summary>
    [Fact]
    public async Task SaveApiKeyAsync_ShouldDelegateToProviderConfig()
    {
        // Arrange
        var providers = new List<LLMProviderInfo>
        {
            LLMProviderInfo.Unconfigured("openai", "OpenAI")
        };

        _registry.AvailableProviders.Returns(providers);
        _registry.GetDefaultProvider().Throws(new ProviderNotConfiguredException("No default"));

        _vault.SecretExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _vault.StoreSecretAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var vm = CreateViewModel();
        await vm.LoadProvidersAsync();

        // Set input
        vm.SelectedProvider!.ApiKeyInput = "sk-newkey123";

        // Act
        await vm.SaveApiKeyCommand.ExecuteAsync(null);

        // Assert
        await _vault.Received(1).StoreSecretAsync(
            "openai:api-key",
            "sk-newkey123",
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that CanSaveApiKey is false with Core tier.
    /// </summary>
    [Fact]
    public async Task CanSaveApiKey_WithCoreTier_ShouldBeFalse()
    {
        // Arrange
        _licenseContext.GetCurrentTier().Returns(LicenseTier.Core);

        var providers = new List<LLMProviderInfo>
        {
            LLMProviderInfo.Unconfigured("openai", "OpenAI")
        };

        _registry.AvailableProviders.Returns(providers);
        _registry.GetDefaultProvider().Throws(new ProviderNotConfiguredException("No default"));

        _vault.SecretExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var vm = CreateViewModel();
        await vm.LoadProvidersAsync();

        // Set input
        vm.SelectedProvider!.ApiKeyInput = "sk-test123";

        // Assert - even with input, can't save at Core tier
        vm.CanConfigure.Should().BeFalse();
        vm.CanSaveApiKey.Should().BeFalse();
    }

    #endregion

    #region DeleteApiKeyAsync Tests

    /// <summary>
    /// Tests that DeleteApiKeyAsync delegates to ProviderConfigViewModel.
    /// </summary>
    [Fact]
    public async Task DeleteApiKeyAsync_ShouldDelegateToProviderConfig()
    {
        // Arrange
        var providers = new List<LLMProviderInfo>
        {
            new LLMProviderInfo("openai", "OpenAI", new[] { "gpt-4o" }, IsConfigured: true, SupportsStreaming: true)
        };

        _registry.AvailableProviders.Returns(providers);
        _registry.GetDefaultProvider().Throws(new ProviderNotConfiguredException("No default"));

        _vault.SecretExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        _vault.GetSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("sk-test123"));
        _vault.DeleteSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var vm = CreateViewModel();
        await vm.LoadProvidersAsync();

        // Act
        await vm.DeleteApiKeyCommand.ExecuteAsync(null);

        // Assert
        await _vault.Received(1).DeleteSecretAsync(
            "openai:api-key",
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Property Change Notification Tests

    /// <summary>
    /// Tests that selecting a provider triggers property changed for related properties.
    /// </summary>
    [Fact]
    public async Task SelectedProvider_WhenChanged_ShouldNotifyRelatedProperties()
    {
        // Arrange
        var providers = new List<LLMProviderInfo>
        {
            new LLMProviderInfo("openai", "OpenAI", new[] { "gpt-4o" }, IsConfigured: true, SupportsStreaming: true),
            new LLMProviderInfo("anthropic", "Anthropic", new[] { "claude-3" }, IsConfigured: true, SupportsStreaming: true)
        };

        _registry.AvailableProviders.Returns(providers);
        _registry.GetDefaultProvider().Throws(new ProviderNotConfiguredException("No default"));

        _vault.SecretExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        _vault.GetSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("sk-test123"));

        var vm = CreateViewModel();
        await vm.LoadProvidersAsync();

        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != null)
            {
                changedProperties.Add(e.PropertyName);
            }
        };

        // Act
        vm.SelectedProvider = vm.Providers[1];

        // Assert
        changedProperties.Should().Contain(nameof(LLMSettingsViewModel.CanTestConnection));
        changedProperties.Should().Contain(nameof(LLMSettingsViewModel.CanSetAsDefault));
    }

    #endregion

    #region Dispose Tests

    /// <summary>
    /// Tests that Dispose clears providers and selected provider.
    /// </summary>
    [Fact]
    public async Task Dispose_ShouldClearProvidersAndSelection()
    {
        // Arrange
        var providers = new List<LLMProviderInfo>
        {
            LLMProviderInfo.Create("openai", "OpenAI", new[] { "gpt-4o" })
        };

        _registry.AvailableProviders.Returns(providers);
        _registry.GetDefaultProvider().Throws(new ProviderNotConfiguredException("No default"));

        _vault.SecretExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var vm = CreateViewModel();
        await vm.LoadProvidersAsync();

        // Act
        vm.Dispose();

        // Assert
        vm.Providers.Should().BeEmpty();
        vm.SelectedProvider.Should().BeNull();
    }

    /// <summary>
    /// Tests that Dispose can be called multiple times safely.
    /// </summary>
    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act & Assert
        var action = () =>
        {
            vm.Dispose();
            vm.Dispose();
            vm.Dispose();
        };

        action.Should().NotThrow();
    }

    #endregion
}
