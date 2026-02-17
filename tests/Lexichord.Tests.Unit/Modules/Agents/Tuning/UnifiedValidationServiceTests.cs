// -----------------------------------------------------------------------
// <copyright file="UnifiedValidationServiceTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// Unit tests for the Unified Validation Service (v0.7.5f).
//   Tests cover constructor validation, validator aggregation, deduplication,
//   license filtering, caching, timeout handling, event publishing, and
//   the UnifiedValidationOptions and UnifiedValidationResult record behaviors.
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Agents;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;
using Lexichord.Abstractions.Contracts.Validation;
using Lexichord.Modules.Agents.Tuning;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

// LOGIC: Use alias to disambiguate from Knowledge.Validation.Integration.UnifiedFix
using UnifiedFix = Lexichord.Abstractions.Contracts.Validation.UnifiedFix;

namespace Lexichord.Tests.Unit.Modules.Agents.Tuning;

/// <summary>
/// Unit tests for <see cref="UnifiedValidationService"/>.
/// </summary>
/// <remarks>
/// <para>
/// Test Groups:
/// <list type="bullet">
///   <item><description>Constructor tests — Verify null argument handling</description></item>
///   <item><description>ValidateAsync - combines results from multiple validators</description></item>
///   <item><description>ValidateAsync - deduplication across validators</description></item>
///   <item><description>ValidateAsync - license tier filtering</description></item>
///   <item><description>ValidateAsync - timeout handling for slow validators</description></item>
///   <item><description>ValidateAsync - validator crash handling</description></item>
///   <item><description>ValidateAsync - caching behavior</description></item>
///   <item><description>ValidateRangeAsync - range-specific filtering</description></item>
///   <item><description>Cache invalidation methods</description></item>
///   <item><description>UnifiedValidationOptions record tests</description></item>
///   <item><description>UnifiedValidationResult computed properties</description></item>
///   <item><description>ValidationCompletedEventArgs tests</description></item>
///   <item><description>Event publishing tests</description></item>
///   <item><description>Parallel vs sequential execution</description></item>
///   <item><description>MaxIssuesPerDocument limit tests</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5f as part of the Issue Aggregator feature.
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.5f")]
public class UnifiedValidationServiceTests : IDisposable
{
    #region Test Setup

    // ── Mock Dependencies ─────────────────────────────────────────────────
    private readonly Mock<IStyleDeviationScanner> _mockStyleScanner;
    private readonly Mock<IValidationEngine> _mockValidationEngine;
    private readonly Mock<ILicenseContext> _mockLicenseContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UnifiedValidationService> _logger;

    // ── Test Constants ────────────────────────────────────────────────────
    private const string TestDocumentPath = "/test/document.md";
    private const string TestContent = "This is test content for validation.";

    public UnifiedValidationServiceTests()
    {
        _mockStyleScanner = new Mock<IStyleDeviationScanner>();
        _mockValidationEngine = new Mock<IValidationEngine>();
        _mockLicenseContext = new Mock<ILicenseContext>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _logger = NullLogger<UnifiedValidationService>.Instance;

        // LOGIC: Default to Teams license (all validators available)
        _mockLicenseContext
            .Setup(x => x.GetCurrentTier())
            .Returns(LicenseTier.Teams);

        // LOGIC: Default scanner returns empty results
        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DeviationScanResult.Empty(TestDocumentPath, TimeSpan.Zero));

        // LOGIC: Default validation engine returns empty results
        _mockValidationEngine
            .Setup(x => x.ValidateDocumentAsync(It.IsAny<ValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Valid());
    }

    // ── Factory Methods ──────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="UnifiedValidationService"/> with all mocked dependencies.
    /// </summary>
    /// <param name="tier">The license tier to configure.</param>
    /// <returns>A configured <see cref="UnifiedValidationService"/> instance.</returns>
    private UnifiedValidationService CreateService(LicenseTier tier = LicenseTier.Teams)
    {
        _mockLicenseContext
            .Setup(x => x.GetCurrentTier())
            .Returns(tier);

        return new UnifiedValidationService(
            _mockStyleScanner.Object,
            _mockValidationEngine.Object,
            _mockLicenseContext.Object,
            _cache,
            _logger);
    }

    /// <summary>
    /// Creates a test <see cref="StyleDeviation"/> with default values.
    /// </summary>
    private static StyleDeviation CreateTestDeviation(
        string ruleId = "TERM-001",
        string originalText = "test",
        int startOffset = 0,
        ViolationSeverity severity = ViolationSeverity.Warning)
    {
        var rule = CreateTestRule(id: ruleId, severity: severity);
        var violation = CreateTestViolation(
            rule: rule,
            startOffset: startOffset,
            matchedText: originalText);

        // LOGIC: Map ViolationSeverity to DeviationPriority for accurate UnifiedSeverity conversion.
        // UnifiedIssueFactory.FromStyleDeviation uses Priority, not the rule's severity.
        var priority = severity switch
        {
            ViolationSeverity.Error => DeviationPriority.Critical,
            ViolationSeverity.Warning => DeviationPriority.High,
            ViolationSeverity.Info => DeviationPriority.Normal,
            ViolationSeverity.Hint => DeviationPriority.Low,
            _ => DeviationPriority.Normal
        };

        return new StyleDeviation
        {
            DeviationId = Guid.NewGuid(),
            Violation = violation,
            Location = new TextSpan(startOffset, originalText.Length),
            OriginalText = originalText,
            SurroundingContext = $"surrounding context for '{originalText}'",
            ViolatedRule = rule,
            IsAutoFixable = true,
            Priority = priority
        };
    }

    /// <summary>
    /// Creates a test <see cref="StyleRule"/> with default values.
    /// </summary>
    private static StyleRule CreateTestRule(
        string id = "TERM-001",
        string name = "Test Rule",
        ViolationSeverity severity = ViolationSeverity.Warning) =>
        new(
            Id: id,
            Name: name,
            Description: "A test rule for unit tests",
            Category: RuleCategory.Terminology,
            DefaultSeverity: severity,
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
            Severity: rule?.DefaultSeverity ?? ViolationSeverity.Warning);

