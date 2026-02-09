// =============================================================================
// File: LinterIntegrationTests.cs
// Description: Unit tests for LinterIntegration (v0.6.5j).
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;
using Lexichord.Modules.Knowledge.Validation.Integration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge.Integration;

/// <summary>
/// Tests for <see cref="LinterIntegration"/>.
/// </summary>
/// <remarks>Feature: v0.6.5j — Linter Integration.</remarks>
[Trait("Feature", "v0.6.5j")]
public sealed class LinterIntegrationTests
{
    // =========================================================================
    // Fields
    // =========================================================================

    private readonly IValidationEngine _validationEngine;
    private readonly IStyleEngine _styleEngine;
    private readonly IUnifiedFindingAdapter _adapter;
    private readonly ICombinedFixWorkflow _fixWorkflow;
    private readonly LinterIntegration _integration;

    // =========================================================================
    // Constructor
    // =========================================================================

    public LinterIntegrationTests()
    {
        _validationEngine = Substitute.For<IValidationEngine>();
        _styleEngine = Substitute.For<IStyleEngine>();
        _adapter = Substitute.For<IUnifiedFindingAdapter>();
        _fixWorkflow = Substitute.For<ICombinedFixWorkflow>();

        var logger = Substitute.For<ILogger<LinterIntegration>>();
        _integration = new LinterIntegration(
            _validationEngine, _styleEngine, _adapter, _fixWorkflow, logger);
    }

    // =========================================================================
    // GetUnifiedFindingsAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetUnifiedFindingsAsync_BothEmpty_ReturnsEmptyResult()
    {
        // Arrange
        SetupValidationResult();
        SetupLinterResult();
        var options = UnifiedFindingOptions.Default();

        // Act
        var result = await _integration.GetUnifiedFindingsAsync("doc1", "content", options);

        // Assert
        result.Findings.Should().BeEmpty();
        result.ValidationCount.Should().Be(0);
        result.LinterCount.Should().Be(0);
        result.Status.Should().Be(UnifiedStatus.Pass);
    }

    [Fact]
    public async Task GetUnifiedFindingsAsync_ValidationOnly_IncludesValidationFindings()
    {
        // Arrange
        var vf = ValidationFinding.Error("schema", "SCH001", "Missing field");
        SetupValidationResult(vf);
        SetupLinterResult();

        var unified = CreateUnifiedFinding(FindingSource.Validation, UnifiedSeverity.Error, "SCH001");
        _adapter.FromValidationFinding(vf).Returns(unified);

        var options = new UnifiedFindingOptions { IncludeValidation = true, IncludeLinter = false };

        // Act
        var result = await _integration.GetUnifiedFindingsAsync("doc1", "content", options);

        // Assert
        result.Findings.Should().HaveCount(1);
        result.ValidationCount.Should().Be(1);
        result.LinterCount.Should().Be(0);
        result.Status.Should().Be(UnifiedStatus.Fail);
    }

    [Fact]
    public async Task GetUnifiedFindingsAsync_LinterOnly_IncludesLinterFindings()
    {
        // Arrange
        SetupValidationResult();
        var sv = CreateStyleViolation("no-jargon", ViolationSeverity.Warning);
        SetupLinterResult(sv);

        var unified = CreateUnifiedFinding(FindingSource.StyleLinter, UnifiedSeverity.Warning, "no-jargon");
        _adapter.FromStyleViolation(sv).Returns(unified);

        var options = new UnifiedFindingOptions { IncludeValidation = false, IncludeLinter = true };

        // Act
        var result = await _integration.GetUnifiedFindingsAsync("doc1", "content", options);

        // Assert
        result.Findings.Should().HaveCount(1);
        result.ValidationCount.Should().Be(0);
        result.LinterCount.Should().Be(1);
        result.Status.Should().Be(UnifiedStatus.PassWithWarnings);
    }

