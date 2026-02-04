// -----------------------------------------------------------------------
// <copyright file="LLMSettingsPageTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.LLM.Presentation.ViewModels;
using Lexichord.Modules.LLM.Settings;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.LLM.Settings;

/// <summary>
/// Unit tests for <see cref="LLMSettingsPage"/>.
/// </summary>
/// <remarks>
/// Tests cover:
/// <list type="bullet">
///   <item><description>Constructor validation</description></item>
///   <item><description>Property values</description></item>
///   <item><description>ISettingsPage contract compliance</description></item>
/// </list>
/// <para>
/// Note: <see cref="LLMSettingsPage.CreateView"/> is not tested here because it requires
/// Avalonia UI runtime. Integration tests should cover view creation.
/// </para>
/// <para>
/// <b>Version:</b> v0.6.1d
/// </para>
/// </remarks>
public class LLMSettingsPageTests
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="LLMSettingsPageTests"/> class.
    /// </summary>
    public LLMSettingsPageTests()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();
    }

    #region Constructor Tests

    /// <summary>
    /// Tests that constructor throws on null service provider.
    /// </summary>
    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new LLMSettingsPage(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceProvider");
    }

    /// <summary>
    /// Tests that constructor accepts valid service provider.
    /// </summary>
    [Fact]
    public void Constructor_WithValidServiceProvider_ShouldNotThrow()
    {
        // Act
        var action = () => new LLMSettingsPage(_serviceProvider);

        // Assert
        action.Should().NotThrow();
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// Tests that CategoryId has the expected value.
    /// </summary>
    [Fact]
    public void CategoryId_ShouldReturnExpectedValue()
    {
        // Arrange
        var page = new LLMSettingsPage(_serviceProvider);

        // Assert
        page.CategoryId.Should().Be("llm.providers");
    }

    /// <summary>
    /// Tests that DisplayName has the expected value.
    /// </summary>
    [Fact]
    public void DisplayName_ShouldReturnExpectedValue()
    {
        // Arrange
        var page = new LLMSettingsPage(_serviceProvider);

        // Assert
        page.DisplayName.Should().Be("LLM Providers");
    }

    /// <summary>
    /// Tests that ParentCategoryId is null (root category).
    /// </summary>
    [Fact]
    public void ParentCategoryId_ShouldBeNull()
    {
        // Arrange
        var page = new LLMSettingsPage(_serviceProvider);

        // Assert
        page.ParentCategoryId.Should().BeNull();
    }

    /// <summary>
    /// Tests that Icon has the expected value.
    /// </summary>
    [Fact]
    public void Icon_ShouldReturnExpectedValue()
    {
        // Arrange
        var page = new LLMSettingsPage(_serviceProvider);

        // Assert
        page.Icon.Should().Be("Robot");
    }

    /// <summary>
    /// Tests that SortOrder has the expected value.
    /// </summary>
    [Fact]
    public void SortOrder_ShouldReturnExpectedValue()
    {
        // Arrange
        var page = new LLMSettingsPage(_serviceProvider);

        // Assert
        page.SortOrder.Should().Be(75);
    }

    /// <summary>
    /// Tests that SortOrder places the page after Account but before Editor.
    /// </summary>
    [Fact]
    public void SortOrder_ShouldBeAfterAccountAndBeforeEditor()
    {
        // Arrange
        var page = new LLMSettingsPage(_serviceProvider);
        const int accountSortOrder = 0;
        const int editorSortOrder = 100;

        // Assert
        page.SortOrder.Should().BeGreaterThan(accountSortOrder);
        page.SortOrder.Should().BeLessThan(editorSortOrder);
    }

    /// <summary>
    /// Tests that RequiredTier is Core (visible to all users).
    /// </summary>
    [Fact]
    public void RequiredTier_ShouldBeCore()
    {
        // Arrange
        var page = new LLMSettingsPage(_serviceProvider);

        // Assert
        page.RequiredTier.Should().Be(LicenseTier.Core);
    }

    /// <summary>
    /// Tests that SearchKeywords contains expected terms.
    /// </summary>
    [Fact]
    public void SearchKeywords_ShouldContainExpectedTerms()
    {
        // Arrange
        var page = new LLMSettingsPage(_serviceProvider);

        // Assert
        page.SearchKeywords.Should().NotBeEmpty();
        page.SearchKeywords.Should().Contain("llm");
        page.SearchKeywords.Should().Contain("api");
        page.SearchKeywords.Should().Contain("openai");
        page.SearchKeywords.Should().Contain("anthropic");
        page.SearchKeywords.Should().Contain("provider");
    }

    /// <summary>
    /// Tests that SearchKeywords includes AI-related terms.
    /// </summary>
    [Fact]
    public void SearchKeywords_ShouldIncludeAITerms()
    {
        // Arrange
        var page = new LLMSettingsPage(_serviceProvider);

        // Assert
        page.SearchKeywords.Should().Contain("ai");
        page.SearchKeywords.Should().Contain("gpt");
        page.SearchKeywords.Should().Contain("claude");
    }

    #endregion

    #region ISettingsPage Contract Tests

    /// <summary>
    /// Tests that the page implements ISettingsPage.
    /// </summary>
    [Fact]
    public void Page_ShouldImplementISettingsPage()
    {
        // Arrange
        var page = new LLMSettingsPage(_serviceProvider);

        // Assert
        page.Should().BeAssignableTo<ISettingsPage>();
    }

    /// <summary>
    /// Tests that CategoryId is not null or empty.
    /// </summary>
    [Fact]
    public void CategoryId_ShouldNotBeNullOrEmpty()
    {
        // Arrange
        var page = new LLMSettingsPage(_serviceProvider);

        // Assert
        page.CategoryId.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Tests that DisplayName is not null or empty.
    /// </summary>
    [Fact]
    public void DisplayName_ShouldNotBeNullOrEmpty()
    {
        // Arrange
        var page = new LLMSettingsPage(_serviceProvider);

        // Assert
        page.DisplayName.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Tests that SortOrder is non-negative.
    /// </summary>
    [Fact]
    public void SortOrder_ShouldBeNonNegative()
    {
        // Arrange
        var page = new LLMSettingsPage(_serviceProvider);

        // Assert
        page.SortOrder.Should().BeGreaterThanOrEqualTo(0);
    }

    /// <summary>
    /// Tests that SearchKeywords is not null.
    /// </summary>
    [Fact]
    public void SearchKeywords_ShouldNotBeNull()
    {
        // Arrange
        var page = new LLMSettingsPage(_serviceProvider);

        // Assert
        page.SearchKeywords.Should().NotBeNull();
    }

    #endregion
}
