// -----------------------------------------------------------------------
// <copyright file="AnthropicOptionsTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.LLM.Providers.Anthropic;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.LLM.Providers.Anthropic;

/// <summary>
/// Unit tests for <see cref="AnthropicOptions"/>.
/// </summary>
public class AnthropicOptionsTests
{
    #region Default Values Tests

    /// <summary>
    /// Tests that the default base URL is the standard Anthropic endpoint.
    /// </summary>
    [Fact]
    public void DefaultBaseUrl_ShouldBeAnthropicEndpoint()
    {
        // Act
        var options = new AnthropicOptions();

        // Assert
        options.BaseUrl.Should().Be("https://api.anthropic.com/v1");
    }

    /// <summary>
    /// Tests that the default model is claude-3-haiku-20240307.
    /// </summary>
    [Fact]
    public void DefaultModel_ShouldBeClaude3Haiku()
    {
        // Act
        var options = new AnthropicOptions();

        // Assert
        options.DefaultModel.Should().Be("claude-3-haiku-20240307");
    }

    /// <summary>
    /// Tests that the default API version is 2024-01-01.
    /// </summary>
    [Fact]
    public void DefaultApiVersion_ShouldBe20240101()
    {
        // Act
        var options = new AnthropicOptions();

        // Assert
        options.ApiVersion.Should().Be("2024-01-01");
    }

    /// <summary>
    /// Tests that the default max retries is 3.
    /// </summary>
    [Fact]
    public void DefaultMaxRetries_ShouldBe3()
    {
        // Act
        var options = new AnthropicOptions();

        // Assert
        options.MaxRetries.Should().Be(3);
    }

    /// <summary>
    /// Tests that the default timeout is 30 seconds.
    /// </summary>
    [Fact]
    public void DefaultTimeoutSeconds_ShouldBe30()
    {
        // Act
        var options = new AnthropicOptions();

        // Assert
        options.TimeoutSeconds.Should().Be(30);
    }

    #endregion

    #region Vault Key Tests

    /// <summary>
    /// Tests that the vault key constant has the expected value.
    /// </summary>
    [Fact]
    public void VaultKey_ShouldBeAnthropicApiKey()
    {
        // Assert
        AnthropicOptions.VaultKey.Should().Be("anthropic:api-key");
    }

    #endregion

    #region HTTP Client Name Tests

    /// <summary>
    /// Tests that the HTTP client name constant has the expected value.
    /// </summary>
    [Fact]
    public void HttpClientName_ShouldBeAnthropic()
    {
        // Assert
        AnthropicOptions.HttpClientName.Should().Be("Anthropic");
    }

    #endregion

    #region MessagesEndpoint Tests

    /// <summary>
    /// Tests that the messages endpoint is constructed correctly with default base URL.
    /// </summary>
    [Fact]
    public void MessagesEndpoint_WithDefaultBaseUrl_ShouldConstructCorrectly()
    {
        // Arrange
        var options = new AnthropicOptions();

        // Assert
        options.MessagesEndpoint.Should().Be("https://api.anthropic.com/v1/messages");
    }

    /// <summary>
    /// Tests that the messages endpoint is constructed correctly with custom base URL.
    /// </summary>
    [Fact]
    public void MessagesEndpoint_WithCustomBaseUrl_ShouldConstructCorrectly()
    {
        // Arrange
        var options = new AnthropicOptions(BaseUrl: "https://custom-api.example.com/v1");

        // Assert
        options.MessagesEndpoint.Should().Be("https://custom-api.example.com/v1/messages");
    }

    /// <summary>
    /// Tests that the messages endpoint handles base URL without trailing slash.
    /// </summary>
    [Fact]
    public void MessagesEndpoint_WithBaseUrlWithoutTrailingSlash_ShouldConstructCorrectly()
    {
        // Arrange
        var options = new AnthropicOptions(BaseUrl: "https://api.example.com");

        // Assert
        options.MessagesEndpoint.Should().Be("https://api.example.com/messages");
    }

    #endregion

    #region Custom Values Tests

    /// <summary>
    /// Tests that custom values are preserved in the record.
    /// </summary>
    [Fact]
    public void Constructor_WithCustomValues_ShouldPreserveValues()
    {
        // Arrange & Act
        var options = new AnthropicOptions(
            BaseUrl: "https://custom-api.example.com/v2",
            DefaultModel: "claude-3-opus-20240229",
            ApiVersion: "2025-01-01",
            MaxRetries: 5,
            TimeoutSeconds: 60);

        // Assert
        options.BaseUrl.Should().Be("https://custom-api.example.com/v2");
        options.DefaultModel.Should().Be("claude-3-opus-20240229");
        options.ApiVersion.Should().Be("2025-01-01");
        options.MaxRetries.Should().Be(5);
        options.TimeoutSeconds.Should().Be(60);
    }

    /// <summary>
    /// Tests that records are equal when all values match.
    /// </summary>
    [Fact]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var options1 = new AnthropicOptions(
            BaseUrl: "https://api.anthropic.com/v1",
            DefaultModel: "claude-3-haiku-20240307",
            ApiVersion: "2024-01-01",
            MaxRetries: 3,
            TimeoutSeconds: 30);
        var options2 = new AnthropicOptions();

