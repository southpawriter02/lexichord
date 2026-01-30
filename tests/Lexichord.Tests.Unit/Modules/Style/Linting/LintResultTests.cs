using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style.Linting;

/// <summary>
/// Unit tests for LintResult.
/// </summary>
/// <remarks>
/// LOGIC: Verifies factory methods and property calculations work correctly
/// as specified in LCS-DES-023a.
/// </remarks>
public class LintResultTests
{
    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        // Arrange
        var testRule = CreateTestRule();
        var violations = new List<StyleViolation>
        {
            new(testRule, "Test violation", 0, 10, 1, 0, 1, 10, "matched", null, ViolationSeverity.Warning)
        };

        // Act
        var result = LintResult.Success("doc-123", violations, TimeSpan.FromMilliseconds(150));

        // Assert
        result.DocumentId.Should().Be("doc-123");
        result.IsSuccess.Should().BeTrue();
        result.WasCancelled.Should().BeFalse();
        result.Error.Should().BeNull();
        result.Violations.Should().BeEquivalentTo(violations);
        result.Duration.Should().Be(TimeSpan.FromMilliseconds(150));
        result.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Success_WithEmptyViolations_CreatesResult()
    {
        // Act
        var result = LintResult.Success("doc-123", [], TimeSpan.FromMilliseconds(50));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void Cancelled_CreatesCancelledResult()
    {
        // Act
        var result = LintResult.Cancelled("doc-123", TimeSpan.FromMilliseconds(75));

        // Assert
        result.DocumentId.Should().Be("doc-123");
        result.IsSuccess.Should().BeFalse();
        result.WasCancelled.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Violations.Should().BeEmpty();
        result.Duration.Should().Be(TimeSpan.FromMilliseconds(75));
    }

    [Fact]
    public void Failed_CreatesFailedResult()
    {
        // Act
        var result = LintResult.Failed("doc-123", "Regex timeout", TimeSpan.FromMilliseconds(1000));

        // Assert
        result.DocumentId.Should().Be("doc-123");
        result.IsSuccess.Should().BeFalse();
        result.WasCancelled.Should().BeFalse();
        result.Error.Should().Be("Regex timeout");
        result.Violations.Should().BeEmpty();
        result.Duration.Should().Be(TimeSpan.FromMilliseconds(1000));
    }

    [Fact]
    public void IsSuccess_ReturnsFalse_WhenCancelled()
    {
        // Arrange
        var result = new LintResult
        {
            DocumentId = "doc-123",
            Timestamp = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero,
            WasCancelled = true
        };

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void IsSuccess_ReturnsFalse_WhenError()
    {
        // Arrange
        var result = new LintResult
        {
            DocumentId = "doc-123",
            Timestamp = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero,
            Error = "Some error"
        };

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void IsSuccess_ReturnsTrue_WhenNoCancellationOrError()
    {
        // Arrange
        var result = new LintResult
        {
            DocumentId = "doc-123",
            Timestamp = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero
        };

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Record_SupportsWithExpression()
    {
        // Arrange
        var original = LintResult.Success("doc-123", [], TimeSpan.Zero);

        // Act
        var modified = original with { DocumentId = "doc-456" };

        // Assert
        original.DocumentId.Should().Be("doc-123");
        modified.DocumentId.Should().Be("doc-456");
    }

    private static StyleRule CreateTestRule() => new(
        Id: "TST001",
        Name: "Test Rule",
        Description: "A test rule",
        Category: RuleCategory.Terminology,
        DefaultSeverity: ViolationSeverity.Warning,
        Pattern: "test",
        PatternType: PatternType.Literal,
        Suggestion: null);
}
