// -----------------------------------------------------------------------
// <copyright file="TuningPanelViewModelTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// Unit tests for the Tuning Panel ViewModel (v0.7.5c).
//   Tests cover constructor validation, scan flow, accept/reject/modify/skip
//   commands, bulk operations, navigation, filtering, SuggestionCardViewModel,
//   and dispose behavior.
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Agents;
using Lexichord.Abstractions.Contracts.Agents.Events;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Undo;
using Lexichord.Modules.Agents.Editor.Events;
using Lexichord.Modules.Agents.Tuning;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Agents.Tuning;

/// <summary>
/// Unit tests for <see cref="TuningPanelViewModel"/> and <see cref="SuggestionCardViewModel"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.7.5c as part of the Accept/Reject UI feature.
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.5c")]
public class TuningPanelViewModelTests : IDisposable
{
    #region Test Setup

    // ── Mock Dependencies ─────────────────────────────────────────────────
    private readonly Mock<IStyleDeviationScanner> _mockScanner;
    private readonly Mock<IFixSuggestionGenerator> _mockGenerator;
    private readonly Mock<IEditorService> _mockEditorService;
    private readonly Mock<IUndoRedoService> _mockUndoService;
    private readonly Mock<ILicenseContext> _mockLicenseContext;
    private readonly Mock<IMediator> _mockMediator;
    private readonly ILogger<TuningPanelViewModel> _logger;

    // ── SUT Tracking ─────────────────────────────────────────────────────
    private TuningPanelViewModel? _sut;

    public TuningPanelViewModelTests()
    {
        _mockScanner = new Mock<IStyleDeviationScanner>();
        _mockGenerator = new Mock<IFixSuggestionGenerator>();
        _mockEditorService = new Mock<IEditorService>();
        _mockUndoService = new Mock<IUndoRedoService>();
        _mockLicenseContext = new Mock<ILicenseContext>();
        _mockMediator = new Mock<IMediator>();
        _logger = NullLogger<TuningPanelViewModel>.Instance;

        // LOGIC: Default license setup — WriterPro tier (licensed for Tuning Agent).
        _mockLicenseContext
            .Setup(x => x.IsFeatureEnabled(FeatureCodes.TuningAgent))
            .Returns(true);
        _mockLicenseContext
            .Setup(x => x.GetCurrentTier())
            .Returns(LicenseTier.WriterPro);
    }

    public void Dispose()
    {
        _sut?.Dispose();
    }

    // ── Factory Methods ──────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="TuningPanelViewModel"/> with all mocked dependencies.
    /// </summary>
    /// <param name="withUndoService">Whether to inject the undo service (vs. null).</param>
    /// <param name="licensed">Whether the mock license context should report as licensed.</param>
    /// <returns>A configured <see cref="TuningPanelViewModel"/> instance.</returns>
    private TuningPanelViewModel CreateViewModel(
        bool withUndoService = false,
        bool licensed = true)
    {
        if (!licensed)
        {
            _mockLicenseContext
                .Setup(x => x.IsFeatureEnabled(FeatureCodes.TuningAgent))
                .Returns(false);
            _mockLicenseContext
                .Setup(x => x.GetCurrentTier())
                .Returns(LicenseTier.Core);
        }

        _sut = new TuningPanelViewModel(
            _mockScanner.Object,
            _mockGenerator.Object,
            _mockEditorService.Object,
            withUndoService ? _mockUndoService.Object : null,
            null,
            _mockLicenseContext.Object,
            _mockMediator.Object,
            _logger);

        return _sut;
    }

