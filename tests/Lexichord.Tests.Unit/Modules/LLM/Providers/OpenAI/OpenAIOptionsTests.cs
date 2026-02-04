// -----------------------------------------------------------------------
// <copyright file="OpenAIOptionsTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.LLM.Providers.OpenAI;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.LLM.Providers.OpenAI;

/// <summary>
/// Unit tests for <see cref="OpenAIOptions"/>.
/// </summary>
public class OpenAIOptionsTests
{
    #region Default Values Tests

    /// <summary>
    /// Tests that the default base URL is the standard OpenAI endpoint.
    /// </summary>
    [Fact]
    public void DefaultBaseUrl_ShouldBeOpenAIEndpoint()
    {
        // Act
        var options = new OpenAIOptions();

        // Assert
        options.BaseUrl.Should().Be("https://api.openai.com/v1");
    }

    /// <summary>
    /// Tests that the default model is gpt-4o-mini.
    /// </summary>
    [Fact]
    public void DefaultModel_ShouldBeGpt4oMini()
    {
        // Act
        var options = new OpenAIOptions();

        // Assert
        options.DefaultModel.Should().Be("gpt-4o-mini");
    }

    /// <summary>
    /// Tests that the default max retries is 3.
    /// </summary>
    [Fact]
    public void DefaultMaxRetries_ShouldBe3()
    {
        // Act
        var options = new OpenAIOptions();

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
        var options = new OpenAIOptions();

        // Assert
        options.TimeoutSeconds.Should().Be(30);
    }

    #endregion

    #region Vault Key Tests

    /// <summary>
    /// Tests that the vault key constant has the expected value.
    /// </summary>
    [Fact]
    public void VaultKey_ShouldBeOpenAiApiKey()
    {
        // Assert
        OpenAIOptions.VaultKey.Should().Be("openai:api-key");
    }

    #endregion

    #region HTTP Client Name Tests

    /// <summary>
    /// Tests that the HTTP client name constant has the expected value.
    /// </summary>
    [Fact]
    public void HttpClientName_ShouldBeOpenAI()
    {
        // Assert
        OpenAIOptions.HttpClientName.Should().Be("OpenAI");
    }

    #endregion

    #region CompletionsEndpoint Tests

    /// <summary>
    /// Tests that the completions endpoint is constructed correctly with default base URL.
    /// </summary>
    [Fact]
    public void CompletionsEndpoint_WithDefaultBaseUrl_ShouldConstructCorrectly()
    {
        // Arrange
        var options = new OpenAIOptions();

        // Assert
        options.CompletionsEndpoint.Should().Be("https://api.openai.com/v1/chat/completions");
    }

    /// <summary>
    /// Tests that the completions endpoint is constructed correctly with custom base URL.
    /// </summary>
    [Fact]
    public void CompletionsEndpoint_WithCustomBaseUrl_ShouldConstructCorrectly()
    {
        // Arrange
        var options = new OpenAIOptions(BaseUrl: "https://custom-api.example.com/v1");

        // Assert
        options.CompletionsEndpoint.Should().Be("https://custom-api.example.com/v1/chat/completions");
    }

    /// <summary>
    /// Tests that the completions endpoint handles base URL without trailing slash.
    /// </summary>
    [Fact]
    public void CompletionsEndpoint_WithBaseUrlWithoutTrailingSlash_ShouldConstructCorrectly()
    {
        // Arrange
        var options = new OpenAIOptions(BaseUrl: "https://api.example.com");

        // Assert
        options.CompletionsEndpoint.Should().Be("https://api.example.com/chat/completions");
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
        var options = new OpenAIOptions(
            BaseUrl: "https://custom-api.example.com/v2",
            DefaultModel: "gpt-4",
            MaxRetries: 5,
            TimeoutSeconds: 60);

        // Assert
        options.BaseUrl.Should().Be("https://custom-api.example.com/v2");
        options.DefaultModel.Should().Be("gpt-4");
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
        var options1 = new OpenAIOptions(
            BaseUrl: "https://api.openai.com/v1",
            DefaultModel: "gpt-4o-mini",
            MaxRetries: 3,
            TimeoutSeconds: 30);
        var options2 = new OpenAIOptions();

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
        var options1 = new OpenAIOptions(DefaultModel: "gpt-4");
        var options2 = new OpenAIOptions(DefaultModel: "gpt-4o-mini");

        // Assert
        options1.Should().NotBe(options2);
    }

    #endregion

    #region Supported Models Tests

    /// <summary>
    /// Tests that supported models list contains expected models.
    /// </summary>
    [Fact]
    public void SupportedModels_ShouldContainExpectedModels()
    {
        // Assert
        OpenAIOptions.SupportedModels.Should().Contain("gpt-4o");
        OpenAIOptions.SupportedModels.Should().Contain("gpt-4o-mini");
        OpenAIOptions.SupportedModels.Should().Contain("gpt-4-turbo");
        OpenAIOptions.SupportedModels.Should().Contain("gpt-3.5-turbo");
    }

    /// <summary>
    /// Tests that supported models list has the expected count.
    /// </summary>
    [Fact]
    public void SupportedModels_ShouldHaveExpectedCount()
    {
        // Assert
        OpenAIOptions.SupportedModels.Should().HaveCount(4);
    }

    /// <summary>
    /// Tests that supported models list is read-only.
    /// </summary>
    [Fact]
    public void SupportedModels_ShouldBeReadOnly()
    {
        // Assert - IReadOnlyList cannot be modified
        OpenAIOptions.SupportedModels.Should().BeAssignableTo<IReadOnlyList<string>>();
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
        var original = new OpenAIOptions();

        // Act
        var modified = original with { DefaultModel = "gpt-4", TimeoutSeconds = 120 };

        // Assert
        modified.DefaultModel.Should().Be("gpt-4");
        modified.TimeoutSeconds.Should().Be(120);
        modified.BaseUrl.Should().Be(original.BaseUrl); // Unchanged
        modified.MaxRetries.Should().Be(original.MaxRetries); // Unchanged
    }

    #endregion
}
