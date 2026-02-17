// -----------------------------------------------------------------------
// <copyright file="FixWorkflowContractTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// Unit tests for the v0.7.5h Combined Fix Workflow contract types.
//   Tests cover FixWorkflowOptions, FixApplyResult, FixConflictCase,
//   FixTransaction, enums, exceptions, and event args.
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;
using Lexichord.Abstractions.Contracts.Validation;
using Lexichord.Abstractions.Contracts.Validation.Events;

// Alias to disambiguate from Knowledge.Validation.Integration.UnifiedFix
using UnifiedFix = Lexichord.Abstractions.Contracts.Validation.UnifiedFix;

namespace Lexichord.Tests.Unit.Abstractions.Validation;

/// <summary>
/// Unit tests for the v0.7.5h Combined Fix Workflow contract types:
/// <see cref="FixWorkflowOptions"/>, <see cref="FixApplyResult"/>,
/// <see cref="FixConflictCase"/>, <see cref="FixTransaction"/>,
/// <see cref="ConflictHandlingStrategy"/>, <see cref="FixConflictType"/>,
/// <see cref="FixConflictSeverity"/>, exceptions, and event args.
/// </summary>
/// <remarks>
/// <para>
/// Test Groups:
/// <list type="bullet">
///   <item><description>FixWorkflowOptions — Default values, static Default, with-expression</description></item>
///   <item><description>FixApplyResult — Empty factory, computed properties, FixOperationTrace</description></item>
///   <item><description>FixConflictCase — Construction, IsBlocking by severity</description></item>
///   <item><description>FixTransaction — Construction, FixCount computed property</description></item>
///   <item><description>Enums — ConflictHandlingStrategy, FixConflictType, FixConflictSeverity</description></item>
///   <item><description>Exceptions — FixConflictException, FixApplicationTimeoutException, DocumentCorruptionException</description></item>
///   <item><description>Event Args — FixesAppliedEventArgs, FixConflictDetectedEventArgs</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5h as part of the Combined Fix Workflow feature.
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.5h")]
public class FixWorkflowContractTests : IDisposable
{
    /// <inheritdoc />
    public void Dispose()
    {
        // No resources to dispose — pattern kept for consistency.
    }

    #region FixWorkflowOptions Tests

