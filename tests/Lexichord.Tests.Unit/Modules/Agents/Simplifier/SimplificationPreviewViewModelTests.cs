// -----------------------------------------------------------------------
// <copyright file="SimplificationPreviewViewModelTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Simplifier;
using Lexichord.Abstractions.Agents.Simplifier.Events;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Agents.Simplifier;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Simplifier;

/// <summary>
/// Unit tests for <see cref="SimplificationPreviewViewModel"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.4c")]
public class SimplificationPreviewViewModelTests
{
    private readonly ISimplificationPipeline _mockPipeline;
    private readonly IReadabilityTargetService _mockTargetService;
    private readonly IEditorService _mockEditorService;
    private readonly IMediator _mockMediator;
    private readonly ILicenseContext _mockLicenseContext;
    private readonly ILogger<SimplificationPreviewViewModel> _logger;

    public SimplificationPreviewViewModelTests()
    {
        _mockPipeline = Substitute.For<ISimplificationPipeline>();
        _mockTargetService = Substitute.For<IReadabilityTargetService>();
        _mockEditorService = Substitute.For<IEditorService>();
        _mockMediator = Substitute.For<IMediator>();
        _mockLicenseContext = Substitute.For<ILicenseContext>();
        _logger = NullLogger<SimplificationPreviewViewModel>.Instance;

        // Default setup: licensed
        _mockLicenseContext.IsFeatureEnabled(FeatureCodes.SimplifierAgent).Returns(true);
        _mockLicenseContext.Tier.Returns(LicenseTier.WriterPro);
    }

