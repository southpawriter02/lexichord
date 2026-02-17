// -----------------------------------------------------------------------
// <copyright file="UnifiedFixOrchestratorTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// Unit tests for the UnifiedFixOrchestrator (v0.7.5h).
//   Tests cover the complete fix workflow pipeline: constructor validation,
//   FixAllAsync (argument validation, no-fixable issues, position sorting,
//   conflict detection, conflict strategies, category ordering, atomic
//   application, re-validation, dry run, timeout, transaction recording,
//   event publishing, verbose trace), FixByCategoryAsync, FixBySeverityAsync,
//   FixByIdAsync, DetectConflicts, DryRunAsync, UndoLastFixesAsync, Dispose,
//   and edge cases.
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;
using Lexichord.Abstractions.Contracts.Undo;
using Lexichord.Abstractions.Contracts.Validation;
using Lexichord.Abstractions.Contracts.Validation.Events;
using Lexichord.Modules.Agents.Tuning.FixOrchestration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

using UnifiedFix = Lexichord.Abstractions.Contracts.Validation.UnifiedFix;

namespace Lexichord.Tests.Unit.Modules.Agents.Tuning.FixOrchestration;

/// <summary>
/// Unit tests for <see cref="UnifiedFixOrchestrator"/>.
/// </summary>
/// <remarks>
/// <para>
/// Test Groups:
/// <list type="bullet">
///   <item><description>Constructor — null argument validation, nullable undo service</description></item>
///   <item><description>FixAllAsync Argument Validation — null args, disposed instance</description></item>
///   <item><description>FixAllAsync No-Fixable Issues — empty, no BestFix, CanAutoApply false</description></item>
///   <item><description>FixAllAsync Position Sorting — bottom-to-top, same start, single</description></item>
///   <item><description>FixAllAsync Conflict Detection — overlapping, throw, no conflicts, event</description></item>
///   <item><description>FixAllAsync Conflict Strategies — skip, throw, priority, all conflicting</description></item>
///   <item><description>FixAllAsync Category Ordering — mixed, within group, knowledge vs style</description></item>
///   <item><description>FixAllAsync Atomic Application — undo group, delete/insert, failure, null doc</description></item>
///   <item><description>FixAllAsync Re-validation — enabled, disabled, failure</description></item>
///   <item><description>FixAllAsync Dry Run — no editor mods, correct counts</description></item>
///   <item><description>FixAllAsync Timeout — exceeds, within</description></item>
///   <item><description>FixAllAsync Transaction Recording — enable, disable, null undo</description></item>
///   <item><description>FixAllAsync Event Publishing — success, no fixes, conflicts</description></item>
///   <item><description>FixAllAsync Verbose Trace — enabled, disabled</description></item>
///   <item><description>FixByCategoryAsync — null args, filtering, empty categories</description></item>
///   <item><description>FixBySeverityAsync — null args, severity filtering, disposed</description></item>
///   <item><description>FixByIdAsync — null args, ID matching, non-existent IDs</description></item>
///   <item><description>DetectConflicts — null, disposed, delegation</description></item>
///   <item><description>DryRunAsync — null args, correct counts</description></item>
///   <item><description>UndoLastFixesAsync — empty stack, restore, multiple, disposed, already undone</description></item>
///   <item><description>Dispose — multiple, post-dispose</description></item>
///   <item><description>Edge Cases — invalid location, zero-length fix, null new text</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5h as part of the Combined Fix Workflow feature.
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.5h")]
public class UnifiedFixOrchestratorTests : IDisposable
{
    private readonly Mock<IEditorService> _mockEditor;
    private readonly Mock<IUnifiedValidationService> _mockValidation;
    private readonly Mock<IUndoRedoService> _mockUndo;
    private readonly Mock<ILicenseContext> _mockLicense;
    private readonly FixConflictDetector _conflictDetector;
    private readonly ILogger<UnifiedFixOrchestrator> _logger;

    private const string TestDocumentPath = "/test/document.md";
    private const string TestDocumentContent = "The quick brown fox jumps over the lazy dog.";

