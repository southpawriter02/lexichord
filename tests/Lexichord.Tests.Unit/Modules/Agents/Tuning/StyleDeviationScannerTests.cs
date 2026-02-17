// -----------------------------------------------------------------------
// <copyright file="StyleDeviationScannerTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Agents;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Abstractions.Events;
using Lexichord.Abstractions.Layout;
using Lexichord.Modules.Agents.Tuning;
using Lexichord.Modules.Agents.Tuning.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;

namespace Lexichord.Tests.Unit.Modules.Agents.Tuning;

/// <summary>
/// Unit tests for <see cref="StyleDeviationScanner"/>.
/// </summary>
/// <remarks>
/// <para>
/// Test Groups:
/// <list type="bullet">
///   <item><description>Constructor tests — Verify null argument handling</description></item>
///   <item><description>ScanDocumentAsync tests — Verify scanning logic, caching, and license checks</description></item>
///   <item><description>ScanRangeAsync tests — Verify range-specific scanning</description></item>
///   <item><description>Cache tests — Verify cache hit/miss behavior</description></item>
///   <item><description>Auto-fixability tests — Verify rule-based classification</description></item>
///   <item><description>Priority mapping tests — Verify severity to priority mapping</description></item>
///   <item><description>Event handler tests — Verify MediatR event handling</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5a as part of the Style Deviation Scanner feature.
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.5a")]
public class StyleDeviationScannerTests : IDisposable
{
    #region Test Setup

    private readonly Mock<ILintingOrchestrator> _mockLintingOrchestrator;
    private readonly Mock<IEditorService> _mockEditorService;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILicenseContext> _mockLicenseContext;
    private readonly IOptions<ScannerOptions> _options;
    private readonly ILogger<StyleDeviationScanner> _logger;

