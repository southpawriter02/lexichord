// -----------------------------------------------------------------------
// <copyright file="StrategyToggleItemTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.Agents.Chat.ViewModels;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.ViewModels;

/// <summary>
/// Unit tests for <see cref="StrategyToggleItem"/>.
/// </summary>
/// <remarks>
/// Tests verify construction, toggle callback invocation,
/// tooltip generation, and null guard behavior.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.2d")]
public class StrategyToggleItemTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_NullStrategyId_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new StrategyToggleItem(null!, "Display", true, (_, _) => { });

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("strategyId");
    }

    [Fact]
    public void Constructor_NullDisplayName_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new StrategyToggleItem("id", null!, true, (_, _) => { });

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("displayName");
    }

    [Fact]
    public void Constructor_NullOnToggle_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new StrategyToggleItem("id", "Display", true, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("onToggle");
    }

    [Fact]
    public void Constructor_ValidArguments_SetsProperties()
    {
        // Act
        var item = new StrategyToggleItem("document", "Document Content", true, (_, _) => { });

        // Assert
        item.StrategyId.Should().Be("document");
        item.DisplayName.Should().Be("Document Content");
        item.IsEnabled.Should().BeTrue();
    }

    #endregion

    #region Toggle Callback Tests

    [Fact]
    public void IsEnabled_Changed_InvokesOnToggleCallback()
    {
        // Arrange
        string? toggledId = null;
        bool? toggledEnabled = null;
        var item = new StrategyToggleItem("rag", "Related Docs", true,
            (id, enabled) =>
            {
                toggledId = id;
                toggledEnabled = enabled;
            });

        // Act
        item.IsEnabled = false;

        // Assert
        toggledId.Should().Be("rag");
        toggledEnabled.Should().BeFalse();
    }

    [Fact]
    public void IsEnabled_SetToSameValue_DoesNotInvokeCallback()
    {
        // Arrange
        var callCount = 0;
        var item = new StrategyToggleItem("doc", "Document", true,
            (_, _) => callCount++);

        // Act — set to same value
        item.IsEnabled = true;

        // Assert — CommunityToolkit.Mvvm won't raise PropertyChanged for same value,
        // so the partial method OnIsEnabledChanged won't fire.
        callCount.Should().Be(0);
    }

    #endregion

    #region Tooltip Tests

    [Theory]
    [InlineData("document", "document content")]
    [InlineData("selection", "selected text")]
    [InlineData("cursor", "cursor position")]
    [InlineData("heading", "heading structure")]
    [InlineData("rag", "semantically related")]
    [InlineData("style", "style rules")]
    [InlineData("knowledge", "knowledge graph")]
    public void Tooltip_KnownStrategy_ReturnsDescriptiveText(string strategyId, string expectedSubstring)
    {
        // Arrange
        var item = new StrategyToggleItem(strategyId, "Display", true, (_, _) => { });

        // Assert
        item.Tooltip.Should().ContainEquivalentOf(expectedSubstring);
    }

    [Fact]
    public void Tooltip_UnknownStrategy_ReturnsGenericText()
    {
        // Arrange
        var item = new StrategyToggleItem("custom", "My Custom Strategy", true, (_, _) => { });

        // Assert
        item.Tooltip.Should().Contain("My Custom Strategy");
    }

    #endregion
}
