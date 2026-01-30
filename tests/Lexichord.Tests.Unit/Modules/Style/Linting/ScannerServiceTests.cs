using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Modules.Style.Services.Linting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Style.Linting;

/// <summary>
/// Unit tests for <see cref="ScannerService"/>.
/// </summary>
/// <remarks>
/// Version: v0.2.3c
/// </remarks>
public sealed class ScannerServiceTests
{
    private readonly ILogger<ScannerService> _logger;
    private readonly Mock<ILintingConfiguration> _configMock;

    public ScannerServiceTests()
    {
        _logger = NullLogger<ScannerService>.Instance;
        _configMock = new Mock<ILintingConfiguration>();

        // Default configuration
        _configMock.Setup(c => c.PatternCacheMaxSize).Returns(100);
        _configMock.Setup(c => c.PatternTimeoutMilliseconds).Returns(100);
        _configMock.Setup(c => c.UseComplexityAnalysis).Returns(true);
    }

    private ScannerService CreateService() =>
        new ScannerService(_configMock.Object, _logger);

    private static StyleRule CreateRule(
        string id,
        string pattern,
        PatternType patternType = PatternType.Regex,
        bool isEnabled = true) =>
        new StyleRule(
            Id: id,
            Name: $"Rule {id}",
            Description: "Test rule",
            Category: RuleCategory.Syntax,
            DefaultSeverity: ViolationSeverity.Warning,
            Pattern: pattern,
            PatternType: patternType,
            Suggestion: null,
            IsEnabled: isEnabled);

    #region Basic Pattern Matching

