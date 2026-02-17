// -----------------------------------------------------------------------
// <copyright file="IssueFilterServiceTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using FluentAssertions;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;
using Lexichord.Abstractions.Contracts.Validation;
using Lexichord.Modules.Agents.Tuning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

// Alias to disambiguate from Integration.UnifiedFix
using UnifiedFix = Lexichord.Abstractions.Contracts.Validation.UnifiedFix;

namespace Lexichord.Tests.Unit.Modules.Agents.Tuning;

/// <summary>
/// Comprehensive unit tests for <see cref="IssueFilterService"/>.
/// </summary>
/// <remarks>
/// <para>
/// Test Groups:
/// <list type="bullet">
///   <item><description>Constructor — Null argument validation</description></item>
///   <item><description>FilterAsync — Category, severity, code/exclude, text search, location, line range, auto-fixable, manual, validator, pagination, combined</description></item>
///   <item><description>SearchAsync — Message, code, validator, case-insensitive, with options</description></item>
///   <item><description>FilterByCategoryAsync — Single/multiple categories</description></item>
///   <item><description>FilterBySeverityAsync — Each level, boundary values</description></item>
///   <item><description>FilterByLocationAsync — Containment, partial overlap</description></item>
///   <item><description>FilterByLineRangeAsync — Valid and invalid ranges</description></item>
///   <item><description>SortAsync — Each criterion, multi-criteria, ascending/descending</description></item>
///   <item><description>CountBySeverity / CountByCategory — Grouping correctness</description></item>
///   <item><description>Presets — Save, load, list, delete, overwrite, defaults</description></item>
///   <item><description>FilterCriteria — Each subclass Matches method</description></item>
///   <item><description>Edge cases — Empty, conflicting filters, null handling</description></item>
///   <item><description>Performance — 1000 issues filter target</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5i as part of the Issue Filters feature.
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.5i")]
public class IssueFilterServiceTests
{
    private readonly IIssueFilterService _sut;

    public IssueFilterServiceTests()
    {
        _sut = new IssueFilterService(NullLogger<IssueFilterService>.Instance);
    }

    // ── Test Data Helpers ─────────────────────────────────────────────────

    private static UnifiedIssue CreateIssue(
        IssueCategory category = IssueCategory.Style,
        UnifiedSeverity severity = UnifiedSeverity.Warning,
        string sourceId = "STYLE_001",
        string message = "Test issue",
        int start = 0,
        int length = 10,
        string? sourceType = "StyleLinter",
        bool canAutoFix = false)
    {
        var fixes = canAutoFix
            ? new[] { UnifiedFix.Replacement(new TextSpan(start, length), "old", "new", "Fix", 0.9) }
            : Array.Empty<UnifiedFix>();

        return new UnifiedIssue(
            IssueId: Guid.NewGuid(),
            SourceId: sourceId,
            Category: category,
            Severity: severity,
            Message: message,
            Location: new TextSpan(start, length),
            OriginalText: "original",
            Fixes: fixes,
            SourceType: sourceType,
            OriginalSource: null);
    }

    private static IReadOnlyList<UnifiedIssue> CreateMixedIssues()
    {
        return new[]
        {
            CreateIssue(IssueCategory.Style, UnifiedSeverity.Error, "STYLE_001", "Use active voice", 0, 20, "StyleLinter", true),
            CreateIssue(IssueCategory.Style, UnifiedSeverity.Warning, "STYLE_002", "Avoid passive voice", 100, 15, "StyleLinter", false),
            CreateIssue(IssueCategory.Grammar, UnifiedSeverity.Error, "GRAM_001", "Missing comma", 200, 5, "GrammarLinter", true),
            CreateIssue(IssueCategory.Grammar, UnifiedSeverity.Info, "GRAM_002", "Consider semicolon", 300, 8, "GrammarLinter", false),
            CreateIssue(IssueCategory.Knowledge, UnifiedSeverity.Warning, "AXIOM_001", "Term not in knowledge base", 400, 12, "Validation", true),
            CreateIssue(IssueCategory.Knowledge, UnifiedSeverity.Hint, "AXIOM_002", "Deprecated term usage", 500, 10, "Validation", false),
            CreateIssue(IssueCategory.Structure, UnifiedSeverity.Info, "STRUCT_001", "Heading level skip", 600, 25, "Validation", false),
            CreateIssue(IssueCategory.Custom, UnifiedSeverity.Hint, "CUSTOM_001", "Custom rule violation", 700, 30, "StyleLinter", true),
        };
    }

    // ── Constructor Tests ─────────────────────────────────────────────────

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new IssueFilterService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_InitializesDefaultPresets()
    {
        // Arrange & Act
        var service = new IssueFilterService(NullLogger<IssueFilterService>.Instance);

        // Assert
        var presets = service.ListPresets();
        presets.Should().Contain("Errors Only");
        presets.Should().Contain("Warnings and Errors");
        presets.Should().Contain("Auto-Fixable Only");
        presets.Should().Contain("Style Issues");
        presets.Should().Contain("Grammar Issues");
        presets.Should().Contain("Knowledge Issues");
        presets.Should().HaveCount(6);
    }