    /// <summary>
    /// Verifies that a default-constructed FixWorkflowOptions has the expected default values.
    /// </summary>
    [Fact]
    public void FixWorkflowOptions_DefaultConstructor_HasExpectedDefaults()
    {
        // Act
        var options = new FixWorkflowOptions();

        // Assert
        options.DryRun.Should().BeFalse();
        options.ConflictStrategy.Should().Be(ConflictHandlingStrategy.SkipConflicting);
        options.ReValidateAfterFixes.Should().BeTrue();
        options.MaxFixIterations.Should().Be(3);
        options.EnableUndo.Should().BeTrue();
        options.Timeout.Should().Be(TimeSpan.FromSeconds(5));
        options.Verbose.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that the static Default property returns an instance with correct defaults.
    /// </summary>
    [Fact]
    public void FixWorkflowOptions_Default_ReturnsCorrectDefaults()
    {
        // Act
        var options = FixWorkflowOptions.Default;

        // Assert
        options.Should().NotBeNull();
        options.DryRun.Should().BeFalse();
        options.ConflictStrategy.Should().Be(ConflictHandlingStrategy.SkipConflicting);
        options.ReValidateAfterFixes.Should().BeTrue();
        options.MaxFixIterations.Should().Be(3);
        options.EnableUndo.Should().BeTrue();
        options.Timeout.Should().Be(TimeSpan.FromSeconds(5));
        options.Verbose.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that a with-expression creates a modified copy without changing the original.
    /// </summary>
    [Fact]
    public void FixWorkflowOptions_WithExpression_CreatesModifiedCopy()
    {
        // Arrange
        var original = FixWorkflowOptions.Default;

        // Act
        var modified = original with
        {
            DryRun = true,
            MaxFixIterations = 10,
            Verbose = true
        };

        // Assert
        modified.DryRun.Should().BeTrue();
        modified.MaxFixIterations.Should().Be(10);
        modified.Verbose.Should().BeTrue();
        // Unchanged properties should carry over
        modified.ConflictStrategy.Should().Be(ConflictHandlingStrategy.SkipConflicting);
        modified.ReValidateAfterFixes.Should().BeTrue();
        modified.EnableUndo.Should().BeTrue();
        modified.Timeout.Should().Be(TimeSpan.FromSeconds(5));
        // Original should be unaffected
        original.DryRun.Should().BeFalse();
        original.MaxFixIterations.Should().Be(3);
        original.Verbose.Should().BeFalse();
    }

    #endregion

    #region FixApplyResult Tests

    /// <summary>
    /// Verifies that the Empty factory creates a successful result with zero counts.
    /// </summary>
    [Fact]
    public void FixApplyResult_Empty_CreatesSuccessfulResultWithZeroCounts()
    {
        // Arrange
        var duration = TimeSpan.FromMilliseconds(42);

        // Act
        var result = FixApplyResult.Empty(duration);

        // Assert
        result.Success.Should().BeTrue();
        result.AppliedCount.Should().Be(0);
        result.SkippedCount.Should().Be(0);
        result.FailedCount.Should().Be(0);
        result.Duration.Should().Be(duration);
        result.ResolvedIssues.Should().BeEmpty();
        result.RemainingIssues.Should().BeEmpty();
        result.ConflictingIssues.Should().BeEmpty();
        result.DetectedConflicts.Should().BeEmpty();
        result.OperationTrace.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that TotalCount, HasAppliedFixes, HasRemainingIssues, and HasConflicts
    /// return correct computed values.
    /// </summary>
    [Fact]
    public void FixApplyResult_ComputedProperties_ReturnCorrectValues()
    {
        // Arrange
        var remainingIssue = CreateTestIssue();
        var conflict = new FixConflictCase
        {
            Type = FixConflictType.OverlappingPositions,
            ConflictingIssueIds = [Guid.NewGuid(), Guid.NewGuid()],
            Description = "Overlapping spans"
        };

        var result = new FixApplyResult
        {
            Success = false,
            AppliedCount = 3,
            SkippedCount = 2,
            FailedCount = 1,
            RemainingIssues = [remainingIssue],
            DetectedConflicts = [conflict]
        };

        // Assert
        result.TotalCount.Should().Be(6);
        result.HasAppliedFixes.Should().BeTrue();
        result.HasRemainingIssues.Should().BeTrue();
        result.HasConflicts.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that computed boolean properties return false on an empty result.
    /// </summary>
    [Fact]
    public void FixApplyResult_EmptyResult_ComputedBooleansAreFalse()
    {
        // Arrange
        var result = FixApplyResult.Empty(TimeSpan.Zero);

        // Assert
        result.HasAppliedFixes.Should().BeFalse();
        result.HasRemainingIssues.Should().BeFalse();
        result.HasConflicts.Should().BeFalse();
        result.TotalCount.Should().Be(0);
    }

    /// <summary>
    /// Verifies that FixOperationTrace can be constructed with all parameters.
    /// </summary>
    [Fact]
    public void FixOperationTrace_Constructor_SetsAllProperties()
    {
        // Arrange
        var issueId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;

        // Act
        var trace = new FixOperationTrace(
            issueId,
            timestamp,
            "Replace text at position 10",
            "Success",
            null);

        // Assert
        trace.IssueId.Should().Be(issueId);
        trace.Timestamp.Should().Be(timestamp);
        trace.Operation.Should().Be("Replace text at position 10");
        trace.Status.Should().Be("Success");
        trace.ErrorMessage.Should().BeNull();
    }

    #endregion

    #region FixConflictCase Tests

    /// <summary>
    /// Verifies that FixConflictCase can be constructed with all required properties.
    /// </summary>
    [Fact]
    public void FixConflictCase_Constructor_SetsAllProperties()
    {
        // Arrange
        var issueId1 = Guid.NewGuid();
        var issueId2 = Guid.NewGuid();

        // Act
        var conflict = new FixConflictCase
        {
            Type = FixConflictType.ContradictorySuggestions,
            ConflictingIssueIds = [issueId1, issueId2],
            Description = "Both fixes target the same range with different text",
            SuggestedResolution = "Apply the higher-confidence fix",
            Severity = FixConflictSeverity.Error
        };

        // Assert
        conflict.Type.Should().Be(FixConflictType.ContradictorySuggestions);
        conflict.ConflictingIssueIds.Should().HaveCount(2);
        conflict.ConflictingIssueIds.Should().Contain(issueId1);
        conflict.ConflictingIssueIds.Should().Contain(issueId2);
        conflict.Description.Should().Be("Both fixes target the same range with different text");
        conflict.SuggestedResolution.Should().Be("Apply the higher-confidence fix");
        conflict.Severity.Should().Be(FixConflictSeverity.Error);
    }

    /// <summary>
    /// Verifies that IsBlocking returns true for Error severity and false otherwise.
    /// </summary>
    [Theory]
    [InlineData(FixConflictSeverity.Error, true)]
    [InlineData(FixConflictSeverity.Warning, false)]
    [InlineData(FixConflictSeverity.Info, false)]
    public void FixConflictCase_IsBlocking_ReturnsCorrectValueBySeverity(
        FixConflictSeverity severity, bool expectedBlocking)
    {
        // Arrange
        var conflict = new FixConflictCase
        {
            Type = FixConflictType.OverlappingPositions,
            ConflictingIssueIds = [Guid.NewGuid()],
            Description = "Test conflict",
            Severity = severity
        };

        // Act & Assert
        conflict.IsBlocking.Should().Be(expectedBlocking);
    }

    #endregion

    #region FixTransaction Tests

    /// <summary>
    /// Verifies that FixTransaction can be constructed and FixCount is computed correctly.
    /// </summary>
    [Fact]
    public void FixTransaction_Constructor_SetsPropertiesAndComputesFixCount()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var issueIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var appliedAt = DateTime.UtcNow;

        // Act
        var transaction = new FixTransaction
        {
            Id = transactionId,
            DocumentPath = "/documents/test.md",
            DocumentBefore = "Original content",
            DocumentAfter = "Modified content",
            FixedIssueIds = issueIds,
            AppliedAt = appliedAt,
            IsUndone = false
        };

        // Assert
        transaction.Id.Should().Be(transactionId);
        transaction.DocumentPath.Should().Be("/documents/test.md");
        transaction.DocumentBefore.Should().Be("Original content");
        transaction.DocumentAfter.Should().Be("Modified content");
        transaction.FixedIssueIds.Should().HaveCount(3);
        transaction.AppliedAt.Should().Be(appliedAt);
        transaction.IsUndone.Should().BeFalse();
        transaction.FixCount.Should().Be(3);
    }

    #endregion

    #region Enum Tests

    /// <summary>
    /// Verifies that ConflictHandlingStrategy has the expected values.
    /// </summary>
    [Theory]
    [InlineData(ConflictHandlingStrategy.ThrowException, 0)]
    [InlineData(ConflictHandlingStrategy.SkipConflicting, 1)]
    [InlineData(ConflictHandlingStrategy.PromptUser, 2)]
    [InlineData(ConflictHandlingStrategy.PriorityBased, 3)]
    public void ConflictHandlingStrategy_HasExpectedValues(
        ConflictHandlingStrategy strategy, int expectedValue)
    {
        // Assert
        ((int)strategy).Should().Be(expectedValue);
    }

    /// <summary>
    /// Verifies that FixConflictType has 5 values with expected ordinals.
    /// </summary>
    [Theory]
    [InlineData(FixConflictType.OverlappingPositions, 0)]
    [InlineData(FixConflictType.ContradictorySuggestions, 1)]
    [InlineData(FixConflictType.DependentFixes, 2)]
    [InlineData(FixConflictType.CreatesNewIssue, 3)]
    [InlineData(FixConflictType.InvalidLocation, 4)]
    public void FixConflictType_HasExpectedValues(
        FixConflictType conflictType, int expectedValue)
    {
        // Assert
        ((int)conflictType).Should().Be(expectedValue);
    }

    /// <summary>
    /// Verifies that FixConflictSeverity has 3 values with expected ordinals.
    /// </summary>
    [Theory]
    [InlineData(FixConflictSeverity.Info, 0)]
    [InlineData(FixConflictSeverity.Warning, 1)]
    [InlineData(FixConflictSeverity.Error, 2)]
    public void FixConflictSeverity_HasExpectedValues(
        FixConflictSeverity severity, int expectedValue)
    {
        // Assert
        ((int)severity).Should().Be(expectedValue);
    }

    #endregion

    #region Exception Tests

    /// <summary>
    /// Verifies that FixConflictException stores conflicts and message.
    /// </summary>
    [Fact]
    public void FixConflictException_Constructor_StoresConflictsAndMessage()
    {
        // Arrange
        var conflicts = new List<FixConflictCase>
        {
            new()
            {
                Type = FixConflictType.OverlappingPositions,
                ConflictingIssueIds = [Guid.NewGuid(), Guid.NewGuid()],
                Description = "Overlap at position 10-15",
                Severity = FixConflictSeverity.Error
            }
        };
        var message = "2 conflicts detected between fixes";

        // Act
        var exception = new FixConflictException(conflicts, message);

        // Assert
        exception.Conflicts.Should().HaveCount(1);
        exception.Conflicts[0].Type.Should().Be(FixConflictType.OverlappingPositions);
        exception.Message.Should().Be(message);
    }

    /// <summary>
    /// Verifies that FixApplicationTimeoutException stores applied count and elapsed duration.
    /// </summary>
    [Fact]
    public void FixApplicationTimeoutException_Constructor_StoresCountAndDuration()
    {
        // Arrange
        var appliedCount = 5;
        var duration = TimeSpan.FromSeconds(10.5);

        // Act
        var exception = new FixApplicationTimeoutException(appliedCount, duration);

        // Assert
        exception.AppliedCount.Should().Be(5);
        exception.ElapsedDuration.Should().Be(duration);
        exception.Message.Should().Contain("10.5");
        exception.Message.Should().Contain("5 fixes applied");
    }

    /// <summary>
    /// Verifies that DocumentCorruptionException stores the message.
    /// </summary>
    [Fact]
    public void DocumentCorruptionException_Constructor_StoresMessage()
    {
        // Arrange
        var message = "Text at position 10 does not match expected content";

        // Act
        var exception = new DocumentCorruptionException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    #endregion

    #region Event Args Tests

    /// <summary>
    /// Verifies that FixesAppliedEventArgs stores DocumentPath, Result, and Timestamp.
    /// </summary>
    [Fact]
    public void FixesAppliedEventArgs_Constructor_StoresAllProperties()
    {
        // Arrange
        var documentPath = "/documents/chapter1.md";
        var result = FixApplyResult.Empty(TimeSpan.FromMilliseconds(100));
        var beforeCreation = DateTime.UtcNow;

        // Act
        var args = new FixesAppliedEventArgs(documentPath, result);

        // Assert
        args.DocumentPath.Should().Be(documentPath);
        args.Result.Should().Be(result);
        args.Timestamp.Should().BeCloseTo(beforeCreation, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Verifies that FixConflictDetectedEventArgs stores conflicts and computes
    /// HasBlockingConflicts and BlockingConflictCount correctly.
    /// </summary>
    [Fact]
    public void FixConflictDetectedEventArgs_Constructor_StoresConflictsAndComputesBlocking()
    {
        // Arrange
        var conflicts = new List<FixConflictCase>
        {
            new()
            {
                Type = FixConflictType.OverlappingPositions,
                ConflictingIssueIds = [Guid.NewGuid()],
                Description = "Blocking conflict",
                Severity = FixConflictSeverity.Error
            },
            new()
            {
                Type = FixConflictType.DependentFixes,
                ConflictingIssueIds = [Guid.NewGuid()],
                Description = "Non-blocking conflict",
                Severity = FixConflictSeverity.Warning
            }
        };
        var beforeCreation = DateTime.UtcNow;

        // Act
        var args = new FixConflictDetectedEventArgs("/documents/test.md", conflicts);

        // Assert
        args.DocumentPath.Should().Be("/documents/test.md");
        args.Conflicts.Should().HaveCount(2);
        args.HasBlockingConflicts.Should().BeTrue();
        args.BlockingConflictCount.Should().Be(1);
        args.Timestamp.Should().BeCloseTo(beforeCreation, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a test <see cref="UnifiedIssue"/> with sensible defaults.
    /// </summary>
    private static UnifiedIssue CreateTestIssue(
        IssueCategory category = IssueCategory.Grammar,
        UnifiedSeverity severity = UnifiedSeverity.Warning,
        int start = 10,
        int length = 5)
    {
        var fix = UnifiedFix.Replacement(
            new TextSpan(start, length),
            "old",
            "new",
            "Test fix",
            0.9);

        return new UnifiedIssue(
            Guid.NewGuid(),
            "TEST-001",
            category,
            severity,
            "Test issue",
            new TextSpan(start, length),
            "old",
            [fix],
            "TestSource",
            null);
    }

    #endregion
}
