using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style.Linting;

/// <summary>
/// Unit tests for DocumentLintState.
/// </summary>
/// <remarks>
/// LOGIC: Verifies state transitions via builder methods work correctly
/// and maintain immutability per LCS-DES-023a.
/// </remarks>
public class DocumentLintStateTests
{
    [Fact]
    public void Constructor_SetsDocumentId()
    {
        // Act
        var state = new DocumentLintState { DocumentId = "doc-123" };

        // Assert
        state.DocumentId.Should().Be("doc-123");
        state.State.Should().Be(LintLifecycleState.Idle);
        state.LintCount.Should().Be(0);
        state.LastViolations.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithFilePath_SetsFilePath()
    {
        // Act
        var state = new DocumentLintState
        {
            DocumentId = "doc-123",
            FilePath = "/path/to/file.md"
        };

        // Assert
        state.FilePath.Should().Be("/path/to/file.md");
    }

    [Fact]
    public void CreatePending_ReturnsNewStateWithPendingStatus()
    {
        // Arrange
        var original = new DocumentLintState { DocumentId = "doc-123" };

        // Act
        var pending = original.CreatePending();

        // Assert
        pending.State.Should().Be(LintLifecycleState.Pending);
        original.State.Should().Be(LintLifecycleState.Idle); // Immutable
    }

    [Fact]
    public void StartAnalyzing_ReturnsNewStateWithAnalyzingStatus()
    {
        // Arrange
        var original = new DocumentLintState { DocumentId = "doc-123" };

        // Act
        var analyzing = original.StartAnalyzing();

        // Assert
        analyzing.State.Should().Be(LintLifecycleState.Analyzing);
        original.State.Should().Be(LintLifecycleState.Idle); // Immutable
    }

    [Fact]
    public void CompleteWith_UpdatesStateAndResults()
    {
        // Arrange
        var original = new DocumentLintState { DocumentId = "doc-123" }
            .CreatePending()
            .StartAnalyzing();
        var testRule = CreateTestRule();
        var violations = new List<StyleViolation>
        {
            new(testRule, "Test violation", 0, 10, 1, 0, 1, 10, "matched", null, ViolationSeverity.Warning)
        };
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var completed = original.CompleteWith(violations, timestamp);

        // Assert
        completed.State.Should().Be(LintLifecycleState.Idle);
        completed.LastLintTime.Should().Be(timestamp);
        completed.LintCount.Should().Be(1);
        completed.LastViolations.Should().BeEquivalentTo(violations);
    }

    [Fact]
    public void CompleteWith_IncrementsLintCount()
    {
        // Arrange
        var state = new DocumentLintState { DocumentId = "doc-123" };
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        state = state.CompleteWith([], timestamp);
        state = state.CompleteWith([], timestamp.AddMinutes(1));
        state = state.CompleteWith([], timestamp.AddMinutes(2));

        // Assert
        state.LintCount.Should().Be(3);
    }

    [Fact]
    public void CancelToIdle_PreservesLastViolations()
    {
        // Arrange
        var testRule = CreateTestRule();
        var violations = new List<StyleViolation>
        {
            new(testRule, "Test violation", 0, 10, 1, 0, 1, 10, "matched", null, ViolationSeverity.Warning)
        };
        var original = new DocumentLintState { DocumentId = "doc-123" }
            .CompleteWith(violations, DateTimeOffset.UtcNow)
            .CreatePending()
            .StartAnalyzing();

        // Act
        var cancelled = original.CancelToIdle();

        // Assert
        cancelled.State.Should().Be(LintLifecycleState.Idle);
        cancelled.LastViolations.Should().BeEquivalentTo(violations);
        cancelled.LintCount.Should().Be(1); // Not incremented on cancel
    }

    [Fact]
    public void StateTransitions_ChainCorrectly()
    {
        // Arrange
        var state = new DocumentLintState { DocumentId = "doc-123" };

        // Act - full lifecycle
        var pending = state.CreatePending();
        var analyzing = pending.StartAnalyzing();
        var completed = analyzing.CompleteWith([], DateTimeOffset.UtcNow);

        // Assert
        state.State.Should().Be(LintLifecycleState.Idle);
        pending.State.Should().Be(LintLifecycleState.Pending);
        analyzing.State.Should().Be(LintLifecycleState.Analyzing);
        completed.State.Should().Be(LintLifecycleState.Idle);
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

