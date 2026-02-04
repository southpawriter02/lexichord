// -----------------------------------------------------------------------
// <copyright file="ModelDefaultsTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.LLM.Configuration;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.LLM.Configuration;

/// <summary>
/// Unit tests for <see cref="ModelDefaults"/>.
/// </summary>
public class ModelDefaultsTests
{
    #region GetDefaults Tests

    /// <summary>
    /// Tests that known OpenAI models return defaults with correct model and MaxTokens.
    /// </summary>
    [Theory]
    [InlineData("gpt-4o")]
    [InlineData("gpt-4o-mini")]
    [InlineData("gpt-4-turbo")]
    [InlineData("gpt-3.5-turbo")]
    public void GetDefaults_WithKnownOpenAIModel_ShouldReturnOptionsWithModel(string model)
    {
        // Act
        var options = ModelDefaults.GetDefaults(model);

        // Assert
        options.Model.Should().Be(model);
        options.MaxTokens.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that known Anthropic models return defaults with correct model.
    /// </summary>
    [Theory]
    [InlineData("claude-3-opus-20240229")]
    [InlineData("claude-3-sonnet-20240229")]
    [InlineData("claude-3-haiku-20240307")]
    [InlineData("claude-3-5-sonnet-20241022")]
    public void GetDefaults_WithKnownAnthropicModel_ShouldReturnOptionsWithModel(string model)
    {
        // Act
        var options = ModelDefaults.GetDefaults(model);

        // Assert
        options.Model.Should().Be(model);
        options.MaxTokens.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that unknown models return default options with the model set.
    /// </summary>
    [Fact]
    public void GetDefaults_WithUnknownModel_ShouldReturnDefaultOptionsWithModel()
    {
        // Arrange
        var unknownModel = "unknown-model-12345";

        // Act
        var options = ModelDefaults.GetDefaults(unknownModel);

        // Assert
        options.Model.Should().Be(unknownModel);
    }

    /// <summary>
    /// Tests that empty model returns empty default options.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void GetDefaults_WithEmptyOrNullModel_ShouldReturnDefaultOptions(string? model)
    {
        // Act
        var options = ModelDefaults.GetDefaults(model!);

        // Assert
        options.Model.Should().BeNull();
    }

    #endregion

    #region GetModelList Tests

    /// <summary>
    /// Tests that OpenAI provider returns known models.
    /// </summary>
    [Fact]
    public void GetModelList_WithOpenAI_ShouldReturnKnownModels()
    {
        // Act
        var models = ModelDefaults.GetModelList("openai");

        // Assert
        models.Should().NotBeEmpty();
        models.Should().Contain("gpt-4o");
        models.Should().Contain("gpt-4o-mini");
    }

    /// <summary>
    /// Tests that Anthropic provider returns known models.
    /// </summary>
    [Fact]
    public void GetModelList_WithAnthropic_ShouldReturnKnownModels()
    {
        // Act
        var models = ModelDefaults.GetModelList("anthropic");

        // Assert
        models.Should().NotBeEmpty();
        models.Should().Contain("claude-3-opus-20240229");
        models.Should().Contain("claude-3-haiku-20240307");
    }

    /// <summary>
    /// Tests that unknown provider returns empty list.
    /// </summary>
    [Fact]
    public void GetModelList_WithUnknownProvider_ShouldReturnEmptyList()
    {
        // Act
        var models = ModelDefaults.GetModelList("unknown-provider");

        // Assert
        models.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that provider name is case-insensitive.
    /// </summary>
    [Theory]
    [InlineData("openai")]
    [InlineData("OpenAI")]
    [InlineData("OPENAI")]
    public void GetModelList_ShouldBeCaseInsensitive(string provider)
    {
        // Act
        var models = ModelDefaults.GetModelList(provider);

        // Assert
        models.Should().NotBeEmpty();
    }

    #endregion

    #region GetModelInfo Tests

    /// <summary>
    /// Tests that GetModelInfo returns info for known models.
    /// </summary>
    [Fact]
    public void GetModelInfo_WithKnownModel_ShouldReturnInfo()
    {
        // Act
        var info = ModelDefaults.GetModelInfo("gpt-4o");

        // Assert
        info.Should().NotBeNull();
        info!.Id.Should().Be("gpt-4o");
        info.ContextWindow.Should().BeGreaterThan(0);
        info.MaxOutputTokens.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that GetModelInfo returns null for unknown models.
    /// </summary>
    [Fact]
    public void GetModelInfo_WithUnknownModel_ShouldReturnNull()
    {
        // Act
        var info = ModelDefaults.GetModelInfo("unknown-model");

        // Assert
        info.Should().BeNull();
    }

    #endregion

    #region GetContextWindow Tests

    /// <summary>
    /// Tests that known models return their context window size.
    /// </summary>
    [Fact]
    public void GetContextWindow_WithKnownModel_ShouldReturnCorrectSize()
    {
        // Act
        var contextWindow = ModelDefaults.GetContextWindow("gpt-4o");

        // Assert
        contextWindow.Should().Be(128000);
    }

    /// <summary>
    /// Tests that unknown models return default context window.
    /// </summary>
    [Fact]
    public void GetContextWindow_WithUnknownModel_ShouldReturnDefault()
    {
        // Act
        var contextWindow = ModelDefaults.GetContextWindow("unknown-model");

        // Assert
        contextWindow.Should().Be(ModelDefaults.DefaultContextWindow);
    }

    #endregion

    #region SupportsVision Tests

    /// <summary>
    /// Tests that vision-capable models are identified correctly.
    /// </summary>
    [Theory]
    [InlineData("gpt-4o", true)]
    [InlineData("gpt-4o-mini", true)]
    [InlineData("gpt-3.5-turbo", false)]
    [InlineData("claude-3-opus-20240229", true)]
    public void SupportsVision_ShouldReturnCorrectValue(string model, bool expected)
    {
        // Act
        var supportsVision = ModelDefaults.SupportsVision(model);

        // Assert
        supportsVision.Should().Be(expected);
    }

    #endregion

    #region SupportsTools Tests

    /// <summary>
    /// Tests that tool-capable models are identified correctly.
    /// </summary>
    [Theory]
    [InlineData("gpt-4o", true)]
    [InlineData("gpt-3.5-turbo", true)]
    [InlineData("claude-3-opus-20240229", true)]
    public void SupportsTools_ShouldReturnCorrectValue(string model, bool expected)
    {
        // Act
        var supportsTools = ModelDefaults.SupportsTools(model);

        // Assert
        supportsTools.Should().Be(expected);
    }

    #endregion

    #region GetAllKnownModels/Providers Tests

    /// <summary>
    /// Tests that GetAllKnownModels returns a non-empty list.
    /// </summary>
    [Fact]
    public void GetAllKnownModels_ShouldReturnNonEmptyList()
    {
        // Act
        var models = ModelDefaults.GetAllKnownModels();

        // Assert
        models.Should().NotBeEmpty();
        models.Should().Contain("gpt-4o");
    }

    /// <summary>
    /// Tests that GetAllKnownProviders returns OpenAI and Anthropic.
    /// </summary>
    [Fact]
    public void GetAllKnownProviders_ShouldReturnKnownProviders()
    {
        // Act
        var providers = ModelDefaults.GetAllKnownProviders();

        // Assert
        providers.Should().NotBeEmpty();
        providers.Should().Contain("openai");
        providers.Should().Contain("anthropic");
    }

    #endregion
}
