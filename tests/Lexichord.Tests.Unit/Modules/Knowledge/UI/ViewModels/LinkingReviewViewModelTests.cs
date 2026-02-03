// =============================================================================
// File: LinkingReviewViewModelTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for LinkingReviewViewModel (v0.5.5c-i).
// =============================================================================
// LOGIC: Tests the linking review ViewModel functionality:
//   - Loading pending items
//   - Accept/Reject/Skip commands
//   - Select alternate candidate
//   - Group decisions
//   - Statistics updates
//   - Filter application
// =============================================================================
// VERSION: v0.5.5c-i (Linking Review UI)
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Knowledge.UI.ViewModels.LinkingReview;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.UI.ViewModels;

/// <summary>
/// Unit tests for <see cref="LinkingReviewViewModel"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.5c-i")]
public class LinkingReviewViewModelTests
{
    private readonly Mock<ILinkingReviewService> _reviewServiceMock;
    private readonly Mock<ILogger<LinkingReviewViewModel>> _loggerMock;
    private readonly LinkingReviewViewModel _viewModel;

    public LinkingReviewViewModelTests()
    {
        _reviewServiceMock = new Mock<ILinkingReviewService>();
        _loggerMock = new Mock<ILogger<LinkingReviewViewModel>>();

        // Setup default statistics
        _reviewServiceMock
            .Setup(s => s.GetStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ReviewStats.Empty);

        _viewModel = new LinkingReviewViewModel(
            _reviewServiceMock.Object,
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullReviewService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new LinkingReviewViewModel(null!, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("reviewService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new LinkingReviewViewModel(_reviewServiceMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Assert
        _viewModel.PendingItems.Should().BeEmpty();
        _viewModel.SelectedItem.Should().BeNull();
        _viewModel.Stats.Should().BeNull();
        _viewModel.IsLoading.Should().BeFalse();
        _viewModel.ApplyToGroup.Should().BeFalse();
        _viewModel.CanReview.Should().BeTrue();
    }

    #endregion

    #region LoadAsync Tests

    [Fact]
    public async Task LoadAsync_PopulatesPendingItems()
    {
        // Arrange
        var pending = new[]
        {
            CreatePendingItem("mention1", 0.65f),
            CreatePendingItem("mention2", 0.55f)
        };

        _reviewServiceMock
            .Setup(s => s.GetPendingAsync(It.IsAny<ReviewFilter?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pending);

        // Act
        await _viewModel.LoadCommand.ExecuteAsync(null);

        // Assert
        _viewModel.PendingItems.Should().HaveCount(2);
        _viewModel.SelectedItem.Should().Be(pending[0]);
    }

    [Fact]
    public async Task LoadAsync_LoadsStatistics()
    {
        // Arrange
        var stats = new ReviewStats { PendingCount = 10, ReviewedToday = 5 };

        _reviewServiceMock
            .Setup(s => s.GetPendingAsync(It.IsAny<ReviewFilter?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PendingLinkItem>());
        _reviewServiceMock
            .Setup(s => s.GetStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        // Act
        await _viewModel.LoadCommand.ExecuteAsync(null);

        // Assert
        _viewModel.Stats.Should().Be(stats);
    }

    [Fact]
    public async Task LoadAsync_SetsIsLoadingDuringOperation()
    {
        // Arrange
        var tcs = new TaskCompletionSource<IReadOnlyList<PendingLinkItem>>();

        _reviewServiceMock
            .Setup(s => s.GetPendingAsync(It.IsAny<ReviewFilter?>(), It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        // Act
        var loadTask = _viewModel.LoadCommand.ExecuteAsync(null);

        // Assert - IsLoading should be true during operation
        _viewModel.IsLoading.Should().BeTrue();

        // Complete the operation
        tcs.SetResult(Array.Empty<PendingLinkItem>());
        await loadTask;

        // Assert - IsLoading should be false after completion
        _viewModel.IsLoading.Should().BeFalse();
    }

    #endregion

    #region AcceptCommand Tests

    [Fact]
    public async Task AcceptCommand_SubmitsAcceptDecision()
    {
        // Arrange
        var pending = CreatePendingItem("mention1", 0.65f);
        _viewModel.PendingItems.Add(pending);
        _viewModel.SelectedItem = pending;

        LinkReviewDecision? capturedDecision = null;
        _reviewServiceMock
            .Setup(s => s.SubmitDecisionAsync(It.IsAny<LinkReviewDecision>(), It.IsAny<CancellationToken>()))
            .Callback<LinkReviewDecision, CancellationToken>((d, _) => capturedDecision = d)
            .Returns(Task.CompletedTask);

        // Act
        await _viewModel.AcceptCommand.ExecuteAsync(null);

        // Assert
        capturedDecision.Should().NotBeNull();
        capturedDecision!.Action.Should().Be(ReviewAction.Accept);
        capturedDecision.PendingLinkId.Should().Be(pending.Id);
    }

    [Fact]
    public async Task AcceptCommand_RemovesItemFromList()
    {
        // Arrange
        var pending = CreatePendingItem("mention1", 0.65f);
        _viewModel.PendingItems.Add(pending);
        _viewModel.SelectedItem = pending;

        _reviewServiceMock
            .Setup(s => s.SubmitDecisionAsync(It.IsAny<LinkReviewDecision>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _viewModel.AcceptCommand.ExecuteAsync(null);

        // Assert
        _viewModel.PendingItems.Should().NotContain(pending);
    }

    [Fact]
    public async Task AcceptCommand_SelectsNextItem()
    {
        // Arrange
        var pending1 = CreatePendingItem("mention1", 0.65f);
        var pending2 = CreatePendingItem("mention2", 0.55f);
        _viewModel.PendingItems.Add(pending1);
        _viewModel.PendingItems.Add(pending2);
        _viewModel.SelectedItem = pending1;

        _reviewServiceMock
            .Setup(s => s.SubmitDecisionAsync(It.IsAny<LinkReviewDecision>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _viewModel.AcceptCommand.ExecuteAsync(null);

        // Assert
        _viewModel.SelectedItem.Should().Be(pending2);
    }

    [Fact]
    public async Task AcceptCommand_WithNoSelectedItem_DoesNothing()
    {
        // Arrange
        _viewModel.SelectedItem = null;

        // Act
        await _viewModel.AcceptCommand.ExecuteAsync(null);

        // Assert
        _reviewServiceMock.Verify(
            s => s.SubmitDecisionAsync(It.IsAny<LinkReviewDecision>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region RejectCommand Tests

    [Fact]
    public async Task RejectCommand_SubmitsRejectDecision()
    {
        // Arrange
        var pending = CreatePendingItem("mention1", 0.65f);
        _viewModel.PendingItems.Add(pending);
        _viewModel.SelectedItem = pending;

        LinkReviewDecision? capturedDecision = null;
        _reviewServiceMock
            .Setup(s => s.SubmitDecisionAsync(It.IsAny<LinkReviewDecision>(), It.IsAny<CancellationToken>()))
            .Callback<LinkReviewDecision, CancellationToken>((d, _) => capturedDecision = d)
            .Returns(Task.CompletedTask);

        // Act
        await _viewModel.RejectCommand.ExecuteAsync(null);

        // Assert
        capturedDecision.Should().NotBeNull();
        capturedDecision!.Action.Should().Be(ReviewAction.Reject);
    }

    #endregion

    #region SkipCommand Tests

    [Fact]
    public async Task SkipCommand_SubmitsSkipDecision()
    {
        // Arrange
        var pending = CreatePendingItem("mention1", 0.65f);
        _viewModel.PendingItems.Add(pending);
        _viewModel.SelectedItem = pending;

        LinkReviewDecision? capturedDecision = null;
        _reviewServiceMock
            .Setup(s => s.SubmitDecisionAsync(It.IsAny<LinkReviewDecision>(), It.IsAny<CancellationToken>()))
            .Callback<LinkReviewDecision, CancellationToken>((d, _) => capturedDecision = d)
            .Returns(Task.CompletedTask);

        // Act
        await _viewModel.SkipCommand.ExecuteAsync(null);

        // Assert
        capturedDecision.Should().NotBeNull();
        capturedDecision!.Action.Should().Be(ReviewAction.Skip);
    }

    #endregion

    #region SelectAlternateCommand Tests

    [Fact]
    public async Task SelectAlternateCommand_SubmitsAlternateDecision()
    {
        // Arrange
        var pending = CreatePendingItem("mention1", 0.65f);
        var candidate = new LinkCandidate(
            Guid.NewGuid(), "Alternate Entity", "Endpoint", 0.72f);

        _viewModel.PendingItems.Add(pending);
        _viewModel.SelectedItem = pending;

        LinkReviewDecision? capturedDecision = null;
        _reviewServiceMock
            .Setup(s => s.SubmitDecisionAsync(It.IsAny<LinkReviewDecision>(), It.IsAny<CancellationToken>()))
            .Callback<LinkReviewDecision, CancellationToken>((d, _) => capturedDecision = d)
            .Returns(Task.CompletedTask);

        // Act
        await _viewModel.SelectAlternateCommand.ExecuteAsync(candidate);

        // Assert
        capturedDecision.Should().NotBeNull();
        capturedDecision!.Action.Should().Be(ReviewAction.SelectAlternate);
        capturedDecision.SelectedEntityId.Should().Be(candidate.EntityId);
    }

    #endregion

    #region ApplyToGroup Tests

    [Fact]
    public async Task AcceptCommand_WithApplyToGroup_SetsGroupFlag()
    {
        // Arrange
        var pending = CreatePendingItem("mention1", 0.65f) with
        {
            IsGrouped = true,
            GroupId = "group1",
            GroupCount = 5
        };

        _viewModel.PendingItems.Add(pending);
        _viewModel.SelectedItem = pending;
        _viewModel.ApplyToGroup = true;

        LinkReviewDecision? capturedDecision = null;
        _reviewServiceMock
            .Setup(s => s.SubmitDecisionAsync(It.IsAny<LinkReviewDecision>(), It.IsAny<CancellationToken>()))
            .Callback<LinkReviewDecision, CancellationToken>((d, _) => capturedDecision = d)
            .Returns(Task.CompletedTask);

        // Act
        await _viewModel.AcceptCommand.ExecuteAsync(null);

        // Assert
        capturedDecision.Should().NotBeNull();
        capturedDecision!.ApplyToGroup.Should().BeTrue();
    }

    [Fact]
    public async Task AcceptCommand_WithoutApplyToGroup_DoesNotSetGroupFlag()
    {
        // Arrange
        var pending = CreatePendingItem("mention1", 0.65f) with
        {
            IsGrouped = true,
            GroupId = "group1",
            GroupCount = 5
        };

        _viewModel.PendingItems.Add(pending);
        _viewModel.SelectedItem = pending;
        _viewModel.ApplyToGroup = false;

        LinkReviewDecision? capturedDecision = null;
        _reviewServiceMock
            .Setup(s => s.SubmitDecisionAsync(It.IsAny<LinkReviewDecision>(), It.IsAny<CancellationToken>()))
            .Callback<LinkReviewDecision, CancellationToken>((d, _) => capturedDecision = d)
            .Returns(Task.CompletedTask);

        // Act
        await _viewModel.AcceptCommand.ExecuteAsync(null);

        // Assert
        capturedDecision.Should().NotBeNull();
        capturedDecision!.ApplyToGroup.Should().BeFalse();
    }

    #endregion

    #region CreateNewEntityCommand Tests

    [Fact]
    public async Task CreateNewEntityCommand_SubmitsCreateNewDecision()
    {
        // Arrange
        var pending = CreatePendingItem("mention1", 0.65f);
        _viewModel.PendingItems.Add(pending);
        _viewModel.SelectedItem = pending;

        LinkReviewDecision? capturedDecision = null;
        _reviewServiceMock
            .Setup(s => s.SubmitDecisionAsync(It.IsAny<LinkReviewDecision>(), It.IsAny<CancellationToken>()))
            .Callback<LinkReviewDecision, CancellationToken>((d, _) => capturedDecision = d)
            .Returns(Task.CompletedTask);

        // Act
        await _viewModel.CreateNewEntityCommand.ExecuteAsync(null);

        // Assert
        capturedDecision.Should().NotBeNull();
        capturedDecision!.Action.Should().Be(ReviewAction.CreateNew);
        capturedDecision.NewEntityProperties.Should().NotBeNull();
        capturedDecision.NewEntityProperties!["name"].Should().Be(pending.Mention.Value);
    }

    #endregion

    #region MarkNotEntityCommand Tests

    [Fact]
    public async Task MarkNotEntityCommand_SubmitsNotEntityDecision()
    {
        // Arrange
        var pending = CreatePendingItem("mention1", 0.65f);
        _viewModel.PendingItems.Add(pending);
        _viewModel.SelectedItem = pending;

        LinkReviewDecision? capturedDecision = null;
        _reviewServiceMock
            .Setup(s => s.SubmitDecisionAsync(It.IsAny<LinkReviewDecision>(), It.IsAny<CancellationToken>()))
            .Callback<LinkReviewDecision, CancellationToken>((d, _) => capturedDecision = d)
            .Returns(Task.CompletedTask);

        // Act
        await _viewModel.MarkNotEntityCommand.ExecuteAsync(null);

        // Assert
        capturedDecision.Should().NotBeNull();
        capturedDecision!.Action.Should().Be(ReviewAction.NotAnEntity);
    }

    #endregion

    #region SelectedItem Tests

    [Fact]
    public void SelectedItem_WithNoItems_IsNull()
    {
        // Assert
        _viewModel.SelectedItem.Should().BeNull();
    }

    [Fact]
    public async Task SelectedItem_AfterAllItemsRemoved_IsNull()
    {
        // Arrange
        var pending = CreatePendingItem("mention1", 0.65f);
        _viewModel.PendingItems.Add(pending);
        _viewModel.SelectedItem = pending;

        _reviewServiceMock
            .Setup(s => s.SubmitDecisionAsync(It.IsAny<LinkReviewDecision>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _viewModel.AcceptCommand.ExecuteAsync(null);

        // Assert
        _viewModel.SelectedItem.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a pending link item for testing.
    /// </summary>
    private static PendingLinkItem CreatePendingItem(string mentionValue, float confidence)
    {
        var proposedEntityId = Guid.NewGuid();
        return new PendingLinkItem
        {
            Id = Guid.NewGuid(),
            Mention = new EntityMention
            {
                EntityType = "Endpoint",
                Value = mentionValue
            },
            ProposedEntityId = proposedEntityId,
            ProposedEntityName = $"Entity for {mentionValue}",
            Confidence = confidence,
            DocumentId = Guid.NewGuid(),
            DocumentTitle = "Test Document",
            ExtendedContext = $"...some context around {mentionValue}...",
            Candidates = new[]
            {
                new LinkCandidate(proposedEntityId, $"Entity for {mentionValue}", "Endpoint", confidence),
                new LinkCandidate(Guid.NewGuid(), "Alternative Entity", "Endpoint", confidence - 0.1f)
            }
        };
    }

    #endregion
}