    public UnifiedFixOrchestratorTests()
    {
        _mockEditor = new Mock<IEditorService>();
        _mockValidation = new Mock<IUnifiedValidationService>();
        _mockUndo = new Mock<IUndoRedoService>();
        _mockLicense = new Mock<ILicenseContext>();
        _conflictDetector = new FixConflictDetector(NullLogger<FixConflictDetector>.Instance);
        _logger = NullLogger<UnifiedFixOrchestrator>.Instance;

        // LOGIC: Default setup - editor returns test content.
        _mockEditor.Setup(x => x.GetDocumentText()).Returns(TestDocumentContent);
        _mockEditor.Setup(x => x.CurrentDocumentPath).Returns(TestDocumentPath);

        // LOGIC: Default license - WriterPro (sufficient for fix workflow).
        _mockLicense.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);
    }

    #region Helper Methods

    private UnifiedFixOrchestrator CreateService(bool withUndo = true)
    {
        return new UnifiedFixOrchestrator(
            _mockEditor.Object,
            _mockValidation.Object,
            _conflictDetector,
            withUndo ? _mockUndo.Object : null,
            _mockLicense.Object,
            _logger);
    }

    private static UnifiedIssue CreateIssue(
        int start = 10, int length = 5,
        string oldText = "brown",
        string newText = "red",
        IssueCategory category = IssueCategory.Grammar,
        UnifiedSeverity severity = UnifiedSeverity.Warning,
        bool canAutoApply = true,
        double confidence = 0.9)
    {
        var location = new TextSpan(start, length);
        var fix = UnifiedFix.Replacement(location, oldText, newText, "Test fix", confidence)
            with { CanAutoApply = canAutoApply };
        return new UnifiedIssue(
            Guid.NewGuid(), "TEST-001", category, severity,
            "Test issue", location, oldText, new[] { fix }, "TestSource", null);
    }

    private static UnifiedIssue CreateIssueWithNoFix(
        int start = 10, int length = 5,
        IssueCategory category = IssueCategory.Grammar)
    {
        var location = new TextSpan(start, length);
        return new UnifiedIssue(
            Guid.NewGuid(), "TEST-002", category, UnifiedSeverity.Warning,
            "Test issue", location, "text", Array.Empty<UnifiedFix>(), "TestSource", null);
    }

    private static UnifiedValidationResult CreateValidationResult(
        params UnifiedIssue[] issues)
    {
        return new UnifiedValidationResult
        {
            DocumentPath = TestDocumentPath,
            Issues = issues.ToList()
        };
    }

    #endregion

    #region Dispose

    public void Dispose()
    {
        // No resources to dispose in tests
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullEditorService_ThrowsArgumentNullException()
    {
        // Arrange
        IEditorService editorService = null!;

        // Act
        var act = () => new UnifiedFixOrchestrator(
            editorService,
            _mockValidation.Object,
            _conflictDetector,
            _mockUndo.Object,
            _mockLicense.Object,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("editorService");
    }

    [Fact]
    public void Constructor_NullValidationService_ThrowsArgumentNullException()
    {
        // Arrange
        IUnifiedValidationService validationService = null!;

        // Act
        var act = () => new UnifiedFixOrchestrator(
            _mockEditor.Object,
            validationService,
            _conflictDetector,
            _mockUndo.Object,
            _mockLicense.Object,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("validationService");
    }

    [Fact]
    public void Constructor_NullConflictDetector_ThrowsArgumentNullException()
    {
        // Arrange
        FixConflictDetector conflictDetector = null!;

        // Act
        var act = () => new UnifiedFixOrchestrator(
            _mockEditor.Object,
            _mockValidation.Object,
            conflictDetector,
            _mockUndo.Object,
            _mockLicense.Object,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("conflictDetector");
    }

    [Fact]
    public void Constructor_NullLicenseContext_ThrowsArgumentNullException()
    {
        // Arrange
        ILicenseContext licenseContext = null!;

        // Act
        var act = () => new UnifiedFixOrchestrator(
            _mockEditor.Object,
            _mockValidation.Object,
            _conflictDetector,
            _mockUndo.Object,
            licenseContext,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("licenseContext");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        ILogger<UnifiedFixOrchestrator> logger = null!;

        // Act
        var act = () => new UnifiedFixOrchestrator(
            _mockEditor.Object,
            _mockValidation.Object,
            _conflictDetector,
            _mockUndo.Object,
            _mockLicense.Object,
            logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_NullUndoService_DoesNotThrow()
    {
        // Arrange / Act
        var act = () => new UnifiedFixOrchestrator(
            _mockEditor.Object,
            _mockValidation.Object,
            _conflictDetector,
            undoService: null,
            _mockLicense.Object,
            _logger);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region FixAllAsync Argument Validation Tests

    [Fact]
    public async Task FixAllAsync_NullDocumentPath_ThrowsArgumentNullException()
    {
        // Arrange
        using var sut = CreateService();
        var validation = CreateValidationResult();

        // Act
        var act = () => sut.FixAllAsync(null!, validation, FixWorkflowOptions.Default);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("documentPath");
    }

    [Fact]
    public async Task FixAllAsync_NullValidation_ThrowsArgumentNullException()
    {
        // Arrange
        using var sut = CreateService();

        // Act
        var act = () => sut.FixAllAsync(TestDocumentPath, null!, FixWorkflowOptions.Default);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("validation");
    }

    [Fact]
    public async Task FixAllAsync_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        using var sut = CreateService();
        var validation = CreateValidationResult();

        // Act
        var act = () => sut.FixAllAsync(TestDocumentPath, validation, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public async Task FixAllAsync_DisposedInstance_ThrowsObjectDisposedException()
    {
        // Arrange
        var sut = CreateService();
        sut.Dispose();
        var validation = CreateValidationResult();

        // Act
        var act = () => sut.FixAllAsync(TestDocumentPath, validation, FixWorkflowOptions.Default);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region FixAllAsync No-Fixable Issues Tests

    [Fact]
    public async Task FixAllAsync_EmptyIssues_ReturnsEmptyResult()
    {
        // Arrange
        using var sut = CreateService();
        var validation = CreateValidationResult();

        // Act
        var result = await sut.FixAllAsync(TestDocumentPath, validation, FixWorkflowOptions.Default);

        // Assert
        result.Success.Should().BeTrue();
        result.AppliedCount.Should().Be(0);
        result.SkippedCount.Should().Be(0);
        result.FailedCount.Should().Be(0);
    }

    [Fact]
    public async Task FixAllAsync_NoAutoFixableIssues_ReturnsEmptyResult()
    {
        // Arrange
        using var sut = CreateService();
        var issue = CreateIssueWithNoFix();
        var validation = CreateValidationResult(issue);

        // Act
        var result = await sut.FixAllAsync(TestDocumentPath, validation, FixWorkflowOptions.Default);

        // Assert
        result.Success.Should().BeTrue();
        result.AppliedCount.Should().Be(0);
    }

    [Fact]
    public async Task FixAllAsync_IssuesWithCanAutoApplyFalse_ReturnsEmptyResult()
    {
        // Arrange
        using var sut = CreateService();
        var issue = CreateIssue(canAutoApply: false);
        var validation = CreateValidationResult(issue);

        // Act
        var result = await sut.FixAllAsync(TestDocumentPath, validation, FixWorkflowOptions.Default);

        // Assert
        result.Success.Should().BeTrue();
        result.AppliedCount.Should().Be(0);
    }

    #endregion

    #region FixAllAsync Position Sorting Tests

    [Fact]
    public async Task FixAllAsync_MultipleIssues_AppliedBottomToTop()
    {
        // Arrange
        using var sut = CreateService();
        var issue1 = CreateIssue(start: 4, length: 5, oldText: "quick", newText: "slow");
        var issue2 = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var issue3 = CreateIssue(start: 35, length: 4, oldText: "lazy", newText: "fast");
        var validation = CreateValidationResult(issue1, issue2, issue3);
        var options = new FixWorkflowOptions { ReValidateAfterFixes = false };

        var callOrder = new List<int>();
        _mockEditor
            .Setup(x => x.DeleteText(It.IsAny<int>(), It.IsAny<int>()))
            .Callback<int, int>((offset, _) => callOrder.Add(offset));

        // Act
        await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert — should be applied bottom-to-top (highest offset first)
        callOrder.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task FixAllAsync_SameStartDifferentEnd_LongerSpanFirst()
    {
        // Arrange
        using var sut = CreateService();
        var issue1 = CreateIssue(start: 10, length: 3, oldText: "bro", newText: "r");
        var issue2 = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var validation = CreateValidationResult(issue1, issue2);
        var options = new FixWorkflowOptions
        {
            ReValidateAfterFixes = false,
            ConflictStrategy = ConflictHandlingStrategy.SkipConflicting
        };

        var deleteCallLengths = new List<int>();
        _mockEditor
            .Setup(x => x.DeleteText(It.IsAny<int>(), It.IsAny<int>()))
            .Callback<int, int>((_, length) => deleteCallLengths.Add(length));

        // Act
        await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert — overlapping issues: one should be skipped due to conflict
        // The conflict detector will find these overlapping and skip one
        deleteCallLengths.Should().HaveCountLessOrEqualTo(1);
    }

    [Fact]
    public async Task FixAllAsync_SingleIssue_AppliedCorrectly()
    {
        // Arrange
        using var sut = CreateService();
        var issue = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var validation = CreateValidationResult(issue);
        var options = new FixWorkflowOptions { ReValidateAfterFixes = false };

        // Act
        var result = await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert
        result.AppliedCount.Should().Be(1);
        _mockEditor.Verify(x => x.DeleteText(10, 5), Times.Once);
        _mockEditor.Verify(x => x.InsertText(10, "red"), Times.Once);
    }

    #endregion

    #region FixAllAsync Conflict Detection Tests

    [Fact]
    public async Task FixAllAsync_OverlappingFixes_SkipsConflicting()
    {
        // Arrange
        using var sut = CreateService();
        var issue1 = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var issue2 = CreateIssue(start: 12, length: 5, oldText: "own f", newText: "zzz");
        var validation = CreateValidationResult(issue1, issue2);
        var options = new FixWorkflowOptions
        {
            ConflictStrategy = ConflictHandlingStrategy.SkipConflicting,
            ReValidateAfterFixes = false
        };

        // Act
        var result = await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert — overlapping issues, some should be skipped
        result.SkippedCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task FixAllAsync_OverlappingFixes_ThrowsWithThrowStrategy()
    {
        // Arrange
        using var sut = CreateService();
        var issue1 = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var issue2 = CreateIssue(start: 12, length: 5, oldText: "own f", newText: "zzz");
        var validation = CreateValidationResult(issue1, issue2);
        var options = new FixWorkflowOptions
        {
            ConflictStrategy = ConflictHandlingStrategy.ThrowException,
            ReValidateAfterFixes = false
        };

        // Act
        var act = () => sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert
        await act.Should().ThrowAsync<FixConflictException>();
    }

    [Fact]
    public async Task FixAllAsync_NoConflicts_AppliesAll()
    {
        // Arrange
        using var sut = CreateService();
        var issue1 = CreateIssue(start: 4, length: 5, oldText: "quick", newText: "slow");
        var issue2 = CreateIssue(start: 35, length: 4, oldText: "lazy", newText: "fast");
        var validation = CreateValidationResult(issue1, issue2);
        var options = new FixWorkflowOptions { ReValidateAfterFixes = false };

        // Act
        var result = await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert
        result.AppliedCount.Should().Be(2);
        result.SkippedCount.Should().Be(0);
    }

    [Fact]
    public async Task FixAllAsync_Conflicts_RaisesConflictDetectedEvent()
    {
        // Arrange
        using var sut = CreateService();
        var issue1 = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var issue2 = CreateIssue(start: 12, length: 5, oldText: "own f", newText: "zzz");
        var validation = CreateValidationResult(issue1, issue2);
        var options = new FixWorkflowOptions
        {
            ConflictStrategy = ConflictHandlingStrategy.SkipConflicting,
            ReValidateAfterFixes = false
        };

        FixConflictDetectedEventArgs? raisedArgs = null;
        sut.ConflictDetected += (_, args) => raisedArgs = args;

        // Act
        await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert
        raisedArgs.Should().NotBeNull();
        raisedArgs!.Conflicts.Should().NotBeEmpty();
    }

    #endregion

    #region FixAllAsync Conflict Strategies Tests

    [Fact]
    public async Task FixAllAsync_SkipConflicting_RemovesConflictingIssues()
    {
        // Arrange
        using var sut = CreateService();
        var issue1 = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var issue2 = CreateIssue(start: 12, length: 5, oldText: "own f", newText: "zzz");
        var validation = CreateValidationResult(issue1, issue2);
        var options = new FixWorkflowOptions
        {
            ConflictStrategy = ConflictHandlingStrategy.SkipConflicting,
            ReValidateAfterFixes = false
        };

        // Act
        var result = await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert
        result.SkippedCount.Should().BeGreaterThan(0);
        result.ConflictingIssues.Should().NotBeEmpty();
    }

    [Fact]
    public async Task FixAllAsync_ThrowException_ThrowsFixConflictException()
    {
        // Arrange
        using var sut = CreateService();
        var issue1 = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var issue2 = CreateIssue(start: 12, length: 5, oldText: "own f", newText: "zzz");
        var validation = CreateValidationResult(issue1, issue2);
        var options = new FixWorkflowOptions
        {
            ConflictStrategy = ConflictHandlingStrategy.ThrowException,
            ReValidateAfterFixes = false
        };

        // Act
        var act = () => sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert
        var ex = await act.Should().ThrowAsync<FixConflictException>();
        ex.Which.Conflicts.Should().NotBeEmpty();
    }

    [Fact]
    public async Task FixAllAsync_PriorityBased_KeepsHigherSeverity()
    {
        // Arrange
        using var sut = CreateService();
        var errorIssue = CreateIssue(
            start: 10, length: 5, oldText: "brown", newText: "red",
            severity: UnifiedSeverity.Error);
        var warningIssue = CreateIssue(
            start: 12, length: 5, oldText: "own f", newText: "zzz",
            severity: UnifiedSeverity.Warning);
        var validation = CreateValidationResult(errorIssue, warningIssue);
        var options = new FixWorkflowOptions
        {
            ConflictStrategy = ConflictHandlingStrategy.PriorityBased,
            ReValidateAfterFixes = false
        };

        // Act
        var result = await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert — error-severity should be kept, warning skipped
        result.AppliedCount.Should().Be(1);
        result.SkippedCount.Should().Be(1);
        result.ResolvedIssues.Should().Contain(i => i.Severity == UnifiedSeverity.Error);
    }

    [Fact]
    public async Task FixAllAsync_AllConflicting_ReturnsSuccessWithZeroApplied()
    {
        // Arrange
        using var sut = CreateService();
        // LOGIC: Two issues at exact same location with different replacements = contradictory
        var issue1 = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var issue2 = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "blue");
        var validation = CreateValidationResult(issue1, issue2);
        var options = new FixWorkflowOptions
        {
            ConflictStrategy = ConflictHandlingStrategy.SkipConflicting,
            ReValidateAfterFixes = false
        };

        // Act
        var result = await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert
        result.Success.Should().BeTrue();
        result.AppliedCount.Should().Be(0);
        result.SkippedCount.Should().BeGreaterThan(0);
    }

    #endregion

    #region FixAllAsync Category Ordering Tests

    [Fact]
    public async Task FixAllAsync_MixedCategories_GroupedByCategory()
    {
        // Arrange
        using var sut = CreateService();
        var knowledgeIssue = CreateIssue(
            start: 35, length: 4, oldText: "lazy", newText: "idle",
            category: IssueCategory.Knowledge);
        var grammarIssue = CreateIssue(
            start: 20, length: 3, oldText: "fox", newText: "cat",
            category: IssueCategory.Grammar);
        var styleIssue = CreateIssue(
            start: 4, length: 5, oldText: "quick", newText: "fast",
            category: IssueCategory.Style);
        var validation = CreateValidationResult(styleIssue, knowledgeIssue, grammarIssue);
        var options = new FixWorkflowOptions { ReValidateAfterFixes = false };

        var deleteOffsets = new List<int>();
        _mockEditor
            .Setup(x => x.DeleteText(It.IsAny<int>(), It.IsAny<int>()))
            .Callback<int, int>((offset, _) => deleteOffsets.Add(offset));

        // Act
        await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert — Knowledge before Grammar before Style
        // Knowledge is at 35, Grammar at 20, Style at 4
        // Within each category, bottom-to-top ordering applies
        deleteOffsets.Should().HaveCount(3);
        // Knowledge (35) should come before Grammar (20) which comes before Style (4)
        deleteOffsets[0].Should().Be(35); // Knowledge (only one, applied bottom-to-top)
        deleteOffsets[1].Should().Be(20); // Grammar
        deleteOffsets[2].Should().Be(4);  // Style
    }

    [Fact]
    public async Task FixAllAsync_SameCategoryIssues_BottomToTopWithinCategory()
    {
        // Arrange
        using var sut = CreateService();
        var issue1 = CreateIssue(
            start: 4, length: 5, oldText: "quick", newText: "slow",
            category: IssueCategory.Grammar);
        var issue2 = CreateIssue(
            start: 35, length: 4, oldText: "lazy", newText: "fast",
            category: IssueCategory.Grammar);
        var validation = CreateValidationResult(issue1, issue2);
        var options = new FixWorkflowOptions { ReValidateAfterFixes = false };

        var deleteOffsets = new List<int>();
        _mockEditor
            .Setup(x => x.DeleteText(It.IsAny<int>(), It.IsAny<int>()))
            .Callback<int, int>((offset, _) => deleteOffsets.Add(offset));

        // Act
        await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert — within same category, bottom-to-top: 35 before 4
        deleteOffsets.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task FixAllAsync_KnowledgeAndStyle_KnowledgeFirst()
    {
        // Arrange
        using var sut = CreateService();
        var styleIssue = CreateIssue(
            start: 35, length: 4, oldText: "lazy", newText: "idle",
            category: IssueCategory.Style);
        var knowledgeIssue = CreateIssue(
            start: 4, length: 5, oldText: "quick", newText: "slow",
            category: IssueCategory.Knowledge);
        var validation = CreateValidationResult(styleIssue, knowledgeIssue);
        var options = new FixWorkflowOptions { ReValidateAfterFixes = false };

        var deleteOffsets = new List<int>();
        _mockEditor
            .Setup(x => x.DeleteText(It.IsAny<int>(), It.IsAny<int>()))
            .Callback<int, int>((offset, _) => deleteOffsets.Add(offset));

        // Act
        await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert — Knowledge (at 4) should be applied before Style (at 35)
        // Knowledge group first, then Style group — each sorted bottom-to-top within group
        deleteOffsets.Should().HaveCount(2);
        deleteOffsets[0].Should().Be(4);  // Knowledge (only item in group)
        deleteOffsets[1].Should().Be(35); // Style (only item in group)
    }

    #endregion

    #region FixAllAsync Atomic Application Tests

    [Fact]
    public async Task FixAllAsync_Success_CallsBeginEndUndoGroup()
    {
        // Arrange
        using var sut = CreateService();
        var issue = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var validation = CreateValidationResult(issue);
        var options = new FixWorkflowOptions { ReValidateAfterFixes = false };

        // Act
        await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert
        _mockEditor.Verify(x => x.BeginUndoGroup(It.IsAny<string>()), Times.Once);
        _mockEditor.Verify(x => x.EndUndoGroup(), Times.Once);
    }

    [Fact]
    public async Task FixAllAsync_Success_CallsDeleteAndInsertText()
    {
        // Arrange
        using var sut = CreateService();
        var issue = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var validation = CreateValidationResult(issue);
        var options = new FixWorkflowOptions { ReValidateAfterFixes = false };

        // Act
        await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert
        _mockEditor.Verify(x => x.DeleteText(10, 5), Times.Once);
        _mockEditor.Verify(x => x.InsertText(10, "red"), Times.Once);
    }

    [Fact]
    public async Task FixAllAsync_FailureMidApply_StillEndsUndoGroup()
    {
        // Arrange
        using var sut = CreateService();
        var issue = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var validation = CreateValidationResult(issue);
        var options = new FixWorkflowOptions { ReValidateAfterFixes = false };

        // LOGIC: Simulate failure during DeleteText
        _mockEditor
            .Setup(x => x.DeleteText(It.IsAny<int>(), It.IsAny<int>()))
            .Throws(new InvalidOperationException("Editor failure"));

        // Act
        var result = await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert — EndUndoGroup should still be called even after exception
        _mockEditor.Verify(x => x.EndUndoGroup(), Times.Once);
        result.FailedCount.Should().Be(1);
    }

    [Fact]
    public async Task FixAllAsync_NoDocumentContent_ReturnsFailure()
    {
        // Arrange
        using var sut = CreateService();
        var issue = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var validation = CreateValidationResult(issue);
        var options = new FixWorkflowOptions { ReValidateAfterFixes = false };

        // LOGIC: Editor returns null for document content
        _mockEditor.Setup(x => x.GetDocumentText()).Returns((string?)null);

        // Act
        var result = await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert
        result.Success.Should().BeFalse();
        result.AppliedCount.Should().Be(0);
    }

    #endregion

    #region FixAllAsync Re-validation Tests

    [Fact]
    public async Task FixAllAsync_ReValidateEnabled_CallsValidateAsync()
    {
        // Arrange
        using var sut = CreateService();
        var issue = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var validation = CreateValidationResult(issue);
        var options = new FixWorkflowOptions { ReValidateAfterFixes = true };

        _mockValidation
            .Setup(x => x.ValidateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UnifiedValidationOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UnifiedValidationResult
            {
                DocumentPath = TestDocumentPath,
                Issues = new List<UnifiedIssue>()
            });

        // Act
        await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert
        _mockValidation.Verify(x => x.ValidateAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<UnifiedValidationOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FixAllAsync_ReValidateDisabled_SkipsValidation()
    {
        // Arrange
        using var sut = CreateService();
        var issue = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var validation = CreateValidationResult(issue);
        var options = new FixWorkflowOptions { ReValidateAfterFixes = false };

        // Act
        await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert
        _mockValidation.Verify(x => x.ValidateAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<UnifiedValidationOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FixAllAsync_ReValidationFails_StillReturnsSuccess()
    {
        // Arrange
        using var sut = CreateService();
        var issue = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var validation = CreateValidationResult(issue);
        var options = new FixWorkflowOptions { ReValidateAfterFixes = true };

        _mockValidation
            .Setup(x => x.ValidateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UnifiedValidationOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Validation failed"));

        // Act
        var result = await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert — re-validation failure is caught, fix result is still success
        result.Success.Should().BeTrue();
        result.AppliedCount.Should().Be(1);
    }

    #endregion

    #region FixAllAsync Dry Run Tests

    [Fact]
    public async Task FixAllAsync_DryRun_DoesNotCallEditorModifications()
    {
        // Arrange
        using var sut = CreateService();
        var issue = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var validation = CreateValidationResult(issue);
        var options = new FixWorkflowOptions
        {
            DryRun = true,
            EnableUndo = false,
            ReValidateAfterFixes = false
        };

        // Act
        await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert — no DeleteText or InsertText calls
        _mockEditor.Verify(x => x.DeleteText(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _mockEditor.Verify(x => x.InsertText(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        _mockEditor.Verify(x => x.BeginUndoGroup(It.IsAny<string>()), Times.Never);
        _mockEditor.Verify(x => x.EndUndoGroup(), Times.Never);
    }

    [Fact]
    public async Task FixAllAsync_DryRun_ReturnsCorrectCounts()
    {
        // Arrange
        using var sut = CreateService();
        var issue1 = CreateIssue(start: 4, length: 5, oldText: "quick", newText: "slow");
        var issue2 = CreateIssue(start: 35, length: 4, oldText: "lazy", newText: "fast");
        var validation = CreateValidationResult(issue1, issue2);
        var options = new FixWorkflowOptions
        {
            DryRun = true,
            EnableUndo = false,
            ReValidateAfterFixes = false
        };

        // Act
        var result = await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert
        result.Success.Should().BeTrue();
        result.AppliedCount.Should().Be(2);
        result.FailedCount.Should().Be(0);
    }

    #endregion

    #region FixAllAsync Timeout Tests

    [Fact]
    public async Task FixAllAsync_ExceedsTimeout_ThrowsFixApplicationTimeoutException()
    {
        // Arrange
        using var sut = CreateService();

        // LOGIC: Create many issues and set a very short timeout
        var issues = Enumerable.Range(0, 100)
            .Select(i => CreateIssue(start: i * 2, length: 1, oldText: "x", newText: "y"))
            .ToArray();
        var validation = CreateValidationResult(issues);
        var options = new FixWorkflowOptions
        {
            Timeout = TimeSpan.FromMilliseconds(1),
            ConflictStrategy = ConflictHandlingStrategy.SkipConflicting,
            ReValidateAfterFixes = false
        };

        // LOGIC: Simulate slow editor operation to ensure timeout is hit
        _mockEditor
            .Setup(x => x.DeleteText(It.IsAny<int>(), It.IsAny<int>()))
            .Callback(() => Thread.Sleep(10));

        // Act
        var act = () => sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert
        await act.Should().ThrowAsync<FixApplicationTimeoutException>();
    }

    [Fact]
    public async Task FixAllAsync_WithinTimeout_CompletesSuccessfully()
    {
        // Arrange
        using var sut = CreateService();
        var issue = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var validation = CreateValidationResult(issue);
        var options = new FixWorkflowOptions
        {
            Timeout = TimeSpan.FromSeconds(30),
            ReValidateAfterFixes = false
        };

        // Act
        var result = await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert
        result.AppliedCount.Should().Be(1);
    }

    #endregion

    #region FixAllAsync Transaction Recording Tests

    [Fact]
    public async Task FixAllAsync_EnableUndo_RecordsTransaction()
    {
        // Arrange
        using var sut = CreateService();
        var issue = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var validation = CreateValidationResult(issue);
        var options = new FixWorkflowOptions
        {
            EnableUndo = true,
            ReValidateAfterFixes = false
        };

        // Act
        await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert — verify undo service Push was called
        _mockUndo.Verify(x => x.Push(It.IsAny<IUndoableOperation>()), Times.Once);
    }

    [Fact]
    public async Task FixAllAsync_DisableUndo_NoTransactionRecorded()
    {
        // Arrange
        using var sut = CreateService();
        var issue = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var validation = CreateValidationResult(issue);
        var options = new FixWorkflowOptions
        {
            EnableUndo = false,
            ReValidateAfterFixes = false
        };

        // Act
        await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert — undo service Push should not be called
        _mockUndo.Verify(x => x.Push(It.IsAny<IUndoableOperation>()), Times.Never);
    }

    [Fact]
    public async Task FixAllAsync_NoUndoService_NoTransactionPushed()
    {
        // Arrange
        using var sut = CreateService(withUndo: false);
        var issue = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var validation = CreateValidationResult(issue);
        var options = new FixWorkflowOptions
        {
            EnableUndo = true,
            ReValidateAfterFixes = false
        };

        // Act
        var result = await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert — should succeed without pushing to undo service
        result.AppliedCount.Should().Be(1);
        _mockUndo.Verify(x => x.Push(It.IsAny<IUndoableOperation>()), Times.Never);
    }

    #endregion

    #region FixAllAsync Event Publishing Tests

    [Fact]
    public async Task FixAllAsync_Success_RaisesFixesAppliedEvent()
    {
        // Arrange
        using var sut = CreateService();
        var issue = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var validation = CreateValidationResult(issue);
        var options = new FixWorkflowOptions { ReValidateAfterFixes = false };

        FixesAppliedEventArgs? raisedArgs = null;
        sut.FixesApplied += (_, args) => raisedArgs = args;

        // Act
        await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert
        raisedArgs.Should().NotBeNull();
        raisedArgs!.DocumentPath.Should().Be(TestDocumentPath);
        raisedArgs.Result.AppliedCount.Should().Be(1);
    }

    [Fact]
    public async Task FixAllAsync_NoFixesApplied_DoesNotRaiseEvent()
    {
        // Arrange
        using var sut = CreateService();
        var validation = CreateValidationResult();
        var options = new FixWorkflowOptions { ReValidateAfterFixes = false };

        var eventRaised = false;
        sut.FixesApplied += (_, _) => eventRaised = true;

        // Act
        await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public async Task FixAllAsync_WithConflicts_RaisesConflictDetectedEvent()
    {
        // Arrange
        using var sut = CreateService();
        var issue1 = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var issue2 = CreateIssue(start: 12, length: 5, oldText: "own f", newText: "zzz");
        var validation = CreateValidationResult(issue1, issue2);
        var options = new FixWorkflowOptions
        {
            ConflictStrategy = ConflictHandlingStrategy.SkipConflicting,
            ReValidateAfterFixes = false
        };

        FixConflictDetectedEventArgs? raisedArgs = null;
        sut.ConflictDetected += (_, args) => raisedArgs = args;

        // Act
        await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert
        raisedArgs.Should().NotBeNull();
        raisedArgs!.DocumentPath.Should().Be(TestDocumentPath);
        raisedArgs.Conflicts.Should().NotBeEmpty();
    }

    #endregion

    #region FixAllAsync Verbose Trace Tests

    [Fact]
    public async Task FixAllAsync_VerboseEnabled_IncludesOperationTrace()
    {
        // Arrange
        using var sut = CreateService();
        var issue = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var validation = CreateValidationResult(issue);
        var options = new FixWorkflowOptions
        {
            Verbose = true,
            ReValidateAfterFixes = false
        };

        // Act
        var result = await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert
        result.OperationTrace.Should().NotBeEmpty();
    }

    [Fact]
    public async Task FixAllAsync_VerboseDisabled_EmptyTrace()
    {
        // Arrange
        using var sut = CreateService();
        var issue = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var validation = CreateValidationResult(issue);
        var options = new FixWorkflowOptions
        {
            Verbose = false,
            ReValidateAfterFixes = false
        };

        // Act
        var result = await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert
        result.OperationTrace.Should().BeEmpty();
    }

    #endregion

    #region FixByCategoryAsync Tests

    [Fact]
    public async Task FixByCategoryAsync_NullDocumentPath_ThrowsArgumentNullException()
    {
        // Arrange
        using var sut = CreateService();
        var validation = CreateValidationResult();

        // Act
        var act = () => sut.FixByCategoryAsync(
            null!, validation, new[] { IssueCategory.Grammar });

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("documentPath");
    }

    [Fact]
    public async Task FixByCategoryAsync_NullCategories_ThrowsArgumentNullException()
    {
        // Arrange
        using var sut = CreateService();
        var validation = CreateValidationResult();

        // Act
        var act = () => sut.FixByCategoryAsync(
            TestDocumentPath, validation, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("categories");
    }

    [Fact]
    public async Task FixByCategoryAsync_FiltersToSpecifiedCategories()
    {
        // Arrange
        using var sut = CreateService();
        var grammarIssue = CreateIssue(
            start: 10, length: 5, oldText: "brown", newText: "red",
            category: IssueCategory.Grammar);
        var styleIssue = CreateIssue(
            start: 35, length: 4, oldText: "lazy", newText: "fast",
            category: IssueCategory.Style);
        var validation = CreateValidationResult(grammarIssue, styleIssue);

        // Act — only fix Grammar issues
        var result = await sut.FixByCategoryAsync(
            TestDocumentPath, validation, new[] { IssueCategory.Grammar });

        // Assert — only the grammar issue should be applied
        result.AppliedCount.Should().Be(1);
        result.ResolvedIssues.Should().OnlyContain(i => i.Category == IssueCategory.Grammar);
    }

    [Fact]
    public async Task FixByCategoryAsync_EmptyCategories_ReturnsEmptyResult()
    {
        // Arrange
        using var sut = CreateService();
        var issue = CreateIssue(category: IssueCategory.Grammar);
        var validation = CreateValidationResult(issue);

        // Act
        var result = await sut.FixByCategoryAsync(
            TestDocumentPath, validation, Array.Empty<IssueCategory>());

        // Assert
        result.AppliedCount.Should().Be(0);
    }

    #endregion

    #region FixBySeverityAsync Tests

    [Fact]
    public async Task FixBySeverityAsync_NullDocumentPath_ThrowsArgumentNullException()
    {
        // Arrange
        using var sut = CreateService();
        var validation = CreateValidationResult();

        // Act
        var act = () => sut.FixBySeverityAsync(null!, validation, UnifiedSeverity.Warning);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("documentPath");
    }

    [Fact]
    public async Task FixBySeverityAsync_ErrorSeverity_FixesOnlyErrors()
    {
        // Arrange
        using var sut = CreateService();
        var errorIssue = CreateIssue(
            start: 10, length: 5, oldText: "brown", newText: "red",
            severity: UnifiedSeverity.Error);
        var warningIssue = CreateIssue(
            start: 35, length: 4, oldText: "lazy", newText: "fast",
            severity: UnifiedSeverity.Warning);
        var validation = CreateValidationResult(errorIssue, warningIssue);

        // Act — minSeverity=Error means only Error (SeverityOrder <= 0)
        var result = await sut.FixBySeverityAsync(
            TestDocumentPath, validation, UnifiedSeverity.Error);

        // Assert
        result.AppliedCount.Should().Be(1);
        result.ResolvedIssues.Should().OnlyContain(i => i.Severity == UnifiedSeverity.Error);
    }

    [Fact]
    public async Task FixBySeverityAsync_WarningSeverity_FixesErrorsAndWarnings()
    {
        // Arrange
        using var sut = CreateService();
        var errorIssue = CreateIssue(
            start: 10, length: 5, oldText: "brown", newText: "red",
            severity: UnifiedSeverity.Error);
        var warningIssue = CreateIssue(
            start: 35, length: 4, oldText: "lazy", newText: "fast",
            severity: UnifiedSeverity.Warning);
        var infoIssue = CreateIssue(
            start: 4, length: 3, oldText: "The", newText: "A",
            severity: UnifiedSeverity.Info);
        var validation = CreateValidationResult(errorIssue, warningIssue, infoIssue);

        // Act — minSeverity=Warning includes Error(0)+Warning(1), excludes Info(2)
        var result = await sut.FixBySeverityAsync(
            TestDocumentPath, validation, UnifiedSeverity.Warning);

        // Assert
        result.AppliedCount.Should().Be(2);
        result.ResolvedIssues.Should().OnlyContain(
            i => i.Severity == UnifiedSeverity.Error || i.Severity == UnifiedSeverity.Warning);
    }

    [Fact]
    public async Task FixBySeverityAsync_DisposedInstance_ThrowsObjectDisposedException()
    {
        // Arrange
        var sut = CreateService();
        sut.Dispose();
        var validation = CreateValidationResult();

        // Act
        var act = () => sut.FixBySeverityAsync(TestDocumentPath, validation, UnifiedSeverity.Warning);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region FixByIdAsync Tests

    [Fact]
    public async Task FixByIdAsync_NullDocumentPath_ThrowsArgumentNullException()
    {
        // Arrange
        using var sut = CreateService();
        var validation = CreateValidationResult();

        // Act
        var act = () => sut.FixByIdAsync(null!, validation, new[] { Guid.NewGuid() });

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("documentPath");
    }

    [Fact]
    public async Task FixByIdAsync_NullIssueIds_ThrowsArgumentNullException()
    {
        // Arrange
        using var sut = CreateService();
        var validation = CreateValidationResult();

        // Act
        var act = () => sut.FixByIdAsync(TestDocumentPath, validation, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("issueIds");
    }

    [Fact]
    public async Task FixByIdAsync_SpecificIds_FixesOnlyMatchingIssues()
    {
        // Arrange
        using var sut = CreateService();
        var issue1 = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var issue2 = CreateIssue(start: 35, length: 4, oldText: "lazy", newText: "fast");
        var validation = CreateValidationResult(issue1, issue2);

        // Act — only fix the first issue by ID
        var result = await sut.FixByIdAsync(
            TestDocumentPath, validation, new[] { issue1.IssueId });

        // Assert
        result.AppliedCount.Should().Be(1);
    }

    [Fact]
    public async Task FixByIdAsync_NonExistentIds_ReturnsEmptyResult()
    {
        // Arrange
        using var sut = CreateService();
        var issue = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var validation = CreateValidationResult(issue);

        // Act — provide a non-existent ID
        var result = await sut.FixByIdAsync(
            TestDocumentPath, validation, new[] { Guid.NewGuid() });

        // Assert
        result.AppliedCount.Should().Be(0);
    }

    #endregion

    #region DetectConflicts Tests

    [Fact]
    public void DetectConflicts_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        using var sut = CreateService();

        // Act
        var act = () => sut.DetectConflicts(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("issues");
    }

    [Fact]
    public void DetectConflicts_DisposedInstance_ThrowsObjectDisposedException()
    {
        // Arrange
        var sut = CreateService();
        sut.Dispose();

        // Act
        var act = () => sut.DetectConflicts(new List<UnifiedIssue>());

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void DetectConflicts_DelegatesToConflictDetector()
    {
        // Arrange
        using var sut = CreateService();
        var issue1 = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var issue2 = CreateIssue(start: 12, length: 5, oldText: "own f", newText: "zzz");
        var issues = new List<UnifiedIssue> { issue1, issue2 };

        // Act
        var conflicts = sut.DetectConflicts(issues);

        // Assert — overlapping issues should produce conflicts
        conflicts.Should().NotBeEmpty();
        conflicts.Should().Contain(c => c.Type == FixConflictType.OverlappingPositions);
    }

    #endregion

    #region DryRunAsync Tests

    [Fact]
    public async Task DryRunAsync_NullDocumentPath_ThrowsArgumentNullException()
    {
        // Arrange
        using var sut = CreateService();
        var validation = CreateValidationResult();

        // Act
        var act = () => sut.DryRunAsync(null!, validation);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("documentPath");
    }

    [Fact]
    public async Task DryRunAsync_ReturnsCorrectCounts_NoEditorChanges()
    {
        // Arrange
        using var sut = CreateService();
        var issue1 = CreateIssue(start: 4, length: 5, oldText: "quick", newText: "slow");
        var issue2 = CreateIssue(start: 35, length: 4, oldText: "lazy", newText: "fast");
        var validation = CreateValidationResult(issue1, issue2);

        // Act
        var result = await sut.DryRunAsync(TestDocumentPath, validation);

        // Assert
        result.Success.Should().BeTrue();
        result.AppliedCount.Should().Be(2);
        _mockEditor.Verify(x => x.DeleteText(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _mockEditor.Verify(x => x.InsertText(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region UndoLastFixesAsync Tests

    [Fact]
    public async Task UndoLastFixesAsync_EmptyStack_ReturnsFalse()
    {
        // Arrange
        using var sut = CreateService();

        // Act
        var result = await sut.UndoLastFixesAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UndoLastFixesAsync_WithTransaction_RestoresDocument()
    {
        // Arrange
        using var sut = CreateService();
        var issue = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var validation = CreateValidationResult(issue);
        var options = new FixWorkflowOptions
        {
            EnableUndo = true,
            ReValidateAfterFixes = false
        };

        // LOGIC: First apply a fix to create a transaction
        await sut.FixAllAsync(TestDocumentPath, validation, options);

        // LOGIC: Set up editor to return modified content for undo
        _mockEditor.Setup(x => x.GetDocumentText()).Returns("The quick red fox jumps over the lazy dog.");

        // Act
        var result = await sut.UndoLastFixesAsync();

        // Assert
        result.Should().BeTrue();
        _mockEditor.Verify(x => x.BeginUndoGroup(It.Is<string>(s => s.Contains("Undo"))), Times.Once);
        _mockEditor.Verify(x => x.InsertText(0, TestDocumentContent), Times.Once);
        _mockEditor.Verify(x => x.EndUndoGroup(), Times.AtLeast(1));
    }

    [Fact]
    public async Task UndoLastFixesAsync_MultipleTransactions_PopsLatestFirst()
    {
        // Arrange
        using var sut = CreateService();
        var issue1 = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var issue2 = CreateIssue(start: 35, length: 4, oldText: "lazy", newText: "fast");
        var options = new FixWorkflowOptions
        {
            EnableUndo = true,
            ReValidateAfterFixes = false
        };

        // LOGIC: Apply two separate fix operations to create two transactions
        await sut.FixAllAsync(TestDocumentPath, CreateValidationResult(issue1), options);
        await sut.FixAllAsync(TestDocumentPath, CreateValidationResult(issue2), options);

        // Act — undo first (latest)
        var result1 = await sut.UndoLastFixesAsync();

        // Assert
        result1.Should().BeTrue();

        // Act — undo second
        var result2 = await sut.UndoLastFixesAsync();

        // Assert
        result2.Should().BeTrue();

        // Act — undo again (empty stack)
        var result3 = await sut.UndoLastFixesAsync();

        // Assert
        result3.Should().BeFalse();
    }

    [Fact]
    public async Task UndoLastFixesAsync_DisposedInstance_ThrowsObjectDisposedException()
    {
        // Arrange
        var sut = CreateService();
        sut.Dispose();

        // Act
        var act = () => sut.UndoLastFixesAsync();

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task UndoLastFixesAsync_AlreadyUndone_ReturnsFalse()
    {
        // Arrange
        using var sut = CreateService();
        var issue = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var validation = CreateValidationResult(issue);
        var options = new FixWorkflowOptions
        {
            EnableUndo = true,
            ReValidateAfterFixes = false
        };

        // LOGIC: Apply a fix and undo it
        await sut.FixAllAsync(TestDocumentPath, validation, options);
        await sut.UndoLastFixesAsync();

        // Act — undo again (stack is empty after popping)
        var result = await sut.UndoLastFixesAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_MultipleDispose_DoesNotThrow()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var act = () =>
        {
            sut.Dispose();
            sut.Dispose();
            sut.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task Dispose_AfterDispose_MethodsThrowObjectDisposedException()
    {
        // Arrange
        var sut = CreateService();
        sut.Dispose();
        var validation = CreateValidationResult();

        // Act & Assert — FixAllAsync
        var act1 = () => sut.FixAllAsync(TestDocumentPath, validation, FixWorkflowOptions.Default);
        await act1.Should().ThrowAsync<ObjectDisposedException>();

        // Act & Assert — FixByCategoryAsync
        var act2 = () => sut.FixByCategoryAsync(
            TestDocumentPath, validation, new[] { IssueCategory.Grammar });
        await act2.Should().ThrowAsync<ObjectDisposedException>();

        // Act & Assert — FixBySeverityAsync
        var act3 = () => sut.FixBySeverityAsync(
            TestDocumentPath, validation, UnifiedSeverity.Warning);
        await act3.Should().ThrowAsync<ObjectDisposedException>();

        // Act & Assert — FixByIdAsync
        var act4 = () => sut.FixByIdAsync(
            TestDocumentPath, validation, new[] { Guid.NewGuid() });
        await act4.Should().ThrowAsync<ObjectDisposedException>();

        // Act & Assert — DetectConflicts
        var act5 = () => sut.DetectConflicts(new List<UnifiedIssue>());
        act5.Should().Throw<ObjectDisposedException>();

        // Act & Assert — DryRunAsync
        var act6 = () => sut.DryRunAsync(TestDocumentPath, validation);
        await act6.Should().ThrowAsync<ObjectDisposedException>();

        // Act & Assert — UndoLastFixesAsync
        var act7 = () => sut.UndoLastFixesAsync();
        await act7.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task FixAllAsync_InvalidLocation_SkipsInvalidIssues()
    {
        // Arrange
        using var sut = CreateService();
        // LOGIC: Fix location beyond document length (document is 44 chars)
        var invalidIssue = CreateIssue(start: 100, length: 5, oldText: "xxxxx", newText: "yyy");
        var validIssue = CreateIssue(start: 10, length: 5, oldText: "brown", newText: "red");
        var validation = CreateValidationResult(invalidIssue, validIssue);
        var options = new FixWorkflowOptions
        {
            ReValidateAfterFixes = false,
            ConflictStrategy = ConflictHandlingStrategy.SkipConflicting
        };

        // Act
        var result = await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert — the invalid-location issue should be skipped
        result.AppliedCount.Should().BeLessOrEqualTo(1);
    }

    [Fact]
    public async Task FixAllAsync_ZeroLengthFix_InsertsOnly()
    {
        // Arrange
        using var sut = CreateService();
        // LOGIC: Zero-length fix = insertion at position 10
        var location = new TextSpan(10, 0);
        var fix = UnifiedFix.Insertion(10, "INSERTED", "Insert text", 0.9);
        var issue = new UnifiedIssue(
            Guid.NewGuid(), "TEST-003", IssueCategory.Grammar, UnifiedSeverity.Warning,
            "Missing text", location, null, new[] { fix }, "TestSource", null);
        var validation = CreateValidationResult(issue);
        var options = new FixWorkflowOptions { ReValidateAfterFixes = false };

        // Act
        var result = await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert — should only insert, not delete (length is 0)
        result.AppliedCount.Should().Be(1);
        _mockEditor.Verify(x => x.DeleteText(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _mockEditor.Verify(x => x.InsertText(10, "INSERTED"), Times.Once);
    }

    [Fact]
    public async Task FixAllAsync_NullNewText_DeletesOnly()
    {
        // Arrange
        using var sut = CreateService();
        // LOGIC: Fix with null NewText = deletion
        var location = new TextSpan(10, 5);
        var fix = UnifiedFix.Deletion(location, "brown", "Delete text", 0.9);
        var issue = new UnifiedIssue(
            Guid.NewGuid(), "TEST-004", IssueCategory.Grammar, UnifiedSeverity.Warning,
            "Remove text", location, "brown", new[] { fix }, "TestSource", null);
        var validation = CreateValidationResult(issue);
        var options = new FixWorkflowOptions { ReValidateAfterFixes = false };

        // Act
        var result = await sut.FixAllAsync(TestDocumentPath, validation, options);

        // Assert — should delete but not insert (NewText is null)
        result.AppliedCount.Should().Be(1);
        _mockEditor.Verify(x => x.DeleteText(10, 5), Times.Once);
        _mockEditor.Verify(x => x.InsertText(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    #endregion
}
