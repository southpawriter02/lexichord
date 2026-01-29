using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Domain;

/// <summary>
/// Unit tests for StyleViolation record.
/// </summary>
/// <remarks>
/// LOGIC: Verifies position calculations and context generation.
/// </remarks>
public class StyleViolationTests
{
    private readonly StyleRule _testRule;

    public StyleViolationTests()
    {
        _testRule = new StyleRule(
            Id: "test",
            Name: "Test Rule",
            Description: "A test rule",
            Category: RuleCategory.Terminology,
            DefaultSeverity: ViolationSeverity.Warning,
            Pattern: "test",
            PatternType: PatternType.Literal,
            Suggestion: "Use 'example'",
            IsEnabled: true);
    }

    [Fact]
    public void Length_CalculatedCorrectly()
    {
        // Arrange
        var violation = new StyleViolation(
            Rule: _testRule,
            Message: "Test message",
            StartOffset: 10,
            EndOffset: 14,
            StartLine: 1,
            StartColumn: 11,
            EndLine: 1,
            EndColumn: 15,
            MatchedText: "test",
            Suggestion: "Use 'example'",
            Severity: ViolationSeverity.Warning);

        // Assert
        violation.Length.Should().Be(4);
    }

    [Fact]
    public void GetSurroundingContext_IncludesContext()
    {
        // Arrange
        var content = "This is a test of the system.";
        var violation = new StyleViolation(
            Rule: _testRule,
            Message: "Test message",
            StartOffset: 10,
            EndOffset: 14,
            StartLine: 1,
            StartColumn: 11,
            EndLine: 1,
            EndColumn: 15,
            MatchedText: "test",
            Suggestion: null,
            Severity: ViolationSeverity.Warning);

        // Act
        var context = violation.GetSurroundingContext(content, contextChars: 5);

        // Assert
        context.Should().Contain("[test]");
        context.Should().Contain("is a ");
        context.Should().Contain(" of t");
    }

    [Fact]
    public void GetSurroundingContext_HandlesStartOfContent()
    {
        // Arrange
        var content = "test at start of content";
        var violation = new StyleViolation(
            Rule: _testRule,
            Message: "Test message",
            StartOffset: 0,
            EndOffset: 4,
            StartLine: 1,
            StartColumn: 1,
            EndLine: 1,
            EndColumn: 5,
            MatchedText: "test",
            Suggestion: null,
            Severity: ViolationSeverity.Warning);

        // Act
        var context = violation.GetSurroundingContext(content, contextChars: 10);

        // Assert
        context.Should().StartWith("[test]");
        context.Should().Contain(" at start ");
    }

    [Fact]
    public void GetSurroundingContext_HandlesEndOfContent()
    {
        // Arrange
        var content = "content ends with test";
        var violation = new StyleViolation(
            Rule: _testRule,
            Message: "Test message",
            StartOffset: 18,
            EndOffset: 22,
            StartLine: 1,
            StartColumn: 19,
            EndLine: 1,
            EndColumn: 23,
            MatchedText: "test",
            Suggestion: null,
            Severity: ViolationSeverity.Warning);

        // Act
        var context = violation.GetSurroundingContext(content, contextChars: 10);

        // Assert
        context.Should().EndWith("[test]");
        context.Should().Contain("ends with ");
    }

    [Fact]
    public void GetSurroundingContext_AddsEllipsis()
    {
        // Arrange
        var content = "This is a very long string with test in the middle of it all.";
        var startOffset = content.IndexOf("test");
        var violation = new StyleViolation(
            Rule: _testRule,
            Message: "Test message",
            StartOffset: startOffset,
            EndOffset: startOffset + 4,
            StartLine: 1,
            StartColumn: startOffset + 1,
            EndLine: 1,
            EndColumn: startOffset + 5,
            MatchedText: "test",
            Suggestion: null,
            Severity: ViolationSeverity.Warning);

        // Act
        var context = violation.GetSurroundingContext(content, contextChars: 5);

        // Assert
        context.Should().StartWith("...");
        context.Should().EndWith("...");
    }

    [Fact]
    public void WithSeverity_CreatesCopyWithNewSeverity()
    {
        // Arrange
        var violation = new StyleViolation(
            Rule: _testRule,
            Message: "Test",
            StartOffset: 0,
            EndOffset: 4,
            StartLine: 1,
            StartColumn: 1,
            EndLine: 1,
            EndColumn: 5,
            MatchedText: "test",
            Suggestion: null,
            Severity: ViolationSeverity.Warning);

        // Act
        var elevated = violation.WithSeverity(ViolationSeverity.Error);

        // Assert
        elevated.Severity.Should().Be(ViolationSeverity.Error);
        violation.Severity.Should().Be(ViolationSeverity.Warning); // Original unchanged
    }

    [Fact]
    public void WithMessage_CreatesCopyWithNewMessage()
    {
        // Arrange
        var violation = new StyleViolation(
            Rule: _testRule,
            Message: "Original message",
            StartOffset: 0,
            EndOffset: 4,
            StartLine: 1,
            StartColumn: 1,
            EndLine: 1,
            EndColumn: 5,
            MatchedText: "test",
            Suggestion: null,
            Severity: ViolationSeverity.Warning);

        // Act
        var updated = violation.WithMessage("New message");

        // Assert
        updated.Message.Should().Be("New message");
        violation.Message.Should().Be("Original message"); // Original unchanged
    }

    [Fact]
    public void ViolationProperties_AreCorrectlySet()
    {
        // Arrange & Act
        var violation = new StyleViolation(
            Rule: _testRule,
            Message: "Test message",
            StartOffset: 10,
            EndOffset: 15,
            StartLine: 2,
            StartColumn: 5,
            EndLine: 2,
            EndColumn: 10,
            MatchedText: "match",
            Suggestion: "replacement",
            Severity: ViolationSeverity.Error);

        // Assert
        violation.Rule.Should().BeSameAs(_testRule);
        violation.Message.Should().Be("Test message");
        violation.StartOffset.Should().Be(10);
        violation.EndOffset.Should().Be(15);
        violation.StartLine.Should().Be(2);
        violation.StartColumn.Should().Be(5);
        violation.EndLine.Should().Be(2);
        violation.EndColumn.Should().Be(10);
        violation.MatchedText.Should().Be("match");
        violation.Suggestion.Should().Be("replacement");
        violation.Severity.Should().Be(ViolationSeverity.Error);
        violation.Length.Should().Be(5);
    }
}