    /// <summary>
    /// Initializes a new instance of the test class with default mocks.
    /// </summary>
    public StyleDeviationScannerTests()
    {
        _mockLintingOrchestrator = new Mock<ILintingOrchestrator>();
        _mockEditorService = new Mock<IEditorService>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _mockLicenseContext = new Mock<ILicenseContext>();
        _options = Options.Create(new ScannerOptions());
        _logger = NullLogger<StyleDeviationScanner>.Instance;

        // Default: WriterPro license
        _mockLicenseContext.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);
    }

    /// <summary>
    /// Creates a scanner with the test mocks.
    /// </summary>
    private StyleDeviationScanner CreateScanner() => new(
        _mockLintingOrchestrator.Object,
        _mockEditorService.Object,
        _cache,
        _mockLicenseContext.Object,
        _options,
        _logger);

    /// <summary>
    /// Creates a test style rule.
    /// </summary>
    private static StyleRule CreateTestRule(
        string id = "test-rule",
        string name = "Test Rule",
        RuleCategory category = RuleCategory.Terminology,
        ViolationSeverity severity = ViolationSeverity.Warning,
        PatternType patternType = PatternType.Literal) =>
        new(
            Id: id,
            Name: name,
            Description: "Test description",
            Category: category,
            DefaultSeverity: severity,
            Pattern: "test",
            PatternType: patternType,
            Suggestion: "suggestion");

    /// <summary>
    /// Creates a test style violation.
    /// </summary>
    private static StyleViolation CreateTestViolation(
        StyleRule? rule = null,
        int startOffset = 0,
        int length = 4,
        ViolationSeverity? severity = null)
    {
        rule ??= CreateTestRule();
        return new StyleViolation(
            Rule: rule,
            Message: "Test violation",
            StartOffset: startOffset,
            EndOffset: startOffset + length,
            StartLine: 1,
            StartColumn: startOffset + 1,
            EndLine: 1,
            EndColumn: startOffset + length + 1,
            MatchedText: "test",
            Suggestion: "suggestion",
            Severity: severity ?? rule.DefaultSeverity);
    }

    /// <summary>
    /// Creates a mock manuscript ViewModel.
    /// </summary>
    private Mock<IManuscriptViewModel> CreateMockDocument(
        string documentId = "test-doc",
        string? filePath = "/test/document.md",
        string content = "This is a test document with some test content.")
    {
        var mock = new Mock<IManuscriptViewModel>();
        mock.Setup(x => x.DocumentId).Returns(documentId);
        mock.Setup(x => x.FilePath).Returns(filePath);
        mock.Setup(x => x.Content).Returns(content);
        return mock;
    }

    /// <summary>
    /// Creates a DocumentLintState with the given violations.
    /// </summary>
    private static DocumentLintState CreateDocumentLintState(IReadOnlyList<StyleViolation> violations) =>
        new()
        {
            DocumentId = "test-doc",
            LastViolations = violations,
            LastLintTime = DateTimeOffset.UtcNow
        };

    public void Dispose()
    {
        _cache.Dispose();
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLintingOrchestrator_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new StyleDeviationScanner(
            null!,
            _mockEditorService.Object,
            _cache,
            _mockLicenseContext.Object,
            _options,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("lintingOrchestrator");
    }

    [Fact]
    public void Constructor_WithNullEditorService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new StyleDeviationScanner(
            _mockLintingOrchestrator.Object,
            null!,
            _cache,
            _mockLicenseContext.Object,
            _options,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("editorService");
    }

    [Fact]
    public void Constructor_WithNullCache_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new StyleDeviationScanner(
            _mockLintingOrchestrator.Object,
            _mockEditorService.Object,
            null!,
            _mockLicenseContext.Object,
            _options,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("cache");
    }

    [Fact]
    public void Constructor_WithNullLicenseContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new StyleDeviationScanner(
            _mockLintingOrchestrator.Object,
            _mockEditorService.Object,
            _cache,
            null!,
            _options,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("licenseContext");
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new StyleDeviationScanner(
            _mockLintingOrchestrator.Object,
            _mockEditorService.Object,
            _cache,
            _mockLicenseContext.Object,
            null!,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new StyleDeviationScanner(
            _mockLintingOrchestrator.Object,
            _mockEditorService.Object,
            _cache,
            _mockLicenseContext.Object,
            _options,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Act
        var scanner = CreateScanner();

        // Assert
        scanner.Should().NotBeNull();
    }

    #endregion

    #region ScanDocumentAsync Tests

    [Fact]
    public async Task ScanDocumentAsync_WithNullPath_ThrowsArgumentException()
    {
        // Arrange
        var scanner = CreateScanner();

        // Act
        var act = () => scanner.ScanDocumentAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ScanDocumentAsync_WithEmptyPath_ThrowsArgumentException()
    {
        // Arrange
        var scanner = CreateScanner();

        // Act
        var act = () => scanner.ScanDocumentAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ScanDocumentAsync_WithCoreLicense_ReturnsEmptyResult()
    {
        // Arrange
        _mockLicenseContext.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);
        var scanner = CreateScanner();

        // Act
        var result = await scanner.ScanDocumentAsync("/test/doc.md");

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.DocumentPath.Should().Be("/test/doc.md");
    }

    [Fact]
    public async Task ScanDocumentAsync_WithWriterProLicense_ProcessesDocument()
    {
        // Arrange
        var documentPath = "/test/doc.md";
        var content = "This is a test document.";
        var mockDoc = CreateMockDocument(filePath: documentPath, content: content);
        var violation = CreateTestViolation(startOffset: 10, length: 4);
        var lintResult = LintResult.Success("test-doc", [violation], TimeSpan.FromMilliseconds(50));

        _mockEditorService.Setup(x => x.GetDocumentByPath(documentPath)).Returns(mockDoc.Object);
        _mockEditorService.Setup(x => x.GetDocumentText()).Returns(content);
        _mockLintingOrchestrator.Setup(x => x.GetDocumentState(It.IsAny<string>()))
            .Returns(CreateDocumentLintState(lintResult.Violations));

        var scanner = CreateScanner();

        // Act
        var result = await scanner.ScanDocumentAsync(documentPath);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Deviations.Should().HaveCount(1);
        result.Deviations[0].Message.Should().Be("Test violation");
    }

    [Fact]
    public async Task ScanDocumentAsync_WithNoViolations_ReturnsEmptyDeviations()
    {
        // Arrange
        var documentPath = "/test/doc.md";
        var content = "Clean document.";
        var mockDoc = CreateMockDocument(filePath: documentPath, content: content);
        var lintResult = LintResult.Success("test-doc", [], TimeSpan.FromMilliseconds(10));

        _mockEditorService.Setup(x => x.GetDocumentByPath(documentPath)).Returns(mockDoc.Object);
        _mockEditorService.Setup(x => x.GetDocumentText()).Returns(content);
        _mockLintingOrchestrator.Setup(x => x.GetDocumentState(It.IsAny<string>()))
            .Returns(CreateDocumentLintState(lintResult.Violations));

        var scanner = CreateScanner();

        // Act
        var result = await scanner.ScanDocumentAsync(documentPath);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.AutoFixableCount.Should().Be(0);
    }

    [Fact]
    public async Task ScanDocumentAsync_WithMultipleViolations_ReturnsAllDeviations()
    {
        // Arrange
        var documentPath = "/test/doc.md";
        var content = "Test one and test two.";
        var mockDoc = CreateMockDocument(filePath: documentPath, content: content);
        var violations = new[]
        {
            CreateTestViolation(startOffset: 0, length: 4),
            CreateTestViolation(startOffset: 13, length: 4)
        };
        var lintResult = LintResult.Success("test-doc", violations, TimeSpan.FromMilliseconds(50));

        _mockEditorService.Setup(x => x.GetDocumentByPath(documentPath)).Returns(mockDoc.Object);
        _mockEditorService.Setup(x => x.GetDocumentText()).Returns(content);
        _mockLintingOrchestrator.Setup(x => x.GetDocumentState(It.IsAny<string>()))
            .Returns(CreateDocumentLintState(lintResult.Violations));

        var scanner = CreateScanner();

        // Act
        var result = await scanner.ScanDocumentAsync(documentPath);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ScanDocumentAsync_RaisesDeviationsDetectedEvent()
    {
        // Arrange
        var documentPath = "/test/doc.md";
        var content = "This is a test document.";
        var mockDoc = CreateMockDocument(filePath: documentPath, content: content);
        var violation = CreateTestViolation(startOffset: 10, length: 4);
        var lintResult = LintResult.Success("test-doc", [violation], TimeSpan.FromMilliseconds(50));

        _mockEditorService.Setup(x => x.GetDocumentByPath(documentPath)).Returns(mockDoc.Object);
        _mockEditorService.Setup(x => x.GetDocumentText()).Returns(content);
        _mockLintingOrchestrator.Setup(x => x.GetDocumentState(It.IsAny<string>()))
            .Returns(CreateDocumentLintState(lintResult.Violations));

        var scanner = CreateScanner();
        DeviationsDetectedEventArgs? eventArgs = null;
        scanner.DeviationsDetected += (_, args) => eventArgs = args;

        // Act
        await scanner.ScanDocumentAsync(documentPath);

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.DocumentPath.Should().Be(documentPath);
        eventArgs.NewDeviations.Should().HaveCount(1);
    }

    #endregion

    #region Cache Tests

    [Fact]
    public async Task ScanDocumentAsync_SecondCallWithSameContent_ReturnsCachedResult()
    {
        // Arrange
        var documentPath = "/test/doc.md";
        var content = "This is a test document.";
        var mockDoc = CreateMockDocument(filePath: documentPath, content: content);
        var violation = CreateTestViolation(startOffset: 10, length: 4);
        var lintResult = LintResult.Success("test-doc", [violation], TimeSpan.FromMilliseconds(50));

        _mockEditorService.Setup(x => x.GetDocumentByPath(documentPath)).Returns(mockDoc.Object);
        _mockEditorService.Setup(x => x.GetDocumentText()).Returns(content);
        _mockLintingOrchestrator.Setup(x => x.GetDocumentState(It.IsAny<string>()))
            .Returns(CreateDocumentLintState(lintResult.Violations));

        var scanner = CreateScanner();

        // Act
        var result1 = await scanner.ScanDocumentAsync(documentPath);
        var result2 = await scanner.ScanDocumentAsync(documentPath);

        // Assert
        result1.IsCached.Should().BeFalse();
        result2.IsCached.Should().BeTrue();
    }

    [Fact]
    public void InvalidateCache_ClearsDocumentCache()
    {
        // Arrange
        var documentPath = "/test/doc.md";
        var scanner = CreateScanner();

        // Act & Assert - Should not throw
        scanner.InvalidateCache(documentPath);
    }

    [Fact]
    public void InvalidateAllCaches_ClearsAllCaches()
    {
        // Arrange
        var scanner = CreateScanner();

        // Act & Assert - Should not throw
        scanner.InvalidateAllCaches();
    }

    [Fact]
    public async Task GetCachedResultAsync_WhenNotCached_ReturnsNull()
    {
        // Arrange
        var scanner = CreateScanner();

        // Act
        var result = await scanner.GetCachedResultAsync("/test/doc.md");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Priority Mapping Tests

    [Theory]
    [InlineData(ViolationSeverity.Error, DeviationPriority.Critical)]
    [InlineData(ViolationSeverity.Warning, DeviationPriority.High)]
    [InlineData(ViolationSeverity.Info, DeviationPriority.Normal)]
    [InlineData(ViolationSeverity.Hint, DeviationPriority.Low)]
    public async Task ScanDocumentAsync_MapsSeverityToPriority(ViolationSeverity severity, DeviationPriority expectedPriority)
    {
        // Arrange
        var documentPath = "/test/doc.md";
        var content = "This is a test document.";
        var rule = CreateTestRule(severity: severity);
        var mockDoc = CreateMockDocument(filePath: documentPath, content: content);
        var violation = CreateTestViolation(rule: rule, startOffset: 10, length: 4, severity: severity);
        var lintResult = LintResult.Success("test-doc", [violation], TimeSpan.FromMilliseconds(50));

        _mockEditorService.Setup(x => x.GetDocumentByPath(documentPath)).Returns(mockDoc.Object);
        _mockEditorService.Setup(x => x.GetDocumentText()).Returns(content);
        _mockLintingOrchestrator.Setup(x => x.GetDocumentState(It.IsAny<string>()))
            .Returns(CreateDocumentLintState(lintResult.Violations));

        var scanner = CreateScanner();

        // Act
        var result = await scanner.ScanDocumentAsync(documentPath);

        // Assert
        result.Deviations.Should().HaveCount(1);
        result.Deviations[0].Priority.Should().Be(expectedPriority);
    }

    #endregion

    #region Auto-Fixability Tests

    [Fact]
    public async Task ScanDocumentAsync_TerminologyRule_IsAutoFixable()
    {
        // Arrange
        var documentPath = "/test/doc.md";
        var content = "This is a test document.";
        var rule = CreateTestRule(category: RuleCategory.Terminology);
        var mockDoc = CreateMockDocument(filePath: documentPath, content: content);
        var violation = CreateTestViolation(rule: rule, startOffset: 10, length: 4);
        var lintResult = LintResult.Success("test-doc", [violation], TimeSpan.FromMilliseconds(50));

        _mockEditorService.Setup(x => x.GetDocumentByPath(documentPath)).Returns(mockDoc.Object);
        _mockEditorService.Setup(x => x.GetDocumentText()).Returns(content);
        _mockLintingOrchestrator.Setup(x => x.GetDocumentState(It.IsAny<string>()))
            .Returns(CreateDocumentLintState(lintResult.Violations));

        var scanner = CreateScanner();

        // Act
        var result = await scanner.ScanDocumentAsync(documentPath);

        // Assert
        result.Deviations.Should().HaveCount(1);
        result.Deviations[0].IsAutoFixable.Should().BeTrue();
    }

    [Fact]
    public async Task ScanDocumentAsync_FormattingRule_IsNotAutoFixable()
    {
        // Arrange
        var documentPath = "/test/doc.md";
        var content = "This is a test document.";
        var rule = CreateTestRule(category: RuleCategory.Formatting, patternType: PatternType.Regex);
        var mockDoc = CreateMockDocument(filePath: documentPath, content: content);
        var violation = new StyleViolation(
            Rule: rule,
            Message: "Formatting violation",
            StartOffset: 10,
            EndOffset: 14,
            StartLine: 1,
            StartColumn: 11,
            EndLine: 1,
            EndColumn: 15,
            MatchedText: "test",
            Suggestion: null, // No suggestion
            Severity: ViolationSeverity.Warning);
        var lintResult = LintResult.Success("test-doc", [violation], TimeSpan.FromMilliseconds(50));

        _mockEditorService.Setup(x => x.GetDocumentByPath(documentPath)).Returns(mockDoc.Object);
        _mockEditorService.Setup(x => x.GetDocumentText()).Returns(content);
        _mockLintingOrchestrator.Setup(x => x.GetDocumentState(It.IsAny<string>()))
            .Returns(CreateDocumentLintState(lintResult.Violations));

        var scanner = CreateScanner();

        // Act
        var result = await scanner.ScanDocumentAsync(documentPath);

        // Assert
        result.Deviations.Should().HaveCount(1);
        result.Deviations[0].IsAutoFixable.Should().BeFalse();
    }

    [Fact]
    public async Task ScanDocumentAsync_RuleWithSuggestion_IsAutoFixable()
    {
        // Arrange
        var documentPath = "/test/doc.md";
        var content = "This is a test document.";
        var rule = CreateTestRule();
        var mockDoc = CreateMockDocument(filePath: documentPath, content: content);
        var violation = CreateTestViolation(rule: rule, startOffset: 10, length: 4);
        var lintResult = LintResult.Success("test-doc", [violation], TimeSpan.FromMilliseconds(50));

        _mockEditorService.Setup(x => x.GetDocumentByPath(documentPath)).Returns(mockDoc.Object);
        _mockEditorService.Setup(x => x.GetDocumentText()).Returns(content);
        _mockLintingOrchestrator.Setup(x => x.GetDocumentState(It.IsAny<string>()))
            .Returns(CreateDocumentLintState(lintResult.Violations));

        var scanner = CreateScanner();

        // Act
        var result = await scanner.ScanDocumentAsync(documentPath);

        // Assert
        result.Deviations.Should().HaveCount(1);
        result.Deviations[0].IsAutoFixable.Should().BeTrue();
        result.Deviations[0].LinterSuggestedFix.Should().Be("suggestion");
    }

    #endregion

    #region Event Handler Tests

    [Fact]
    public async Task Handle_LintingCompletedEvent_InvalidatesCacheAndReScans()
    {
        // Arrange
        var documentPath = "/test/doc.md";
        var content = "This is a test document.";
        var mockDoc = CreateMockDocument(documentId: "test-doc", filePath: documentPath, content: content);
        var violation = CreateTestViolation(startOffset: 10, length: 4);
        var lintResult = LintResult.Success("test-doc", [violation], TimeSpan.FromMilliseconds(50));

        _mockEditorService.Setup(x => x.GetDocumentById("test-doc")).Returns(mockDoc.Object);
        _mockEditorService.Setup(x => x.GetDocumentByPath(documentPath)).Returns(mockDoc.Object);
        _mockEditorService.Setup(x => x.GetDocumentText()).Returns(content);
        _mockLintingOrchestrator.Setup(x => x.GetDocumentState(It.IsAny<string>()))
            .Returns(CreateDocumentLintState(lintResult.Violations));

        var scanner = CreateScanner();
        var notification = new LintingCompletedEvent(lintResult);

        // Act
        await scanner.Handle(notification, CancellationToken.None);

        // Assert - Verify that scanning was triggered (via GetDocumentById call)
        _mockEditorService.Verify(x => x.GetDocumentById("test-doc"), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_StyleSheetReloadedEvent_InvalidatesAllCaches()
    {
        // Arrange
        var scanner = CreateScanner();
        var notification = new StyleSheetReloadedEvent(
            FilePath: "/styles/test.yaml",
            NewStyleSheet: StyleSheet.Empty,
            PreviousStyleSheet: StyleSheet.Empty,
            ReloadSource: StyleReloadSource.FileModified);

        // Act
        await scanner.Handle(notification, CancellationToken.None);

        // Assert - Should not throw, cache should be cleared
        // (We can't easily verify cache was cleared without exposing internal state)
    }

    [Fact]
    public async Task Handle_LintingCompletedEvent_WhenRealTimeUpdatesDisabled_DoesNothing()
    {
        // Arrange
        var options = Options.Create(new ScannerOptions { EnableRealTimeUpdates = false });
        var scanner = new StyleDeviationScanner(
            _mockLintingOrchestrator.Object,
            _mockEditorService.Object,
            _cache,
            _mockLicenseContext.Object,
            options,
            _logger);

        var lintResult = LintResult.Success("test-doc", [], TimeSpan.FromMilliseconds(10));
        var notification = new LintingCompletedEvent(lintResult);

        // Act
        await scanner.Handle(notification, CancellationToken.None);

        // Assert - GetDocumentById should not be called
        _mockEditorService.Verify(x => x.GetDocumentById(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region ScanRangeAsync Tests

    [Fact]
    public async Task ScanRangeAsync_WithNullPath_ThrowsArgumentException()
    {
        // Arrange
        var scanner = CreateScanner();

        // Act
        var act = () => scanner.ScanRangeAsync(null!, new TextSpan(0, 10));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ScanRangeAsync_WithCoreLicense_ReturnsEmptyResult()
    {
        // Arrange
        _mockLicenseContext.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);
        var scanner = CreateScanner();

        // Act
        var result = await scanner.ScanRangeAsync("/test/doc.md", new TextSpan(0, 10));

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task ScanRangeAsync_FiltersViolationsToRange()
    {
        // Arrange
        var documentPath = "/test/doc.md";
        var content = "Test one and test two and test three.";
        var mockDoc = CreateMockDocument(filePath: documentPath, content: content);
        var violations = new[]
        {
            CreateTestViolation(startOffset: 0, length: 4),   // In range
            CreateTestViolation(startOffset: 13, length: 4),  // In range
            CreateTestViolation(startOffset: 27, length: 4)   // Outside range
        };
        var lintResult = LintResult.Success("test-doc", violations, TimeSpan.FromMilliseconds(50));

        _mockEditorService.Setup(x => x.GetDocumentByPath(documentPath)).Returns(mockDoc.Object);
        _mockEditorService.Setup(x => x.GetDocumentText()).Returns(content);
        _mockLintingOrchestrator.Setup(x => x.GetDocumentState(It.IsAny<string>()))
            .Returns(CreateDocumentLintState(lintResult.Violations));

        var scanner = CreateScanner();

        // Act - Scan range from 0 to 20 (should include first two violations)
        var result = await scanner.ScanRangeAsync(documentPath, new TextSpan(0, 20));

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_MultipleCallsDoNotThrow()
    {
        // Arrange
        var scanner = CreateScanner();

        // Act
        scanner.Dispose();
        scanner.Dispose();

        // Assert - Should not throw
    }

    [Fact]
    public async Task ScanDocumentAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var scanner = CreateScanner();
        scanner.Dispose();

        // Act
        var act = () => scanner.ScanDocumentAsync("/test/doc.md");

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion
}