    [Fact]
    public async Task GetUnifiedFindingsAsync_BothSources_CombinesFindings()
    {
        // Arrange
        var vf = ValidationFinding.Warn("schema", "SCH001", "Warning");
        SetupValidationResult(vf);

        var sv = CreateStyleViolation("no-jargon", ViolationSeverity.Info);
        SetupLinterResult(sv);

        var uf1 = CreateUnifiedFinding(FindingSource.Validation, UnifiedSeverity.Warning, "SCH001");
        var uf2 = CreateUnifiedFinding(FindingSource.StyleLinter, UnifiedSeverity.Info, "no-jargon");
        _adapter.FromValidationFinding(vf).Returns(uf1);
        _adapter.FromStyleViolation(sv).Returns(uf2);

        var options = UnifiedFindingOptions.Default();

        // Act
        var result = await _integration.GetUnifiedFindingsAsync("doc1", "content", options);

        // Assert
        result.Findings.Should().HaveCount(2);
        result.ValidationCount.Should().Be(1);
        result.LinterCount.Should().Be(1);
    }

    [Fact]
    public async Task GetUnifiedFindingsAsync_SortsBySeverity()
    {
        // Arrange
        var vf = ValidationFinding.Information("schema", "SCH003", "Info finding");
        SetupValidationResult(vf);

        var sv = CreateStyleViolation("no-jargon", ViolationSeverity.Error);
        SetupLinterResult(sv);

        var ufInfo = CreateUnifiedFinding(FindingSource.Validation, UnifiedSeverity.Info, "SCH003");
        var ufError = CreateUnifiedFinding(FindingSource.StyleLinter, UnifiedSeverity.Error, "no-jargon");
        _adapter.FromValidationFinding(vf).Returns(ufInfo);
        _adapter.FromStyleViolation(sv).Returns(ufError);

        var options = UnifiedFindingOptions.Default();

        // Act
        var result = await _integration.GetUnifiedFindingsAsync("doc1", "content", options);

        // Assert — Error (0) should come before Info (2)
        result.Findings[0].Severity.Should().Be(UnifiedSeverity.Error);
        result.Findings[1].Severity.Should().Be(UnifiedSeverity.Info);
    }

    [Fact]
    public async Task GetUnifiedFindingsAsync_FiltersBySeverity()
    {
        // Arrange
        var vf1 = ValidationFinding.Error("schema", "SCH001", "Error");
        var vf2 = ValidationFinding.Information("schema", "SCH002", "Info");
        SetupValidationResult(vf1, vf2);
        SetupLinterResult();

        var ufError = CreateUnifiedFinding(FindingSource.Validation, UnifiedSeverity.Error, "SCH001");
        var ufInfo = CreateUnifiedFinding(FindingSource.Validation, UnifiedSeverity.Info, "SCH002");
        _adapter.FromValidationFinding(vf1).Returns(ufError);
        _adapter.FromValidationFinding(vf2).Returns(ufInfo);

        // Only include Error and Warning (exclude Info and Hint)
        var options = new UnifiedFindingOptions { MinSeverity = UnifiedSeverity.Warning };

        // Act
        var result = await _integration.GetUnifiedFindingsAsync("doc1", "content", options);

        // Assert — only Error should survive (Warning=1 < Info=2, so Info is excluded)
        result.Findings.Should().HaveCount(1);
        result.Findings[0].Severity.Should().Be(UnifiedSeverity.Error);
    }

    [Fact]
    public async Task GetUnifiedFindingsAsync_FiltersByCategory()
    {
        // Arrange
        var vf = ValidationFinding.Error("schema", "SCH001", "Schema error");
        SetupValidationResult(vf);

        var sv = CreateStyleViolation("no-jargon", ViolationSeverity.Warning);
        SetupLinterResult(sv);

        var ufSchema = CreateUnifiedFinding(FindingSource.Validation, UnifiedSeverity.Error, "SCH001",
            FindingCategory.Schema);
        var ufStyle = CreateUnifiedFinding(FindingSource.StyleLinter, UnifiedSeverity.Warning, "no-jargon",
            FindingCategory.Style);
        _adapter.FromValidationFinding(vf).Returns(ufSchema);
        _adapter.FromStyleViolation(sv).Returns(ufStyle);

        // Only include Schema findings
        var categories = new HashSet<FindingCategory> { FindingCategory.Schema };
        var options = new UnifiedFindingOptions { Categories = categories };

        // Act
        var result = await _integration.GetUnifiedFindingsAsync("doc1", "content", options);

        // Assert
        result.Findings.Should().HaveCount(1);
        result.Findings[0].Category.Should().Be(FindingCategory.Schema);
    }

