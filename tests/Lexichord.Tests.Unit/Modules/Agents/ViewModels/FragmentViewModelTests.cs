// -----------------------------------------------------------------------
// <copyright file="FragmentViewModelTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Modules.Agents.Chat.ViewModels;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.ViewModels;

/// <summary>
/// Unit tests for <see cref="FragmentViewModel"/>.
/// </summary>
/// <remarks>
/// Tests verify fragment display properties, content truncation,
/// expand/collapse behavior, and source-specific icon mapping.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.2d")]
public class FragmentViewModelTests
{
    #region Helper Factories

    private static FragmentViewModel CreateViewModel(
        string sourceId = "test",
        string label = "Test Fragment",
        string content = "Test content",
        int tokenEstimate = 50,
        float relevance = 0.8f)
    {
        var fragment = new ContextFragment(sourceId, label, content, tokenEstimate, relevance);
        return new FragmentViewModel(fragment);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullFragment_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new FragmentViewModel(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("fragment");
    }

    [Fact]
    public void Constructor_ValidFragment_SetsProperties()
    {
        // Act
        var vm = CreateViewModel(
            sourceId: "document",
            label: "Document Content",
            content: "Some text",
            tokenEstimate: 100,
            relevance: 0.95f);

        // Assert
        vm.SourceId.Should().Be("document");
        vm.Label.Should().Be("Document Content");
        vm.FullContent.Should().Be("Some text");
        vm.TokenCount.Should().Be(100);
        vm.Relevance.Should().Be(0.95f);
    }

    #endregion

    #region Read-Only Property Tests

    [Fact]
    public void TokenCountText_FormatsWithCommasAndLabel()
    {
        // Arrange
        var vm = CreateViewModel(tokenEstimate: 1234);

        // Assert
        vm.TokenCountText.Should().Be("1,234 tokens");
    }

    [Fact]
    public void RelevanceText_FormatsAsPercentage()
    {
        // Arrange
        var vm = CreateViewModel(relevance: 0.85f);

        // Assert
        vm.RelevanceText.Should().Contain("85");
    }

    #endregion

    #region Content Truncation Tests

    [Fact]
    public void TruncatedContent_ShortContent_ReturnsFullContent()
    {
        // Arrange
        var shortContent = "Short content";
        var vm = CreateViewModel(content: shortContent);

        // Assert
        vm.TruncatedContent.Should().Be(shortContent);
    }

    [Fact]
    public void TruncatedContent_LongContent_TruncatesWithEllipsis()
    {
        // Arrange
        var longContent = new string('x', 500);
        var vm = CreateViewModel(content: longContent);

        // Assert
        vm.TruncatedContent.Should().HaveLength(203); // 200 + "..."
        vm.TruncatedContent.Should().EndWith("...");
    }

    [Fact]
    public void TruncatedContent_ExactPreviewLength_ReturnsFullContent()
    {
        // Arrange
        var exactContent = new string('x', 200);
        var vm = CreateViewModel(content: exactContent);

        // Assert
        vm.TruncatedContent.Should().Be(exactContent);
        vm.TruncatedContent.Should().HaveLength(200);
    }

    #endregion

    #region NeedsExpansion Tests

    [Fact]
    public void NeedsExpansion_ShortContent_ReturnsFalse()
    {
        // Arrange
        var vm = CreateViewModel(content: "Short");

        // Assert
        vm.NeedsExpansion.Should().BeFalse();
    }

    [Fact]
    public void NeedsExpansion_LongContent_ReturnsTrue()
    {
        // Arrange
        var vm = CreateViewModel(content: new string('x', 300));

        // Assert
        vm.NeedsExpansion.Should().BeTrue();
    }

    #endregion

    #region IsExpanded and DisplayContent Tests

    [Fact]
    public void DisplayContent_NotExpanded_ReturnsTruncatedContent()
    {
        // Arrange
        var vm = CreateViewModel(content: new string('x', 500));

        // Assert
        vm.IsExpanded.Should().BeFalse();
        vm.DisplayContent.Should().Be(vm.TruncatedContent);
    }

    [Fact]
    public void DisplayContent_Expanded_ReturnsFullContent()
    {
        // Arrange
        var vm = CreateViewModel(content: new string('x', 500));

        // Act
        vm.IsExpanded = true;

        // Assert
        vm.DisplayContent.Should().Be(vm.FullContent);
    }

    [Fact]
    public void ExpandButtonText_NotExpanded_ShowsShowMore()
    {
        // Arrange
        var vm = CreateViewModel();

        // Assert
        vm.ExpandButtonText.Should().Be("Show More");
    }

    [Fact]
    public void ExpandButtonText_Expanded_ShowsShowLess()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.IsExpanded = true;

        // Assert
        vm.ExpandButtonText.Should().Be("Show Less");
    }

    #endregion

    #region ToggleExpanded Command Tests

    [Fact]
    public void ToggleExpandedCommand_TogglesIsExpanded()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.IsExpanded.Should().BeFalse();

        // Act
        vm.ToggleExpandedCommand.Execute(null);

        // Assert
        vm.IsExpanded.Should().BeTrue();

        // Act again
        vm.ToggleExpandedCommand.Execute(null);

        // Assert
        vm.IsExpanded.Should().BeFalse();
    }

    #endregion

    #region SourceIcon Tests

    [Theory]
    [InlineData("document", "📄")]
    [InlineData("selection", "✂️")]
    [InlineData("cursor", "📍")]
    [InlineData("heading", "📑")]
    [InlineData("rag", "🔍")]
    [InlineData("style", "🎨")]
    [InlineData("knowledge", "🧠")]
    [InlineData("unknown", "📋")]
    [InlineData("custom-source", "📋")]
    public void SourceIcon_ReturnsCorrectIcon(string sourceId, string expectedIcon)
    {
        // Arrange
        var vm = CreateViewModel(sourceId: sourceId);

        // Assert
        vm.SourceIcon.Should().Be(expectedIcon);
    }

    #endregion

    #region PropertyChanged Tests

    [Fact]
    public void IsExpanded_Changed_RaisesPropertyChangedForDependentProperties()
    {
        // Arrange
        var vm = CreateViewModel(content: new string('x', 500));
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        // Act
        vm.IsExpanded = true;

        // Assert
        changedProperties.Should().Contain("IsExpanded");
        changedProperties.Should().Contain("DisplayContent");
        changedProperties.Should().Contain("ExpandButtonText");
    }

    #endregion
}
