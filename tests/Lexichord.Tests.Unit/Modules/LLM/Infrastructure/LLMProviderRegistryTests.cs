// -----------------------------------------------------------------------
// <copyright file="LLMProviderRegistryTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Abstractions.Contracts.Security;
using Lexichord.Modules.LLM.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.LLM.Infrastructure;

/// <summary>
/// Unit tests for <see cref="LLMProviderRegistry"/>.
/// </summary>
public class LLMProviderRegistryTests
{
    private readonly MockKeyedServiceProvider _serviceProvider;
    private readonly ISystemSettingsRepository _settings;
    private readonly ISecureVault _vault;
    private readonly ILogger<LLMProviderRegistry> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LLMProviderRegistryTests"/> class.
    /// </summary>
    public LLMProviderRegistryTests()
    {
        _serviceProvider = new MockKeyedServiceProvider();
        _settings = Substitute.For<ISystemSettingsRepository>();
        _vault = Substitute.For<ISecureVault>();
        _logger = NullLogger<LLMProviderRegistry>.Instance;

        // Default settings behavior: return empty string for default provider
        _settings.GetValueAsync(LLMProviderRegistry.DefaultProviderSettingsKey, Arg.Any<string>())
            .Returns(Task.FromResult(string.Empty));
    }

    /// <summary>
    /// Creates a new registry instance with standard test dependencies.
    /// </summary>
    private LLMProviderRegistry CreateRegistry()
    {
        return new LLMProviderRegistry(_serviceProvider, _settings, _vault, _logger);
    }

    /// <summary>
    /// Mock service provider that supports keyed services.
    /// </summary>
    private sealed class MockKeyedServiceProvider : IServiceProvider, IKeyedServiceProvider
    {
        private readonly ConcurrentDictionary<(Type, object?), object?> _keyedServices = new();
        private readonly ConcurrentDictionary<Type, object?> _services = new();

        public void RegisterKeyedService<T>(object key, T? service)
        {
            _keyedServices[(typeof(T), key)] = service;
        }

        public void RegisterService<T>(T? service)
        {
            _services[typeof(T)] = service;
        }

        public object? GetService(Type serviceType)
        {
            return _services.TryGetValue(serviceType, out var service) ? service : null;
        }

        public object? GetKeyedService(Type serviceType, object? serviceKey)
        {
            return _keyedServices.TryGetValue((serviceType, serviceKey), out var service) ? service : null;
        }

        public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
        {
            var service = GetKeyedService(serviceType, serviceKey);
            if (service is null)
            {
                throw new InvalidOperationException($"No keyed service of type {serviceType} with key {serviceKey} was registered.");
            }

            return service;
        }
    }

    #region Constructor Tests

