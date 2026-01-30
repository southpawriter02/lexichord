using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Modules.Style.Services.Linting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Style.Linting;

/// <summary>
/// Unit tests for <see cref="ViolationAggregator"/>.
/// </summary>
/// <remarks>
/// Version: v0.2.3d
/// </remarks>
public sealed class ViolationAggregatorTests
{
    private readonly ILogger<ViolationAggregator> _logger;
    private readonly Mock<ILintingConfiguration> _configMock;

    public ViolationAggregatorTests()
    {
        _logger = NullLogger<ViolationAggregator>.Instance;
        _configMock = new Mock<ILintingConfiguration>();

        // Default configuration
        _configMock.Setup(c => c.MaxViolationsPerDocument).Returns(1000);
    }

    private ViolationAggregator CreateAggregator() =>
        new ViolationAggregator(_configMock.Object, _logger);

    private static StyleRule CreateRule(
        string id = "rule-001",
        ViolationSeverity severity = ViolationSeverity.Warning,
        string description = "Test violation: '{text}'",
        string? suggestion = null) =>
        new StyleRule(
            Id: id,
            Name: $"Rule {id}",
            Description: description,
            Category: RuleCategory.Syntax,
            DefaultSeverity: severity,
            Pattern: @"\btest\b",
            PatternType: PatternType.Regex,
            Suggestion: suggestion,
            IsEnabled: true);

    private static ScanMatch CreateMatch(
        StyleRule? rule = null,
        string matchedText = "badword",
        int startOffset = 0,
        IReadOnlyDictionary<string, string>? captureGroups = null)
    {
        var r = rule ?? CreateRule();
        return new ScanMatch(
            RuleId: r.Id,
            StartOffset: startOffset,
            Length: matchedText.Length,
            MatchedText: matchedText,
            Rule: r,
            CaptureGroups: captureGroups);
    }

    #region Basic Aggregation

    [Fact]
    public void Aggregate_EmptyMatches_ReturnsEmpty()
    {
        // Arrange
        var aggregator = CreateAggregator();

        // Act
        var violations = aggregator.Aggregate(
            [],
            "doc-001",
            "Some content");

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void Aggregate_SingleMatch_ReturnsOneViolation()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var content = "Hello badword world";
        var match = CreateMatch(startOffset: 6);

        // Act
        var violations = aggregator.Aggregate(
            [match],
            "doc-001",
            content);

        // Assert
        violations.Should().ContainSingle();
        var violation = violations[0];
        violation.DocumentId.Should().Be("doc-001");
        violation.RuleId.Should().Be("rule-001");
        violation.ViolatingText.Should().Be("badword");
        violation.StartOffset.Should().Be(6);
        violation.Length.Should().Be(7);
    }

    [Fact]
    public void Aggregate_MultipleMatches_ReturnsMultipleViolations()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var content = "One bad two bad three";
        var matches = new[]
        {
            CreateMatch(startOffset: 4, matchedText: "bad"),
            CreateMatch(startOffset: 12, matchedText: "bad")
        };

        // Act
        var violations = aggregator.Aggregate(matches, "doc-001", content);