        // Assert
        options1.Should().Be(options2);
    }

    /// <summary>
    /// Tests that records are not equal when values differ.
    /// </summary>
    [Fact]
    public void Equality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var options1 = new AnthropicOptions(DefaultModel: "claude-3-opus-20240229");
        var options2 = new AnthropicOptions(DefaultModel: "claude-3-haiku-20240307");

        // Assert
        options1.Should().NotBe(options2);
    }

    #endregion

    #region Supported Models Tests

    /// <summary>
    /// Tests that supported models list contains expected Claude 3.5 Sonnet model.
    /// </summary>
    [Fact]
    public void SupportedModels_ShouldContainClaude35Sonnet()
    {
        // Assert
        AnthropicOptions.SupportedModels.Should().Contain("claude-3-5-sonnet-20241022");
    }

    /// <summary>
    /// Tests that supported models list contains expected Claude 3 Opus model.
    /// </summary>
    [Fact]
    public void SupportedModels_ShouldContainClaude3Opus()
    {
        // Assert
        AnthropicOptions.SupportedModels.Should().Contain("claude-3-opus-20240229");
    }

    /// <summary>
    /// Tests that supported models list contains expected Claude 3 Sonnet model.
    /// </summary>
    [Fact]
    public void SupportedModels_ShouldContainClaude3Sonnet()
    {
        // Assert
        AnthropicOptions.SupportedModels.Should().Contain("claude-3-sonnet-20240229");
    }

    /// <summary>
    /// Tests that supported models list contains expected Claude 3 Haiku model.
    /// </summary>
    [Fact]
    public void SupportedModels_ShouldContainClaude3Haiku()
    {
        // Assert
        AnthropicOptions.SupportedModels.Should().Contain("claude-3-haiku-20240307");
    }

    /// <summary>
    /// Tests that supported models list contains all expected models.
    /// </summary>
    [Fact]
    public void SupportedModels_ShouldContainAllExpectedModels()
    {
        // Assert
        AnthropicOptions.SupportedModels.Should().BeEquivalentTo(new[]
        {
            "claude-3-5-sonnet-20241022",
            "claude-3-opus-20240229",
            "claude-3-sonnet-20240229",
            "claude-3-haiku-20240307"
        });
    }

    /// <summary>
    /// Tests that supported models list has the expected count.
    /// </summary>
    [Fact]
    public void SupportedModels_ShouldHaveExpectedCount()
    {
        // Assert
        AnthropicOptions.SupportedModels.Should().HaveCount(4);
    }

    /// <summary>
    /// Tests that supported models list is read-only.
    /// </summary>
    [Fact]
    public void SupportedModels_ShouldBeReadOnly()
    {
        // Assert - IReadOnlyList cannot be modified
        AnthropicOptions.SupportedModels.Should().BeAssignableTo<IReadOnlyList<string>>();
    }

    #endregion

    #region Record With Expression Tests

    /// <summary>
    /// Tests that the with expression creates a new instance with modified values.
    /// </summary>
    [Fact]
    public void WithExpression_ShouldCreateNewInstanceWithModifiedValues()
    {
        // Arrange
        var original = new AnthropicOptions();

        // Act
        var modified = original with { DefaultModel = "claude-3-opus-20240229", TimeoutSeconds = 120 };

        // Assert
        modified.DefaultModel.Should().Be("claude-3-opus-20240229");
        modified.TimeoutSeconds.Should().Be(120);
        modified.BaseUrl.Should().Be(original.BaseUrl); // Unchanged
        modified.MaxRetries.Should().Be(original.MaxRetries); // Unchanged
        modified.ApiVersion.Should().Be(original.ApiVersion); // Unchanged
    }

    /// <summary>
    /// Tests that modifying ApiVersion with the with expression works correctly.
    /// </summary>
    [Fact]
    public void WithExpression_ModifyingApiVersion_ShouldWork()
    {
        // Arrange
        var original = new AnthropicOptions();

        // Act
        var modified = original with { ApiVersion = "2025-01-01" };

        // Assert
        modified.ApiVersion.Should().Be("2025-01-01");
        modified.DefaultModel.Should().Be(original.DefaultModel); // Unchanged
    }

    #endregion

    #region Model Name Validation Tests

    /// <summary>
    /// Tests that all supported models follow the expected naming pattern.
    /// </summary>
    [Fact]
    public void SupportedModels_AllShouldStartWithClaude()
    {
        // Assert
        AnthropicOptions.SupportedModels.Should().OnlyContain(
            model => model.StartsWith("claude-", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Tests that all supported models have date suffixes.
    /// </summary>
    [Fact]
    public void SupportedModels_AllShouldHaveDateSuffix()
    {
        // Assert - All Claude 3 models have date suffixes like -20240229
        AnthropicOptions.SupportedModels.Should().OnlyContain(
            model => System.Text.RegularExpressions.Regex.IsMatch(model, @"-\d{8}$"));
    }

    #endregion
}