    /// <summary>
    /// Tests that constructor throws on null service provider.
    /// </summary>
    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new LLMProviderRegistry(null!, _settings, _vault, _logger);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceProvider");
    }

    /// <summary>
    /// Tests that constructor throws on null settings.
    /// </summary>
    [Fact]
    public void Constructor_WithNullSettings_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new LLMProviderRegistry(_serviceProvider, null!, _vault, _logger);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("settings");
    }

    /// <summary>
    /// Tests that constructor throws on null vault.
    /// </summary>
    [Fact]
    public void Constructor_WithNullVault_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new LLMProviderRegistry(_serviceProvider, _settings, null!, _logger);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("vault");
    }

    /// <summary>
    /// Tests that constructor throws on null logger.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new LLMProviderRegistry(_serviceProvider, _settings, _vault, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region RegisterProvider Tests

    /// <summary>
    /// Tests that RegisterProvider adds provider to registry.
    /// </summary>
    [Fact]
    public void RegisterProvider_WithValidInfo_ShouldAddToRegistry()
    {
        // Arrange
        var registry = CreateRegistry();
        var info = LLMProviderInfo.Create("openai", "OpenAI", ["gpt-4o"]);

        // Act
        registry.RegisterProvider(info);

        // Assert
        registry.AvailableProviders.Should().ContainSingle()
            .Which.Name.Should().Be("openai");
    }

    /// <summary>
    /// Tests that RegisterProvider throws on null info.
    /// </summary>
    [Fact]
    public void RegisterProvider_WithNullInfo_ShouldThrowArgumentNullException()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        var action = () => registry.RegisterProvider(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("info");
    }

    /// <summary>
    /// Tests that RegisterProvider replaces existing provider with same name.
    /// </summary>
    [Fact]
    public void RegisterProvider_WithExistingName_ShouldReplaceProvider()
    {
        // Arrange
        var registry = CreateRegistry();
        var original = LLMProviderInfo.Create("openai", "OpenAI", ["gpt-4"]);
        var replacement = LLMProviderInfo.Create("openai", "OpenAI Updated", ["gpt-4o"]);

        // Act
        registry.RegisterProvider(original);
        registry.RegisterProvider(replacement);

        // Assert
        registry.AvailableProviders.Should().ContainSingle()
            .Which.DisplayName.Should().Be("OpenAI Updated");
    }

    /// <summary>
    /// Tests that RegisterProvider handles case-insensitive names.
    /// </summary>
    [Fact]
    public void RegisterProvider_WithDifferentCase_ShouldTreatAsSameProvider()
    {
        // Arrange
        var registry = CreateRegistry();
        var info1 = LLMProviderInfo.Create("OpenAI", "OpenAI", ["gpt-4"]);
        var info2 = LLMProviderInfo.Create("OPENAI", "OpenAI Updated", ["gpt-4o"]);

        // Act
        registry.RegisterProvider(info1);
        registry.RegisterProvider(info2);

        // Assert
        registry.AvailableProviders.Should().ContainSingle();
    }

    #endregion

    #region GetProvider Tests

    /// <summary>
    /// Tests that GetProvider throws on null provider name.
    /// </summary>
    [Fact]
    public void GetProvider_WithNullName_ShouldThrowArgumentException()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        var action = () => registry.GetProvider(null!);

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that GetProvider throws on empty provider name.
    /// </summary>
    [Fact]
    public void GetProvider_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        var action = () => registry.GetProvider(string.Empty);

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that GetProvider throws ProviderNotFoundException for unknown provider.
    /// </summary>
    [Fact]
    public void GetProvider_WithUnknownName_ShouldThrowProviderNotFoundException()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        var action = () => registry.GetProvider("unknown-provider");

        // Assert
        action.Should().Throw<ProviderNotFoundException>()
            .Which.ProviderName.Should().Be("unknown-provider");
    }

    /// <summary>
    /// Tests that GetProvider throws ProviderNotConfiguredException when not configured.
    /// </summary>
    [Fact]
    public void GetProvider_WithUnconfiguredProvider_ShouldThrowProviderNotConfiguredException()
    {
        // Arrange
        var registry = CreateRegistry();
        var info = LLMProviderInfo.Unconfigured("openai", "OpenAI");
        registry.RegisterProvider(info);

        // Act
        var action = () => registry.GetProvider("openai");

        // Assert
        action.Should().Throw<ProviderNotConfiguredException>();
    }

    /// <summary>
    /// Tests that GetProvider resolves configured provider.
    /// </summary>
    [Fact]
    public void GetProvider_WithConfiguredProvider_ShouldReturnService()
    {
        // Arrange
        var registry = CreateRegistry();
        var info = new LLMProviderInfo("openai", "OpenAI", ["gpt-4o"], IsConfigured: true, SupportsStreaming: true);
        registry.RegisterProvider(info);

        var mockService = Substitute.For<IChatCompletionService>();
        mockService.ProviderName.Returns("openai");

        _serviceProvider.RegisterKeyedService<IChatCompletionService>("openai", mockService);

        // Act
        var result = registry.GetProvider("openai");

        // Assert
        result.Should().Be(mockService);
    }

    /// <summary>
    /// Tests that GetProvider is case-insensitive.
    /// </summary>
    [Fact]
    public void GetProvider_WithDifferentCase_ShouldResolveProvider()
    {
        // Arrange
        var registry = CreateRegistry();
        var info = new LLMProviderInfo("openai", "OpenAI", ["gpt-4o"], IsConfigured: true, SupportsStreaming: true);
        registry.RegisterProvider(info);

        var mockService = Substitute.For<IChatCompletionService>();
        mockService.ProviderName.Returns("openai");

        _serviceProvider.RegisterKeyedService<IChatCompletionService>("openai", mockService);

        // Act
        var result = registry.GetProvider("OpenAI");

        // Assert
        result.Should().Be(mockService);
    }

    /// <summary>
    /// Tests that GetProvider throws when service is not in DI container.
    /// </summary>
    [Fact]
    public void GetProvider_WithMissingServiceInDI_ShouldThrowProviderNotFoundException()
    {
        // Arrange
        var registry = CreateRegistry();
        var info = new LLMProviderInfo("openai", "OpenAI", ["gpt-4o"], IsConfigured: true, SupportsStreaming: true);
        registry.RegisterProvider(info);

        // Don't register any keyed service - simulate missing service in DI

        // Act
        var action = () => registry.GetProvider("openai");

        // Assert
        action.Should().Throw<ProviderNotFoundException>()
            .WithMessage("*DI container*");
    }

    #endregion

    #region GetDefaultProvider Tests

    /// <summary>
    /// Tests that GetDefaultProvider returns persisted default when available.
    /// </summary>
    [Fact]
    public void GetDefaultProvider_WithPersistedDefault_ShouldReturnProvider()
    {
        // Arrange
        var registry = CreateRegistry();
        var info = new LLMProviderInfo("anthropic", "Anthropic", ["claude-3"], IsConfigured: true, SupportsStreaming: true);
        registry.RegisterProvider(info);

        var mockService = Substitute.For<IChatCompletionService>();
        mockService.ProviderName.Returns("anthropic");

        _serviceProvider.RegisterKeyedService<IChatCompletionService>("anthropic", mockService);

        _settings.GetValueAsync(LLMProviderRegistry.DefaultProviderSettingsKey, Arg.Any<string>())
            .Returns(Task.FromResult("anthropic"));

        // Act
        var result = registry.GetDefaultProvider();

        // Assert
        result.Should().Be(mockService);
    }

    /// <summary>
    /// Tests that GetDefaultProvider falls back to first configured when no default set.
    /// </summary>
    [Fact]
    public void GetDefaultProvider_WithNoDefault_ShouldFallbackToFirstConfigured()
    {
        // Arrange
        var registry = CreateRegistry();
        var info1 = LLMProviderInfo.Unconfigured("openai", "OpenAI");
        var info2 = new LLMProviderInfo("anthropic", "Anthropic", ["claude-3"], IsConfigured: true, SupportsStreaming: true);
        registry.RegisterProvider(info1);
        registry.RegisterProvider(info2);

        var mockService = Substitute.For<IChatCompletionService>();
        mockService.ProviderName.Returns("anthropic");

        _serviceProvider.RegisterKeyedService<IChatCompletionService>("anthropic", mockService);

        // Act
        var result = registry.GetDefaultProvider();

        // Assert
        result.Should().Be(mockService);
    }

    /// <summary>
    /// Tests that GetDefaultProvider throws when no providers are configured.
    /// </summary>
    [Fact]
    public void GetDefaultProvider_WithNoConfiguredProviders_ShouldThrowProviderNotConfiguredException()
    {
        // Arrange
        var registry = CreateRegistry();
        var info = LLMProviderInfo.Unconfigured("openai", "OpenAI");
        registry.RegisterProvider(info);

        // Act
        var action = () => registry.GetDefaultProvider();

        // Assert
        action.Should().Throw<ProviderNotConfiguredException>();
    }

    /// <summary>
    /// Tests that GetDefaultProvider falls back when persisted default is not registered.
    /// </summary>
    [Fact]
    public void GetDefaultProvider_WithUnregisteredPersistedDefault_ShouldFallbackToFirstConfigured()
    {
        // Arrange
        var registry = CreateRegistry();
        var info = new LLMProviderInfo("anthropic", "Anthropic", ["claude-3"], IsConfigured: true, SupportsStreaming: true);
        registry.RegisterProvider(info);

        var mockService = Substitute.For<IChatCompletionService>();
        mockService.ProviderName.Returns("anthropic");

        _serviceProvider.RegisterKeyedService<IChatCompletionService>("anthropic", mockService);

        _settings.GetValueAsync(LLMProviderRegistry.DefaultProviderSettingsKey, Arg.Any<string>())
            .Returns(Task.FromResult("unknown-provider"));

        // Act
        var result = registry.GetDefaultProvider();

        // Assert
        result.Should().Be(mockService);
    }

    #endregion

    #region SetDefaultProvider Tests

    /// <summary>
    /// Tests that SetDefaultProvider throws on null name.
    /// </summary>
    [Fact]
    public void SetDefaultProvider_WithNullName_ShouldThrowArgumentException()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        var action = () => registry.SetDefaultProvider(null!);

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that SetDefaultProvider throws on unknown provider.
    /// </summary>
    [Fact]
    public void SetDefaultProvider_WithUnknownProvider_ShouldThrowProviderNotFoundException()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        var action = () => registry.SetDefaultProvider("unknown");

        // Assert
        action.Should().Throw<ProviderNotFoundException>();
    }

    /// <summary>
    /// Tests that SetDefaultProvider persists the preference.
    /// </summary>
    [Fact]
    public void SetDefaultProvider_WithValidProvider_ShouldPersistToSettings()
    {
        // Arrange
        var registry = CreateRegistry();
        var info = LLMProviderInfo.Unconfigured("openai", "OpenAI");
        registry.RegisterProvider(info);

        _settings.SetValueAsync(
            LLMProviderRegistry.DefaultProviderSettingsKey,
            "openai",
            Arg.Any<string>())
            .Returns(Task.CompletedTask);

        // Act
        registry.SetDefaultProvider("openai");

        // Assert
        _settings.Received(1).SetValueAsync(
            LLMProviderRegistry.DefaultProviderSettingsKey,
            "openai",
            Arg.Any<string>());
    }

    /// <summary>
    /// Tests that SetDefaultProvider normalizes provider name.
    /// </summary>
    [Fact]
    public void SetDefaultProvider_WithUppercaseName_ShouldNormalize()
    {
        // Arrange
        var registry = CreateRegistry();
        var info = LLMProviderInfo.Unconfigured("openai", "OpenAI");
        registry.RegisterProvider(info);

        _settings.SetValueAsync(
            LLMProviderRegistry.DefaultProviderSettingsKey,
            "openai",
            Arg.Any<string>())
            .Returns(Task.CompletedTask);

        // Act
        registry.SetDefaultProvider("OPENAI");

        // Assert
        _settings.Received(1).SetValueAsync(
            LLMProviderRegistry.DefaultProviderSettingsKey,
            "openai",
            Arg.Any<string>());
    }

    #endregion

    #region IsProviderConfigured Tests

    /// <summary>
    /// Tests that IsProviderConfigured returns false for null name.
    /// </summary>
    [Fact]
    public void IsProviderConfigured_WithNullName_ShouldReturnFalse()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        var result = registry.IsProviderConfigured(null!);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that IsProviderConfigured returns false for empty name.
    /// </summary>
    [Fact]
    public void IsProviderConfigured_WithEmptyName_ShouldReturnFalse()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        var result = registry.IsProviderConfigured(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that IsProviderConfigured returns false for unknown provider.
    /// </summary>
    [Fact]
    public void IsProviderConfigured_WithUnknownProvider_ShouldReturnFalse()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        var result = registry.IsProviderConfigured("unknown");

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that IsProviderConfigured returns false for unconfigured provider.
    /// </summary>
    [Fact]
    public void IsProviderConfigured_WithUnconfiguredProvider_ShouldReturnFalse()
    {
        // Arrange
        var registry = CreateRegistry();
        var info = LLMProviderInfo.Unconfigured("openai", "OpenAI");
        registry.RegisterProvider(info);

        // Act
        var result = registry.IsProviderConfigured("openai");

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that IsProviderConfigured returns true for configured provider.
    /// </summary>
    [Fact]
    public void IsProviderConfigured_WithConfiguredProvider_ShouldReturnTrue()
    {
        // Arrange
        var registry = CreateRegistry();
        var info = new LLMProviderInfo("openai", "OpenAI", ["gpt-4o"], IsConfigured: true, SupportsStreaming: true);
        registry.RegisterProvider(info);

        // Act
        var result = registry.IsProviderConfigured("openai");

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Tests that IsProviderConfigured is case-insensitive.
    /// </summary>
    [Fact]
    public void IsProviderConfigured_WithDifferentCase_ShouldWork()
    {
        // Arrange
        var registry = CreateRegistry();
        var info = new LLMProviderInfo("openai", "OpenAI", ["gpt-4o"], IsConfigured: true, SupportsStreaming: true);
        registry.RegisterProvider(info);

        // Act & Assert
        registry.IsProviderConfigured("OpenAI").Should().BeTrue();
        registry.IsProviderConfigured("OPENAI").Should().BeTrue();
    }

    #endregion

    #region AvailableProviders Tests

    /// <summary>
    /// Tests that AvailableProviders returns empty list when no providers registered.
    /// </summary>
    [Fact]
    public void AvailableProviders_WhenEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act & Assert
        registry.AvailableProviders.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that AvailableProviders returns all registered providers.
    /// </summary>
    [Fact]
    public void AvailableProviders_WithMultipleProviders_ShouldReturnAll()
    {
        // Arrange
        var registry = CreateRegistry();
        registry.RegisterProvider(LLMProviderInfo.Unconfigured("openai", "OpenAI"));
        registry.RegisterProvider(LLMProviderInfo.Unconfigured("anthropic", "Anthropic"));
        registry.RegisterProvider(LLMProviderInfo.Unconfigured("ollama", "Ollama"));

        // Act
        var providers = registry.AvailableProviders;

        // Assert
        providers.Should().HaveCount(3);
        providers.Select(p => p.Name).Should().Contain(new[] { "openai", "anthropic", "ollama" });
    }

    #endregion

    #region RefreshConfigurationStatusAsync Tests

    /// <summary>
    /// Tests that RefreshConfigurationStatusAsync updates provider configuration status.
    /// </summary>
    [Fact]
    public async Task RefreshConfigurationStatusAsync_ShouldUpdateProviderStatus()
    {
        // Arrange
        var registry = CreateRegistry();
        var info = LLMProviderInfo.Unconfigured("openai", "OpenAI");
        registry.RegisterProvider(info);

        _vault.SecretExistsAsync("openai:api-key", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act
        await registry.RefreshConfigurationStatusAsync();

        // Assert
        registry.IsProviderConfigured("openai").Should().BeTrue();
    }

    /// <summary>
    /// Tests that RefreshConfigurationStatusAsync handles vault errors gracefully.
    /// </summary>
    [Fact]
    public async Task RefreshConfigurationStatusAsync_WithVaultError_ShouldContinueWithOtherProviders()
    {
        // Arrange
        var registry = CreateRegistry();
        registry.RegisterProvider(LLMProviderInfo.Unconfigured("openai", "OpenAI"));
        registry.RegisterProvider(LLMProviderInfo.Unconfigured("anthropic", "Anthropic"));

        _vault.SecretExistsAsync("openai:api-key", Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Vault error"));

        _vault.SecretExistsAsync("anthropic:api-key", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act
        await registry.RefreshConfigurationStatusAsync();

        // Assert
        registry.IsProviderConfigured("openai").Should().BeFalse();
        registry.IsProviderConfigured("anthropic").Should().BeTrue();
    }

    /// <summary>
    /// Tests that RefreshConfigurationStatusAsync supports cancellation.
    /// </summary>
    [Fact]
    public async Task RefreshConfigurationStatusAsync_WithCancellation_ShouldHonorToken()
    {
        // Arrange
        var registry = CreateRegistry();
        registry.RegisterProvider(LLMProviderInfo.Unconfigured("openai", "OpenAI"));

        using var cts = new CancellationTokenSource();

        _vault.SecretExistsAsync("openai:api-key", Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var token = callInfo.Arg<CancellationToken>();
                token.ThrowIfCancellationRequested();
                return Task.FromResult(true);
            });

        // Act - Cancel before running
        cts.Cancel();

        // The implementation catches exceptions, so this won't throw
        // but the vault check will be cancelled
        await registry.RefreshConfigurationStatusAsync(cts.Token);

        // Assert - Status should remain unchanged since check was cancelled
        registry.IsProviderConfigured("openai").Should().BeFalse();
    }

    #endregion
}