        // Assert
        violations.Should().HaveCount(2);
    }

    #endregion

    #region Position Calculation

    [Fact]
    public void Aggregate_FirstLine_CalculatesCorrectPosition()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var content = "Hello bad world";
        //            0123456789...
        var match = CreateMatch(startOffset: 6, matchedText: "bad");

        // Act
        var violations = aggregator.Aggregate([match], "doc-001", content);

        // Assert
        var v = violations[0];
        v.Line.Should().Be(1);
        v.Column.Should().Be(7); // 0-indexed 6 -> 1-indexed 7
        v.EndColumn.Should().Be(10); // 6 + 3 + 1 = 10
    }

    [Fact]
    public void Aggregate_SecondLine_CalculatesCorrectPosition()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var content = "Line one\nLine bad two";
        //            012345678 901234567890
        var match = CreateMatch(startOffset: 14, matchedText: "bad");

        // Act
        var violations = aggregator.Aggregate([match], "doc-001", content);

        // Assert
        var v = violations[0];
        v.Line.Should().Be(2);
        v.Column.Should().Be(6); // offset 14 - line start 9 = 5 -> 1-indexed = 6
    }

    [Fact]
    public void Aggregate_MultiLineViolation_CalculatesEndPosition()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var content = "Start\nmulti\nline bad";
        //            012345 678901 234567890
        // "multi\nline" starts at 6, length 11
        var match = CreateMatch(startOffset: 6, matchedText: "multi\nline");

        // Act
        var violations = aggregator.Aggregate([match], "doc-001", content);

        // Assert
        var v = violations[0];
        v.Line.Should().Be(2);
        v.EndLine.Should().Be(3);
    }

    #endregion

    #region Deduplication

    [Fact]
    public void Aggregate_OverlappingMatches_KeepsHigherSeverity()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var content = "This is bad content here";
        //            01234567890123456789...
        var warningRule = CreateRule("warning-001", ViolationSeverity.Warning);
        var errorRule = CreateRule("error-001", ViolationSeverity.Error);

        var matches = new[]
        {
            CreateMatch(warningRule, "bad content", 8),
            CreateMatch(errorRule, "bad", 8) // Overlaps with first
        };

        // Act
        var violations = aggregator.Aggregate(matches, "doc-001", content);

        // Assert - Higher severity (Error) should win
        violations.Should().ContainSingle();
        violations[0].Severity.Should().Be(ViolationSeverity.Error);
    }

    [Fact]
    public void Aggregate_EqualSeverityOverlap_KeepsEarlierStart()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var content = "This is bad content here";
        var rule1 = CreateRule("rule-001", ViolationSeverity.Warning);
        var rule2 = CreateRule("rule-002", ViolationSeverity.Warning);

        var matches = new[]
        {
            CreateMatch(rule1, "bad", 8),
            CreateMatch(rule2, "bad content", 8) // Same start, different length
        };

        // Act
        var violations = aggregator.Aggregate(matches, "doc-001", content);

        // Assert - First one in sorted order kept
        violations.Should().ContainSingle();
        violations[0].RuleId.Should().Be("rule-001");
    }

    [Fact]
    public void Aggregate_NonOverlappingMatches_KeepsAll()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var content = "First bad ... second bad";
        //            0123456789012345678901234
        var matches = new[]
        {
            CreateMatch(startOffset: 6, matchedText: "bad"),
            CreateMatch(startOffset: 21, matchedText: "bad")
        };

        // Act
        var violations = aggregator.Aggregate(matches, "doc-001", content);

        // Assert
        violations.Should().HaveCount(2);
    }

    #endregion

    #region Sorting

    [Fact]
    public void Aggregate_MultipleMatches_SortedByPosition()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var content = "A bad\nB bad\nC bad";
        // Add in non-sorted order
        var matches = new[]
        {
            CreateMatch(startOffset: 12, matchedText: "bad"), // Line 3
            CreateMatch(startOffset: 2, matchedText: "bad"),  // Line 1
            CreateMatch(startOffset: 8, matchedText: "bad")   // Line 2
        };

        // Act
        var violations = aggregator.Aggregate(matches, "doc-001", content);

        // Assert
        violations.Should().HaveCount(3);
        violations[0].Line.Should().Be(1);
        violations[1].Line.Should().Be(2);
        violations[2].Line.Should().Be(3);
    }

    [Fact]
    public void Aggregate_SameLineDifferentColumn_SortedByColumn()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var content = "AAA BBB CCC";
        //            012345678901
        var matches = new[]
        {
            CreateMatch(startOffset: 8, matchedText: "CCC"),
            CreateMatch(startOffset: 0, matchedText: "AAA"),
            CreateMatch(startOffset: 4, matchedText: "BBB")
        };

        // Act
        var violations = aggregator.Aggregate(matches, "doc-001", content);

        // Assert
        violations.Should().HaveCount(3);
        violations[0].Column.Should().Be(1); // AAA
        violations[1].Column.Should().Be(5); // BBB
        violations[2].Column.Should().Be(9); // CCC
    }

    #endregion

    #region Max Violations Limit

    [Fact]
    public void Aggregate_ExceedsLimit_TruncatesToMax()
    {
        // Arrange
        _configMock.Setup(c => c.MaxViolationsPerDocument).Returns(5);
        var aggregator = CreateAggregator();
        var content = string.Join(" ", Enumerable.Repeat("bad", 10));
        var matches = Enumerable.Range(0, 10)
            .Select(i => CreateMatch(startOffset: i * 4, matchedText: "bad"))
            .ToList();

        // Act
        var violations = aggregator.Aggregate(matches, "doc-001", content);

        // Assert
        violations.Should().HaveCount(5);
    }

    [Fact]
    public void Aggregate_UnderLimit_ReturnsAll()
    {
        // Arrange
        _configMock.Setup(c => c.MaxViolationsPerDocument).Returns(100);
        var aggregator = CreateAggregator();
        var matches = new[]
        {
            CreateMatch(startOffset: 0, matchedText: "bad"),
            CreateMatch(startOffset: 10, matchedText: "bad")
        };

        // Act
        var violations = aggregator.Aggregate(matches, "doc-001", "content bad and bad");

        // Assert
        violations.Should().HaveCount(2);
    }

    #endregion

    #region Caching

    [Fact]
    public void GetViolations_AfterAggregate_ReturnsCached()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var match = CreateMatch();
        aggregator.Aggregate([match], "doc-001", "content bad");

        // Act
        var cached = aggregator.GetViolations("doc-001");

        // Assert
        cached.Should().ContainSingle();
    }

    [Fact]
    public void GetViolations_NoAggregate_ReturnsEmpty()
    {
        // Arrange
        var aggregator = CreateAggregator();

        // Act
        var violations = aggregator.GetViolations("unknown-doc");

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void ClearViolations_RemovesCache()
    {
        // Arrange
        var aggregator = CreateAggregator();
        aggregator.Aggregate([CreateMatch()], "doc-001", "bad content");

        // Act
        aggregator.ClearViolations("doc-001");
        var violations = aggregator.GetViolations("doc-001");

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void ClearViolations_UnknownDocument_NoOp()
    {
        // Arrange
        var aggregator = CreateAggregator();

        // Act & Assert - Should not throw
        aggregator.ClearViolations("nonexistent");
    }

    #endregion

    #region GetViolation

    [Fact]
    public void GetViolation_ExistingId_ReturnsViolation()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var violations = aggregator.Aggregate([CreateMatch()], "doc-001", "bad content");
        var id = violations[0].Id;

        // Act
        var result = aggregator.GetViolation("doc-001", id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
    }

    [Fact]
    public void GetViolation_NonexistentId_ReturnsNull()
    {
        // Arrange
        var aggregator = CreateAggregator();
        aggregator.Aggregate([CreateMatch()], "doc-001", "bad content");

        // Act
        var result = aggregator.GetViolation("doc-001", "nonexistent-id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetViolation_NonexistentDocument_ReturnsNull()
    {
        // Arrange
        var aggregator = CreateAggregator();

        // Act
        var result = aggregator.GetViolation("unknown", "any-id");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetViolationAt

    [Fact]
    public void GetViolationAt_InsideViolation_ReturnsViolation()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var content = "Hello bad world";
        aggregator.Aggregate([CreateMatch(startOffset: 6, matchedText: "bad")], "doc-001", content);

        // Act
        var result = aggregator.GetViolationAt("doc-001", 7);

        // Assert - offset 7 is inside "bad" (6-8)
        result.Should().NotBeNull();
        result!.ViolatingText.Should().Be("bad");
    }

    [Fact]
    public void GetViolationAt_AtStart_ReturnsViolation()
    {
        // Arrange
        var aggregator = CreateAggregator();
        aggregator.Aggregate([CreateMatch(startOffset: 6, matchedText: "bad")], "doc-001", "Hello bad");

        // Act
        var result = aggregator.GetViolationAt("doc-001", 6);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void GetViolationAt_AtEnd_ReturnsNull()
    {
        // Arrange
        var aggregator = CreateAggregator();
        aggregator.Aggregate([CreateMatch(startOffset: 6, matchedText: "bad")], "doc-001", "Hello bad");

        // Act - offset 9 is just after "bad" (6+3=9, exclusive)
        var result = aggregator.GetViolationAt("doc-001", 9);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetViolationAt_OutsideAll_ReturnsNull()
    {
        // Arrange
        var aggregator = CreateAggregator();
        aggregator.Aggregate([CreateMatch(startOffset: 6, matchedText: "bad")], "doc-001", "Hello bad");

        // Act
        var result = aggregator.GetViolationAt("doc-001", 0);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetViolationAt_NoViolations_ReturnsNull()
    {
        // Arrange
        var aggregator = CreateAggregator();

        // Act
        var result = aggregator.GetViolationAt("doc-001", 5);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetViolationsInRange

    [Fact]
    public void GetViolationsInRange_OverlappingViolations_ReturnsAll()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var content = "AAA BBB CCC DDD";
        //            012345678901234
        var matches = new[]
        {
            CreateMatch(startOffset: 0, matchedText: "AAA"),
            CreateMatch(startOffset: 4, matchedText: "BBB"),
            CreateMatch(startOffset: 8, matchedText: "CCC"),
            CreateMatch(startOffset: 12, matchedText: "DDD")
        };
        aggregator.Aggregate(matches, "doc-001", content);

        // Act - Range 4-10 should include BBB (4-6) and CCC (8-10)
        var violations = aggregator.GetViolationsInRange("doc-001", 4, 11);

        // Assert
        violations.Should().HaveCount(2);
        violations.Should().Contain(v => v.ViolatingText == "BBB");
        violations.Should().Contain(v => v.ViolatingText == "CCC");
    }

    [Fact]
    public void GetViolationsInRange_NoOverlap_ReturnsEmpty()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var content = "AAA ... BBB";
        aggregator.Aggregate([CreateMatch(startOffset: 0, matchedText: "AAA")], "doc-001", content);

        // Act - Range well beyond the violation
        var violations = aggregator.GetViolationsInRange("doc-001", 10, 20);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void GetViolationsInRange_EmptyDocument_ReturnsEmpty()
    {
        // Arrange
        var aggregator = CreateAggregator();

        // Act
        var violations = aggregator.GetViolationsInRange("unknown", 0, 100);

        // Assert
        violations.Should().BeEmpty();
    }

    #endregion

    #region GetViolationCounts

    [Fact]
    public void GetViolationCounts_MixedSeverities_ReturnsCorrectCounts()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var content = "AAA BBB CCC DDD";
        var errorRule = CreateRule("err", ViolationSeverity.Error);
        var warningRule = CreateRule("warn", ViolationSeverity.Warning);
        var infoRule = CreateRule("info", ViolationSeverity.Info);

        var matches = new[]
        {
            CreateMatch(errorRule, "AAA", 0),
            CreateMatch(warningRule, "BBB", 4),
            CreateMatch(warningRule, "CCC", 8),
            CreateMatch(infoRule, "DDD", 12)
        };
        aggregator.Aggregate(matches, "doc-001", content);

        // Act
        var counts = aggregator.GetViolationCounts("doc-001");

        // Assert
        counts[ViolationSeverity.Error].Should().Be(1);
        counts[ViolationSeverity.Warning].Should().Be(2);
        counts[ViolationSeverity.Info].Should().Be(1);
        counts[ViolationSeverity.Hint].Should().Be(0);
    }

    [Fact]
    public void GetViolationCounts_NoViolations_ReturnsZeros()
    {
        // Arrange
        var aggregator = CreateAggregator();

        // Act
        var counts = aggregator.GetViolationCounts("unknown");

        // Assert
        counts[ViolationSeverity.Error].Should().Be(0);
        counts[ViolationSeverity.Warning].Should().Be(0);
        counts[ViolationSeverity.Info].Should().Be(0);
        counts[ViolationSeverity.Hint].Should().Be(0);
    }

    #endregion

    #region Message Expansion

    [Fact]
    public void Aggregate_RuleWithTemplate_ExpandsMessage()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var rule = CreateRule(
            description: "Avoid '{text}', consider simple words.",
            suggestion: "use");
        var match = CreateMatch(rule, "utilize", 0);

        // Act
        var violations = aggregator.Aggregate([match], "doc-001", "utilize bad words");

        // Assert
        violations[0].Message.Should().Be("Avoid 'utilize', consider simple words.");
    }

    [Fact]
    public void Aggregate_RuleWithSuggestion_IncludesSuggestion()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var rule = CreateRule(suggestion: "use");
        var match = CreateMatch(rule, "utilize", 0);

        // Act
        var violations = aggregator.Aggregate([match], "doc-001", "utilize");

        // Assert
        violations[0].Suggestion.Should().Be("use");
        violations[0].HasSuggestion.Should().BeTrue();
    }

    [Fact]
    public void Aggregate_RuleWithoutSuggestion_HasSuggestionFalse()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var rule = CreateRule(suggestion: null);
        var match = CreateMatch(rule, "bad", 0);

        // Act
        var violations = aggregator.Aggregate([match], "doc-001", "bad content");

        // Assert
        violations[0].Suggestion.Should().BeNull();
        violations[0].HasSuggestion.Should().BeFalse();
    }

    #endregion

    #region Violation ID Stability

    [Fact]
    public void Aggregate_SameInput_ProducesSameIds()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var content = "Hello bad world";
        var match = CreateMatch(startOffset: 6, matchedText: "bad");

        // Act
        var violations1 = aggregator.Aggregate([match], "doc-001", content);
        aggregator.ClearViolations("doc-001");
        var violations2 = aggregator.Aggregate([match], "doc-001", content);

        // Assert
        violations1[0].Id.Should().Be(violations2[0].Id);
    }

    [Fact]
    public void Aggregate_DifferentPosition_ProducesDifferentId()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var content = "bad ... bad";
        var match1 = CreateMatch(startOffset: 0, matchedText: "bad");
        var match2 = CreateMatch(startOffset: 8, matchedText: "bad");

        // Act
        var violations = aggregator.Aggregate([match1, match2], "doc-001", content);

        // Assert
        violations[0].Id.Should().NotBe(violations[1].Id);
    }

    #endregion
}
