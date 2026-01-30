using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Modules.Editor.ViewModels;

namespace Lexichord.Tests.Unit.Modules.Style.Tooltip;

/// <summary>
/// Unit tests for <see cref="ViolationTooltipViewModel"/>.
/// </summary>
public sealed class ViolationTooltipViewModelTests
{
    [Fact]
    public void HasRecommendation_WhenRecommendationSet_ReturnsTrue()
    {
        var sut = new ViolationTooltipViewModel { Recommendation = "Fix this" };
        Assert.True(sut.HasRecommendation);
    }

    [Fact]
    public void HasRecommendation_WhenRecommendationNull_ReturnsFalse()
    {
        var sut = new ViolationTooltipViewModel { Recommendation = null };
        Assert.False(sut.HasRecommendation);
    }

    [Fact]
    public void HasRecommendation_WhenRecommendationEmpty_ReturnsFalse()
    {
        var sut = new ViolationTooltipViewModel { Recommendation = string.Empty };
        Assert.False(sut.HasRecommendation);
    }

    [Fact]
    public void HasExplanation_WhenExplanationSet_ReturnsTrue()
    {
        var sut = new ViolationTooltipViewModel { Explanation = "This is why" };
        Assert.True(sut.HasExplanation);
    }

    [Fact]
    public void HasExplanation_WhenExplanationNull_ReturnsFalse()
    {
        var sut = new ViolationTooltipViewModel { Explanation = null };
        Assert.False(sut.HasExplanation);
    }

    [Fact]
    public void HasExplanation_WhenExplanationEmpty_ReturnsFalse()
    {
        var sut = new ViolationTooltipViewModel { Explanation = string.Empty };
        Assert.False(sut.HasExplanation);
    }

    [Fact]
    public void HasMultiple_WhenTotalCountGreaterThanOne_ReturnsTrue()
    {
        var sut = new ViolationTooltipViewModel { TotalCount = 3 };
        Assert.True(sut.HasMultiple);
    }

    [Fact]
    public void HasMultiple_WhenTotalCountOne_ReturnsFalse()
    {
        var sut = new ViolationTooltipViewModel { TotalCount = 1 };
        Assert.False(sut.HasMultiple);
    }

    [Fact]
    public void HasMultiple_WhenTotalCountZero_ReturnsFalse()
    {
        var sut = new ViolationTooltipViewModel { TotalCount = 0 };
        Assert.False(sut.HasMultiple);
    }

    [Fact]
    public void NavigatePreviousCommand_RaisesNavigateRequested()
    {
        var sut = new ViolationTooltipViewModel();
        var eventRaised = false;
        NavigateDirection? direction = null;

        sut.NavigateRequested += (s, e) =>
        {
            eventRaised = true;
            direction = e.Direction;
        };

        sut.NavigatePreviousCommand.Execute(null);

        Assert.True(eventRaised);
        Assert.Equal(NavigateDirection.Previous, direction);
    }

    [Fact]
    public void NavigateNextCommand_RaisesNavigateRequested()
    {
        var sut = new ViolationTooltipViewModel();
        var eventRaised = false;
        NavigateDirection? direction = null;

        sut.NavigateRequested += (s, e) =>
        {
            eventRaised = true;
            direction = e.Direction;
        };

        sut.NavigateNextCommand.Execute(null);

        Assert.True(eventRaised);
        Assert.Equal(NavigateDirection.Next, direction);
    }

    [Fact]
    public void Update_SetsAllProperties()
    {
        // LOGIC: Use SolidColorBrush with Color.FromRgb instead of platform-dependent Brushes
        // and set null for Geometry (which requires platform initialization).
        var sut = new ViolationTooltipViewModel();
        Avalonia.Media.IBrush brush = new Avalonia.Media.SolidColorBrush(
            Avalonia.Media.Color.FromRgb(255, 0, 0));

        sut.Update(
            ruleName: "TestRule",
            message: "Test message",
            recommendation: "Test recommendation",
            explanation: "Test explanation",
            borderColor: brush,
            iconPath: null,
            currentIndex: 2,
            totalCount: 5);

        Assert.Equal("TestRule", sut.RuleName);
        Assert.Equal("Test message", sut.Message);
        Assert.Equal("Test recommendation", sut.Recommendation);
        Assert.Equal("Test explanation", sut.Explanation);
        Assert.Same(brush, sut.BorderColor);
        Assert.Null(sut.IconPath);
        Assert.Equal(2, sut.CurrentIndex);
        Assert.Equal(5, sut.TotalCount);
    }

    [Fact]
    public void Update_NotifiesPropertyChanged()
    {
        var sut = new ViolationTooltipViewModel();
        var changedProperties = new List<string?>();
        sut.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

        Avalonia.Media.IBrush brush = new Avalonia.Media.SolidColorBrush(
            Avalonia.Media.Color.FromRgb(255, 0, 0));

        sut.Update(
            ruleName: "Rule",
            message: "Msg",
            recommendation: "Rec",
            explanation: "Exp",
            borderColor: brush,
            iconPath: null,
            currentIndex: 1,
            totalCount: 2);

        Assert.Contains(nameof(sut.RuleName), changedProperties);
        Assert.Contains(nameof(sut.Message), changedProperties);
        Assert.Contains(nameof(sut.Recommendation), changedProperties);
        Assert.Contains(nameof(sut.Explanation), changedProperties);
        Assert.Contains(nameof(sut.HasRecommendation), changedProperties);
        Assert.Contains(nameof(sut.HasExplanation), changedProperties);
        Assert.Contains(nameof(sut.HasMultiple), changedProperties);
    }
}