    // ── Constructor Tests ────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullPipeline_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SimplificationPreviewViewModel(
            null!,
            _mockTargetService,
            _mockEditorService,
            _mockMediator,
            _mockLicenseContext,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("pipeline");
    }

    [Fact]
    public void Constructor_NullTargetService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SimplificationPreviewViewModel(
            _mockPipeline,
            null!,
            _mockEditorService,
            _mockMediator,
            _mockLicenseContext,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("targetService");
    }

    [Fact]
    public void Constructor_NullEditorService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SimplificationPreviewViewModel(
            _mockPipeline,
            _mockTargetService,
            null!,
            _mockMediator,
            _mockLicenseContext,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("editorService");
    }

    [Fact]
    public void Constructor_NullMediator_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SimplificationPreviewViewModel(
            _mockPipeline,
            _mockTargetService,
            _mockEditorService,
            null!,
            _mockLicenseContext,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("mediator");
    }

    [Fact]
    public void Constructor_NullLicenseContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SimplificationPreviewViewModel(
            _mockPipeline,
            _mockTargetService,
            _mockEditorService,
            _mockMediator,
            null!,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("licenseContext");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SimplificationPreviewViewModel(
            _mockPipeline,
            _mockTargetService,
            _mockEditorService,
            _mockMediator,
            _mockLicenseContext,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ValidDependencies_InitializesCorrectly()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.Should().NotBeNull();
        viewModel.Changes.Should().BeEmpty();
        viewModel.AvailablePresets.Should().BeEmpty();
        viewModel.ViewMode.Should().Be(DiffViewMode.SideBySide);
        viewModel.IsLoading.Should().BeTrue();
        viewModel.IsProcessing.Should().BeFalse();
    }

    // ── InitializeAsync Tests ────────────────────────────────────────────

    [Fact]
    public async Task InitializeAsync_LoadsPresets()
    {
        // Arrange
        var presets = new List<AudiencePreset>
        {
            new("general-public", "General Public", 8.0, 20, true, "For broad audiences", true),
            new("technical", "Technical", 12.0, 25, false, "For professionals", true)
        };
        _mockTargetService.GetAllPresetsAsync(Arg.Any<CancellationToken>())
            .Returns(presets.AsReadOnly());

        var viewModel = CreateViewModel();

        // Act
        await viewModel.InitializeAsync("/path/to/doc.md");

        // Assert
        viewModel.AvailablePresets.Should().HaveCount(2);
        viewModel.AvailablePresets[0].Name.Should().Be("General Public");
    }

    [Fact]
    public async Task InitializeAsync_CapturesEditorSelection()
    {
        // Arrange
        _mockEditorService.SelectionStart.Returns(10);
        _mockEditorService.SelectionLength.Returns(50);
        _mockTargetService.GetAllPresetsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AudiencePreset>().AsReadOnly());

        var viewModel = CreateViewModel();

        // Act
        await viewModel.InitializeAsync("/path/to/doc.md");

        // Assert - captured values used internally for applying changes
        _mockEditorService.Received(1).SelectionStart.Should();
        _mockEditorService.Received(1).SelectionLength.Should();
    }

    [Fact]
    public async Task InitializeAsync_ValidatesLicense()
    {
        // Arrange
        _mockLicenseContext.IsFeatureEnabled(FeatureCodes.SimplifierAgent).Returns(false);
        _mockTargetService.GetAllPresetsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AudiencePreset>().AsReadOnly());

        var viewModel = CreateViewModel();

        // Act
        await viewModel.InitializeAsync("/path/to/doc.md");

        // Assert
        viewModel.IsLicensed.Should().BeFalse();
        viewModel.LicenseWarning.Should().NotBeNullOrEmpty();
    }

    // ── SetResult Tests ──────────────────────────────────────────────────

    [Fact]
    public void SetResult_NullResult_ThrowsArgumentNullException()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var act = () => viewModel.SetResult(null!, "original");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("result");
    }

    [Fact]
    public void SetResult_NullOriginalText_ThrowsArgumentNullException()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var result = CreateSuccessResult();

        // Act
        var act = () => viewModel.SetResult(result, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("originalText");
    }

    [Fact]
    public void SetResult_SetsTextProperties()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var result = CreateSuccessResult();

        // Act
        viewModel.SetResult(result, "Original complex text");

        // Assert
        viewModel.OriginalText.Should().Be("Original complex text");
        viewModel.SimplifiedText.Should().Be("Simplified text");
    }

    [Fact]
    public void SetResult_SetsMetrics()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var result = CreateSuccessResult();

        // Act
        viewModel.SetResult(result, "Original text");

        // Assert
        viewModel.OriginalMetrics.Should().NotBeNull();
        viewModel.SimplifiedMetrics.Should().NotBeNull();
        viewModel.GradeLevelReduction.Should().BeApproximately(4.0, 0.01);
    }

    [Fact]
    public void SetResult_CreatesChangeViewModels()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var changes = new List<SimplificationChange>
        {
            new("utilize", "use", SimplificationChangeType.WordSimplification, "Simplified word"),
            new("commence", "begin", SimplificationChangeType.WordSimplification, "Simplified word")
        };
        var result = CreateSuccessResultWithChanges(changes);

        // Act
        viewModel.SetResult(result, "Original text");

        // Assert
        viewModel.Changes.Should().HaveCount(2);
        viewModel.Changes[0].OriginalText.Should().Be("utilize");
        viewModel.Changes[1].OriginalText.Should().Be("commence");
    }

    [Fact]
    public void SetResult_ChangesAreSelectedByDefault()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var changes = new List<SimplificationChange>
        {
            new("word1", "simple1", SimplificationChangeType.WordSimplification, "Test")
        };
        var result = CreateSuccessResultWithChanges(changes);

        // Act
        viewModel.SetResult(result, "Original text");

        // Assert
        viewModel.Changes.All(c => c.IsSelected).Should().BeTrue();
    }

    [Fact]
    public void SetResult_MarksLoadingComplete()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var result = CreateSuccessResult();

        // Act
        viewModel.SetResult(result, "Original text");

        // Assert
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void SetResult_HandlesErrorResult()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var target = ReadabilityTarget.FromExplicit(8.0, 20, true);
        var result = SimplificationResult.Failed(
            "Original text",
            SimplificationStrategy.Balanced,
            target,
            "LLM timeout",
            TimeSpan.FromSeconds(5));

        // Act
        viewModel.SetResult(result, "Original text");

        // Assert
        viewModel.ErrorMessage.Should().Be("LLM timeout");
        viewModel.HasError.Should().BeTrue();
    }

    // ── Selection Tests ──────────────────────────────────────────────────

    [Fact]
    public void SelectedChangeCount_ReflectsSelection()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var changes = new List<SimplificationChange>
        {
            new("w1", "s1", SimplificationChangeType.WordSimplification, "T1"),
            new("w2", "s2", SimplificationChangeType.WordSimplification, "T2"),
            new("w3", "s3", SimplificationChangeType.WordSimplification, "T3")
        };
        var result = CreateSuccessResultWithChanges(changes);
        viewModel.SetResult(result, "Original text");

        // Initially all selected
        viewModel.SelectedChangeCount.Should().Be(3);

        // Act - deselect one
        viewModel.Changes[0].IsSelected = false;

        // Assert
        viewModel.SelectedChangeCount.Should().Be(2);
    }

    [Fact]
    public void AllChangesSelected_TrueWhenAllSelected()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var changes = new List<SimplificationChange>
        {
            new("w1", "s1", SimplificationChangeType.WordSimplification, "T1"),
            new("w2", "s2", SimplificationChangeType.WordSimplification, "T2")
        };
        var result = CreateSuccessResultWithChanges(changes);
        viewModel.SetResult(result, "Original text");

        // Assert
        viewModel.AllChangesSelected.Should().BeTrue();
    }

    [Fact]
    public void AllChangesSelected_FalseWhenSomeDeselected()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var changes = new List<SimplificationChange>
        {
            new("w1", "s1", SimplificationChangeType.WordSimplification, "T1"),
            new("w2", "s2", SimplificationChangeType.WordSimplification, "T2")
        };
        var result = CreateSuccessResultWithChanges(changes);
        viewModel.SetResult(result, "Original text");

        // Act
        viewModel.Changes[0].IsSelected = false;

        // Assert
        viewModel.AllChangesSelected.Should().BeFalse();
    }

    // ── Command Execution Tests ──────────────────────────────────────────

    [Fact]
    public void AcceptAllCommand_CanExecute_FalseWhenNoChanges()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var result = CreateSuccessResultWithChanges(new List<SimplificationChange>());
        viewModel.SetResult(result, "Original text");

        // Assert
        viewModel.AcceptAllCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void AcceptAllCommand_CanExecute_TrueWhenHasChanges()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var changes = new List<SimplificationChange>
        {
            new("w1", "s1", SimplificationChangeType.WordSimplification, "T1")
        };
        var result = CreateSuccessResultWithChanges(changes);
        viewModel.SetResult(result, "Original text");

        // Assert
        viewModel.AcceptAllCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task AcceptAllCommand_CanExecute_FalseWhenNotLicensed()
    {
        // Arrange
        _mockLicenseContext.IsFeatureEnabled(FeatureCodes.SimplifierAgent).Returns(false);
        var viewModel = CreateViewModel();

        // LOGIC: Must call InitializeAsync to trigger license validation
        await viewModel.InitializeAsync("/test/document.md");

        var changes = new List<SimplificationChange>
        {
            new("w1", "s1", SimplificationChangeType.WordSimplification, "T1")
        };
        var result = CreateSuccessResultWithChanges(changes);
        viewModel.SetResult(result, "Original text");

        // Assert
        viewModel.AcceptAllCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void AcceptSelectedCommand_CanExecute_FalseWhenNoneSelected()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var changes = new List<SimplificationChange>
        {
            new("w1", "s1", SimplificationChangeType.WordSimplification, "T1")
        };
        var result = CreateSuccessResultWithChanges(changes);
        viewModel.SetResult(result, "Original text");

        // Deselect all
        viewModel.Changes[0].IsSelected = false;

        // Assert
        viewModel.AcceptSelectedCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task AcceptAllCommand_PublishesAcceptedEvent()
    {
        // Arrange
        _mockEditorService.SelectionStart.Returns(0);
        _mockEditorService.SelectionLength.Returns(10);
        _mockTargetService.GetAllPresetsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AudiencePreset>().AsReadOnly());

        var viewModel = CreateViewModel();
        await viewModel.InitializeAsync("/path/to/doc.md");

        var changes = new List<SimplificationChange>
        {
            new("w1", "s1", SimplificationChangeType.WordSimplification, "T1")
        };
        var result = CreateSuccessResultWithChanges(changes);
        viewModel.SetResult(result, "Original text");

        // Act
        await viewModel.AcceptAllCommand.ExecuteAsync(null);

        // Assert
        await _mockMediator.Received(1).Publish(
            Arg.Is<SimplificationAcceptedEvent>(e =>
                e.TotalChangeCount == 1 &&
                e.AcceptedChangeCount == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectAllCommand_PublishesRejectedEvent()
    {
        // Arrange
        _mockTargetService.GetAllPresetsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AudiencePreset>().AsReadOnly());

        var viewModel = CreateViewModel();
        await viewModel.InitializeAsync("/path/to/doc.md");

        var result = CreateSuccessResult();
        viewModel.SetResult(result, "Original text");

        // Act
        await viewModel.RejectAllCommand.ExecuteAsync(null);

        // Assert
        await _mockMediator.Received(1).Publish(
            Arg.Is<SimplificationRejectedEvent>(e =>
                e.Reason == SimplificationRejectedEvent.ReasonUserCancelled),
            Arg.Any<CancellationToken>());
    }

    // ── ViewMode Tests ───────────────────────────────────────────────────

    [Fact]
    public void SetViewModeCommand_ChangesViewMode()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.SetViewModeCommand.Execute(DiffViewMode.Inline);

        // Assert
        viewModel.ViewMode.Should().Be(DiffViewMode.Inline);
    }

    [Theory]
    [InlineData(DiffViewMode.SideBySide)]
    [InlineData(DiffViewMode.Inline)]
    [InlineData(DiffViewMode.ChangesOnly)]
    public void SetViewModeCommand_SupportsAllModes(DiffViewMode mode)
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.SetViewModeCommand.Execute(mode);

        // Assert
        viewModel.ViewMode.Should().Be(mode);
    }

    // ── Selection Commands Tests ─────────────────────────────────────────

    [Fact]
    public void SelectAllChangesCommand_SelectsAllChanges()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var changes = new List<SimplificationChange>
        {
            new("w1", "s1", SimplificationChangeType.WordSimplification, "T1"),
            new("w2", "s2", SimplificationChangeType.WordSimplification, "T2")
        };
        var result = CreateSuccessResultWithChanges(changes);
        viewModel.SetResult(result, "Original text");

        // Deselect all first
        foreach (var change in viewModel.Changes)
        {
            change.IsSelected = false;
        }

        // Act
        viewModel.SelectAllChangesCommand.Execute(null);

        // Assert
        viewModel.Changes.All(c => c.IsSelected).Should().BeTrue();
    }

    [Fact]
    public void DeselectAllChangesCommand_DeselectsAllChanges()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var changes = new List<SimplificationChange>
        {
            new("w1", "s1", SimplificationChangeType.WordSimplification, "T1"),
            new("w2", "s2", SimplificationChangeType.WordSimplification, "T2")
        };
        var result = CreateSuccessResultWithChanges(changes);
        viewModel.SetResult(result, "Original text");

        // Act
        viewModel.DeselectAllChangesCommand.Execute(null);

        // Assert
        viewModel.Changes.All(c => !c.IsSelected).Should().BeTrue();
    }

    [Fact]
    public void ToggleChangeCommand_TogglesSelection()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var changes = new List<SimplificationChange>
        {
            new("w1", "s1", SimplificationChangeType.WordSimplification, "T1")
        };
        var result = CreateSuccessResultWithChanges(changes);
        viewModel.SetResult(result, "Original text");

        var changeVm = viewModel.Changes[0];
        var initialState = changeVm.IsSelected;

        // Act
        viewModel.ToggleChangeCommand.Execute(changeVm);

        // Assert
        changeVm.IsSelected.Should().Be(!initialState);
    }

    // ── CloseRequested Event Tests ───────────────────────────────────────

    [Fact]
    public async Task AcceptAllCommand_RaisesCloseRequestedWithAccepted()
    {
        // Arrange
        _mockEditorService.SelectionStart.Returns(0);
        _mockEditorService.SelectionLength.Returns(10);
        _mockTargetService.GetAllPresetsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AudiencePreset>().AsReadOnly());

        var viewModel = CreateViewModel();
        await viewModel.InitializeAsync("/path/to/doc.md");

        var changes = new List<SimplificationChange>
        {
            new("w1", "s1", SimplificationChangeType.WordSimplification, "T1")
        };
        var result = CreateSuccessResultWithChanges(changes);
        viewModel.SetResult(result, "Original text");

        CloseRequestedEventArgs? receivedArgs = null;
        viewModel.CloseRequested += (s, e) => receivedArgs = e;

        // Act
        await viewModel.AcceptAllCommand.ExecuteAsync(null);

        // Assert
        receivedArgs.Should().NotBeNull();
        receivedArgs!.Accepted.Should().BeTrue();
    }

    [Fact]
    public async Task RejectAllCommand_RaisesCloseRequestedWithRejected()
    {
        // Arrange
        _mockTargetService.GetAllPresetsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AudiencePreset>().AsReadOnly());

        var viewModel = CreateViewModel();
        await viewModel.InitializeAsync("/path/to/doc.md");

        var result = CreateSuccessResult();
        viewModel.SetResult(result, "Original text");

        CloseRequestedEventArgs? receivedArgs = null;
        viewModel.CloseRequested += (s, e) => receivedArgs = e;

        // Act
        await viewModel.RejectAllCommand.ExecuteAsync(null);

        // Assert
        receivedArgs.Should().NotBeNull();
        receivedArgs!.Accepted.Should().BeFalse();
    }

    // ── Helper Methods ───────────────────────────────────────────────────

    private SimplificationPreviewViewModel CreateViewModel() =>
        new(
            _mockPipeline,
            _mockTargetService,
            _mockEditorService,
            _mockMediator,
            _mockLicenseContext,
            _logger);

    private static SimplificationResult CreateSuccessResult() =>
        new()
        {
            SimplifiedText = "Simplified text",
            OriginalMetrics = new ReadabilityMetrics
            {
                FleschKincaidGradeLevel = 12.0,
                GunningFogIndex = 13.0,
                FleschReadingEase = 50,
                WordCount = 100,
                SentenceCount = 5,
                SyllableCount = 150,
                ComplexWordCount = 15
            },
            SimplifiedMetrics = new ReadabilityMetrics
            {
                FleschKincaidGradeLevel = 8.0,
                GunningFogIndex = 9.0,
                FleschReadingEase = 70,
                WordCount = 80,
                SentenceCount = 6,
                SyllableCount = 100,
                ComplexWordCount = 8
            },
            Changes = Array.Empty<SimplificationChange>(),
            TokenUsage = new UsageMetrics(1000, 500, 0.025m),
            ProcessingTime = TimeSpan.FromSeconds(3),
            StrategyUsed = SimplificationStrategy.Balanced,
            TargetUsed = ReadabilityTarget.FromExplicit(8.0, 20, true),
            Success = true
        };

    private static SimplificationResult CreateSuccessResultWithChanges(
        IReadOnlyList<SimplificationChange> changes) =>
        new()
        {
            SimplifiedText = "Simplified text",
            OriginalMetrics = new ReadabilityMetrics
            {
                FleschKincaidGradeLevel = 12.0,
                GunningFogIndex = 13.0,
                FleschReadingEase = 50,
                WordCount = 100,
                SentenceCount = 5,
                SyllableCount = 150,
                ComplexWordCount = 15
            },
            SimplifiedMetrics = new ReadabilityMetrics
            {
                FleschKincaidGradeLevel = 8.0,
                GunningFogIndex = 9.0,
                FleschReadingEase = 70,
                WordCount = 80,
                SentenceCount = 6,
                SyllableCount = 100,
                ComplexWordCount = 8
            },
            Changes = changes,
            TokenUsage = new UsageMetrics(1000, 500, 0.025m),
            ProcessingTime = TimeSpan.FromSeconds(3),
            StrategyUsed = SimplificationStrategy.Balanced,
            TargetUsed = ReadabilityTarget.FromExplicit(8.0, 20, true),
            Success = true
        };
}