    #endregion

    // ── FilterAsync Tests ─────────────────────────────────────────────────

    #region FilterAsync Tests

    [Fact]
    public async Task FilterAsync_WithDefaultOptions_ReturnsAllIssues()
    {
        // Arrange
        var issues = CreateMixedIssues();

        // Act
        var result = await _sut.FilterAsync(issues, IssueFilterOptions.Default);

        // Assert
        result.Should().HaveCount(issues.Count);
    }

    [Fact]
    public async Task FilterAsync_WithNullIssues_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => _sut.FilterAsync(null!, IssueFilterOptions.Default);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("issues");
    }

    [Fact]
    public async Task FilterAsync_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => _sut.FilterAsync(Array.Empty<UnifiedIssue>(), null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public async Task FilterAsync_ByCategoryStyle_ReturnsOnlyStyleIssues()
    {
        // Arrange
        var issues = CreateMixedIssues();
        var options = new IssueFilterOptions { Categories = [IssueCategory.Style] };

        // Act
        var result = await _sut.FilterAsync(issues, options);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(i => i.Category == IssueCategory.Style);
    }

    [Fact]
    public async Task FilterAsync_ByMultipleCategories_ReturnsMatchingIssues()
    {
        // Arrange
        var issues = CreateMixedIssues();
        var options = new IssueFilterOptions { Categories = [IssueCategory.Style, IssueCategory.Grammar] };

        // Act
        var result = await _sut.FilterAsync(issues, options);

        // Assert
        result.Should().HaveCount(4);
        result.Should().OnlyContain(i => i.Category == IssueCategory.Style || i.Category == IssueCategory.Grammar);
    }

    [Fact]
    public async Task FilterAsync_BySeverityWarning_ReturnsErrorAndWarningOnly()
    {
        // Arrange
        var issues = CreateMixedIssues();
        // MinimumSeverity=Warning means include Error(0) and Warning(1)
        var options = new IssueFilterOptions { MinimumSeverity = UnifiedSeverity.Warning };

        // Act
        var result = await _sut.FilterAsync(issues, options);

        // Assert — Error=0, Warning=1 pass; Info=2, Hint=3 don't
        result.Should().OnlyContain(i =>
            i.Severity == UnifiedSeverity.Error || i.Severity == UnifiedSeverity.Warning);
        result.Should().HaveCount(4); // 2 errors + 2 warnings
    }

    [Fact]
    public async Task FilterAsync_BySeverityError_ReturnsOnlyErrors()
    {
        // Arrange
        var issues = CreateMixedIssues();
        var options = new IssueFilterOptions { MinimumSeverity = UnifiedSeverity.Error };

        // Act
        var result = await _sut.FilterAsync(issues, options);

        // Assert
        result.Should().OnlyContain(i => i.Severity == UnifiedSeverity.Error);
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task FilterAsync_BySeverityRange_ReturnsOnlyWarningAndInfo()
    {
        // Arrange
        var issues = CreateMixedIssues();
        // MaximumSeverity=Warning(1) excludes Error(0)
        // MinimumSeverity=Info(2) excludes Hint(3)
        var options = new IssueFilterOptions
        {
            MaximumSeverity = UnifiedSeverity.Warning,
            MinimumSeverity = UnifiedSeverity.Info
        };

        // Act
        var result = await _sut.FilterAsync(issues, options);

        // Assert
        result.Should().OnlyContain(i =>
            i.Severity == UnifiedSeverity.Warning || i.Severity == UnifiedSeverity.Info);
    }

    [Fact]
    public async Task FilterAsync_ByIssueCode_ReturnsMatchingCodes()
    {
        // Arrange
        var issues = CreateMixedIssues();
        var options = new IssueFilterOptions { IssueCodes = ["STYLE_001"] };

        // Act
        var result = await _sut.FilterAsync(issues, options);

        // Assert
        result.Should().HaveCount(1);
        result[0].SourceId.Should().Be("STYLE_001");
    }

    [Fact]
    public async Task FilterAsync_ByIssueCodeWildcard_ReturnsMatchingPattern()
    {
        // Arrange
        var issues = CreateMixedIssues();
        var options = new IssueFilterOptions { IssueCodes = ["STYLE_*"] };

        // Act
        var result = await _sut.FilterAsync(issues, options);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(i => i.SourceId.StartsWith("STYLE_"));
    }

    [Fact]
    public async Task FilterAsync_ByExcludeCode_ExcludesMatchingCodes()
    {
        // Arrange
        var issues = CreateMixedIssues();
        var options = new IssueFilterOptions { ExcludeCodes = ["STYLE_*"] };

        // Act
        var result = await _sut.FilterAsync(issues, options);

        // Assert
        result.Should().HaveCount(6);
        result.Should().NotContain(i => i.SourceId.StartsWith("STYLE_"));
    }

    [Fact]
    public async Task FilterAsync_BySearchText_MatchesMessage()
    {
        // Arrange
        var issues = CreateMixedIssues();
        var options = new IssueFilterOptions { SearchText = "voice" };

        // Act
        var result = await _sut.FilterAsync(issues, options);

        // Assert
        result.Should().HaveCount(2); // "Use active voice" and "Avoid passive voice"
    }

    [Fact]
    public async Task FilterAsync_BySearchText_CaseInsensitive()
    {
        // Arrange
        var issues = CreateMixedIssues();
        var options = new IssueFilterOptions { SearchText = "VOICE" };

        // Act
        var result = await _sut.FilterAsync(issues, options);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task FilterAsync_BySearchText_MatchesSourceId()
    {
        // Arrange
        var issues = CreateMixedIssues();
        var options = new IssueFilterOptions { SearchText = "AXIOM" };

        // Act
        var result = await _sut.FilterAsync(issues, options);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(i => i.SourceId.Contains("AXIOM"));
    }

    [Fact]
    public async Task FilterAsync_BySearchText_MatchesSourceType()
    {
        // Arrange
        var issues = CreateMixedIssues();
        var options = new IssueFilterOptions { SearchText = "GrammarLinter" };

        // Act
        var result = await _sut.FilterAsync(issues, options);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(i => i.SourceType == "GrammarLinter");
    }

    [Fact]
    public async Task FilterAsync_ByLocation_ReturnsContainedIssues()
    {
        // Arrange
        var issues = CreateMixedIssues();
        // Location span 0-250 should contain issues at 0-20, 100-115, 200-205
        var options = new IssueFilterOptions { LocationSpan = new TextSpan(0, 250) };

        // Act
        var result = await _sut.FilterAsync(issues, options);

        // Assert
        result.Should().HaveCount(3);
        result.Should().OnlyContain(i => i.Location.Start >= 0 && i.Location.End <= 250);
    }

    [Fact]
    public async Task FilterAsync_ByAutoFixable_ReturnsOnlyAutoFixable()
    {
        // Arrange
        var issues = CreateMixedIssues();
        var options = new IssueFilterOptions { OnlyAutoFixable = true };

        // Act
        var result = await _sut.FilterAsync(issues, options);

        // Assert
        result.Should().HaveCount(4); // STYLE_001, GRAM_001, AXIOM_001, CUSTOM_001
        result.Should().OnlyContain(i => i.CanAutoFix);
    }

    [Fact]
    public async Task FilterAsync_ByManual_ReturnsOnlyManualIssues()
    {
        // Arrange
        var issues = CreateMixedIssues();
        var options = new IssueFilterOptions { OnlyManual = true };

        // Act
        var result = await _sut.FilterAsync(issues, options);

        // Assert
        result.Should().HaveCount(4);
        result.Should().OnlyContain(i => !i.CanAutoFix);
    }

    [Fact]
    public async Task FilterAsync_BothAutoFixableAndManual_ReturnsEmpty()
    {
        // Arrange — conflicting filters
        var issues = CreateMixedIssues();
        var options = new IssueFilterOptions { OnlyAutoFixable = true, OnlyManual = true };

        // Act
        var result = await _sut.FilterAsync(issues, options);

        // Assert — no issue can be both auto-fixable and manual
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FilterAsync_ByValidatorName_ReturnsMatchingValidator()
    {
        // Arrange
        var issues = CreateMixedIssues();
        var options = new IssueFilterOptions { ValidatorNames = ["Validation"] };

        // Act
        var result = await _sut.FilterAsync(issues, options);

        // Assert
        result.Should().HaveCount(3); // AXIOM_001, AXIOM_002, STRUCT_001
        result.Should().OnlyContain(i => i.SourceType == "Validation");
    }

    [Fact]
    public async Task FilterAsync_WithPagination_ReturnsCorrectSubset()
    {
        // Arrange
        var issues = CreateMixedIssues();
        var options = new IssueFilterOptions { Offset = 2, Limit = 3 };

        // Act
        var result = await _sut.FilterAsync(issues, options);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task FilterAsync_WithLimit_TruncatesResults()
    {
        // Arrange
        var issues = CreateMixedIssues();
        var options = new IssueFilterOptions { Limit = 2 };

        // Act
        var result = await _sut.FilterAsync(issues, options);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task FilterAsync_WithSorting_SortsBySeverity()
    {
        // Arrange
        var issues = CreateMixedIssues();
        var options = new IssueFilterOptions { SortBy = [SortCriteria.Severity] };

        // Act
        var result = await _sut.FilterAsync(issues, options);

        // Assert — Error(0) first, Hint(3) last
        result.Should().HaveCount(8);
        result[0].Severity.Should().Be(UnifiedSeverity.Error);
        result[1].Severity.Should().Be(UnifiedSeverity.Error);
        result[^1].Severity.Should().Be(UnifiedSeverity.Hint);
    }

    [Fact]
    public async Task FilterAsync_CombinedFilters_AppliesAllCriteria()
    {
        // Arrange — Style category + Warning+ severity + auto-fixable
        var issues = CreateMixedIssues();
        var options = new IssueFilterOptions
        {
            Categories = [IssueCategory.Style],
            MinimumSeverity = UnifiedSeverity.Warning,
            OnlyAutoFixable = true
        };

        // Act
        var result = await _sut.FilterAsync(issues, options);

        // Assert — only STYLE_001 (Error, auto-fixable, Style)
        result.Should().HaveCount(1);
        result[0].SourceId.Should().Be("STYLE_001");
    }

    [Fact]
    public async Task FilterAsync_EmptyIssues_ReturnsEmpty()
    {
        // Act
        var result = await _sut.FilterAsync(Array.Empty<UnifiedIssue>(), IssueFilterOptions.Default);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FilterAsync_ByLineRange_ReturnsIssuesInRange()
    {
        // Arrange — line range 1-3 (lines are ~80 chars, so chars 0-239)
        var issues = CreateMixedIssues();
        var options = new IssueFilterOptions { LineRange = (1, 3) };

        // Act
        var result = await _sut.FilterAsync(issues, options);

        // Assert — issues at positions 0-20 (line 1), 100-115 (line 2), 200-205 (line 3) match
        result.Should().HaveCount(3);
    }

    #endregion

    // ── SearchAsync Tests ─────────────────────────────────────────────────

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_ByMessage_FindsMatchingIssues()
    {
        // Arrange
        var issues = CreateMixedIssues();

        // Act
        var result = await _sut.SearchAsync(issues, "comma");

        // Assert
        result.Should().HaveCount(1);
        result[0].Message.Should().Contain("comma");
    }

    [Fact]
    public async Task SearchAsync_WithAdditionalOptions_CombinesFilters()
    {
        // Arrange
        var issues = CreateMixedIssues();
        var options = new IssueFilterOptions { Categories = [IssueCategory.Style] };

        // Act — search for "voice" but only in Style category
        var result = await _sut.SearchAsync(issues, "voice", options);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(i => i.Category == IssueCategory.Style);
    }

    [Fact]
    public async Task SearchAsync_WithNullIssues_ThrowsArgumentNullException()
    {
        var act = () => _sut.SearchAsync(null!, "query");
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("issues");
    }

    [Fact]
    public async Task SearchAsync_WithNullQuery_ThrowsArgumentNullException()
    {
        var act = () => _sut.SearchAsync(Array.Empty<UnifiedIssue>(), null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("query");
    }

    #endregion

    // ── FilterByCategoryAsync Tests ───────────────────────────────────────

    #region FilterByCategoryAsync Tests

    [Fact]
    public async Task FilterByCategoryAsync_SingleCategory_ReturnsOnlyMatching()
    {
        // Arrange
        var issues = CreateMixedIssues();

        // Act
        var result = await _sut.FilterByCategoryAsync(issues, [IssueCategory.Grammar]);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(i => i.Category == IssueCategory.Grammar);
    }

    [Fact]
    public async Task FilterByCategoryAsync_MultipleCategories_ReturnsAll()
    {
        // Arrange
        var issues = CreateMixedIssues();

        // Act
        var result = await _sut.FilterByCategoryAsync(
            issues, [IssueCategory.Style, IssueCategory.Knowledge]);

        // Assert
        result.Should().HaveCount(4);
    }

    #endregion

    // ── FilterBySeverityAsync Tests ───────────────────────────────────────

    #region FilterBySeverityAsync Tests

    [Theory]
    [InlineData(UnifiedSeverity.Error, 2)]    // Only Error(0)
    [InlineData(UnifiedSeverity.Warning, 4)]  // Error(0) + Warning(1)
    [InlineData(UnifiedSeverity.Info, 6)]     // Error + Warning + Info
    [InlineData(UnifiedSeverity.Hint, 8)]     // All
    public async Task FilterBySeverityAsync_RespectsMinimumSeverity(
        UnifiedSeverity minimumSeverity, int expectedCount)
    {
        // Arrange
        var issues = CreateMixedIssues();

        // Act
        var result = await _sut.FilterBySeverityAsync(issues, minimumSeverity);

        // Assert
        result.Should().HaveCount(expectedCount);
        result.Should().OnlyContain(i => (int)i.Severity <= (int)minimumSeverity);
    }

    #endregion

    // ── FilterByLocationAsync Tests ───────────────────────────────────────

    #region FilterByLocationAsync Tests

    [Fact]
    public async Task FilterByLocationAsync_StrictContainment_ReturnsFullyContained()
    {
        // Arrange
        var issues = CreateMixedIssues();
        var location = new TextSpan(90, 130); // covers 90-220

        // Act
        var result = await _sut.FilterByLocationAsync(issues, location, includePartialOverlaps: false);

        // Assert — only issues fully within 90-220
        result.Should().OnlyContain(i => i.Location.Start >= 90 && i.Location.End <= 220);
    }

    [Fact]
    public async Task FilterByLocationAsync_WithPartialOverlaps_IncludesOverlapping()
    {
        // Arrange
        var issues = CreateMixedIssues();
        // Location 95-110 partially overlaps with issue at 100-115
        var location = new TextSpan(95, 15); // covers 95-110

        // Act
        var result = await _sut.FilterByLocationAsync(issues, location, includePartialOverlaps: true);

        // Assert — should include issue at 100-115 (partial overlap)
        result.Should().Contain(i => i.Location.Start == 100);
    }

    [Fact]
    public async Task FilterByLocationAsync_NoOverlap_ReturnsEmpty()
    {
        // Arrange
        var issues = CreateMixedIssues();
        var location = new TextSpan(1000, 50); // no issues in range 1000-1050

        // Act
        var result = await _sut.FilterByLocationAsync(issues, location);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    // ── FilterByLineRangeAsync Tests ──────────────────────────────────────

    #region FilterByLineRangeAsync Tests

    [Fact]
    public async Task FilterByLineRangeAsync_ValidRange_ReturnsMatchingIssues()
    {
        // Arrange
        var issues = CreateMixedIssues();

        // Act — line 1 corresponds to chars 0-79
        var result = await _sut.FilterByLineRangeAsync(issues, 1, 1);

        // Assert — only issues starting in first 80 chars (line 1)
        result.Should().HaveCount(1);
        result[0].Location.Start.Should().BeLessThan(80);
    }

    [Fact]
    public async Task FilterByLineRangeAsync_WideRange_ReturnsAll()
    {
        // Arrange
        var issues = CreateMixedIssues();

        // Act — line range 1-100 should include all
        var result = await _sut.FilterByLineRangeAsync(issues, 1, 100);

        // Assert
        result.Should().HaveCount(issues.Count);
    }

    #endregion

    // ── SortAsync Tests ───────────────────────────────────────────────────

    #region SortAsync Tests

    [Fact]
    public async Task SortAsync_BySeverity_OrdersMostSevereFirst()
    {
        // Arrange
        var issues = CreateMixedIssues();

        // Act
        var result = await _sut.SortAsync(issues, [SortCriteria.Severity]);

        // Assert — Error(0) first, then Warning(1), Info(2), Hint(3)
        result[0].Severity.Should().Be(UnifiedSeverity.Error);
        result[1].Severity.Should().Be(UnifiedSeverity.Error);
        result[2].Severity.Should().Be(UnifiedSeverity.Warning);
        result[3].Severity.Should().Be(UnifiedSeverity.Warning);
        result[^1].Severity.Should().Be(UnifiedSeverity.Hint);
    }

    [Fact]
    public async Task SortAsync_ByLocation_OrdersByPosition()
    {
        // Arrange
        var issues = CreateMixedIssues();

        // Act
        var result = await _sut.SortAsync(issues, [SortCriteria.Location]);

        // Assert — ascending by start position
        for (var i = 1; i < result.Count; i++)
        {
            result[i].Location.Start.Should().BeGreaterThanOrEqualTo(result[i - 1].Location.Start);
        }
    }

    [Fact]
    public async Task SortAsync_ByCategory_OrdersByCategoryValue()
    {
        // Arrange
        var issues = CreateMixedIssues();

        // Act
        var result = await _sut.SortAsync(issues, [SortCriteria.Category]);

        // Assert — Style(0) first, Custom(4) last
        result[0].Category.Should().Be(IssueCategory.Style);
        result[^1].Category.Should().Be(IssueCategory.Custom);
    }

    [Fact]
    public async Task SortAsync_ByMessage_OrdersAlphabetically()
    {
        // Arrange
        var issues = CreateMixedIssues();

        // Act
        var result = await _sut.SortAsync(issues, [SortCriteria.Message]);

        // Assert
        for (var i = 1; i < result.Count; i++)
        {
            string.Compare(result[i].Message, result[i - 1].Message, StringComparison.Ordinal)
                .Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    public async Task SortAsync_ByCode_OrdersBySourceId()
    {
        // Arrange
        var issues = CreateMixedIssues();

        // Act
        var result = await _sut.SortAsync(issues, [SortCriteria.Code]);

        // Assert — alphabetical by SourceId
        for (var i = 1; i < result.Count; i++)
        {
            string.Compare(result[i].SourceId, result[i - 1].SourceId, StringComparison.Ordinal)
                .Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    public async Task SortAsync_ByAutoFixable_AutoFixableFirst()
    {
        // Arrange
        var issues = CreateMixedIssues();

        // Act
        var result = await _sut.SortAsync(issues, [SortCriteria.AutoFixable]);

        // Assert — auto-fixable issues come first (sort key: 0 for true, 1 for false)
        var firstNonFixable = result.ToList().FindIndex(i => !i.CanAutoFix);
        if (firstNonFixable > 0)
        {
            result.Take(firstNonFixable).Should().OnlyContain(i => i.CanAutoFix);
        }
    }

    [Fact]
    public async Task SortAsync_MultiCriteria_AppliesInOrder()
    {
        // Arrange
        var issues = CreateMixedIssues();

        // Act — sort by severity (primary), then location (secondary)
        var result = await _sut.SortAsync(issues, [SortCriteria.Severity, SortCriteria.Location]);

        // Assert — within same severity, sorted by location
        var errors = result.Where(i => i.Severity == UnifiedSeverity.Error).ToList();
        if (errors.Count > 1)
        {
            errors[0].Location.Start.Should().BeLessThanOrEqualTo(errors[1].Location.Start);
        }
    }

    [Fact]
    public async Task SortAsync_EmptyCriteria_PreservesOrder()
    {
        // Arrange
        var issues = CreateMixedIssues();

        // Act
        var result = await _sut.SortAsync(issues, Array.Empty<SortCriteria>());

        // Assert — same as input
        result.Should().BeEquivalentTo(issues, config => config.WithStrictOrdering());
    }

    [Fact]
    public async Task SortAsync_Descending_ReversesSortOrder()
    {
        // Arrange
        var issues = CreateMixedIssues();
        var options = new IssueFilterOptions
        {
            SortBy = [SortCriteria.Severity],
            SortAscending = false
        };

        // Act
        var result = await _sut.FilterAsync(issues, options);

        // Assert — Hint(3) first, Error(0) last in descending
        result[0].Severity.Should().Be(UnifiedSeverity.Hint);
        result[^1].Severity.Should().Be(UnifiedSeverity.Error);
    }

    #endregion

    // ── CountBySeverity / CountByCategory Tests ───────────────────────────

    #region Count Tests

    [Fact]
    public void CountBySeverity_ReturnsCorrectCounts()
    {
        // Arrange
        var issues = CreateMixedIssues();

        // Act
        var counts = _sut.CountBySeverity(issues);

        // Assert
        counts[UnifiedSeverity.Error].Should().Be(2);
        counts[UnifiedSeverity.Warning].Should().Be(2);
        counts[UnifiedSeverity.Info].Should().Be(2);
        counts[UnifiedSeverity.Hint].Should().Be(2);
    }

    [Fact]
    public void CountByCategory_ReturnsCorrectCounts()
    {
        // Arrange
        var issues = CreateMixedIssues();

        // Act
        var counts = _sut.CountByCategory(issues);

        // Assert
        counts[IssueCategory.Style].Should().Be(2);
        counts[IssueCategory.Grammar].Should().Be(2);
        counts[IssueCategory.Knowledge].Should().Be(2);
        counts[IssueCategory.Structure].Should().Be(1);
        counts[IssueCategory.Custom].Should().Be(1);
    }

    [Fact]
    public void CountBySeverity_EmptyList_ReturnsEmptyDictionary()
    {
        // Act
        var counts = _sut.CountBySeverity(Array.Empty<UnifiedIssue>());

        // Assert
        counts.Should().BeEmpty();
    }

    [Fact]
    public void CountByCategory_EmptyList_ReturnsEmptyDictionary()
    {
        // Act
        var counts = _sut.CountByCategory(Array.Empty<UnifiedIssue>());

        // Assert
        counts.Should().BeEmpty();
    }

    [Fact]
    public void CountBySeverity_WithNullIssues_ThrowsArgumentNullException()
    {
        var act = () => _sut.CountBySeverity(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("issues");
    }

    [Fact]
    public void CountByCategory_WithNullIssues_ThrowsArgumentNullException()
    {
        var act = () => _sut.CountByCategory(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("issues");
    }

    #endregion

    // ── Preset Tests ──────────────────────────────────────────────────────

    #region Preset Tests

    [Fact]
    public void SavePreset_AllowsLaterLoad()
    {
        // Arrange
        var options = new IssueFilterOptions { MinimumSeverity = UnifiedSeverity.Error };

        // Act
        _sut.SavePreset("MyFilter", options);
        var loaded = _sut.LoadPreset("MyFilter");

        // Assert
        loaded.Should().NotBeNull();
        loaded!.MinimumSeverity.Should().Be(UnifiedSeverity.Error);
    }

    [Fact]
    public void SavePreset_OverwritesExisting()
    {
        // Arrange
        _sut.SavePreset("Test", new IssueFilterOptions { MinimumSeverity = UnifiedSeverity.Error });

        // Act
        _sut.SavePreset("Test", new IssueFilterOptions { MinimumSeverity = UnifiedSeverity.Warning });
        var loaded = _sut.LoadPreset("Test");

        // Assert
        loaded!.MinimumSeverity.Should().Be(UnifiedSeverity.Warning);
    }

    [Fact]
    public void LoadPreset_NotFound_ReturnsNull()
    {
        // Act
        var result = _sut.LoadPreset("NonExistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ListPresets_ReturnsAllPresetNames()
    {
        // Act
        var presets = _sut.ListPresets();

        // Assert
        presets.Should().HaveCountGreaterThanOrEqualTo(6);
    }

    [Fact]
    public void ListPresets_ReturnsSortedNames()
    {
        // Act
        var presets = _sut.ListPresets();

        // Assert
        presets.Should().BeInAscendingOrder();
    }

    [Fact]
    public void DeletePreset_ExistingPreset_ReturnsTrue()
    {
        // Arrange
        _sut.SavePreset("ToDelete", IssueFilterOptions.Default);

        // Act
        var result = _sut.DeletePreset("ToDelete");

        // Assert
        result.Should().BeTrue();
        _sut.LoadPreset("ToDelete").Should().BeNull();
    }

    [Fact]
    public void DeletePreset_NonExistent_ReturnsFalse()
    {
        // Act
        var result = _sut.DeletePreset("NonExistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SavePreset_WithNullName_ThrowsArgumentNullException()
    {
        var act = () => _sut.SavePreset(null!, IssueFilterOptions.Default);
        act.Should().Throw<ArgumentNullException>().WithParameterName("name");
    }

    [Fact]
    public void SavePreset_WithEmptyName_ThrowsArgumentException()
    {
        var act = () => _sut.SavePreset("", IssueFilterOptions.Default);
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void SavePreset_WithNullOptions_ThrowsArgumentNullException()
    {
        var act = () => _sut.SavePreset("Test", null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public async Task LoadPreset_ErrorsOnly_FiltersCorrectly()
    {
        // Arrange
        var issues = CreateMixedIssues();
        var preset = _sut.LoadPreset("Errors Only");

        // Act
        var result = await _sut.FilterAsync(issues, preset!);

        // Assert
        result.Should().OnlyContain(i => i.Severity == UnifiedSeverity.Error);
    }

    [Fact]
    public async Task LoadPreset_AutoFixableOnly_FiltersCorrectly()
    {
        // Arrange
        var issues = CreateMixedIssues();
        var preset = _sut.LoadPreset("Auto-Fixable Only");

        // Act
        var result = await _sut.FilterAsync(issues, preset!);

        // Assert
        result.Should().OnlyContain(i => i.CanAutoFix);
    }

    #endregion

    // ── FilterCriteria Subclass Tests ─────────────────────────────────────

    #region FilterCriteria Tests

    [Fact]
    public void CategoryFilterCriterion_MatchesCorrectCategory()
    {
        // Arrange
        var criterion = new CategoryFilterCriterion([IssueCategory.Style, IssueCategory.Grammar]);
        var styleIssue = CreateIssue(category: IssueCategory.Style);
        var knowledgeIssue = CreateIssue(category: IssueCategory.Knowledge);

        // Assert
        criterion.Matches(styleIssue).Should().BeTrue();
        criterion.Matches(knowledgeIssue).Should().BeFalse();
    }

    [Fact]
    public void SeverityFilterCriterion_MatchesCorrectSeverity()
    {
        // Arrange — minimum severity Warning(1) means Error(0) and Warning(1) pass
        var criterion = new SeverityFilterCriterion(UnifiedSeverity.Warning);
        var errorIssue = CreateIssue(severity: UnifiedSeverity.Error);
        var warningIssue = CreateIssue(severity: UnifiedSeverity.Warning);
        var infoIssue = CreateIssue(severity: UnifiedSeverity.Info);

        // Assert
        criterion.Matches(errorIssue).Should().BeTrue();
        criterion.Matches(warningIssue).Should().BeTrue();
        criterion.Matches(infoIssue).Should().BeFalse();
    }

    [Fact]
    public void LocationFilterCriterion_StrictContainment_MatchesContainedOnly()
    {
        // Arrange
        var criterion = new LocationFilterCriterion(new TextSpan(50, 100), IncludePartialOverlaps: false);
        var contained = CreateIssue(start: 60, length: 10); // 60-70, within 50-150
        var outside = CreateIssue(start: 200, length: 10);   // 200-210, outside

        // Assert
        criterion.Matches(contained).Should().BeTrue();
        criterion.Matches(outside).Should().BeFalse();
    }

    [Fact]
    public void LocationFilterCriterion_PartialOverlap_MatchesOverlapping()
    {
        // Arrange
        var criterion = new LocationFilterCriterion(new TextSpan(50, 100), IncludePartialOverlaps: true);
        var overlapping = CreateIssue(start: 140, length: 20); // 140-160, overlaps 50-150

        // Assert
        criterion.Matches(overlapping).Should().BeTrue();
    }

    [Fact]
    public void TextSearchFilterCriterion_MatchesMessage()
    {
        // Arrange
        var criterion = new TextSearchFilterCriterion("active");
        var matchingIssue = CreateIssue(message: "Use active voice");
        var nonMatchingIssue = CreateIssue(message: "Missing comma");

        // Assert
        criterion.Matches(matchingIssue).Should().BeTrue();
        criterion.Matches(nonMatchingIssue).Should().BeFalse();
    }

    [Fact]
    public void TextSearchFilterCriterion_MatchesSourceId()
    {
        // Arrange
        var criterion = new TextSearchFilterCriterion("AXIOM");
        var matchingIssue = CreateIssue(sourceId: "AXIOM_001");

        // Assert
        criterion.Matches(matchingIssue).Should().BeTrue();
    }

    [Fact]
    public void TextSearchFilterCriterion_CaseInsensitive()
    {
        // Arrange
        var criterion = new TextSearchFilterCriterion("VOICE");
        var issue = CreateIssue(message: "Use active voice");

        // Assert
        criterion.Matches(issue).Should().BeTrue();
    }

    [Fact]
    public void CodeFilterCriterion_ExactMatch()
    {
        // Arrange
        var criterion = new CodeFilterCriterion(["STYLE_001"]);
        var matching = CreateIssue(sourceId: "STYLE_001");
        var nonMatching = CreateIssue(sourceId: "STYLE_002");

        // Assert
        criterion.Matches(matching).Should().BeTrue();
        criterion.Matches(nonMatching).Should().BeFalse();
    }

    [Fact]
    public void CodeFilterCriterion_WildcardMatch()
    {
        // Arrange
        var criterion = new CodeFilterCriterion(["STYLE_*"]);
        var matching = CreateIssue(sourceId: "STYLE_001");
        var nonMatching = CreateIssue(sourceId: "GRAM_001");

        // Assert
        criterion.Matches(matching).Should().BeTrue();
        criterion.Matches(nonMatching).Should().BeFalse();
    }

    #endregion

    // ── IssueFilterOptions Tests ──────────────────────────────────────────

    #region IssueFilterOptions Tests

    [Fact]
    public void IssueFilterOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new IssueFilterOptions();

        // Assert
        options.Categories.Should().BeEmpty();
        options.MinimumSeverity.Should().Be(UnifiedSeverity.Hint);
        options.MaximumSeverity.Should().Be(UnifiedSeverity.Error);
        options.IssueCodes.Should().BeEmpty();
        options.ExcludeCodes.Should().BeEmpty();
        options.SearchText.Should().BeNull();
        options.LocationSpan.Should().BeNull();
        options.LineRange.Should().BeNull();
        options.OnlyAutoFixable.Should().BeFalse();
        options.OnlyManual.Should().BeFalse();
        options.SortBy.Should().BeEmpty();
        options.SortAscending.Should().BeTrue();
        options.ValidatorNames.Should().BeEmpty();
        options.Limit.Should().Be(0);
        options.Offset.Should().Be(0);
    }

    [Fact]
    public void IssueFilterOptions_Default_ReturnsDefaultInstance()
    {
        // Act
        var options = IssueFilterOptions.Default;

        // Assert
        options.Should().NotBeNull();
        options.MinimumSeverity.Should().Be(UnifiedSeverity.Hint);
        options.MaximumSeverity.Should().Be(UnifiedSeverity.Error);
    }

    #endregion

    // ── SortCriteria Enum Tests ───────────────────────────────────────────

    #region SortCriteria Tests

    [Fact]
    public void SortCriteria_HasExpectedValues()
    {
        // Assert
        ((int)SortCriteria.Severity).Should().Be(0);
        ((int)SortCriteria.Location).Should().Be(1);
        ((int)SortCriteria.Category).Should().Be(2);
        ((int)SortCriteria.Message).Should().Be(3);
        ((int)SortCriteria.Code).Should().Be(4);
        ((int)SortCriteria.ValidatorName).Should().Be(5);
        ((int)SortCriteria.AutoFixable).Should().Be(6);
    }

    #endregion

    // ── SortOptions Tests ─────────────────────────────────────────────────

    #region SortOptions Tests

    [Fact]
    public void SortOptions_DefaultAscending_IsTrue()
    {
        // Arrange & Act
        var options = new SortOptions { Primary = SortCriteria.Severity };

        // Assert
        options.Ascending.Should().BeTrue();
        options.Secondary.Should().BeNull();
        options.Tertiary.Should().BeNull();
    }

    #endregion

    // ── Performance Tests ─────────────────────────────────────────────────

    #region Performance Tests

    [Fact]
    public async Task FilterAsync_1000Issues_CompletesUnder50Ms()
    {
        // Arrange — create 1000 issues with mixed properties
        var issues = Enumerable.Range(0, 1000)
            .Select(i => CreateIssue(
                category: (IssueCategory)(i % 5),
                severity: (UnifiedSeverity)(i % 4),
                sourceId: $"CODE_{i:D4}",
                message: $"Issue number {i}",
                start: i * 10,
                length: 8,
                sourceType: i % 3 == 0 ? "StyleLinter" : i % 3 == 1 ? "GrammarLinter" : "Validation",
                canAutoFix: i % 2 == 0))
            .ToList();

        var options = new IssueFilterOptions
        {
            Categories = [IssueCategory.Style, IssueCategory.Grammar],
            MinimumSeverity = UnifiedSeverity.Info,
            SearchText = "Issue",
            SortBy = [SortCriteria.Severity, SortCriteria.Location]
        };

        // Act
        var sw = Stopwatch.StartNew();
        var result = await _sut.FilterAsync(issues, options);
        sw.Stop();

        // Assert
        sw.ElapsedMilliseconds.Should().BeLessThan(50,
            "filtering 1000 issues should complete in under 50ms");
        result.Should().NotBeEmpty();
    }

    #endregion
}