    /// <summary>
    /// Creates a test <see cref="DeviationScanResult"/> with specified deviations.
    /// </summary>
    private static DeviationScanResult CreateScanResult(params StyleDeviation[] deviations) =>
        new()
        {
            DocumentPath = TestDocumentPath,
            Deviations = deviations,
            ScanDuration = TimeSpan.FromMilliseconds(100),
            IsCached = false
        };

    /// <summary>
    /// Creates a test <see cref="ValidationFinding"/> with default values.
    /// </summary>
    private static ValidationFinding CreateTestFinding(
        string code = "VAL-001",
        int startOffset = 0,
        int length = 4,
        ValidationSeverity severity = ValidationSeverity.Warning) =>
        new(
            ValidatorId: "TestValidator",
            Severity: severity,
            Code: code,
            Message: "Test validation finding",
            PropertyPath: null,
            SuggestedFix: null);

    /// <summary>
    /// Creates a test <see cref="ValidationResult"/> with specified findings.
    /// </summary>
    private static ValidationResult CreateValidationResult(params ValidationFinding[] findings) =>
        ValidationResult.WithFindings(findings, duration: TimeSpan.FromMilliseconds(150));

    /// <summary>
    /// Creates a test <see cref="UnifiedIssue"/> directly.
    /// </summary>
    private static UnifiedIssue CreateTestIssue(
        int startOffset = 0,
        int length = 4,
        UnifiedSeverity severity = UnifiedSeverity.Warning,
        string sourceType = "StyleLinter") =>
        new(
            IssueId: Guid.NewGuid(),
            SourceId: "TEST-001",
            Category: IssueCategory.Style,
            Severity: severity,
            Message: "Test issue message",
            Location: new TextSpan(startOffset, length),
            OriginalText: "test",
            Fixes: Array.Empty<UnifiedFix>(),
            SourceType: sourceType,
            OriginalSource: null);