    [Fact]
    public async Task GetUnifiedFindingsAsync_RespectsMaxFindings()
    {
        // Arrange — 3 validation findings, limit to 2
        var vf1 = ValidationFinding.Error("schema", "SCH001", "Error 1");
        var vf2 = ValidationFinding.Warn("schema", "SCH002", "Warning 1");
        var vf3 = ValidationFinding.Information("schema", "SCH003", "Info 1");
        SetupValidationResult(vf1, vf2, vf3);
        SetupLinterResult();

        _adapter.FromValidationFinding(vf1)
            .Returns(CreateUnifiedFinding(FindingSource.Validation, UnifiedSeverity.Error, "SCH001"));
        _adapter.FromValidationFinding(vf2)
            .Returns(CreateUnifiedFinding(FindingSource.Validation, UnifiedSeverity.Warning, "SCH002"));
        _adapter.FromValidationFinding(vf3)
            .Returns(CreateUnifiedFinding(FindingSource.Validation, UnifiedSeverity.Info, "SCH003"));

        var options = new UnifiedFindingOptions { MaxFindings = 2 };

        // Act
        var result = await _integration.GetUnifiedFindingsAsync("doc1", "content", options);

        // Assert
        result.Findings.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUnifiedFindingsAsync_ComputesByCategory()
    {
        // Arrange
        var vf = ValidationFinding.Error("schema", "SCH001", "Schema error");
        SetupValidationResult(vf);

        var sv = CreateStyleViolation("no-jargon", ViolationSeverity.Warning);
        SetupLinterResult(sv);

        _adapter.FromValidationFinding(vf)
            .Returns(CreateUnifiedFinding(FindingSource.Validation, UnifiedSeverity.Error, "SCH001",
                FindingCategory.Schema));
        _adapter.FromStyleViolation(sv)
            .Returns(CreateUnifiedFinding(FindingSource.StyleLinter, UnifiedSeverity.Warning, "no-jargon",
                FindingCategory.Style));

        var options = UnifiedFindingOptions.Default();

        // Act
        var result = await _integration.GetUnifiedFindingsAsync("doc1", "content", options);

        // Assert
        result.ByCategory.Should().ContainKey(FindingCategory.Schema).WhoseValue.Should().Be(1);
        result.ByCategory.Should().ContainKey(FindingCategory.Style).WhoseValue.Should().Be(1);
        result.BySeverity.Should().ContainKey(UnifiedSeverity.Error).WhoseValue.Should().Be(1);
        result.BySeverity.Should().ContainKey(UnifiedSeverity.Warning).WhoseValue.Should().Be(1);
    }

    [Fact]
    public async Task GetUnifiedFindingsAsync_ValidationFails_ReturnsLinterResults()
    {
        // Arrange — validation engine throws, linter returns findings
        _validationEngine.ValidateDocumentAsync(Arg.Any<ValidationContext>(), Arg.Any<CancellationToken>())
            .Returns<ValidationResult>(x => throw new InvalidOperationException("Validation failed"));

        var sv = CreateStyleViolation("no-jargon", ViolationSeverity.Warning);
        SetupLinterResult(sv);

        var unified = CreateUnifiedFinding(FindingSource.StyleLinter, UnifiedSeverity.Warning, "no-jargon");
        _adapter.FromStyleViolation(sv).Returns(unified);

        var options = UnifiedFindingOptions.Default();

        // Act
        var result = await _integration.GetUnifiedFindingsAsync("doc1", "content", options);

        // Assert — should still have the linter finding
        result.Findings.Should().HaveCount(1);
        result.LinterCount.Should().Be(1);
    }

    // =========================================================================
    // ApplyAllFixesAsync Tests
    // =========================================================================

    [Fact]
    public async Task ApplyAllFixesAsync_EmptyList_ReturnsEmptyResult()
    {
        // Act
        var result = await _integration.ApplyAllFixesAsync("content", []);

        // Assert
        result.Applied.Should().Be(0);
        result.Failed.Should().Be(0);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyAllFixesAsync_NoConflicts_AppliesAll()
    {
        // Arrange
        var fixes = new List<UnifiedFix>
        {
            new() { Source = FindingSource.Validation, Description = "Fix A" },
            new() { Source = FindingSource.StyleLinter, Description = "Fix B" }
        };
        _fixWorkflow.CheckForConflicts(fixes).Returns(FixConflictResult.None());
        _fixWorkflow.OrderFixesForApplication(fixes).Returns(fixes);

        // Act
        var result = await _integration.ApplyAllFixesAsync("content", fixes);

        // Assert
        result.Applied.Should().Be(2);
        result.Failed.Should().Be(0);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyAllFixesAsync_WithConflicts_AppliesNonConflicting()
    {
        // Arrange
        var sharedFindingId = Guid.NewGuid();
        var fixA = new UnifiedFix
        {
            Source = FindingSource.Validation, Description = "Fix A", FindingId = sharedFindingId
        };
        var fixB = new UnifiedFix
        {
            Source = FindingSource.StyleLinter, Description = "Fix B", FindingId = sharedFindingId
        };
        var fixC = new UnifiedFix
        {
            Source = FindingSource.Validation, Description = "Fix C", FindingId = Guid.NewGuid()
        };
        var fixes = new List<UnifiedFix> { fixA, fixB, fixC };

        var conflictResult = new FixConflictResult
        {
            Conflicts = [new FixConflict(fixA, fixB, "Same finding")]
        };
        _fixWorkflow.CheckForConflicts(fixes).Returns(conflictResult);
        _fixWorkflow.OrderFixesForApplication(Arg.Any<IReadOnlyList<UnifiedFix>>())
            .Returns(ci => ci.Arg<IReadOnlyList<UnifiedFix>>());

        // Act
        var result = await _integration.ApplyAllFixesAsync("content", fixes);

        // Assert — fixC should survive; fixA and fixB are conflicting
        result.Applied.Should().Be(1);
        result.Failed.Should().Be(2);
        result.Warnings.Should().HaveCount(1);
    }

    // =========================================================================
    // Helper Methods
    // =========================================================================

    /// <summary>Sets up validation engine to return specified findings.</summary>
    private void SetupValidationResult(params ValidationFinding[] findings)
    {
        var result = findings.Length > 0
            ? ValidationResult.WithFindings(
                findings,
                validatorsRun: 1,
                validatorsSkipped: 0,
                duration: TimeSpan.FromMilliseconds(10))
            : ValidationResult.Valid(
                validatorsRun: 1,
                validatorsSkipped: 0);

        _validationEngine.ValidateDocumentAsync(Arg.Any<ValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(result));
    }

    /// <summary>Sets up style engine to return specified violations.</summary>
    private void SetupLinterResult(params StyleViolation[] violations)
    {
        _styleEngine.AnalyzeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<StyleViolation>>(violations.ToList()));
    }

    /// <summary>Creates a test StyleViolation.</summary>
    private static StyleViolation CreateStyleViolation(string ruleId, ViolationSeverity severity)
    {
        var rule = new StyleRule(
            ruleId, ruleId, "Test description",
            RuleCategory.Terminology, severity,
            "test", PatternType.Literal, "replacement");

        return new StyleViolation(
            rule, $"Test: {ruleId}",
            StartOffset: 0, EndOffset: 4,
            StartLine: 1, StartColumn: 1,
            EndLine: 1, EndColumn: 5,
            MatchedText: "test", Suggestion: "replacement",
            Severity: severity);
    }

    /// <summary>Creates a test UnifiedFinding.</summary>
    private static UnifiedFinding CreateUnifiedFinding(
        FindingSource source,
        UnifiedSeverity severity,
        string code,
        FindingCategory category = FindingCategory.Schema)
    {
        return new UnifiedFinding
        {
            Source = source,
            Severity = severity,
            Code = code,
            Message = $"Test: {code}",
            Category = category
        };
    }
}