    [Fact]
    public async Task ScanAsync_WithRegexPattern_FindsMatches()
    {
        // Arrange
        var service = CreateService();
        var rule = CreateRule("test-001", @"\bfoo\b");
        var content = "The foo is here. Another foo appears.";

        // Act
        var result = await service.ScanAsync(content, rule);

        // Assert
        result.Should().NotBeNull();
        result.RuleId.Should().Be("test-001");
        result.MatchCount.Should().Be(2);
        result.HasMatches.Should().BeTrue();
        result.ScanDuration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task ScanAsync_WithNoMatches_ReturnsEmptyResult()
    {
        // Arrange
        var service = CreateService();
        var rule = CreateRule("test-002", @"\bxyz\b");
        var content = "This content has no matches.";

        // Act
        var result = await service.ScanAsync(content, rule);

        // Assert
        result.MatchCount.Should().Be(0);
        result.HasMatches.Should().BeFalse();
    }

    [Fact]
    public async Task ScanAsync_WithDisabledRule_ReturnsEmpty()
    {
        // Arrange
        var service = CreateService();
        var rule = CreateRule("test-003", @"\bfoo\b", isEnabled: false);
        var content = "The foo is here.";

        // Act
        var result = await service.ScanAsync(content, rule);

        // Assert
        result.Should().BeEquivalentTo(ScannerResult.Empty("test-003"));
    }

    [Fact]
    public async Task ScanAsync_WithEmptyContent_ReturnsEmpty()
    {
        // Arrange
        var service = CreateService();
        var rule = CreateRule("test-004", @"\bfoo\b");

        // Act
        var result = await service.ScanAsync("", rule);

        // Assert
        result.Should().BeEquivalentTo(ScannerResult.Empty("test-004"));
    }

    [Fact]
    public async Task ScanAsync_WithLiteralPattern_FindsMatches()
    {
        // Arrange
        var service = CreateService();
        var rule = CreateRule("test-005", "foo", PatternType.Literal);
        var content = "The foo is here. Another foobar too.";

        // Act
        var result = await service.ScanAsync(content, rule);

        // Assert
        result.MatchCount.Should().Be(2);
    }

    [Fact]
    public async Task ScanAsync_WithLiteralIgnoreCasePattern_FindsCaseInsensitiveMatches()
    {
        // Arrange
        var service = CreateService();
        var rule = CreateRule("test-006", "foo", PatternType.LiteralIgnoreCase);
        var content = "The FOO is here. Another foo appears.";

        // Act
        var result = await service.ScanAsync(content, rule);

        // Assert
        result.MatchCount.Should().Be(2);
    }

    #endregion

    #region Cache Behavior

    [Fact]
    public async Task ScanAsync_SecondCallWithSamePattern_ReturnsCacheHit()
    {
        // Arrange
        var service = CreateService();
        var rule = CreateRule("test-007", @"\btest\b");
        var content = "This is a test.";

        // Act
        var result1 = await service.ScanAsync(content, rule);
        var result2 = await service.ScanAsync(content, rule);

        // Assert
        result1.WasCacheHit.Should().BeFalse();
        result2.WasCacheHit.Should().BeTrue();
    }

    [Fact]
    public async Task ClearCache_ResetsStatistics()
    {
        // Arrange
        var service = CreateService();
        var rule = CreateRule("test-008", @"\btest\b");
        await service.ScanAsync("test content", rule);

        // Act
        service.ClearCache();
        var stats = service.GetStatistics();

        // Assert
        stats.TotalScans.Should().Be(0);
        stats.CacheHits.Should().Be(0);
        stats.CacheMisses.Should().Be(0);
        stats.CurrentCacheSize.Should().Be(0);
    }

    [Fact]
    public async Task GetStatistics_ReturnsAccurateMetrics()
    {
        // Arrange
        var service = CreateService();
        var rule1 = CreateRule("test-009a", @"\bfoo\b");
        var rule2 = CreateRule("test-009b", @"\bbar\b");
        var content = "foo bar foo";

        // Act - First scan: 2 misses (2 different patterns)
        await service.ScanAsync(content, rule1);
        await service.ScanAsync(content, rule2);

        // Second scan: 2 hits
        await service.ScanAsync(content, rule1);
        await service.ScanAsync(content, rule2);

        var stats = service.GetStatistics();

        // Assert
        stats.TotalScans.Should().Be(4);
        stats.CacheHits.Should().Be(2);
        stats.CacheMisses.Should().Be(2);
        stats.CurrentCacheSize.Should().Be(2);
        stats.HitRatio.Should().BeApproximately(0.5, 0.01);
    }

    #endregion

    #region Batch Scanning

    [Fact]
    public async Task ScanBatchAsync_WithMultipleRules_ReturnsAllResults()
    {
        // Arrange
        var service = CreateService();
        var rules = new[]
        {
            CreateRule("batch-001", @"\bfoo\b"),
            CreateRule("batch-002", @"\bbar\b"),
            CreateRule("batch-003", @"\bbaz\b")
        };
        var content = "foo bar baz foo bar";

        // Act
        var results = await service.ScanBatchAsync(content, rules);

        // Assert
        results.Should().HaveCount(3);
        results.Should().Contain(r => r.RuleId == "batch-001" && r.MatchCount == 2);
        results.Should().Contain(r => r.RuleId == "batch-002" && r.MatchCount == 2);
        results.Should().Contain(r => r.RuleId == "batch-003" && r.MatchCount == 1);
    }

    [Fact]
    public async Task ScanBatchAsync_WithEmptyRuleList_ReturnsEmpty()
    {
        // Arrange
        var service = CreateService();

        // Act
        var results = await service.ScanBatchAsync("some content", Array.Empty<StyleRule>());

        // Assert
        results.Should().BeEmpty();
    }

    #endregion

    #region ReDoS Protection

    [Fact]
    public async Task ScanAsync_BlocksDangerousPattern_WhenComplexityAnalysisEnabled()
    {
        // Arrange
        var service = CreateService();
        // Nested quantifiers - known ReDoS pattern
        var rule = CreateRule("redos-001", @"(a+)+$");
        var content = "aaaaaaaaaaaa";

        // Act
        var result = await service.ScanAsync(content, rule);

        // Assert
        result.MatchCount.Should().Be(0, "dangerous patterns should be blocked");
    }

    [Fact]
    public async Task ScanAsync_AllowsDangerousPattern_WhenComplexityAnalysisDisabled()
    {
        // Arrange
        _configMock.Setup(c => c.UseComplexityAnalysis).Returns(false);
        var service = CreateService();
        // Pattern that would normally be blocked
        var rule = CreateRule("redos-002", @"(a+)+");
        var content = "aaa";

        // Act
        var result = await service.ScanAsync(content, rule);

        // Assert
        // Pattern is allowed when analysis is disabled (but may still timeout)
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ScanAsync_HandlesInvalidRegex_Gracefully()
    {
        // Arrange
        var service = CreateService();
        // Invalid regex with unmatched parenthesis
        var rule = CreateRule("invalid-001", @"(unclosed");

        // Act
        var result = await service.ScanAsync("test content", rule);

        // Assert
        result.Should().BeEquivalentTo(ScannerResult.Empty("invalid-001"));
    }

    #endregion

    #region Match Span Accuracy

    [Fact]
    public async Task ScanAsync_ReturnsCorrectMatchSpans()
    {
        // Arrange
        var service = CreateService();
        var rule = CreateRule("span-001", @"\bword\b");
        var content = "A word here and word there.";
        //             0123456789012345678901234567
        // "word" at index 2 and 16

        // Act
        var result = await service.ScanAsync(content, rule);

        // Assert
        result.MatchCount.Should().Be(2);
        result.Matches[0].StartOffset.Should().Be(2);
        result.Matches[0].Length.Should().Be(4);
        result.Matches[1].StartOffset.Should().Be(16);
        result.Matches[1].Length.Should().Be(4);
    }

    #endregion
}