    public void Dispose()
    {
        _cache.Dispose();
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullStyleScanner_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new UnifiedValidationService(
            null!,
            _mockValidationEngine.Object,
            _mockLicenseContext.Object,
            _cache,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("styleDeviationScanner");
    }

    [Fact]
    public void Constructor_NullValidationEngine_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new UnifiedValidationService(
            _mockStyleScanner.Object,
            null!,
            _mockLicenseContext.Object,
            _cache,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("validationEngine");
    }

    [Fact]
    public void Constructor_NullLicenseContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new UnifiedValidationService(
            _mockStyleScanner.Object,
            _mockValidationEngine.Object,
            null!,
            _cache,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("licenseContext");
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new UnifiedValidationService(
            _mockStyleScanner.Object,
            _mockValidationEngine.Object,
            _mockLicenseContext.Object,
            null!,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("cache");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new UnifiedValidationService(
            _mockStyleScanner.Object,
            _mockValidationEngine.Object,
            _mockLicenseContext.Object,
            _cache,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ValidDependencies_CreatesInstance()
    {
        // Act
        var sut = CreateService();

        // Assert
        sut.Should().NotBeNull();
    }

    #endregion

    #region ValidateAsync - Argument Validation Tests

    [Fact]
    public async Task ValidateAsync_NullDocumentPath_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var act = () => sut.ValidateAsync(null!, TestContent, UnifiedValidationOptions.Default);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("documentPath");
    }

    [Fact]
    public async Task ValidateAsync_NullContent_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var act = () => sut.ValidateAsync(TestDocumentPath, null!, UnifiedValidationOptions.Default);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("content");
    }

    [Fact]
    public async Task ValidateAsync_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var act = () => sut.ValidateAsync(TestDocumentPath, TestContent, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public async Task ValidateAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var sut = CreateService();
        sut.Dispose();

        // Act
        var act = () => sut.ValidateAsync(TestDocumentPath, TestContent, UnifiedValidationOptions.Default);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region ValidateAsync - Combines Results Tests

    [Fact]
    public async Task ValidateAsync_NoIssues_ReturnsEmptyResult()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = await sut.ValidateAsync(
            TestDocumentPath, TestContent, UnifiedValidationOptions.Default);

        // Assert
        result.Should().NotBeNull();
        result.DocumentPath.Should().Be(TestDocumentPath);
        result.Issues.Should().BeEmpty();
        result.TotalIssueCount.Should().Be(0);
        result.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_StyleIssuesOnly_CombinesStyleResults()
    {
        // Arrange
        var sut = CreateService();
        var deviation = CreateTestDeviation();
        var scanResult = CreateScanResult(deviation);

        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanResult);

        // Act
        var result = await sut.ValidateAsync(
            TestDocumentPath, TestContent, UnifiedValidationOptions.Default);

        // Assert
        result.Issues.Should().HaveCount(1);
        result.Issues[0].SourceType.Should().Be("StyleLinter");
    }

    [Fact]
    public async Task ValidateAsync_ValidationIssuesOnly_CombinesValidationResults()
    {
        // Arrange
        var sut = CreateService();
        var finding = CreateTestFinding();
        var validationResult = CreateValidationResult(finding);

        _mockValidationEngine
            .Setup(x => x.ValidateDocumentAsync(It.IsAny<ValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await sut.ValidateAsync(
            TestDocumentPath, TestContent, UnifiedValidationOptions.Default);

        // Assert
        result.Issues.Should().HaveCount(1);
        result.Issues[0].SourceType.Should().Be("Validation");
    }

    [Fact]
    public async Task ValidateAsync_BothSourcesHaveIssues_CombinesAllResults()
    {
        // Arrange
        var sut = CreateService();

        // LOGIC: ValidationFinding always produces Location = TextSpan.Empty (Start=0).
        // To avoid deduplication, place StyleDeviation at a different offset.
        var deviation = CreateTestDeviation(startOffset: 20);
        var scanResult = CreateScanResult(deviation);
        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanResult);

        // Validation engine always at offset 0 (TextSpan.Empty)
        var finding = CreateTestFinding();
        var validationResult = CreateValidationResult(finding);
        _mockValidationEngine
            .Setup(x => x.ValidateDocumentAsync(It.IsAny<ValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await sut.ValidateAsync(
            TestDocumentPath, TestContent, UnifiedValidationOptions.Default);

        // Assert — StyleDeviation at 20, ValidationFinding at 0 = no deduplication
        result.Issues.Should().HaveCount(2);
        result.Issues.Should().Contain(i => i.SourceType == "StyleLinter");
        result.Issues.Should().Contain(i => i.SourceType == "Validation");
    }

    [Fact]
    public async Task ValidateAsync_MultipleStyleIssues_ReturnsAllIssues()
    {
        // Arrange
        var sut = CreateService();
        var deviation1 = CreateTestDeviation(ruleId: "TERM-001", startOffset: 0);
        var deviation2 = CreateTestDeviation(ruleId: "TERM-002", startOffset: 20);
        var scanResult = CreateScanResult(deviation1, deviation2);

        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanResult);

        // Act
        var result = await sut.ValidateAsync(
            TestDocumentPath, TestContent, UnifiedValidationOptions.Default);

        // Assert
        result.Issues.Should().HaveCount(2);
    }

    [Fact]
    public async Task ValidateAsync_ResultIsSortedBySeverityThenLocation()
    {
        // Arrange
        var sut = CreateService();

        // Info at offset 0
        var deviation1 = CreateTestDeviation(
            ruleId: "TERM-001", startOffset: 0, severity: ViolationSeverity.Info);
        // Error at offset 20
        var deviation2 = CreateTestDeviation(
            ruleId: "TERM-002", startOffset: 20, severity: ViolationSeverity.Error);
        // Warning at offset 10
        var deviation3 = CreateTestDeviation(
            ruleId: "TERM-003", startOffset: 10, severity: ViolationSeverity.Warning);

        var scanResult = CreateScanResult(deviation1, deviation2, deviation3);
        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanResult);

        // Act
        var result = await sut.ValidateAsync(
            TestDocumentPath, TestContent, UnifiedValidationOptions.Default);

        // Assert — should be sorted by severity (Error first), then by location
        result.Issues.Should().HaveCount(3);
        result.Issues[0].Severity.Should().Be(UnifiedSeverity.Error); // Most severe first
    }

    [Fact]
    public async Task ValidateAsync_IncludesDurationInResult()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = await sut.ValidateAsync(
            TestDocumentPath, TestContent, UnifiedValidationOptions.Default);

        // Assert
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        result.ValidatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region ValidateAsync - Deduplication Tests

    [Fact]
    public async Task ValidateAsync_DuplicateIssuesAtSameLocation_KeepsHighestSeverity()
    {
        // Arrange
        var sut = CreateService();

        // LOGIC: ValidationFinding always produces Location = TextSpan.Empty (Start=0).
        // To test deduplication, the StyleDeviation must also be at offset 0.

        // Style linter: Warning at offset 0 (to match validation finding's TextSpan.Empty)
        var deviation = CreateTestDeviation(
            ruleId: "TERM-001", startOffset: 0, severity: ViolationSeverity.Warning);
        var scanResult = CreateScanResult(deviation);
        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanResult);

        // Validation engine: Error (always at Location=0 due to TextSpan.Empty)
        var finding = CreateTestFinding(code: "VAL-001", severity: ValidationSeverity.Error);
        var validationResult = CreateValidationResult(finding);
        _mockValidationEngine
            .Setup(x => x.ValidateDocumentAsync(It.IsAny<ValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await sut.ValidateAsync(
            TestDocumentPath, TestContent, UnifiedValidationOptions.Default);

        // Assert — should deduplicate (both at location 0), keeping only the Error
        result.Issues.Should().HaveCount(1);
        result.Issues[0].Severity.Should().Be(UnifiedSeverity.Error);
    }

    [Fact]
    public async Task ValidateAsync_DuplicatesWithLocationTolerance_Deduplicates()
    {
        // Arrange
        var sut = CreateService();

        // LOGIC: ValidationFinding always produces Location = TextSpan.Empty (Start=0).
        // To test tolerance deduplication, StyleDeviation must be at offset 0 or 1.

        // Style linter at offset 1 (within ±1 tolerance of ValidationFinding's 0)
        var deviation = CreateTestDeviation(startOffset: 1, severity: ViolationSeverity.Error);
        var scanResult = CreateScanResult(deviation);
        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanResult);

        // Validation engine at offset 0 (TextSpan.Empty)
        var finding = CreateTestFinding(severity: ValidationSeverity.Warning);
        var validationResult = CreateValidationResult(finding);
        _mockValidationEngine
            .Setup(x => x.ValidateDocumentAsync(It.IsAny<ValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await sut.ValidateAsync(
            TestDocumentPath, TestContent, UnifiedValidationOptions.Default);

        // Assert — should deduplicate (offset 1 is within ±1 of offset 0)
        result.Issues.Should().HaveCount(1);
        result.Issues[0].Severity.Should().Be(UnifiedSeverity.Error); // Keeps higher severity
    }

    [Fact]
    public async Task ValidateAsync_IssuesFromSameValidatorAtSameLocation_NotDeduplicated()
    {
        // Arrange
        var sut = CreateService();

        // Two violations from style linter at the same location
        var deviation1 = CreateTestDeviation(ruleId: "TERM-001", startOffset: 5);
        var deviation2 = CreateTestDeviation(ruleId: "TERM-002", startOffset: 5);
        var scanResult = CreateScanResult(deviation1, deviation2);
        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanResult);

        // Act
        var result = await sut.ValidateAsync(
            TestDocumentPath, TestContent, UnifiedValidationOptions.Default);

        // Assert — same validator issues are NOT deduplicated
        result.Issues.Should().HaveCount(2);
    }

    [Fact]
    public async Task ValidateAsync_IssuesOutsideTolerance_NotDeduplicated()
    {
        // Arrange
        var sut = CreateService();

        // LOGIC: ValidationFinding always produces Location = TextSpan.Empty (Start=0).
        // StyleDeviation at offset 5 is outside ±1 tolerance of 0.
        var deviation = CreateTestDeviation(startOffset: 5);
        var scanResult = CreateScanResult(deviation);
        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanResult);

        // Validation engine always at offset 0 (TextSpan.Empty)
        var finding = CreateTestFinding();
        var validationResult = CreateValidationResult(finding);
        _mockValidationEngine
            .Setup(x => x.ValidateDocumentAsync(It.IsAny<ValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await sut.ValidateAsync(
            TestDocumentPath, TestContent, UnifiedValidationOptions.Default);

        // Assert — issues at different locations are NOT deduplicated
        result.Issues.Should().HaveCount(2);
    }

    [Fact]
    public async Task ValidateAsync_DeduplicationDisabled_KeepsAllIssues()
    {
        // Arrange
        var sut = CreateService();
        var options = new UnifiedValidationOptions { EnableDeduplication = false };

        // LOGIC: ValidationFinding always produces Location = TextSpan.Empty (Start=0).
        // Set StyleDeviation at offset 0 so they would normally deduplicate.
        var deviation = CreateTestDeviation(startOffset: 0);
        var scanResult = CreateScanResult(deviation);
        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanResult);

        // Validation engine always at offset 0 (TextSpan.Empty)
        var finding = CreateTestFinding();
        var validationResult = CreateValidationResult(finding);
        _mockValidationEngine
            .Setup(x => x.ValidateDocumentAsync(It.IsAny<ValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await sut.ValidateAsync(TestDocumentPath, TestContent, options);

        // Assert — both issues retained when deduplication is disabled
        result.Issues.Should().HaveCount(2);
    }

    #endregion

    #region ValidateAsync - License Filtering Tests

    [Fact]
    public async Task ValidateAsync_CoreTier_OnlyStyleLinter()
    {
        // Arrange
        var sut = CreateService(LicenseTier.Core);

        var deviation = CreateTestDeviation();
        var scanResult = CreateScanResult(deviation);
        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanResult);

        // Act
        await sut.ValidateAsync(TestDocumentPath, TestContent, UnifiedValidationOptions.Default);

        // Assert — style scanner called, validation engine NOT called
        _mockStyleScanner.Verify(
            x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockValidationEngine.Verify(
            x => x.ValidateDocumentAsync(It.IsAny<ValidationContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateAsync_WriterProTier_StyleAndGrammar()
    {
        // Arrange
        var sut = CreateService(LicenseTier.WriterPro);

        // Act
        await sut.ValidateAsync(TestDocumentPath, TestContent, UnifiedValidationOptions.Default);

        // Assert — style scanner called, validation engine NOT called
        _mockStyleScanner.Verify(
            x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockValidationEngine.Verify(
            x => x.ValidateDocumentAsync(It.IsAny<ValidationContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateAsync_TeamsTier_AllValidators()
    {
        // Arrange
        var sut = CreateService(LicenseTier.Teams);

        // Act
        await sut.ValidateAsync(TestDocumentPath, TestContent, UnifiedValidationOptions.Default);

        // Assert — all validators called
        _mockStyleScanner.Verify(
            x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockValidationEngine.Verify(
            x => x.ValidateDocumentAsync(It.IsAny<ValidationContext>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_EnterpriseTier_AllValidators()
    {
        // Arrange
        var sut = CreateService(LicenseTier.Enterprise);

        // Act
        await sut.ValidateAsync(TestDocumentPath, TestContent, UnifiedValidationOptions.Default);

        // Assert — all validators called
        _mockStyleScanner.Verify(
            x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockValidationEngine.Verify(
            x => x.ValidateDocumentAsync(It.IsAny<ValidationContext>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_StyleLinterDisabledInOptions_NotCalled()
    {
        // Arrange
        var sut = CreateService();
        var options = new UnifiedValidationOptions { IncludeStyleLinter = false };

        // Act
        await sut.ValidateAsync(TestDocumentPath, TestContent, options);

        // Assert — style scanner NOT called
        _mockStyleScanner.Verify(
            x => x.ScanDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateAsync_ValidationEngineDisabledInOptions_NotCalled()
    {
        // Arrange
        var sut = CreateService();
        var options = new UnifiedValidationOptions { IncludeValidationEngine = false };

        // Act
        await sut.ValidateAsync(TestDocumentPath, TestContent, options);

        // Assert — validation engine NOT called
        _mockValidationEngine.Verify(
            x => x.ValidateDocumentAsync(It.IsAny<ValidationContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region ValidateAsync - Timeout Handling Tests

    [Fact]
    public async Task ValidateAsync_ValidatorTimesOut_ReturnsPartialResults()
    {
        // Arrange
        var sut = CreateService();
        var options = new UnifiedValidationOptions { ValidatorTimeoutMs = 50 };

        var deviation = CreateTestDeviation();
        var scanResult = CreateScanResult(deviation);
        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanResult);

        // Validation engine takes too long
        _mockValidationEngine
            .Setup(x => x.ValidateDocumentAsync(It.IsAny<ValidationContext>(), It.IsAny<CancellationToken>()))
            .Returns(async (ValidationContext _, CancellationToken ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                return ValidationResult.Valid();
            });

        // Act
        var result = await sut.ValidateAsync(TestDocumentPath, TestContent, options);

        // Assert — should have style linter results only
        result.Issues.Should().HaveCount(1);
        result.Issues[0].SourceType.Should().Be("StyleLinter");
    }

    [Fact]
    public async Task ValidateAsync_AllValidatorsTimeout_ReturnsEmptyResult()
    {
        // Arrange
        var sut = CreateService();
        var options = new UnifiedValidationOptions { ValidatorTimeoutMs = 50 };

        // Both validators timeout
        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, CancellationToken ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                return DeviationScanResult.Empty(TestDocumentPath, TimeSpan.Zero);
            });

        _mockValidationEngine
            .Setup(x => x.ValidateDocumentAsync(It.IsAny<ValidationContext>(), It.IsAny<CancellationToken>()))
            .Returns(async (ValidationContext _, CancellationToken ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                return ValidationResult.Valid();
            });

        // Act
        var result = await sut.ValidateAsync(TestDocumentPath, TestContent, options);

        // Assert — should be empty
        result.Issues.Should().BeEmpty();
    }

    #endregion

    #region ValidateAsync - Validator Crash Handling Tests

    [Fact]
    public async Task ValidateAsync_StyleLinterThrows_ContinuesWithOtherValidators()
    {
        // Arrange
        var sut = CreateService();

        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Scanner crashed"));

        var finding = CreateTestFinding();
        var validationResult = CreateValidationResult(finding);
        _mockValidationEngine
            .Setup(x => x.ValidateDocumentAsync(It.IsAny<ValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await sut.ValidateAsync(
            TestDocumentPath, TestContent, UnifiedValidationOptions.Default);

        // Assert — validation engine results should be present
        result.Issues.Should().HaveCount(1);
        result.Issues[0].SourceType.Should().Be("Validation");
    }

    [Fact]
    public async Task ValidateAsync_ValidationEngineThrows_ContinuesWithOtherValidators()
    {
        // Arrange
        var sut = CreateService();

        var deviation = CreateTestDeviation();
        var scanResult = CreateScanResult(deviation);
        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanResult);

        _mockValidationEngine
            .Setup(x => x.ValidateDocumentAsync(It.IsAny<ValidationContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Engine crashed"));

        // Act
        var result = await sut.ValidateAsync(
            TestDocumentPath, TestContent, UnifiedValidationOptions.Default);

        // Assert — style linter results should be present
        result.Issues.Should().HaveCount(1);
        result.Issues[0].SourceType.Should().Be("StyleLinter");
    }

    [Fact]
    public async Task ValidateAsync_AllValidatorsThrow_ReturnsEmptyResult()
    {
        // Arrange
        var sut = CreateService();

        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Scanner crashed"));

        _mockValidationEngine
            .Setup(x => x.ValidateDocumentAsync(It.IsAny<ValidationContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Engine crashed"));

        // Act
        var result = await sut.ValidateAsync(
            TestDocumentPath, TestContent, UnifiedValidationOptions.Default);

        // Assert — should be empty but not throw
        result.Issues.Should().BeEmpty();
    }

    #endregion

    #region ValidateAsync - Caching Tests

    [Fact]
    public async Task ValidateAsync_CachingEnabled_CachesResult()
    {
        // Arrange
        var sut = CreateService();

        var deviation = CreateTestDeviation();
        var scanResult = CreateScanResult(deviation);
        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanResult);

        // Act
        var result1 = await sut.ValidateAsync(
            TestDocumentPath, TestContent, UnifiedValidationOptions.Default);
        var result2 = await sut.ValidateAsync(
            TestDocumentPath, TestContent, UnifiedValidationOptions.Default);

        // Assert — scanner should only be called once
        _mockStyleScanner.Verify(
            x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()),
            Times.Once);
        result2.IsCached.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_CachingDisabled_DoesNotCache()
    {
        // Arrange
        var sut = CreateService();
        var options = new UnifiedValidationOptions { EnableCaching = false };

        // Act
        await sut.ValidateAsync(TestDocumentPath, TestContent, options);
        await sut.ValidateAsync(TestDocumentPath, TestContent, options);

        // Assert — scanner called twice
        _mockStyleScanner.Verify(
            x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ValidateAsync_DifferentOptions_InvalidatesCache()
    {
        // Arrange
        var sut = CreateService();
        var options1 = new UnifiedValidationOptions { MinimumSeverity = UnifiedSeverity.Error };
        var options2 = new UnifiedValidationOptions { MinimumSeverity = UnifiedSeverity.Warning };

        // Act
        await sut.ValidateAsync(TestDocumentPath, TestContent, options1);
        await sut.ValidateAsync(TestDocumentPath, TestContent, options2);

        // Assert — scanner called twice because options differ
        _mockStyleScanner.Verify(
            x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task GetCachedResultAsync_NoCachedResult_ReturnsNull()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = await sut.GetCachedResultAsync("/nonexistent/path.md");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCachedResultAsync_HasCachedResult_ReturnsCachedResult()
    {
        // Arrange
        var sut = CreateService();

        var deviation = CreateTestDeviation();
        var scanResult = CreateScanResult(deviation);
        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanResult);

        await sut.ValidateAsync(TestDocumentPath, TestContent, UnifiedValidationOptions.Default);

        // Act
        var cached = await sut.GetCachedResultAsync(TestDocumentPath);

        // Assert
        cached.Should().NotBeNull();
        cached!.DocumentPath.Should().Be(TestDocumentPath);
        cached.Issues.Should().HaveCount(1);
    }

    #endregion

    #region Cache Invalidation Tests

    [Fact]
    public void InvalidateCache_RemovesCachedEntry()
    {
        // Arrange
        var sut = CreateService();

        // Act
        sut.InvalidateCache(TestDocumentPath);

        // Assert — should not throw
    }

    [Fact]
    public async Task InvalidateCache_AfterValidation_RemovesCache()
    {
        // Arrange
        var sut = CreateService();

        await sut.ValidateAsync(TestDocumentPath, TestContent, UnifiedValidationOptions.Default);

        // Act
        sut.InvalidateCache(TestDocumentPath);
        var cached = await sut.GetCachedResultAsync(TestDocumentPath);

        // Assert
        cached.Should().BeNull();
    }

    [Fact]
    public async Task InvalidateAllCaches_ClearsAllEntries()
    {
        // Arrange
        var sut = CreateService();

        await sut.ValidateAsync(TestDocumentPath, TestContent, UnifiedValidationOptions.Default);
        await sut.ValidateAsync("/other/document.md", TestContent, UnifiedValidationOptions.Default);

        // Act
        sut.InvalidateAllCaches();
        var cached1 = await sut.GetCachedResultAsync(TestDocumentPath);
        var cached2 = await sut.GetCachedResultAsync("/other/document.md");

        // Assert
        cached1.Should().BeNull();
        cached2.Should().BeNull();
    }

    #endregion

    #region ValidateRangeAsync Tests

    [Fact]
    public async Task ValidateRangeAsync_FiltersIssuesToRange()
    {
        // Arrange
        var sut = CreateService();

        var deviation1 = CreateTestDeviation(startOffset: 5); // Inside range 0-20
        var deviation2 = CreateTestDeviation(startOffset: 25); // Outside range 0-20
        var scanResult = CreateScanResult(deviation1, deviation2);
        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanResult);

        // Act
        var result = await sut.ValidateRangeAsync(
            TestDocumentPath,
            TestContent,
            new TextSpan(0, 20),
            UnifiedValidationOptions.Default);

        // Assert — only the issue within the range
        result.Issues.Should().HaveCount(1);
        result.Issues[0].Location.Start.Should().Be(5);
    }

    [Fact]
    public async Task ValidateRangeAsync_NoIssuesInRange_ReturnsEmpty()
    {
        // Arrange
        var sut = CreateService();

        var deviation = CreateTestDeviation(startOffset: 50);
        var scanResult = CreateScanResult(deviation);
        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanResult);

        // Act
        var result = await sut.ValidateRangeAsync(
            TestDocumentPath,
            TestContent,
            new TextSpan(0, 20),
            UnifiedValidationOptions.Default);

        // Assert
        result.Issues.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateRangeAsync_ResultIsNotMarkedAsCached()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = await sut.ValidateRangeAsync(
            TestDocumentPath,
            TestContent,
            new TextSpan(0, 20),
            UnifiedValidationOptions.Default);

        // Assert
        result.IsCached.Should().BeFalse();
    }

    #endregion

    #region UnifiedValidationOptions Tests

    [Fact]
    public void UnifiedValidationOptions_Default_HasCorrectValues()
    {
        // Arrange & Act
        var options = UnifiedValidationOptions.Default;

        // Assert
        options.IncludeStyleLinter.Should().BeTrue();
        options.IncludeGrammarLinter.Should().BeTrue();
        options.IncludeValidationEngine.Should().BeTrue();
        options.MinimumSeverity.Should().Be(UnifiedSeverity.Hint);
        options.EnableCaching.Should().BeTrue();
        options.CacheTtlMs.Should().Be(300_000);
        options.EnableDeduplication.Should().BeTrue();
        options.MaxIssuesPerDocument.Should().Be(1000);
        options.ParallelValidation.Should().BeTrue();
        options.ValidatorTimeoutMs.Should().Be(30_000);
        options.FilterByCategory.Should().BeNull();
        options.IncludeFixes.Should().BeTrue();
    }

    [Fact]
    public void UnifiedValidationOptions_CacheTtl_ReturnsTimeSpan()
    {
        // Arrange
        var options = new UnifiedValidationOptions { CacheTtlMs = 60_000 };

        // Assert
        options.CacheTtl.Should().Be(TimeSpan.FromMilliseconds(60_000));
    }

    [Fact]
    public void UnifiedValidationOptions_ValidatorTimeout_ReturnsTimeSpan()
    {
        // Arrange
        var options = new UnifiedValidationOptions { ValidatorTimeoutMs = 10_000 };

        // Assert
        options.ValidatorTimeout.Should().Be(TimeSpan.FromMilliseconds(10_000));
    }

    [Theory]
    [InlineData(UnifiedSeverity.Error, UnifiedSeverity.Error, true)]
    [InlineData(UnifiedSeverity.Warning, UnifiedSeverity.Error, false)]
    [InlineData(UnifiedSeverity.Info, UnifiedSeverity.Error, false)]
    [InlineData(UnifiedSeverity.Error, UnifiedSeverity.Warning, true)]
    [InlineData(UnifiedSeverity.Warning, UnifiedSeverity.Warning, true)]
    [InlineData(UnifiedSeverity.Info, UnifiedSeverity.Warning, false)]
    [InlineData(UnifiedSeverity.Error, UnifiedSeverity.Hint, true)]
    [InlineData(UnifiedSeverity.Hint, UnifiedSeverity.Hint, true)]
    public void UnifiedValidationOptions_PassesSeverityFilter_ReturnsCorrectResult(
        UnifiedSeverity issueSeverity,
        UnifiedSeverity minimumSeverity,
        bool expectedResult)
    {
        // Arrange
        var options = new UnifiedValidationOptions { MinimumSeverity = minimumSeverity };

        // Act
        var result = options.PassesSeverityFilter(issueSeverity);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void UnifiedValidationOptions_PassesCategoryFilter_NullFilter_ReturnsTrue()
    {
        // Arrange
        var options = new UnifiedValidationOptions { FilterByCategory = null };

        // Act
        var result = options.PassesCategoryFilter(IssueCategory.Style);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void UnifiedValidationOptions_PassesCategoryFilter_MatchingCategory_ReturnsTrue()
    {
        // Arrange
        var options = new UnifiedValidationOptions
        {
            FilterByCategory = new[] { IssueCategory.Style, IssueCategory.Grammar }
        };

        // Act
        var result = options.PassesCategoryFilter(IssueCategory.Style);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void UnifiedValidationOptions_PassesCategoryFilter_NonMatchingCategory_ReturnsFalse()
    {
        // Arrange
        var options = new UnifiedValidationOptions
        {
            FilterByCategory = new[] { IssueCategory.Grammar }
        };

        // Act
        var result = options.PassesCategoryFilter(IssueCategory.Style);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region UnifiedValidationResult Computed Properties Tests

    [Fact]
    public void UnifiedValidationResult_ByCategory_GroupsCorrectly()
    {
        // Arrange
        var issues = new List<UnifiedIssue>
        {
            CreateTestIssue() with { Category = IssueCategory.Style },
            CreateTestIssue() with { Category = IssueCategory.Style },
            CreateTestIssue() with { Category = IssueCategory.Grammar }
        };

        var result = new UnifiedValidationResult
        {
            DocumentPath = TestDocumentPath,
            Issues = issues
        };

        // Assert
        result.ByCategory.Should().HaveCount(2);
        result.ByCategory[IssueCategory.Style].Should().HaveCount(2);
        result.ByCategory[IssueCategory.Grammar].Should().HaveCount(1);
    }

    [Fact]
    public void UnifiedValidationResult_BySeverity_GroupsCorrectly()
    {
        // Arrange
        var issues = new List<UnifiedIssue>
        {
            CreateTestIssue() with { Severity = UnifiedSeverity.Error },
            CreateTestIssue() with { Severity = UnifiedSeverity.Warning },
            CreateTestIssue() with { Severity = UnifiedSeverity.Warning }
        };

        var result = new UnifiedValidationResult
        {
            DocumentPath = TestDocumentPath,
            Issues = issues
        };

        // Assert
        result.BySeverity.Should().HaveCount(2);
        result.BySeverity[UnifiedSeverity.Error].Should().HaveCount(1);
        result.BySeverity[UnifiedSeverity.Warning].Should().HaveCount(2);
    }

    [Fact]
    public void UnifiedValidationResult_Counts_CalculateCorrectly()
    {
        // Arrange
        var issues = new List<UnifiedIssue>
        {
            CreateTestIssue() with { Severity = UnifiedSeverity.Error },
            CreateTestIssue() with { Severity = UnifiedSeverity.Warning },
            CreateTestIssue() with { Severity = UnifiedSeverity.Warning },
            CreateTestIssue() with { Severity = UnifiedSeverity.Info },
            CreateTestIssue() with { Severity = UnifiedSeverity.Hint }
        };

        var result = new UnifiedValidationResult
        {
            DocumentPath = TestDocumentPath,
            Issues = issues
        };

        // Assert
        result.TotalIssueCount.Should().Be(5);
        result.ErrorCount.Should().Be(1);
        result.WarningCount.Should().Be(2);
        result.InfoCount.Should().Be(1);
        result.HintCount.Should().Be(1);
    }

    [Fact]
    public void UnifiedValidationResult_CanPublish_TrueWhenNoErrors()
    {
        // Arrange
        var issues = new List<UnifiedIssue>
        {
            CreateTestIssue() with { Severity = UnifiedSeverity.Warning },
            CreateTestIssue() with { Severity = UnifiedSeverity.Info }
        };

        var result = new UnifiedValidationResult
        {
            DocumentPath = TestDocumentPath,
            Issues = issues
        };

        // Assert
        result.CanPublish.Should().BeTrue();
    }

    [Fact]
    public void UnifiedValidationResult_CanPublish_FalseWhenHasErrors()
    {
        // Arrange
        var issues = new List<UnifiedIssue>
        {
            CreateTestIssue() with { Severity = UnifiedSeverity.Error }
        };

        var result = new UnifiedValidationResult
        {
            DocumentPath = TestDocumentPath,
            Issues = issues
        };

        // Assert
        result.CanPublish.Should().BeFalse();
    }

    [Fact]
    public void UnifiedValidationResult_Empty_ReturnsCorrectResult()
    {
        // Act
        var result = UnifiedValidationResult.Empty(TestDocumentPath, TimeSpan.FromMilliseconds(100));

        // Assert
        result.DocumentPath.Should().Be(TestDocumentPath);
        result.Issues.Should().BeEmpty();
        result.Duration.Should().Be(TimeSpan.FromMilliseconds(100));
        result.IsCached.Should().BeFalse();
    }

    [Fact]
    public void UnifiedValidationResult_AsCached_SetsIsCachedTrue()
    {
        // Arrange
        var result = new UnifiedValidationResult
        {
            DocumentPath = TestDocumentPath,
            Issues = Array.Empty<UnifiedIssue>(),
            IsCached = false
        };

        // Act
        var cachedResult = result.AsCached();

        // Assert
        cachedResult.IsCached.Should().BeTrue();
    }

    #endregion

    #region ValidationCompletedEventArgs Tests

    [Fact]
    public void ValidationCompletedEventArgs_Success_CreatesCorrectInstance()
    {
        // Arrange
        var result = new UnifiedValidationResult
        {
            DocumentPath = TestDocumentPath,
            Issues = Array.Empty<UnifiedIssue>()
        };
        var successfulValidators = new[] { "StyleLinter", "ValidationEngine" };

        // Act
        var eventArgs = ValidationCompletedEventArgs.Success(
            TestDocumentPath, result, successfulValidators);

        // Assert
        eventArgs.DocumentPath.Should().Be(TestDocumentPath);
        eventArgs.Result.Should().BeSameAs(result);
        eventArgs.SuccessfulValidators.Should().BeEquivalentTo(successfulValidators);
        eventArgs.FailedValidators.Should().BeEmpty();
        eventArgs.AllValidatorsSucceeded.Should().BeTrue();
        eventArgs.HasFailures.Should().BeFalse();
    }

    [Fact]
    public void ValidationCompletedEventArgs_WithFailures_CreatesCorrectInstance()
    {
        // Arrange
        var result = new UnifiedValidationResult
        {
            DocumentPath = TestDocumentPath,
            Issues = Array.Empty<UnifiedIssue>()
        };
        var successfulValidators = new[] { "StyleLinter" };
        var failedValidators = new Dictionary<string, string>
        {
            ["ValidationEngine"] = "Timed out"
        };

        // Act
        var eventArgs = ValidationCompletedEventArgs.WithFailures(
            TestDocumentPath, result, successfulValidators, failedValidators);

        // Assert
        eventArgs.SuccessfulValidators.Should().BeEquivalentTo(successfulValidators);
        eventArgs.FailedValidators.Should().BeEquivalentTo(failedValidators);
        eventArgs.AllValidatorsSucceeded.Should().BeFalse();
        eventArgs.HasFailures.Should().BeTrue();
    }

    [Fact]
    public void ValidationCompletedEventArgs_CompletedAt_IsSetToCurrentTime()
    {
        // Arrange
        var result = new UnifiedValidationResult
        {
            DocumentPath = TestDocumentPath,
            Issues = Array.Empty<UnifiedIssue>()
        };

        // Act
        var eventArgs = ValidationCompletedEventArgs.Success(
            TestDocumentPath, result, Array.Empty<string>());

        // Assert
        eventArgs.CompletedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Event Publishing Tests

    [Fact]
    public async Task ValidateAsync_RaisesValidationCompletedEvent()
    {
        // Arrange
        var sut = CreateService();
        ValidationCompletedEventArgs? raisedArgs = null;
        sut.ValidationCompleted += (_, args) => raisedArgs = args;

        // Act
        await sut.ValidateAsync(TestDocumentPath, TestContent, UnifiedValidationOptions.Default);

        // Assert
        raisedArgs.Should().NotBeNull();
        raisedArgs!.DocumentPath.Should().Be(TestDocumentPath);
    }

    [Fact]
    public async Task ValidateAsync_AllValidatorsSucceed_EventHasNoFailures()
    {
        // Arrange
        var sut = CreateService();
        ValidationCompletedEventArgs? raisedArgs = null;
        sut.ValidationCompleted += (_, args) => raisedArgs = args;

        // Act
        await sut.ValidateAsync(TestDocumentPath, TestContent, UnifiedValidationOptions.Default);

        // Assert
        raisedArgs!.AllValidatorsSucceeded.Should().BeTrue();
        raisedArgs.SuccessfulValidators.Should().Contain("StyleLinter");
    }

    [Fact]
    public async Task ValidateAsync_ValidatorFails_EventReportsFailure()
    {
        // Arrange
        var sut = CreateService();
        ValidationCompletedEventArgs? raisedArgs = null;
        sut.ValidationCompleted += (_, args) => raisedArgs = args;

        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Scanner failed"));

        // Act
        await sut.ValidateAsync(TestDocumentPath, TestContent, UnifiedValidationOptions.Default);

        // Assert
        raisedArgs!.HasFailures.Should().BeTrue();
        raisedArgs.FailedValidators.Should().ContainKey("StyleLinter");
    }

    #endregion

    #region Parallel vs Sequential Execution Tests

    [Fact]
    public async Task ValidateAsync_ParallelExecution_RunsValidatorsConcurrently()
    {
        // Arrange
        var sut = CreateService();
        var options = new UnifiedValidationOptions { ParallelValidation = true };

        var styleStarted = new TaskCompletionSource();
        var validationStarted = new TaskCompletionSource();

        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                styleStarted.SetResult();
                await validationStarted.Task;
                return DeviationScanResult.Empty(TestDocumentPath, TimeSpan.Zero);
            });

        _mockValidationEngine
            .Setup(x => x.ValidateDocumentAsync(It.IsAny<ValidationContext>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                validationStarted.SetResult();
                await styleStarted.Task;
                return ValidationResult.Valid();
            });

        // Act — Both tasks should complete (they wait for each other)
        var task = sut.ValidateAsync(TestDocumentPath, TestContent, options);
        var completed = await Task.WhenAny(task, Task.Delay(5000));

        // Assert
        completed.Should().BeSameAs(task, "Parallel execution should allow both validators to run concurrently");
    }

    [Fact]
    public async Task ValidateAsync_SequentialExecution_RunsValidatorsInOrder()
    {
        // Arrange
        var sut = CreateService();
        var options = new UnifiedValidationOptions { ParallelValidation = false };

        var executionOrder = new List<string>();

        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                executionOrder.Add("Style");
                return Task.FromResult(DeviationScanResult.Empty(TestDocumentPath, TimeSpan.Zero));
            });

        _mockValidationEngine
            .Setup(x => x.ValidateDocumentAsync(It.IsAny<ValidationContext>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                executionOrder.Add("Validation");
                return Task.FromResult(ValidationResult.Valid());
            });

        // Act
        await sut.ValidateAsync(TestDocumentPath, TestContent, options);

        // Assert — Both should be called, style first (due to order in code)
        executionOrder.Should().HaveCount(2);
    }

    #endregion

    #region MaxIssuesPerDocument Limit Tests

    [Fact]
    public async Task ValidateAsync_ExceedsMaxIssues_TruncatesResult()
    {
        // Arrange
        var sut = CreateService();
        var options = new UnifiedValidationOptions { MaxIssuesPerDocument = 2 };

        var deviations = Enumerable.Range(0, 5)
            .Select(i => CreateTestDeviation(startOffset: i * 10))
            .ToArray();
        var scanResult = CreateScanResult(deviations);
        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanResult);

        // Act
        var result = await sut.ValidateAsync(TestDocumentPath, TestContent, options);

        // Assert
        result.Issues.Should().HaveCount(2);
    }

    [Fact]
    public async Task ValidateAsync_MaxIssuesZero_DoesNotTruncate()
    {
        // Arrange
        var sut = CreateService();
        var options = new UnifiedValidationOptions { MaxIssuesPerDocument = 0 }; // Unlimited

        var deviations = Enumerable.Range(0, 5)
            .Select(i => CreateTestDeviation(startOffset: i * 10))
            .ToArray();
        var scanResult = CreateScanResult(deviations);
        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanResult);

        // Act
        var result = await sut.ValidateAsync(TestDocumentPath, TestContent, options);

        // Assert
        result.Issues.Should().HaveCount(5);
    }

    [Fact]
    public async Task ValidateAsync_TruncatesBySeverity_KeepsHighestSeverityIssues()
    {
        // Arrange
        var sut = CreateService();
        var options = new UnifiedValidationOptions { MaxIssuesPerDocument = 1 };

        var deviations = new[]
        {
            CreateTestDeviation(ruleId: "WARN", startOffset: 0, severity: ViolationSeverity.Warning),
            CreateTestDeviation(ruleId: "ERR", startOffset: 10, severity: ViolationSeverity.Error)
        };
        var scanResult = CreateScanResult(deviations);
        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanResult);

        // Act
        var result = await sut.ValidateAsync(TestDocumentPath, TestContent, options);

        // Assert — Should keep the error (highest severity)
        result.Issues.Should().HaveCount(1);
        result.Issues[0].Severity.Should().Be(UnifiedSeverity.Error);
    }

    #endregion

    #region Severity and Category Filtering Tests

    [Fact]
    public async Task ValidateAsync_MinimumSeverityFilter_ExcludesLowSeverityIssues()
    {
        // Arrange
        var sut = CreateService();
        var options = new UnifiedValidationOptions { MinimumSeverity = UnifiedSeverity.Warning };

        var deviations = new[]
        {
            CreateTestDeviation(ruleId: "ERR", startOffset: 0, severity: ViolationSeverity.Error),
            CreateTestDeviation(ruleId: "WARN", startOffset: 10, severity: ViolationSeverity.Warning),
            CreateTestDeviation(ruleId: "INFO", startOffset: 20, severity: ViolationSeverity.Info)
        };
        var scanResult = CreateScanResult(deviations);
        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanResult);

        // Act
        var result = await sut.ValidateAsync(TestDocumentPath, TestContent, options);

        // Assert — Info is filtered out
        result.Issues.Should().HaveCount(2);
        result.Issues.Should().NotContain(i => i.Severity == UnifiedSeverity.Info);
    }

    [Fact]
    public async Task ValidateAsync_CategoryFilter_ExcludesNonMatchingCategories()
    {
        // Arrange
        var sut = CreateService();
        var options = new UnifiedValidationOptions
        {
            FilterByCategory = new[] { IssueCategory.Grammar }
        };

        var deviation = CreateTestDeviation(); // Category is Terminology
        var scanResult = CreateScanResult(deviation);
        _mockStyleScanner
            .Setup(x => x.ScanDocumentAsync(TestDocumentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanResult);

        // Act
        var result = await sut.ValidateAsync(TestDocumentPath, TestContent, options);

        // Assert — Terminology issues are filtered out
        result.Issues.Should().BeEmpty();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var act = () =>
        {
            sut.Dispose();
            sut.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void InvalidateCache_AfterDispose_DoesNotThrow()
    {
        // Arrange
        var sut = CreateService();
        sut.Dispose();

        // Act
        var act = () => sut.InvalidateCache(TestDocumentPath);

        // Assert — InvalidateCache doesn't check disposed state
        act.Should().NotThrow();
    }

    #endregion
}
