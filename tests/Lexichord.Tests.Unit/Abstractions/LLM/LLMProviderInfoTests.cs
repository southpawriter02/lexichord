// -----------------------------------------------------------------------
// <copyright file="LLMProviderInfoTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.LLM;

/// <summary>
/// Unit tests for <see cref="LLMProviderInfo"/> record.
/// </summary>
public class LLMProviderInfoTests
{
    /// <summary>
    /// Tests that constructor creates instance with all properties set.
    /// </summary>
    [Fact]
    public void Constructor_WithValidValues_ShouldCreateInstance()
    {
        // Arrange
        var models = new List<string> { "gpt-4o", "gpt-4o-mini" };

        // Act
        var info = new LLMProviderInfo(
            Name: "openai",
            DisplayName: "OpenAI",
            SupportedModels: models,
            IsConfigured: true,
            SupportsStreaming: true);

        // Assert
        info.Name.Should().Be("openai");
        info.DisplayName.Should().Be("OpenAI");
        info.SupportedModels.Should().BeEquivalentTo(models);
        info.IsConfigured.Should().BeTrue();
        info.SupportsStreaming.Should().BeTrue();
    }

    /// <summary>
    /// Tests that Unconfigured factory creates instance with IsConfigured false.
    /// </summary>
    [Fact]
    public void Unconfigured_ShouldCreateUnconfiguredInstance()
    {
        // Act
        var info = LLMProviderInfo.Unconfigured("anthropic", "Anthropic");

        // Assert
        info.Name.Should().Be("anthropic");
        info.DisplayName.Should().Be("Anthropic");
        info.IsConfigured.Should().BeFalse();
        info.SupportsStreaming.Should().BeFalse();
        info.SupportedModels.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Unconfigured throws on null name.
    /// </summary>
    [Fact]
    public void Unconfigured_WithNullName_ShouldThrowArgumentException()
    {
        // Act
        var action = () => LLMProviderInfo.Unconfigured(null!, "DisplayName");

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that Unconfigured throws on empty name.
    /// </summary>
    [Fact]
    public void Unconfigured_WithEmptyName_ShouldThrowArgumentException()
    {
        // Act
        var action = () => LLMProviderInfo.Unconfigured(string.Empty, "DisplayName");

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that Unconfigured throws on whitespace name.
    /// </summary>
    [Fact]
    public void Unconfigured_WithWhitespaceName_ShouldThrowArgumentException()
    {
        // Act
        var action = () => LLMProviderInfo.Unconfigured("   ", "DisplayName");

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that Unconfigured throws on null display name.
    /// </summary>
    [Fact]
    public void Unconfigured_WithNullDisplayName_ShouldThrowArgumentException()
    {
        // Act
        var action = () => LLMProviderInfo.Unconfigured("name", null!);

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that Create factory method works correctly.
    /// </summary>
    [Fact]
    public void Create_WithValidValues_ShouldCreateInstance()
    {
        // Arrange
        var models = new List<string> { "claude-3-opus", "claude-3-sonnet" };

        // Act
        var info = LLMProviderInfo.Create("anthropic", "Anthropic", models, supportsStreaming: true);

        // Assert
        info.Name.Should().Be("anthropic");
        info.DisplayName.Should().Be("Anthropic");
        info.SupportedModels.Should().BeEquivalentTo(models);
        info.IsConfigured.Should().BeFalse();
        info.SupportsStreaming.Should().BeTrue();
    }

    /// <summary>
    /// Tests that Create with default streaming defaults to true.
    /// </summary>
    [Fact]
    public void Create_WithoutStreamingParameter_ShouldDefaultToTrue()
    {
        // Act
        var info = LLMProviderInfo.Create("openai", "OpenAI", ["gpt-4o"]);

        // Assert
        info.SupportsStreaming.Should().BeTrue();
    }

    /// <summary>
    /// Tests that Create throws on null name.
    /// </summary>
    [Fact]
    public void Create_WithNullName_ShouldThrowArgumentException()
    {
        // Act
        var action = () => LLMProviderInfo.Create(null!, "DisplayName", ["model"]);

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that Create throws on null models.
    /// </summary>
    [Fact]
    public void Create_WithNullModels_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => LLMProviderInfo.Create("name", "DisplayName", null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests ModelCount property returns correct count.
    /// </summary>
    [Fact]
    public void ModelCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var models = new List<string> { "model1", "model2", "model3" };
        var info = LLMProviderInfo.Create("provider", "Provider", models);

        // Act & Assert
        info.ModelCount.Should().Be(3);
    }

    /// <summary>
    /// Tests HasModels property returns true when models exist.
    /// </summary>
    [Fact]
    public void HasModels_WithModels_ShouldReturnTrue()
    {
        // Arrange
        var info = LLMProviderInfo.Create("provider", "Provider", ["model"]);

        // Act & Assert
        info.HasModels.Should().BeTrue();
    }

    /// <summary>
    /// Tests HasModels property returns false when empty.
    /// </summary>
    [Fact]
    public void HasModels_WithNoModels_ShouldReturnFalse()
    {
        // Arrange
        var info = LLMProviderInfo.Unconfigured("provider", "Provider");

        // Act & Assert
        info.HasModels.Should().BeFalse();
    }

    /// <summary>
    /// Tests SupportsModel returns true for supported model.
    /// </summary>
    [Fact]
    public void SupportsModel_WithSupportedModel_ShouldReturnTrue()
    {
        // Arrange
        var info = LLMProviderInfo.Create("openai", "OpenAI", ["gpt-4o", "gpt-4o-mini"]);

        // Act & Assert
        info.SupportsModel("gpt-4o").Should().BeTrue();
    }

    /// <summary>
    /// Tests SupportsModel returns false for unsupported model.
    /// </summary>
    [Fact]
    public void SupportsModel_WithUnsupportedModel_ShouldReturnFalse()
    {
        // Arrange
        var info = LLMProviderInfo.Create("openai", "OpenAI", ["gpt-4o"]);

        // Act & Assert
        info.SupportsModel("claude-3").Should().BeFalse();
    }

    /// <summary>
    /// Tests SupportsModel returns false for null model.
    /// </summary>
    [Fact]
    public void SupportsModel_WithNullModel_ShouldReturnFalse()
    {
        // Arrange
        var info = LLMProviderInfo.Create("openai", "OpenAI", ["gpt-4o"]);

        // Act & Assert
        info.SupportsModel(null!).Should().BeFalse();
    }

    /// <summary>
    /// Tests SupportsModel returns false for empty model.
    /// </summary>
    [Fact]
    public void SupportsModel_WithEmptyModel_ShouldReturnFalse()
    {
        // Arrange
        var info = LLMProviderInfo.Create("openai", "OpenAI", ["gpt-4o"]);

        // Act & Assert
        info.SupportsModel(string.Empty).Should().BeFalse();
    }

    /// <summary>
    /// Tests SupportsModel is case-sensitive.
    /// </summary>
    [Fact]
    public void SupportsModel_IsCaseSensitive()
    {
        // Arrange
        var info = LLMProviderInfo.Create("openai", "OpenAI", ["gpt-4o"]);

        // Act & Assert
        info.SupportsModel("GPT-4O").Should().BeFalse();
    }

    /// <summary>
    /// Tests WithConfigurationStatus creates updated instance.
    /// </summary>
    [Fact]
    public void WithConfigurationStatus_ShouldCreateUpdatedInstance()
    {
        // Arrange
        var info = LLMProviderInfo.Unconfigured("openai", "OpenAI");

        // Act
        var configured = info.WithConfigurationStatus(true);

        // Assert
        configured.IsConfigured.Should().BeTrue();
        info.IsConfigured.Should().BeFalse(); // Original unchanged
    }

    /// <summary>
    /// Tests WithModels creates updated instance.
    /// </summary>
    [Fact]
    public void WithModels_ShouldCreateUpdatedInstance()
    {
        // Arrange
        var info = LLMProviderInfo.Unconfigured("openai", "OpenAI");
        var newModels = new List<string> { "gpt-4o", "gpt-4o-mini" };

        // Act
        var updated = info.WithModels(newModels);

        // Assert
        updated.SupportedModels.Should().BeEquivalentTo(newModels);
        info.SupportedModels.Should().BeEmpty(); // Original unchanged
    }

    /// <summary>
    /// Tests WithModels throws on null.
    /// </summary>
    [Fact]
    public void WithModels_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var info = LLMProviderInfo.Unconfigured("openai", "OpenAI");

        // Act
        var action = () => info.WithModels(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that with expression creates new instance with updated property.
    /// </summary>
    [Fact]
    public void WithExpression_ShouldCreateNewInstance()
    {
        // Arrange
        var info = LLMProviderInfo.Unconfigured("openai", "OpenAI");

        // Act
        var configured = info with { IsConfigured = true };

        // Assert
        configured.IsConfigured.Should().BeTrue();
        info.IsConfigured.Should().BeFalse(); // Original unchanged
    }

    /// <summary>
    /// Tests record equality when values match.
    /// </summary>
    [Fact]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var models = new List<string> { "gpt-4o" };
        var info1 = new LLMProviderInfo("openai", "OpenAI", models, true, true);
        var info2 = new LLMProviderInfo("openai", "OpenAI", models, true, true);

        // Act & Assert
        info1.Should().Be(info2);
    }

    /// <summary>
    /// Tests record inequality when values differ.
    /// </summary>
    [Fact]
    public void Equality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var models = new List<string> { "gpt-4o" };
        var info1 = new LLMProviderInfo("openai", "OpenAI", models, true, true);
        var info2 = new LLMProviderInfo("openai", "OpenAI", models, false, true);

        // Act & Assert
        info1.Should().NotBe(info2);
    }
}