    /// <summary>
    /// Creates a ViewModel, initializes it, and sets up scan mocks for a standard scan flow.
    /// </summary>
    /// <param name="deviations">The deviations to return from the scanner.</param>
    /// <param name="suggestions">The suggestions to return from the generator.</param>
    /// <param name="documentPath">The document path to return from the editor service.</param>
    /// <param name="withUndoService">Whether to inject the undo service.</param>
    /// <returns>The initialized ViewModel.</returns>
    private TuningPanelViewModel CreateAndSetupScanFlow(
        IReadOnlyList<StyleDeviation> deviations,
        IReadOnlyList<FixSuggestion> suggestions,
        string documentPath = "/test/document.md",
        bool withUndoService = false)
    {
        var vm = CreateViewModel(withUndoService: withUndoService);
        vm.InitializeAsync();

        _mockEditorService
            .Setup(x => x.CurrentDocumentPath)
            .Returns(documentPath);

        var scanResult = new DeviationScanResult
        {
            DocumentPath = documentPath,
            Deviations = deviations
        };

        _mockScanner
            .Setup(x => x.ScanDocumentAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanResult);

        _mockGenerator
            .Setup(x => x.GenerateFixesAsync(
                It.IsAny<IReadOnlyList<StyleDeviation>>(),
                It.IsAny<FixGenerationOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestions);

        return vm;
    }

    /// <summary>
    /// Creates a test <see cref="StyleRule"/> with default values.
    /// </summary>
    private static StyleRule CreateTestRule(
        string id = "test-rule-001",
        string name = "Test Rule") =>
        new(
            Id: id,
            Name: name,
            Description: "A test rule for unit tests",
            Category: RuleCategory.Terminology,
            DefaultSeverity: ViolationSeverity.Warning,
            Pattern: "test",
            PatternType: PatternType.Literal,
            Suggestion: "suggested fix");

    /// <summary>
    /// Creates a test <see cref="StyleViolation"/> with default values.
    /// </summary>
    private static StyleViolation CreateTestViolation(
        StyleRule? rule = null,
        int startOffset = 0,
        string matchedText = "test") =>
        new(
            Rule: rule ?? CreateTestRule(),
            Message: "Test violation message",
            StartOffset: startOffset,
            EndOffset: startOffset + matchedText.Length,
            StartLine: 1,
            StartColumn: startOffset,
            EndLine: 1,
            EndColumn: startOffset + matchedText.Length,
            MatchedText: matchedText,
            Suggestion: "fixed",
            Severity: ViolationSeverity.Warning);

    /// <summary>
    /// Creates a test <see cref="StyleDeviation"/> with default values.
    /// </summary>
    private static StyleDeviation CreateTestDeviation(
        int startOffset = 0,
        string originalText = "test",
        bool isAutoFixable = true,
        DeviationPriority priority = DeviationPriority.Normal,
        StyleRule? rule = null)
    {
        var testRule = rule ?? CreateTestRule();
        var violation = CreateTestViolation(
            rule: testRule,
            startOffset: startOffset,
            matchedText: originalText);

        return new StyleDeviation
        {
            DeviationId = Guid.NewGuid(),
            Violation = violation,
            Location = new TextSpan(startOffset, originalText.Length),
            OriginalText = originalText,
            SurroundingContext = $"surrounding context for '{originalText}'",
            ViolatedRule = testRule,
            IsAutoFixable = isAutoFixable,
            Priority = priority
        };
    }

    /// <summary>
    /// Creates a test <see cref="FixSuggestion"/> with default values.
    /// </summary>
    private static FixSuggestion CreateTestSuggestion(
        Guid? deviationId = null,
        string originalText = "test",
        string suggestedText = "fixed",
        double confidence = 0.85,
        double qualityScore = 0.85) =>
        new()
        {
            SuggestionId = Guid.NewGuid(),
            DeviationId = deviationId ?? Guid.NewGuid(),
            OriginalText = originalText,
            SuggestedText = suggestedText,
            Explanation = "Test explanation for the fix",
            Diff = TextDiff.Empty,
            Confidence = confidence,
            QualityScore = qualityScore
        };

    /// <summary>
    /// Creates a high-confidence <see cref="FixSuggestion"/> that passes
    /// the <see cref="FixSuggestion.IsHighConfidence"/> check.
    /// </summary>
    private static FixSuggestion CreateHighConfidenceSuggestion(
        Guid? deviationId = null,
        string originalText = "test",
        string suggestedText = "fixed") =>
        new()
        {
            SuggestionId = Guid.NewGuid(),
            DeviationId = deviationId ?? Guid.NewGuid(),
            OriginalText = originalText,
            SuggestedText = suggestedText,
            Explanation = "High confidence fix explanation",
            Diff = TextDiff.Empty,
            Confidence = 0.95,
            QualityScore = 0.95,
            IsValidated = true,
            ValidationResult = FixValidationResult.Valid(0.95)
        };

    /// <summary>
    /// Creates a deviation/suggestion pair for scan flow tests.
    /// </summary>
    private static (StyleDeviation Deviation, FixSuggestion Suggestion) CreateDeviationSuggestionPair(
        int startOffset = 0,
        string originalText = "test",
        string suggestedText = "fixed",
        bool highConfidence = false,
        DeviationPriority priority = DeviationPriority.Normal)
    {
        var deviation = CreateTestDeviation(
            startOffset: startOffset,
            originalText: originalText,
            priority: priority);

        var suggestion = highConfidence
            ? CreateHighConfidenceSuggestion(deviation.DeviationId, originalText, suggestedText)
            : CreateTestSuggestion(deviation.DeviationId, originalText, suggestedText);

        return (deviation, suggestion);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullScanner_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new TuningPanelViewModel(
            null!,
            _mockGenerator.Object,
            _mockEditorService.Object,
            null,
            null,
            _mockLicenseContext.Object,
            _mockMediator.Object,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("scanner");
    }

    [Fact]
    public void Constructor_NullSuggestionGenerator_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new TuningPanelViewModel(
            _mockScanner.Object,
            null!,
            _mockEditorService.Object,
            null,
            null,
            _mockLicenseContext.Object,
            _mockMediator.Object,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("suggestionGenerator");
    }

    [Fact]
    public void Constructor_NullEditorService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new TuningPanelViewModel(
            _mockScanner.Object,
            _mockGenerator.Object,
            null!,
            null,
            null,
            _mockLicenseContext.Object,
            _mockMediator.Object,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("editorService");
    }

    [Fact]
    public void Constructor_NullLicenseContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new TuningPanelViewModel(
            _mockScanner.Object,
            _mockGenerator.Object,
            _mockEditorService.Object,
            null,
            null,
            null!,
            _mockMediator.Object,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("licenseContext");
    }

    [Fact]
    public void Constructor_NullMediator_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new TuningPanelViewModel(
            _mockScanner.Object,
            _mockGenerator.Object,
            _mockEditorService.Object,
            null,
            null,
            _mockLicenseContext.Object,
            null!,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("mediator");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new TuningPanelViewModel(
            _mockScanner.Object,
            _mockGenerator.Object,
            _mockEditorService.Object,
            null,
            null,
            _mockLicenseContext.Object,
            _mockMediator.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_NullUndoService_DoesNotThrow()
    {
        // Act
        var act = () => CreateViewModel(withUndoService: false);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_InitializesDefaultState()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        vm.Suggestions.Should().BeEmpty();
        vm.SelectedSuggestion.Should().BeNull();
        vm.IsScanning.Should().BeFalse();
        vm.IsGeneratingFixes.Should().BeFalse();
        vm.IsBulkProcessing.Should().BeFalse();
        vm.TotalDeviations.Should().Be(0);
        vm.ReviewedCount.Should().Be(0);
        vm.AcceptedCount.Should().Be(0);
        vm.RejectedCount.Should().Be(0);
        vm.CurrentFilter.Should().Be(SuggestionFilter.All);
        vm.StatusMessage.Should().Be("Ready to scan");
        vm.ProgressPercent.Should().Be(0);
    }

    #endregion

    #region InitializeAsync Tests

    [Fact]
    public void InitializeAsync_Licensed_SetsHasWriterProLicenseTrue()
    {
        // Arrange
        var vm = CreateViewModel(licensed: true);

        // Act
        vm.InitializeAsync();

        // Assert
        vm.HasWriterProLicense.Should().BeTrue();
    }

    [Fact]
    public void InitializeAsync_Unlicensed_SetsHasWriterProLicenseFalse()
    {
        // Arrange
        var vm = CreateViewModel(licensed: false);

        // Act
        vm.InitializeAsync();

        // Assert
        vm.HasWriterProLicense.Should().BeFalse();
    }

    [Fact]
    public void InitializeAsync_TeamsTier_SetsHasTeamsLicenseTrue()
    {
        // Arrange
        _mockLicenseContext
            .Setup(x => x.GetCurrentTier())
            .Returns(LicenseTier.Teams);
        var vm = CreateViewModel();

        // Act
        vm.InitializeAsync();

        // Assert
        vm.HasTeamsLicense.Should().BeTrue();
    }

    [Fact]
    public void InitializeAsync_Unlicensed_SetsLicenseRequiredStatusMessage()
    {
        // Arrange
        var vm = CreateViewModel(licensed: false);

        // Act
        vm.InitializeAsync();

        // Assert
        vm.StatusMessage.Should().Contain("Writer Pro license required");
    }

    #endregion

    #region ScanDocumentAsync Tests

    [Fact]
    public async Task ScanDocumentAsync_WithDeviations_CreatesSuggestionCards()
    {
        // Arrange
        var (deviation, suggestion) = CreateDeviationSuggestionPair();
        var vm = CreateAndSetupScanFlow(
            new[] { deviation },
            new[] { suggestion });

        // Act
        await vm.ScanDocumentCommand.ExecuteAsync(null);

        // Assert
        vm.Suggestions.Should().HaveCount(1);
        vm.TotalDeviations.Should().Be(1);
    }

    [Fact]
    public async Task ScanDocumentAsync_SelectsFirstSuggestion()
    {
        // Arrange
        var (deviation, suggestion) = CreateDeviationSuggestionPair();
        var vm = CreateAndSetupScanFlow(
            new[] { deviation },
            new[] { suggestion });

        // Act
        await vm.ScanDocumentCommand.ExecuteAsync(null);

        // Assert
        vm.SelectedSuggestion.Should().NotBeNull();
        vm.SelectedSuggestion!.IsExpanded.Should().BeTrue();
    }

    [Fact]
    public async Task ScanDocumentAsync_NoDocumentOpen_SetsStatusMessage()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.InitializeAsync();
        _mockEditorService.Setup(x => x.CurrentDocumentPath).Returns((string?)null);

        // Act
        await vm.ScanDocumentCommand.ExecuteAsync(null);

        // Assert
        vm.StatusMessage.Should().Be("No document open");
        vm.Suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task ScanDocumentAsync_EmptyResults_SetsNoDeviationsMessage()
    {
        // Arrange
        var vm = CreateAndSetupScanFlow(
            Array.Empty<StyleDeviation>(),
            Array.Empty<FixSuggestion>());

        // Act
        await vm.ScanDocumentCommand.ExecuteAsync(null);

        // Assert
        vm.StatusMessage.Should().Contain("No style deviations found");
        vm.TotalDeviations.Should().Be(0);
    }

    [Fact]
    public async Task ScanDocumentAsync_Unlicensed_PublishesUpgradeModalEvent()
    {
        // Arrange
        var vm = CreateViewModel(licensed: false);
        vm.InitializeAsync();

        // Act
        await vm.ScanDocumentCommand.ExecuteAsync(null);

        // Assert
        _mockMediator.Verify(
            x => x.Publish(
                It.IsAny<ShowUpgradeModalEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // LOGIC: Scanner should NOT be called when unlicensed.
        _mockScanner.Verify(
            x => x.ScanDocumentAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ScanDocumentAsync_OnlyAddsSuccessfulSuggestions()
    {
        // Arrange
        var deviation1 = CreateTestDeviation(startOffset: 0, originalText: "bad1");
        var deviation2 = CreateTestDeviation(startOffset: 10, originalText: "bad2");

        var successSuggestion = CreateTestSuggestion(
            deviation1.DeviationId, "bad1", "good1");
        var failedSuggestion = FixSuggestion.Failed(
            deviation2.DeviationId, "bad2", "LLM error");

        var vm = CreateAndSetupScanFlow(
            new[] { deviation1, deviation2 },
            new[] { successSuggestion, failedSuggestion });

        // Act
        await vm.ScanDocumentCommand.ExecuteAsync(null);

        // Assert — only the successful suggestion should be added.
        vm.Suggestions.Should().HaveCount(1);
        vm.Suggestions[0].Suggestion.SuggestionId.Should().Be(successSuggestion.SuggestionId);
    }

    [Fact]
    public async Task ScanDocumentAsync_ScannerError_SetsErrorStatusMessage()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.InitializeAsync();

        _mockEditorService.Setup(x => x.CurrentDocumentPath).Returns("/test/doc.md");
        _mockScanner
            .Setup(x => x.ScanDocumentAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Scanner broke"));

        // Act
        await vm.ScanDocumentCommand.ExecuteAsync(null);

        // Assert
        vm.StatusMessage.Should().Contain("Scan failed");
        vm.IsScanning.Should().BeFalse();
    }

    [Fact]
    public async Task ScanDocumentAsync_ResetsStateBeforeScanning()
    {
        // Arrange
        var (deviation, suggestion) = CreateDeviationSuggestionPair();
        var vm = CreateAndSetupScanFlow(
            new[] { deviation },
            new[] { suggestion });

        // First scan
        await vm.ScanDocumentCommand.ExecuteAsync(null);
        vm.Suggestions.Should().HaveCount(1);

        // Act — second scan should reset
        await vm.ScanDocumentCommand.ExecuteAsync(null);

        // Assert
        vm.Suggestions.Should().HaveCount(1); // not 2
    }

    [Fact]
    public async Task ScanDocumentAsync_SetsProgressPercent()
    {
        // Arrange
        var (deviation, suggestion) = CreateDeviationSuggestionPair();
        var vm = CreateAndSetupScanFlow(
            new[] { deviation },
            new[] { suggestion });

        // Act
        await vm.ScanDocumentCommand.ExecuteAsync(null);

        // Assert — progress should be 100 after completion.
        vm.ProgressPercent.Should().Be(100);
    }

    #endregion

    #region AcceptSuggestionAsync Tests

    [Fact]
    public async Task AcceptSuggestionAsync_UpdatesStatusAndCounters()
    {
        // Arrange
        var (deviation, suggestion) = CreateDeviationSuggestionPair();
        var vm = CreateAndSetupScanFlow(
            new[] { deviation },
            new[] { suggestion });

        _mockEditorService.Setup(x => x.GetDocumentText()).Returns("test content here");
        await vm.ScanDocumentCommand.ExecuteAsync(null);
        var card = vm.Suggestions[0];

        // Act
        await vm.AcceptSuggestionCommand.ExecuteAsync(card);

        // Assert
        card.Status.Should().Be(SuggestionStatus.Accepted);
        card.IsReviewed.Should().BeTrue();
        vm.AcceptedCount.Should().Be(1);
        vm.ReviewedCount.Should().Be(1);
    }

    [Fact]
    public async Task AcceptSuggestionAsync_CallsEditorService()
    {
        // Arrange
        var deviation = CreateTestDeviation(startOffset: 5, originalText: "bad");
        var suggestion = CreateTestSuggestion(deviation.DeviationId, "bad", "good");
        var vm = CreateAndSetupScanFlow(
            new[] { deviation },
            new[] { suggestion });

        _mockEditorService.Setup(x => x.GetDocumentText()).Returns("Some bad text here");
        await vm.ScanDocumentCommand.ExecuteAsync(null);
        var card = vm.Suggestions[0];

        // Act
        await vm.AcceptSuggestionCommand.ExecuteAsync(card);

        // Assert
        _mockEditorService.Verify(x => x.BeginUndoGroup(It.Is<string>(s => s.Contains("Tuning Fix"))), Times.Once);
        _mockEditorService.Verify(x => x.DeleteText(5, 3), Times.Once);
        _mockEditorService.Verify(x => x.InsertText(5, "good"), Times.Once);
        _mockEditorService.Verify(x => x.EndUndoGroup(), Times.Once);
    }

    [Fact]
    public async Task AcceptSuggestionAsync_NullSuggestion_DoesNothing()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.InitializeAsync();

        // Act
        await vm.AcceptSuggestionCommand.ExecuteAsync(null);

        // Assert — no editor calls.
        _mockEditorService.Verify(
            x => x.BeginUndoGroup(It.IsAny<string>()), Times.Never);
        vm.AcceptedCount.Should().Be(0);
    }

    [Fact]
    public async Task AcceptSuggestionAsync_NonPendingSuggestion_DoesNothing()
    {
        // Arrange
        var (deviation, suggestion) = CreateDeviationSuggestionPair();
        var vm = CreateAndSetupScanFlow(
            new[] { deviation },
            new[] { suggestion });

        _mockEditorService.Setup(x => x.GetDocumentText()).Returns("test content");
        await vm.ScanDocumentCommand.ExecuteAsync(null);
        var card = vm.Suggestions[0];
        card.Status = SuggestionStatus.Rejected; // Pre-set to non-pending.

        // Act
        await vm.AcceptSuggestionCommand.ExecuteAsync(card);

        // Assert
        _mockEditorService.Verify(
            x => x.BeginUndoGroup(It.IsAny<string>()), Times.Never);
        vm.AcceptedCount.Should().Be(0);
    }

    [Fact]
    public async Task AcceptSuggestionAsync_PublishesMediatREvent()
    {
        // Arrange
        var (deviation, suggestion) = CreateDeviationSuggestionPair();
        var vm = CreateAndSetupScanFlow(
            new[] { deviation },
            new[] { suggestion });

        _mockEditorService.Setup(x => x.GetDocumentText()).Returns("test content");
        await vm.ScanDocumentCommand.ExecuteAsync(null);
        var card = vm.Suggestions[0];

        // Act
        await vm.AcceptSuggestionCommand.ExecuteAsync(card);

        // Assert
        _mockMediator.Verify(
            x => x.Publish(
                It.IsAny<SuggestionAcceptedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AcceptSuggestionAsync_WithUndoService_PushesUndoOperation()
    {
        // Arrange
        var (deviation, suggestion) = CreateDeviationSuggestionPair();
        var vm = CreateAndSetupScanFlow(
            new[] { deviation },
            new[] { suggestion },
            withUndoService: true);

        _mockEditorService.Setup(x => x.GetDocumentText()).Returns("test content");
        await vm.ScanDocumentCommand.ExecuteAsync(null);
        var card = vm.Suggestions[0];

        // Act
        await vm.AcceptSuggestionCommand.ExecuteAsync(card);

        // Assert
        _mockUndoService.Verify(
            x => x.Push(It.IsAny<TuningUndoableOperation>()),
            Times.Once);
    }

    [Fact]
    public async Task AcceptSuggestionAsync_WithoutUndoService_DoesNotThrow()
    {
        // Arrange
        var (deviation, suggestion) = CreateDeviationSuggestionPair();
        var vm = CreateAndSetupScanFlow(
            new[] { deviation },
            new[] { suggestion },
            withUndoService: false);

        _mockEditorService.Setup(x => x.GetDocumentText()).Returns("test content");
        await vm.ScanDocumentCommand.ExecuteAsync(null);
        var card = vm.Suggestions[0];

        // Act
        var act = () => vm.AcceptSuggestionCommand.ExecuteAsync(card);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region RejectSuggestionAsync Tests

    [Fact]
    public async Task RejectSuggestionAsync_UpdatesStatusToRejected()
    {
        // Arrange
        var (deviation, suggestion) = CreateDeviationSuggestionPair();
        var vm = CreateAndSetupScanFlow(
            new[] { deviation },
            new[] { suggestion });

        await vm.ScanDocumentCommand.ExecuteAsync(null);
        var card = vm.Suggestions[0];

        // Act
        await vm.RejectSuggestionCommand.ExecuteAsync(card);

        // Assert
        card.Status.Should().Be(SuggestionStatus.Rejected);
        card.IsReviewed.Should().BeTrue();
        vm.RejectedCount.Should().Be(1);
        vm.ReviewedCount.Should().Be(1);
    }

    [Fact]
    public async Task RejectSuggestionAsync_DoesNotCallEditorService()
    {
        // Arrange
        var (deviation, suggestion) = CreateDeviationSuggestionPair();
        var vm = CreateAndSetupScanFlow(
            new[] { deviation },
            new[] { suggestion });

        await vm.ScanDocumentCommand.ExecuteAsync(null);
        var card = vm.Suggestions[0];

        // Act
        await vm.RejectSuggestionCommand.ExecuteAsync(card);

        // Assert — reject should NOT modify the document.
        _mockEditorService.Verify(
            x => x.BeginUndoGroup(It.IsAny<string>()), Times.Never);
        _mockEditorService.Verify(
            x => x.DeleteText(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RejectSuggestionAsync_PublishesMediatREvent()
    {
        // Arrange
        var (deviation, suggestion) = CreateDeviationSuggestionPair();
        var vm = CreateAndSetupScanFlow(
            new[] { deviation },
            new[] { suggestion });

        await vm.ScanDocumentCommand.ExecuteAsync(null);
        var card = vm.Suggestions[0];

        // Act
        await vm.RejectSuggestionCommand.ExecuteAsync(card);

        // Assert
        _mockMediator.Verify(
            x => x.Publish(
                It.IsAny<SuggestionRejectedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RejectSuggestionAsync_NullSuggestion_DoesNothing()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.InitializeAsync();

        // Act
        await vm.RejectSuggestionCommand.ExecuteAsync(null);

        // Assert
        vm.RejectedCount.Should().Be(0);
    }

    #endregion

    #region ModifySuggestionAsync Tests

    [Fact]
    public async Task ModifySuggestionAsync_AppliesModifiedTextViaEditor()
    {
        // Arrange
        var deviation = CreateTestDeviation(startOffset: 5, originalText: "bad");
        var suggestion = CreateTestSuggestion(deviation.DeviationId, "bad", "good");
        var vm = CreateAndSetupScanFlow(
            new[] { deviation },
            new[] { suggestion });

        _mockEditorService.Setup(x => x.GetDocumentText()).Returns("Some bad text here");
        await vm.ScanDocumentCommand.ExecuteAsync(null);
        var card = vm.Suggestions[0];

        // Act
        await vm.ModifySuggestionAsync(card, "better");

        // Assert
        _mockEditorService.Verify(x => x.DeleteText(5, 3), Times.Once);
        _mockEditorService.Verify(x => x.InsertText(5, "better"), Times.Once);
    }

    [Fact]
    public async Task ModifySuggestionAsync_SetsStatusToModified()
    {
        // Arrange
        var (deviation, suggestion) = CreateDeviationSuggestionPair();
        var vm = CreateAndSetupScanFlow(
            new[] { deviation },
            new[] { suggestion });

        _mockEditorService.Setup(x => x.GetDocumentText()).Returns("test content");
        await vm.ScanDocumentCommand.ExecuteAsync(null);
        var card = vm.Suggestions[0];

        // Act
        await vm.ModifySuggestionAsync(card, "user modified text");

        // Assert
        card.Status.Should().Be(SuggestionStatus.Modified);
        card.ModifiedText.Should().Be("user modified text");
        card.IsReviewed.Should().BeTrue();
        vm.AcceptedCount.Should().Be(1); // Modify counts as accepted.
        vm.ReviewedCount.Should().Be(1);
    }

    [Fact]
    public async Task ModifySuggestionAsync_PublishesModifiedEvent()
    {
        // Arrange
        var (deviation, suggestion) = CreateDeviationSuggestionPair();
        var vm = CreateAndSetupScanFlow(
            new[] { deviation },
            new[] { suggestion });

        _mockEditorService.Setup(x => x.GetDocumentText()).Returns("test content");
        await vm.ScanDocumentCommand.ExecuteAsync(null);
        var card = vm.Suggestions[0];

        // Act
        await vm.ModifySuggestionAsync(card, "modified");

        // Assert
        _mockMediator.Verify(
            x => x.Publish(
                It.Is<SuggestionAcceptedEvent>(e => e.IsModified),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ModifySuggestionAsync_NullModifiedText_DoesNothing()
    {
        // Arrange
        var (deviation, suggestion) = CreateDeviationSuggestionPair();
        var vm = CreateAndSetupScanFlow(
            new[] { deviation },
            new[] { suggestion });

        await vm.ScanDocumentCommand.ExecuteAsync(null);
        var card = vm.Suggestions[0];

        // Act
        await vm.ModifySuggestionAsync(card, null);

        // Assert
        card.Status.Should().Be(SuggestionStatus.Pending);
        _mockEditorService.Verify(
            x => x.BeginUndoGroup(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region SkipSuggestion Tests

    [Fact]
    public void SkipSuggestion_SetsStatusToSkipped()
    {
        // Arrange
        var deviation = CreateTestDeviation();
        var suggestion = CreateTestSuggestion(deviation.DeviationId);
        var card = new SuggestionCardViewModel(deviation, suggestion);

        var vm = CreateViewModel();
        vm.InitializeAsync();
        vm.Suggestions.Add(card);

        // Act
        vm.SkipSuggestionCommand.Execute(card);

        // Assert
        card.Status.Should().Be(SuggestionStatus.Skipped);
        card.IsReviewed.Should().BeTrue();
        vm.ReviewedCount.Should().Be(1);
    }

    [Fact]
    public void SkipSuggestion_DoesNotIncrementAcceptedOrRejected()
    {
        // Arrange
        var deviation = CreateTestDeviation();
        var suggestion = CreateTestSuggestion(deviation.DeviationId);
        var card = new SuggestionCardViewModel(deviation, suggestion);

        var vm = CreateViewModel();
        vm.InitializeAsync();
        vm.Suggestions.Add(card);

        // Act
        vm.SkipSuggestionCommand.Execute(card);

        // Assert
        vm.AcceptedCount.Should().Be(0);
        vm.RejectedCount.Should().Be(0);
    }

    [Fact]
    public void SkipSuggestion_DoesNotPublishMediatREvent()
    {
        // Arrange
        var deviation = CreateTestDeviation();
        var suggestion = CreateTestSuggestion(deviation.DeviationId);
        var card = new SuggestionCardViewModel(deviation, suggestion);

        var vm = CreateViewModel();
        vm.InitializeAsync();
        vm.Suggestions.Add(card);

        // Act
        vm.SkipSuggestionCommand.Execute(card);

        // Assert — skip should NOT publish any event.
        _mockMediator.Verify(
            x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region AcceptAllHighConfidenceAsync Tests

    [Fact]
    public async Task AcceptAllHighConfidenceAsync_AcceptsOnlyHighConfidencePending()
    {
        // Arrange
        var (d1, s1) = CreateDeviationSuggestionPair(startOffset: 0, highConfidence: true);
        var (d2, s2) = CreateDeviationSuggestionPair(startOffset: 20, highConfidence: false);
        var (d3, s3) = CreateDeviationSuggestionPair(startOffset: 40, highConfidence: true);

        var vm = CreateAndSetupScanFlow(
            new[] { d1, d2, d3 },
            new[] { s1, s2, s3 });

        _mockEditorService.Setup(x => x.GetDocumentText()).Returns(new string('x', 100));
        await vm.ScanDocumentCommand.ExecuteAsync(null);

        // Pre-check: 3 suggestions total, 2 high-confidence.
        vm.Suggestions.Should().HaveCount(3);

        // Act
        await vm.AcceptAllHighConfidenceCommand.ExecuteAsync(null);

        // Assert — only the two high-confidence suggestions should be accepted.
        vm.Suggestions.Count(s => s.Status == SuggestionStatus.Accepted).Should().Be(2);
        vm.Suggestions.Count(s => s.Status == SuggestionStatus.Pending).Should().Be(1);
        vm.AcceptedCount.Should().Be(2);
    }

    [Fact]
    public async Task AcceptAllHighConfidenceAsync_AppliesInReverseDocumentOrder()
    {
        // Arrange
        var (d1, s1) = CreateDeviationSuggestionPair(
            startOffset: 10, originalText: "first", suggestedText: "FIRST", highConfidence: true);
        var (d2, s2) = CreateDeviationSuggestionPair(
            startOffset: 30, originalText: "second", suggestedText: "SECOND", highConfidence: true);

        var vm = CreateAndSetupScanFlow(
            new[] { d1, d2 },
            new[] { s1, s2 });

        await vm.ScanDocumentCommand.ExecuteAsync(null);

        var callOrder = new List<int>();
        _mockEditorService
            .Setup(x => x.DeleteText(It.IsAny<int>(), It.IsAny<int>()))
            .Callback<int, int>((offset, _) => callOrder.Add(offset));

        // Act
        await vm.AcceptAllHighConfidenceCommand.ExecuteAsync(null);

        // Assert — should apply offset=30 before offset=10 (reverse order).
        callOrder.Should().ContainInOrder(30, 10);
    }

    [Fact]
    public async Task AcceptAllHighConfidenceAsync_WrapsInSingleUndoGroup()
    {
        // Arrange
        var (d1, s1) = CreateDeviationSuggestionPair(startOffset: 0, highConfidence: true);
        var (d2, s2) = CreateDeviationSuggestionPair(startOffset: 20, highConfidence: true);

        var vm = CreateAndSetupScanFlow(
            new[] { d1, d2 },
            new[] { s1, s2 });

        await vm.ScanDocumentCommand.ExecuteAsync(null);

        // Act
        await vm.AcceptAllHighConfidenceCommand.ExecuteAsync(null);

        // Assert — one BeginUndoGroup call and one EndUndoGroup call.
        _mockEditorService.Verify(
            x => x.BeginUndoGroup(It.Is<string>(s => s.Contains("Accept All"))), Times.Once);
        _mockEditorService.Verify(x => x.EndUndoGroup(), Times.Once);
    }

    [Fact]
    public async Task AcceptAllHighConfidenceAsync_NoHighConfidence_SetsStatusMessage()
    {
        // Arrange
        var (deviation, suggestion) = CreateDeviationSuggestionPair(highConfidence: false);
        var vm = CreateAndSetupScanFlow(
            new[] { deviation },
            new[] { suggestion });

        await vm.ScanDocumentCommand.ExecuteAsync(null);

        // Act
        await vm.AcceptAllHighConfidenceCommand.ExecuteAsync(null);

        // Assert
        vm.StatusMessage.Should().Contain("No high-confidence suggestions");
        _mockEditorService.Verify(
            x => x.BeginUndoGroup(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AcceptAllHighConfidenceAsync_PublishesEventForEach()
    {
        // Arrange
        var (d1, s1) = CreateDeviationSuggestionPair(startOffset: 0, highConfidence: true);
        var (d2, s2) = CreateDeviationSuggestionPair(startOffset: 20, highConfidence: true);

        var vm = CreateAndSetupScanFlow(
            new[] { d1, d2 },
            new[] { s1, s2 });

        await vm.ScanDocumentCommand.ExecuteAsync(null);

        // Act
        await vm.AcceptAllHighConfidenceCommand.ExecuteAsync(null);

        // Assert — one event per accepted suggestion.
        _mockMediator.Verify(
            x => x.Publish(
                It.IsAny<SuggestionAcceptedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    #endregion

    #region RegenerateSuggestionAsync Tests

    [Fact]
    public async Task RegenerateSuggestionAsync_UpdatesSuggestionOnCard()
    {
        // Arrange
        var (deviation, suggestion) = CreateDeviationSuggestionPair();
        var vm = CreateAndSetupScanFlow(
            new[] { deviation },
            new[] { suggestion });

        await vm.ScanDocumentCommand.ExecuteAsync(null);
        var card = vm.Suggestions[0];

        var regeneratedSuggestion = CreateTestSuggestion(
            deviation.DeviationId, "test", "better fix",
            confidence: 0.95, qualityScore: 0.92);

        _mockGenerator
            .Setup(x => x.RegenerateFixAsync(
                It.IsAny<StyleDeviation>(),
                It.IsAny<string>(),
                It.IsAny<FixGenerationOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(regeneratedSuggestion);

        // Act
        await vm.RegenerateSuggestionAsync(card, "make it more formal");

        // Assert
        card.Suggestion.SuggestionId.Should().Be(regeneratedSuggestion.SuggestionId);
        card.SuggestedText.Should().Be("better fix");
        card.IsRegenerating.Should().BeFalse();
    }

    [Fact]
    public async Task RegenerateSuggestionAsync_NullSuggestion_DoesNothing()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.InitializeAsync();

        // Act
        await vm.RegenerateSuggestionAsync(null, "guidance");

        // Assert
        _mockGenerator.Verify(
            x => x.RegenerateFixAsync(
                It.IsAny<StyleDeviation>(),
                It.IsAny<string>(),
                It.IsAny<FixGenerationOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegenerateSuggestionAsync_Error_SetsErrorStatusMessage()
    {
        // Arrange
        var (deviation, suggestion) = CreateDeviationSuggestionPair();
        var vm = CreateAndSetupScanFlow(
            new[] { deviation },
            new[] { suggestion });

        await vm.ScanDocumentCommand.ExecuteAsync(null);
        var card = vm.Suggestions[0];

        _mockGenerator
            .Setup(x => x.RegenerateFixAsync(
                It.IsAny<StyleDeviation>(),
                It.IsAny<string>(),
                It.IsAny<FixGenerationOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("LLM failed"));

        // Act
        await vm.RegenerateSuggestionAsync(card, null);

        // Assert
        vm.StatusMessage.Should().Contain("Regeneration failed");
        card.IsRegenerating.Should().BeFalse();
    }

    #endregion

    #region NavigateNext/Previous Tests

    [Fact]
    public async Task NavigateNext_MovesToNextPendingSuggestion()
    {
        // Arrange
        var (d1, s1) = CreateDeviationSuggestionPair(startOffset: 0);
        var (d2, s2) = CreateDeviationSuggestionPair(startOffset: 20);
        var (d3, s3) = CreateDeviationSuggestionPair(startOffset: 40);

        var vm = CreateAndSetupScanFlow(
            new[] { d1, d2, d3 },
            new[] { s1, s2, s3 });

        await vm.ScanDocumentCommand.ExecuteAsync(null);

        // Mark second suggestion as accepted (non-pending).
        vm.Suggestions[1].Status = SuggestionStatus.Accepted;

        // Selected is first.
        vm.SelectedSuggestion.Should().Be(vm.Suggestions[0]);

        // Act
        vm.NavigateNextCommand.Execute(null);

        // Assert — should skip suggestion[1] (Accepted) and go to suggestion[2].
        vm.SelectedSuggestion.Should().Be(vm.Suggestions[2]);
    }

    [Fact]
    public async Task NavigateNext_WrapsAroundToBeginning()
    {
        // Arrange
        var (d1, s1) = CreateDeviationSuggestionPair(startOffset: 0);
        var (d2, s2) = CreateDeviationSuggestionPair(startOffset: 20);

        var vm = CreateAndSetupScanFlow(
            new[] { d1, d2 },
            new[] { s1, s2 });

        await vm.ScanDocumentCommand.ExecuteAsync(null);

        // Move selection to last suggestion.
        vm.Suggestions[1].Status = SuggestionStatus.Pending;
        // Manually select the second.
        vm.NavigateNextCommand.Execute(null); // Goes to suggestion[1].

        // Mark suggestion[1] as accepted so next wrap goes to [0].
        vm.Suggestions[1].Status = SuggestionStatus.Accepted;

        // Act
        vm.NavigateNextCommand.Execute(null);

        // Assert — should wrap around to suggestion[0].
        vm.SelectedSuggestion.Should().Be(vm.Suggestions[0]);
    }

    [Fact]
    public void NavigateNext_NoSuggestions_DoesNothing()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.InitializeAsync();

        // Act & Assert — should not throw.
        var act = () => vm.NavigateNextCommand.Execute(null);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task NavigatePrevious_MovesToPreviousSuggestion()
    {
        // Arrange
        var (d1, s1) = CreateDeviationSuggestionPair(startOffset: 0);
        var (d2, s2) = CreateDeviationSuggestionPair(startOffset: 20);
        var (d3, s3) = CreateDeviationSuggestionPair(startOffset: 40);

        var vm = CreateAndSetupScanFlow(
            new[] { d1, d2, d3 },
            new[] { s1, s2, s3 });

        await vm.ScanDocumentCommand.ExecuteAsync(null);

        // Move to second suggestion first.
        vm.NavigateNextCommand.Execute(null);
        vm.SelectedSuggestion.Should().Be(vm.Suggestions[1]);

        // Act
        vm.NavigatePreviousCommand.Execute(null);

        // Assert
        vm.SelectedSuggestion.Should().Be(vm.Suggestions[0]);
    }

    [Fact]
    public async Task NavigatePrevious_FromFirst_WrapsToLast()
    {
        // Arrange
        var (d1, s1) = CreateDeviationSuggestionPair(startOffset: 0);
        var (d2, s2) = CreateDeviationSuggestionPair(startOffset: 20);

        var vm = CreateAndSetupScanFlow(
            new[] { d1, d2 },
            new[] { s1, s2 });

        await vm.ScanDocumentCommand.ExecuteAsync(null);

        // Selected is first (index 0).
        vm.SelectedSuggestion.Should().Be(vm.Suggestions[0]);

        // Act
        vm.NavigatePreviousCommand.Execute(null);

        // Assert — should wrap to last suggestion.
        vm.SelectedSuggestion.Should().Be(vm.Suggestions[1]);
    }

    #endregion

    #region FilteredSuggestions Tests

    [Fact]
    public async Task FilteredSuggestions_AllFilter_ReturnsAll()
    {
        // Arrange
        var (d1, s1) = CreateDeviationSuggestionPair();
        var (d2, s2) = CreateDeviationSuggestionPair(startOffset: 20);
        var vm = CreateAndSetupScanFlow(
            new[] { d1, d2 },
            new[] { s1, s2 });

        await vm.ScanDocumentCommand.ExecuteAsync(null);
        vm.CurrentFilter = SuggestionFilter.All;

        // Act
        var filtered = vm.FilteredSuggestions.ToList();

        // Assert
        filtered.Should().HaveCount(2);
    }

    [Fact]
    public async Task FilteredSuggestions_PendingFilter_ReturnsOnlyPending()
    {
        // Arrange
        var (d1, s1) = CreateDeviationSuggestionPair();
        var (d2, s2) = CreateDeviationSuggestionPair(startOffset: 20);
        var vm = CreateAndSetupScanFlow(
            new[] { d1, d2 },
            new[] { s1, s2 });

        _mockEditorService.Setup(x => x.GetDocumentText()).Returns("test content more");
        await vm.ScanDocumentCommand.ExecuteAsync(null);
        vm.Suggestions[0].Status = SuggestionStatus.Accepted;

        // Act
        vm.CurrentFilter = SuggestionFilter.Pending;
        var filtered = vm.FilteredSuggestions.ToList();

        // Assert
        filtered.Should().HaveCount(1);
        filtered[0].Status.Should().Be(SuggestionStatus.Pending);
    }

    [Fact]
    public async Task FilteredSuggestions_HighConfidenceFilter_ReturnsHighConfidenceOnly()
    {
        // Arrange
        var (d1, s1) = CreateDeviationSuggestionPair(highConfidence: true);
        var (d2, s2) = CreateDeviationSuggestionPair(startOffset: 20, highConfidence: false);
        var vm = CreateAndSetupScanFlow(
            new[] { d1, d2 },
            new[] { s1, s2 });

        await vm.ScanDocumentCommand.ExecuteAsync(null);

        // Act
        vm.CurrentFilter = SuggestionFilter.HighConfidence;
        var filtered = vm.FilteredSuggestions.ToList();

        // Assert
        filtered.Should().HaveCount(1);
        filtered[0].IsHighConfidence.Should().BeTrue();
    }

    [Fact]
    public async Task FilteredSuggestions_HighPriorityFilter_ReturnsHighAndCritical()
    {
        // Arrange
        var (d1, s1) = CreateDeviationSuggestionPair(priority: DeviationPriority.Critical);
        var (d2, s2) = CreateDeviationSuggestionPair(
            startOffset: 20, priority: DeviationPriority.High);
        var (d3, s3) = CreateDeviationSuggestionPair(
            startOffset: 40, priority: DeviationPriority.Normal);
        var (d4, s4) = CreateDeviationSuggestionPair(
            startOffset: 60, priority: DeviationPriority.Low);

        var vm = CreateAndSetupScanFlow(
            new[] { d1, d2, d3, d4 },
            new[] { s1, s2, s3, s4 });

        await vm.ScanDocumentCommand.ExecuteAsync(null);

        // Act
        vm.CurrentFilter = SuggestionFilter.HighPriority;
        var filtered = vm.FilteredSuggestions.ToList();

        // Assert — should include Critical and High.
        filtered.Should().HaveCount(2);
        filtered.Should().OnlyContain(s =>
            s.Priority == DeviationPriority.Critical ||
            s.Priority == DeviationPriority.High);
    }

    #endregion

    #region Computed Properties Tests

    [Fact]
    public async Task HighConfidenceCount_ReturnsCountOfPendingHighConfidence()
    {
        // Arrange
        var (d1, s1) = CreateDeviationSuggestionPair(highConfidence: true);
        var (d2, s2) = CreateDeviationSuggestionPair(startOffset: 20, highConfidence: true);
        var (d3, s3) = CreateDeviationSuggestionPair(startOffset: 40, highConfidence: false);

        var vm = CreateAndSetupScanFlow(
            new[] { d1, d2, d3 },
            new[] { s1, s2, s3 });

        await vm.ScanDocumentCommand.ExecuteAsync(null);
        vm.Suggestions[0].Status = SuggestionStatus.Accepted;

        // Act & Assert — only one pending high-confidence remains.
        vm.HighConfidenceCount.Should().Be(1);
    }

    [Fact]
    public async Task RemainingCount_ReturnsCountOfPendingSuggestions()
    {
        // Arrange
        var (d1, s1) = CreateDeviationSuggestionPair();
        var (d2, s2) = CreateDeviationSuggestionPair(startOffset: 20);
        var (d3, s3) = CreateDeviationSuggestionPair(startOffset: 40);

        var vm = CreateAndSetupScanFlow(
            new[] { d1, d2, d3 },
            new[] { s1, s2, s3 });

        await vm.ScanDocumentCommand.ExecuteAsync(null);
        vm.Suggestions[0].Status = SuggestionStatus.Accepted;
        vm.Suggestions[1].Status = SuggestionStatus.Rejected;

        // Act & Assert
        vm.RemainingCount.Should().Be(1);
    }

    #endregion

    #region Close Command Tests

    [Fact]
    public void Close_RaisesCloseRequestedEvent()
    {
        // Arrange
        var vm = CreateViewModel();
        var eventRaised = false;
        vm.CloseRequested += (_, _) => eventRaised = true;

        // Act
        vm.CloseCommand.Execute(null);

        // Assert
        eventRaised.Should().BeTrue();
    }

    #endregion

    #region SuggestionCardViewModel Tests

    [Fact]
    public void SuggestionCard_Constructor_NullDeviation_Throws()
    {
        // Act
        var act = () => new SuggestionCardViewModel(null!, CreateTestSuggestion());

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("deviation");
    }

    [Fact]
    public void SuggestionCard_Constructor_NullSuggestion_Throws()
    {
        // Act
        var act = () => new SuggestionCardViewModel(CreateTestDeviation(), null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("suggestion");
    }

    [Fact]
    public void SuggestionCard_ComputedProperties_ReturnCorrectValues()
    {
        // Arrange
        var rule = CreateTestRule("rule-42", "Passive Voice");
        var deviation = CreateTestDeviation(rule: rule, originalText: "was written");
        var suggestion = CreateTestSuggestion(
            deviation.DeviationId, "was written", "wrote",
            confidence: 0.92, qualityScore: 0.88);

        // Act
        var card = new SuggestionCardViewModel(deviation, suggestion);

        // Assert
        card.RuleName.Should().Be("Passive Voice");
        card.OriginalText.Should().Be("was written");
        card.SuggestedText.Should().Be("wrote");
        card.Confidence.Should().Be(0.92);
        card.QualityScore.Should().Be(0.88);
        card.ConfidenceDisplay.Should().Be("92%");
        card.QualityDisplay.Should().Be("88%");
        card.Explanation.Should().Be("Test explanation for the fix");
    }

    [Fact]
    public void SuggestionCard_UpdateSuggestion_UpdatesComputedProperties()
    {
        // Arrange
        var deviation = CreateTestDeviation();
        var originalSuggestion = CreateTestSuggestion(deviation.DeviationId);
        var card = new SuggestionCardViewModel(deviation, originalSuggestion);

        var newSuggestion = CreateTestSuggestion(
            deviation.DeviationId, "test", "better",
            confidence: 0.99, qualityScore: 0.97);

        var changedProperties = new List<string>();
        card.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        // Act
        card.UpdateSuggestion(newSuggestion);

        // Assert
        card.SuggestedText.Should().Be("better");
        card.Confidence.Should().Be(0.99);
        card.ConfidenceDisplay.Should().Be("99%");

        changedProperties.Should().Contain(nameof(SuggestionCardViewModel.Suggestion));
        changedProperties.Should().Contain(nameof(SuggestionCardViewModel.Confidence));
        changedProperties.Should().Contain(nameof(SuggestionCardViewModel.SuggestedText));
    }

    [Fact]
    public void SuggestionCard_UpdateSuggestion_NullThrows()
    {
        // Arrange
        var deviation = CreateTestDeviation();
        var suggestion = CreateTestSuggestion(deviation.DeviationId);
        var card = new SuggestionCardViewModel(deviation, suggestion);

        // Act
        var act = () => card.UpdateSuggestion(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SuggestionCard_ToggleAlternatives_TogglesShowAlternatives()
    {
        // Arrange
        var deviation = CreateTestDeviation();
        var suggestion = CreateTestSuggestion(deviation.DeviationId);
        var card = new SuggestionCardViewModel(deviation, suggestion);
        card.ShowAlternatives.Should().BeFalse();

        // Act
        card.ToggleAlternativesCommand.Execute(null);

        // Assert
        card.ShowAlternatives.Should().BeTrue();

        // Act again
        card.ToggleAlternativesCommand.Execute(null);

        // Assert
        card.ShowAlternatives.Should().BeFalse();
    }

    [Fact]
    public void SuggestionCard_PriorityDisplay_ReturnsCorrectLabels()
    {
        // Arrange & Act
        var criticalCard = new SuggestionCardViewModel(
            CreateTestDeviation(priority: DeviationPriority.Critical),
            CreateTestSuggestion());
        var highCard = new SuggestionCardViewModel(
            CreateTestDeviation(priority: DeviationPriority.High),
            CreateTestSuggestion());
        var normalCard = new SuggestionCardViewModel(
            CreateTestDeviation(priority: DeviationPriority.Normal),
            CreateTestSuggestion());
        var lowCard = new SuggestionCardViewModel(
            CreateTestDeviation(priority: DeviationPriority.Low),
            CreateTestSuggestion());

        // Assert
        criticalCard.PriorityDisplay.Should().Be("Critical");
        highCard.PriorityDisplay.Should().Be("High");
        normalCard.PriorityDisplay.Should().Be("Normal");
        lowCard.PriorityDisplay.Should().Be("Low");
    }

    [Fact]
    public void SuggestionCard_DefaultStatus_IsPending()
    {
        // Arrange & Act
        var card = new SuggestionCardViewModel(CreateTestDeviation(), CreateTestSuggestion());

        // Assert
        card.Status.Should().Be(SuggestionStatus.Pending);
        card.IsExpanded.Should().BeFalse();
        card.IsReviewed.Should().BeFalse();
        card.IsRegenerating.Should().BeFalse();
    }

    #endregion

    #region TuningUndoableOperation Tests

    [Fact]
    public void TuningUndoableOperation_ExecuteAsync_AppliesFixText()
    {
        // Arrange
        var op = new TuningUndoableOperation(
            offset: 5,
            originalText: "bad",
            appliedText: "good",
            ruleId: "rule-001",
            editorService: _mockEditorService.Object);

        // Act
        op.ExecuteAsync();

        // Assert
        _mockEditorService.Verify(x => x.BeginUndoGroup(It.Is<string>(s => s.Contains("rule-001"))), Times.Once);
        _mockEditorService.Verify(x => x.DeleteText(5, 3), Times.Once); // "bad".Length = 3
        _mockEditorService.Verify(x => x.InsertText(5, "good"), Times.Once);
        _mockEditorService.Verify(x => x.EndUndoGroup(), Times.Once);
    }

    [Fact]
    public void TuningUndoableOperation_UndoAsync_RestoresOriginalText()
    {
        // Arrange
        var op = new TuningUndoableOperation(
            offset: 5,
            originalText: "bad",
            appliedText: "good",
            ruleId: "rule-001",
            editorService: _mockEditorService.Object);

        // Act
        op.UndoAsync();

        // Assert
        _mockEditorService.Verify(x => x.DeleteText(5, 4), Times.Once); // "good".Length = 4
        _mockEditorService.Verify(x => x.InsertText(5, "bad"), Times.Once);
    }

    [Fact]
    public void TuningUndoableOperation_RedoAsync_ReappliesFixText()
    {
        // Arrange
        var op = new TuningUndoableOperation(
            offset: 5,
            originalText: "bad",
            appliedText: "good",
            ruleId: "rule-001",
            editorService: _mockEditorService.Object);

        // Act
        op.RedoAsync();

        // Assert
        _mockEditorService.Verify(x => x.DeleteText(5, 3), Times.Once); // "bad".Length = 3
        _mockEditorService.Verify(x => x.InsertText(5, "good"), Times.Once);
    }

    [Fact]
    public void TuningUndoableOperation_Properties_AreSetCorrectly()
    {
        // Arrange & Act
        var op = new TuningUndoableOperation(
            offset: 10,
            originalText: "original",
            appliedText: "replacement",
            ruleId: "test-rule",
            editorService: _mockEditorService.Object);

        // Assert
        op.DisplayName.Should().Be("Tuning Fix (test-rule)");
        op.Id.Should().NotBeNullOrEmpty();
        op.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void TuningUndoableOperation_NullOriginalText_Throws()
    {
        // Act
        var act = () => new TuningUndoableOperation(
            0, null!, "applied", "rule", _mockEditorService.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TuningUndoableOperation_NullAppliedText_Throws()
    {
        // Act
        var act = () => new TuningUndoableOperation(
            0, "original", null!, "rule", _mockEditorService.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TuningUndoableOperation_NullEditorService_Throws()
    {
        // Act
        var act = () => new TuningUndoableOperation(
            0, "original", "applied", "rule", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_DisposesCleanly()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        var act = () => vm.Dispose();

        // Assert
        act.Should().NotThrow();
        vm.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public void Dispose_DoubleDispose_DoesNotThrow()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.Dispose();
        var act = () => vm.Dispose();

        // Assert — second dispose should be idempotent.
        act.Should().NotThrow();
    }

    #endregion
}
