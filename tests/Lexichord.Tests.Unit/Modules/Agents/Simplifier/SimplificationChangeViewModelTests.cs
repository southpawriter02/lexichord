// -----------------------------------------------------------------------
// <copyright file="SimplificationChangeViewModelTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Simplifier;
using Lexichord.Modules.Agents.Simplifier;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Simplifier;

/// <summary>
/// Unit tests for <see cref="SimplificationChangeViewModel"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.4c")]
public class SimplificationChangeViewModelTests
{
    // ── Constructor Tests ────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullChange_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SimplificationChangeViewModel(null!, 0);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("change");
    }

    [Fact]
    public void Constructor_ValidChange_SetsProperties()
    {
        // Arrange
        var change = CreateTestChange();

        // Act
        var viewModel = new SimplificationChangeViewModel(change, index: 5);

        // Assert
        viewModel.Index.Should().Be(5);
        viewModel.Change.Should().Be(change);
        viewModel.OriginalText.Should().Be(change.OriginalText);
        viewModel.SimplifiedText.Should().Be(change.SimplifiedText);
        viewModel.ChangeType.Should().Be(change.ChangeType);
        viewModel.Explanation.Should().Be(change.Explanation);
        viewModel.Confidence.Should().Be(change.Confidence);
    }

    [Fact]
    public void Constructor_DefaultsIsSelectedToTrue()
    {
        // Arrange
        var change = CreateTestChange();

        // Act
        var viewModel = new SimplificationChangeViewModel(change, 0);

        // Assert
        viewModel.IsSelected.Should().BeTrue();
    }

    [Fact]
    public void Constructor_DefaultsIsExpandedToFalse()
    {
        // Arrange
        var change = CreateTestChange();

        // Act
        var viewModel = new SimplificationChangeViewModel(change, 0);

        // Assert
        viewModel.IsExpanded.Should().BeFalse();
    }

    [Fact]
    public void Constructor_DefaultsIsHighlightedToFalse()
    {
        // Arrange
        var change = CreateTestChange();

        // Act
        var viewModel = new SimplificationChangeViewModel(change, 0);

        // Assert
        viewModel.IsHighlighted.Should().BeFalse();
    }

    // ── Observable Property Tests ────────────────────────────────────────

    [Fact]
    public void IsSelected_CanBeToggled()
    {
        // Arrange
        var viewModel = new SimplificationChangeViewModel(CreateTestChange(), 0);

        // Act
        viewModel.IsSelected = false;

        // Assert
        viewModel.IsSelected.Should().BeFalse();
    }

    [Fact]
    public void IsSelected_ChangesCanAccept()
    {
        // Arrange
        var viewModel = new SimplificationChangeViewModel(CreateTestChange(), 0);

        // Initial state
        viewModel.CanAccept.Should().BeTrue();

        // Act
        viewModel.IsSelected = false;

        // Assert
        viewModel.CanAccept.Should().BeFalse();
    }

    [Fact]
    public void IsExpanded_CanBeToggled()
    {
        // Arrange
        var viewModel = new SimplificationChangeViewModel(CreateTestChange(), 0);

        // Act
        viewModel.IsExpanded = true;

        // Assert
        viewModel.IsExpanded.Should().BeTrue();
    }

    [Fact]
    public void IsHighlighted_CanBeToggled()
    {
        // Arrange
        var viewModel = new SimplificationChangeViewModel(CreateTestChange(), 0);

        // Act
        viewModel.IsHighlighted = true;

        // Assert
        viewModel.IsHighlighted.Should().BeTrue();
    }

    // ── Display Property Tests ───────────────────────────────────────────

    [Theory]
    [InlineData(SimplificationChangeType.SentenceSplit, "Sentence Split")]
    [InlineData(SimplificationChangeType.JargonReplacement, "Jargon Replaced")]
    [InlineData(SimplificationChangeType.PassiveToActive, "Active Voice")]
    [InlineData(SimplificationChangeType.WordSimplification, "Word Simplified")]
    [InlineData(SimplificationChangeType.ClauseReduction, "Clause Reduced")]
    [InlineData(SimplificationChangeType.TransitionAdded, "Transition Added")]
    [InlineData(SimplificationChangeType.RedundancyRemoved, "Redundancy Removed")]
    [InlineData(SimplificationChangeType.Combined, "Combined Changes")]
    public void ChangeTypeDisplay_ReturnsCorrectDisplayName(
        SimplificationChangeType changeType,
        string expectedDisplay)
    {
        // Arrange
        var change = CreateChangeWithType(changeType);
        var viewModel = new SimplificationChangeViewModel(change, 0);

        // Act & Assert
        viewModel.ChangeTypeDisplay.Should().Be(expectedDisplay);
    }

    [Theory]
    [InlineData(SimplificationChangeType.SentenceSplit, "SplitIcon")]
    [InlineData(SimplificationChangeType.JargonReplacement, "BookOpenIcon")]
    [InlineData(SimplificationChangeType.PassiveToActive, "ArrowRightIcon")]
    [InlineData(SimplificationChangeType.WordSimplification, "TypeIcon")]
    [InlineData(SimplificationChangeType.ClauseReduction, "MinimizeIcon")]
    [InlineData(SimplificationChangeType.TransitionAdded, "LinkIcon")]
    [InlineData(SimplificationChangeType.RedundancyRemoved, "TrashIcon")]
    [InlineData(SimplificationChangeType.Combined, "EditIcon")]
    public void ChangeTypeIcon_ReturnsCorrectIcon(
        SimplificationChangeType changeType,
        string expectedIcon)
    {
        // Arrange
        var change = CreateChangeWithType(changeType);
        var viewModel = new SimplificationChangeViewModel(change, 0);

        // Act & Assert
        viewModel.ChangeTypeIcon.Should().Be(expectedIcon);
    }

    [Theory]
    [InlineData(SimplificationChangeType.SentenceSplit, "badge-blue")]
    [InlineData(SimplificationChangeType.ClauseReduction, "badge-blue")]
    [InlineData(SimplificationChangeType.PassiveToActive, "badge-green")]
    [InlineData(SimplificationChangeType.JargonReplacement, "badge-orange")]
    [InlineData(SimplificationChangeType.WordSimplification, "badge-orange")]
    [InlineData(SimplificationChangeType.TransitionAdded, "badge-purple")]
    [InlineData(SimplificationChangeType.RedundancyRemoved, "badge-purple")]
    [InlineData(SimplificationChangeType.Combined, "badge-gray")]
    public void ChangeTypeBadgeClass_ReturnsCorrectClass(
        SimplificationChangeType changeType,
        string expectedClass)
    {
        // Arrange
        var change = CreateChangeWithType(changeType);
        var viewModel = new SimplificationChangeViewModel(change, 0);

        // Act & Assert
        viewModel.ChangeTypeBadgeClass.Should().Be(expectedClass);
    }

    // ── Preview Truncation Tests ─────────────────────────────────────────

    [Fact]
    public void OriginalTextPreview_TruncatesLongText()
    {
        // Arrange
        var longText = new string('x', 100);
        var change = new SimplificationChange(
            OriginalText: longText,
            SimplifiedText: "short",
            ChangeType: SimplificationChangeType.WordSimplification,
            Explanation: "Truncation test");
        var viewModel = new SimplificationChangeViewModel(change, 0);

        // Act
        var preview = viewModel.OriginalTextPreview;

        // Assert
        preview.Length.Should().Be(50);
        preview.Should().EndWith("...");
    }

    [Fact]
    public void OriginalTextPreview_DoesNotTruncateShortText()
    {
        // Arrange
        var shortText = "Short text";
        var change = new SimplificationChange(
            OriginalText: shortText,
            SimplifiedText: "short",
            ChangeType: SimplificationChangeType.WordSimplification,
            Explanation: "Short text test");
        var viewModel = new SimplificationChangeViewModel(change, 0);

        // Act
        var preview = viewModel.OriginalTextPreview;

        // Assert
        preview.Should().Be(shortText);
    }

    [Fact]
    public void SimplifiedTextPreview_TruncatesLongText()
    {
        // Arrange
        var longText = new string('y', 100);
        var change = new SimplificationChange(
            OriginalText: "original",
            SimplifiedText: longText,
            ChangeType: SimplificationChangeType.WordSimplification,
            Explanation: "Truncation test");
        var viewModel = new SimplificationChangeViewModel(change, 0);

        // Act
        var preview = viewModel.SimplifiedTextPreview;

        // Assert
        preview.Length.Should().Be(50);
        preview.Should().EndWith("...");
    }

    // ── Computed Property Tests ──────────────────────────────────────────

    [Fact]
    public void IsReduction_TrueWhenSimplifiedIsShorter()
    {
        // Arrange
        var change = new SimplificationChange(
            OriginalText: "This is a longer text",
            SimplifiedText: "Short",
            ChangeType: SimplificationChangeType.WordSimplification,
            Explanation: "Reduction test");
        var viewModel = new SimplificationChangeViewModel(change, 0);

        // Act & Assert
        viewModel.IsReduction.Should().BeTrue();
    }

    [Fact]
    public void IsReduction_FalseWhenSimplifiedIsLonger()
    {
        // Arrange
        var change = new SimplificationChange(
            OriginalText: "Short",
            SimplifiedText: "This is much longer text",
            ChangeType: SimplificationChangeType.TransitionAdded,
            Explanation: "Expansion test");
        var viewModel = new SimplificationChangeViewModel(change, 0);

        // Act & Assert
        viewModel.IsReduction.Should().BeFalse();
    }

    [Fact]
    public void LengthDifference_CalculatedCorrectly()
    {
        // Arrange
        var change = new SimplificationChange(
            OriginalText: "1234567890", // 10 chars
            SimplifiedText: "12345", // 5 chars
            ChangeType: SimplificationChangeType.WordSimplification,
            Explanation: "Length test");
        var viewModel = new SimplificationChangeViewModel(change, 0);

        // Act & Assert
        viewModel.LengthDifference.Should().Be(5);
    }

    [Fact]
    public void Location_DelegatesFromChange()
    {
        // Arrange
        var location = new TextLocation(10, 20);
        var change = new SimplificationChange(
            OriginalText: "original",
            SimplifiedText: "simplified",
            ChangeType: SimplificationChangeType.WordSimplification,
            Explanation: "Location test",
            Location: location);
        var viewModel = new SimplificationChangeViewModel(change, 0);

        // Act & Assert
        viewModel.Location.Should().NotBeNull();
        viewModel.Location!.Start.Should().Be(10);
        viewModel.Location.Length.Should().Be(20);
    }

    // ── PropertyChanged Tests ────────────────────────────────────────────

    [Fact]
    public void IsSelected_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = new SimplificationChangeViewModel(CreateTestChange(), 0);
        var propertyChangedRaised = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SimplificationChangeViewModel.IsSelected))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        viewModel.IsSelected = false;

        // Assert
        propertyChangedRaised.Should().BeTrue();
    }

    [Fact]
    public void IsSelected_AlsoNotifiesCanAccept()
    {
        // Arrange
        var viewModel = new SimplificationChangeViewModel(CreateTestChange(), 0);
        var canAcceptChangedRaised = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SimplificationChangeViewModel.CanAccept))
            {
                canAcceptChangedRaised = true;
            }
        };

        // Act
        viewModel.IsSelected = false;

        // Assert
        canAcceptChangedRaised.Should().BeTrue();
    }

    // ── Helper Methods ───────────────────────────────────────────────────

    private static SimplificationChange CreateTestChange() =>
        new(
            OriginalText: "utilize",
            SimplifiedText: "use",
            ChangeType: SimplificationChangeType.WordSimplification,
            Explanation: "Replaced complex word with simpler alternative",
            Confidence: 0.95);

    private static SimplificationChange CreateChangeWithType(SimplificationChangeType changeType) =>
        new(
            OriginalText: "original",
            SimplifiedText: "simplified",
            ChangeType: changeType,
            Explanation: "Test explanation");
}
