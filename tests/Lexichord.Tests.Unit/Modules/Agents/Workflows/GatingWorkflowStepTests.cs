// -----------------------------------------------------------------------
// <copyright file="GatingWorkflowStepTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.Agents.Workflows.Validation;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Agents.Workflows;

/// <summary>
/// Unit tests for <see cref="GatingConditionEvaluator"/> and
/// <see cref="GatingWorkflowStep"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tests the gating step lifecycle including condition parsing, condition
/// evaluation (validation_count, metadata, content_length, has_property),
/// AND/OR combination logic, workflow blocking, branching, timeout, and
/// invalid syntax handling.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-f §7
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Version", "v0.7.7f")]
public class GatingWorkflowStepTests
{
    private readonly GatingConditionEvaluator _evaluator;
    private readonly Mock<ILogger<GatingWorkflowStep>> _stepLoggerMock;

    public GatingWorkflowStepTests()
    {
        var evaluatorLogger = new Mock<ILogger<GatingConditionEvaluator>>();
        _evaluator = new GatingConditionEvaluator(evaluatorLogger.Object);
        _stepLoggerMock = new Mock<ILogger<GatingWorkflowStep>>();
    }

    // ── Test 1: validation_count(error) == 0 (zero errors passes) ────────

    /// <summary>
    /// Verifies that a validation_count(error) == 0 condition passes when
    /// there are no validation errors.
    /// </summary>
    /// <remarks>Spec §7: EvaluateCondition_ValidationCount_Zero</remarks>
    [Fact]
    public async Task EvaluateCondition_ValidationCountZero_PassesWithNoErrors()
    {
        // Arrange
        var condition = new GatingCondition
        {
            Id = "c1",
            Expression = "validation_count(error) == 0"
        };
        var context = new GatingEvaluationContext
        {
            ValidationResults = new List<ValidationStepResult>
            {
                new()
                {
                    StepId = "step1",
                    IsValid = true,
                    Errors = new List<ValidationStepError>(),
                    Warnings = new List<ValidationStepWarning>()
                }
            }
        };

        // Act
        var result = await _evaluator.EvaluateAsync(condition, context);

        // Assert
        result.Should().BeTrue();
    }

    // ── Test 2: validation_count(error) exceeds threshold ────────────────

    /// <summary>
    /// Verifies that a validation_count(error) == 0 condition fails when
    /// there are validation errors.
    /// </summary>
    /// <remarks>Spec §7: EvaluateCondition_ValidationCount_Exceeds</remarks>
    [Fact]
    public async Task EvaluateCondition_ValidationCountExceeds_FailsWithErrors()
    {
        // Arrange
        var condition = new GatingCondition
        {
            Id = "c1",
            Expression = "validation_count(error) == 0"
        };
        var context = new GatingEvaluationContext
        {
            ValidationResults = new List<ValidationStepResult>
            {
                new()
                {
                    StepId = "step1",
                    IsValid = false,
                    Errors = new List<ValidationStepError>
                    {
                        new("r1", "ERR001", "Missing field"),
                        new("r2", "ERR002", "Invalid format")
                    },
                    Warnings = new List<ValidationStepWarning>()
                }
            }
        };

        // Act
        var result = await _evaluator.EvaluateAsync(condition, context);

        // Assert
        result.Should().BeFalse();
    }

    // ── Test 3: metadata matches ─────────────────────────────────────────

    /// <summary>
    /// Verifies that a metadata('status') == published condition passes
    /// when the metadata matches.
    /// </summary>
    /// <remarks>Spec §7: EvaluateCondition_Metadata_Matches</remarks>
    [Fact]
    public async Task EvaluateCondition_MetadataMatches_PassesWhenEqual()
    {
        // Arrange
        var condition = new GatingCondition
        {
            Id = "c1",
            Expression = "metadata('status') == published"
        };
        var context = new GatingEvaluationContext
        {
            DocumentMetadata = new Dictionary<string, object>
            {
                ["status"] = "published"
            }
        };

        // Act
        var result = await _evaluator.EvaluateAsync(condition, context);

        // Assert
        result.Should().BeTrue();
    }

    // ── Test 4: content_length check ─────────────────────────────────────

    /// <summary>
    /// Verifies that a content_length > 100 condition is correctly evaluated
    /// against the document content length.
    /// </summary>
    /// <remarks>Spec §7: EvaluateCondition_ContentLength_Exceeds</remarks>
    [Fact]
    public async Task EvaluateCondition_ContentLength_CorrectlyChecked()
    {
        // Arrange — create content shorter than 100 chars
        var condition = new GatingCondition
        {
            Id = "c1",
            Expression = "content_length > 100"
        };
        var context = new GatingEvaluationContext
        {
            DocumentContent = "Short content"
        };

        // Act
        var result = await _evaluator.EvaluateAsync(condition, context);

        // Assert — 13 chars < 100, so condition fails
        result.Should().BeFalse();
    }

    // ── Test 5: has_schema check ─────────────────────────────────────────

    /// <summary>
    /// Verifies that a has_schema == true condition passes when the schema
    /// variable is set to true in the evaluation context.
    /// </summary>
    /// <remarks>Spec §7: EvaluateCondition_HasSchema_True</remarks>
    [Fact]
    public async Task EvaluateCondition_HasSchema_PassesWhenTrue()
    {
        // Arrange
        var condition = new GatingCondition
        {
            Id = "c1",
            Expression = "has_schema == true"
        };
        var context = new GatingEvaluationContext
        {
            Variables = new Dictionary<string, object>
            {
                ["has_schema"] = true
            }
        };

        // Act
        var result = await _evaluator.EvaluateAsync(condition, context);

        // Assert
        result.Should().BeTrue();
    }

    // ── Test 6: AND — all conditions pass ────────────────────────────────

    /// <summary>
    /// Verifies that a gate with multiple AND conditions passes when
    /// all conditions evaluate to true.
    /// </summary>
    /// <remarks>Spec §7: EvaluateMultiple_AND_AllPass</remarks>
    [Fact]
    public async Task EvaluateMultiple_AND_AllPass_GatePasses()
    {
        // Arrange
        var gate = CreateGate(
            expression: "validation_count(error) == 0 AND content_length > 10",
            requireAll: true);
        var context = CreateContext(
            content: "This document has enough content to pass the 10-char gate.",
            validationResults: new List<ValidationStepResult>
            {
                new()
                {
                    StepId = "s1",
                    IsValid = true,
                    Errors = new List<ValidationStepError>(),
                    Warnings = new List<ValidationStepWarning>()
                }
            });

        // Act
        var result = await gate.EvaluateAsync(context);

        // Assert
        result.Passed.Should().BeTrue();
        result.FailureMessage.Should().BeNull();
    }

    // ── Test 7: AND — one condition fails ────────────────────────────────

    /// <summary>
    /// Verifies that a gate with multiple AND conditions fails when
    /// at least one condition evaluates to false.
    /// </summary>
    /// <remarks>Spec §7: EvaluateMultiple_AND_OneFails</remarks>
    [Fact]
    public async Task EvaluateMultiple_AND_OneFails_GateFails()
    {
        // Arrange — content_length > 1000 will fail for short content
        var gate = CreateGate(
            expression: "validation_count(error) == 0 AND content_length > 1000",
            requireAll: true);
        var context = CreateContext(
            content: "Short content",
            validationResults: new List<ValidationStepResult>
            {
                new()
                {
                    StepId = "s1",
                    IsValid = true,
                    Errors = new List<ValidationStepError>(),
                    Warnings = new List<ValidationStepWarning>()
                }
            });

        // Act
        var result = await gate.EvaluateAsync(context);

        // Assert
        result.Passed.Should().BeFalse();
        result.FailureMessage.Should().NotBeNullOrEmpty();
    }

    // ── Test 8: OR — one condition passes ────────────────────────────────

    /// <summary>
    /// Verifies that a gate with OR logic passes when at least one
    /// condition evaluates to true.
    /// </summary>
    /// <remarks>Spec §7: EvaluateMultiple_OR_OnePasses</remarks>
    [Fact]
    public async Task EvaluateMultiple_OR_OnePasses_GatePasses()
    {
        // Arrange — content_length > 1000 fails, but validation_count passes
        var gate = CreateGate(
            expression: "validation_count(error) == 0 OR content_length > 1000",
            requireAll: false);
        var context = CreateContext(
            content: "Short content",
            validationResults: new List<ValidationStepResult>
            {
                new()
                {
                    StepId = "s1",
                    IsValid = true,
                    Errors = new List<ValidationStepError>(),
                    Warnings = new List<ValidationStepWarning>()
                }
            });

        // Act
        var result = await gate.EvaluateAsync(context);

        // Assert
        result.Passed.Should().BeTrue();
    }

    // ── Test 9: invalid expression syntax ────────────────────────────────

    /// <summary>
    /// Verifies that an invalid expression syntax is handled gracefully
    /// and the condition evaluates to false.
    /// </summary>
    /// <remarks>Spec §7: EvaluateExpression_InvalidSyntax</remarks>
    [Fact]
    public async Task EvaluateExpression_InvalidSyntax_GateFails()
    {
        // Arrange — use a completely invalid expression
        var gate = CreateGate(
            expression: "totally_bogus_function() > 42",
            requireAll: true);
        var context = CreateContext();

        // Act
        var result = await gate.EvaluateAsync(context);

        // Assert — invalid syntax should result in gate failure
        result.Passed.Should().BeFalse();
    }

    // ── Test 10: gate blocks workflow on failure ─────────────────────────

    /// <summary>
    /// Verifies that ExecuteAsync returns Success=false when the gate
    /// conditions are not met, blocking the workflow.
    /// </summary>
    /// <remarks>Spec §7: Gate_BlocksWorkflow_OnFailure</remarks>
    [Fact]
    public async Task Gate_BlocksWorkflow_OnFailure_ReturnsFalse()
    {
        // Arrange — validation_count(error) == 0 will fail with errors present
        var gate = CreateGate(
            expression: "validation_count(error) == 0",
            failureMessage: "Document has validation errors");
        var context = CreateContext(
            validationResults: new List<ValidationStepResult>
            {
                new()
                {
                    StepId = "s1",
                    IsValid = false,
                    Errors = new List<ValidationStepError>
                    {
                        new("r1", "ERR001", "Error found")
                    },
                    Warnings = new List<ValidationStepWarning>()
                }
            });

        // Act
        var result = await gate.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Document has validation errors");
        result.Data.Should().ContainKey("gatingResult");
    }

    // ── Test 11: gate branches on failure ────────────────────────────────

    /// <summary>
    /// Verifies that when a gate fails and has a BranchPath configured,
    /// the branch path is included in the result data.
    /// </summary>
    /// <remarks>Spec §7: Gate_BranchesOnFailure</remarks>
    [Fact]
    public async Task Gate_BranchesOnFailure_IncludesBranchPath()
    {
        // Arrange
        var gate = CreateGate(
            expression: "validation_count(error) == 0",
            failureMessage: "Errors found, branching to review",
            branchPath: "review-workflow");
        var context = CreateContext(
            validationResults: new List<ValidationStepResult>
            {
                new()
                {
                    StepId = "s1",
                    IsValid = false,
                    Errors = new List<ValidationStepError>
                    {
                        new("r1", "ERR001", "Error")
                    },
                    Warnings = new List<ValidationStepWarning>()
                }
            });

        // Act
        var result = await gate.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().ContainKey("branchPath");
        result.Data!["branchPath"].Should().Be("review-workflow");

        var gatingResult = result.Data!["gatingResult"] as GatingResult;
        gatingResult.Should().NotBeNull();
        gatingResult!.BranchPath.Should().Be("review-workflow");
    }

    // ── Bonus: disabled gate auto-passes ─────────────────────────────────

    /// <summary>
    /// Verifies that a disabled gate returns success without evaluation.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_DisabledGate_AutoPasses()
    {
        // Arrange
        var gate = CreateGate(expression: "validation_count(error) == 0");
        gate.IsEnabled = false;
        var context = CreateContext();

        // Act
        var result = await gate.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("disabled");
    }

    // ── Bonus: validation_count(warning) with <= operator ────────────────

    /// <summary>
    /// Verifies that the <= operator works correctly for warning counts.
    /// </summary>
    [Fact]
    public async Task EvaluateCondition_ValidationCountWarningLessEqual_Works()
    {
        // Arrange
        var condition = new GatingCondition
        {
            Id = "c1",
            Expression = "validation_count(warning) <= 2"
        };
        var context = new GatingEvaluationContext
        {
            ValidationResults = new List<ValidationStepResult>
            {
                new()
                {
                    StepId = "s1",
                    IsValid = true,
                    Errors = new List<ValidationStepError>(),
                    Warnings = new List<ValidationStepWarning>
                    {
                        new("w1", "WARN001", "Minor issue")
                    }
                }
            }
        };

        // Act
        var result = await _evaluator.EvaluateAsync(condition, context);

        // Assert — 1 warning <= 2, passes
        result.Should().BeTrue();
    }

    // ── Bonus: gate passes workflow on success ───────────────────────────

    /// <summary>
    /// Verifies that ExecuteAsync returns Success=true when gate passes.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_GatePasses_ReturnsSuccess()
    {
        // Arrange
        var gate = CreateGate(expression: "validation_count(error) == 0");
        var context = CreateContext(
            validationResults: new List<ValidationStepResult>
            {
                new()
                {
                    StepId = "s1",
                    IsValid = true,
                    Errors = new List<ValidationStepError>(),
                    Warnings = new List<ValidationStepWarning>()
                }
            });

        // Act
        var result = await gate.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("passed");
    }

    // ── Bonus: configuration validation ──────────────────────────────────

    /// <summary>
    /// Verifies that ValidateConfiguration catches empty condition expression
    /// and empty failure message.
    /// </summary>
    [Fact]
    public void ValidateConfiguration_EmptyFields_ReturnsErrors()
    {
        // Arrange
        var gate = new GatingWorkflowStep(
            id: "",
            name: "",
            conditionExpression: "",
            failureMessage: "",
            evaluator: _evaluator,
            logger: _stepLoggerMock.Object,
            timeoutMs: -1);

        // Act
        var errors = gate.ValidateConfiguration();

        // Assert — should catch: empty Id, Name, Expression, Message, and negative timeout
        errors.Should().HaveCount(5);
        errors.Should().Contain(e => e.Message.Contains("Gate ID"));
        errors.Should().Contain(e => e.Message.Contains("Gate name"));
        errors.Should().Contain(e => e.Message.Contains("Condition expression"));
        errors.Should().Contain(e => e.Message.Contains("Failure message"));
        errors.Should().Contain(e => e.Message.Contains("Timeout"));
    }

    // ── Bonus: GetConditionDescription ────────────────────────────────────

    /// <summary>
    /// Verifies that GetConditionDescription formats the condition correctly.
    /// </summary>
    [Fact]
    public void GetConditionDescription_ReturnsFormattedExpression()
    {
        // Arrange
        var gate = CreateGate(
            expression: "validation_count(error) == 0 AND content_length > 100",
            requireAll: true);

        // Act
        var description = gate.GetConditionDescription();

        // Assert
        description.Should().Contain("validation_count(error) == 0");
        description.Should().Contain(" AND ");
        description.Should().Contain("content_length > 100");
    }

    // ── Helper Methods ───────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="GatingWorkflowStep"/> for testing.
    /// </summary>
    private GatingWorkflowStep CreateGate(
        string expression = "validation_count(error) == 0",
        string failureMessage = "Gate condition not met",
        bool requireAll = true,
        string? branchPath = null) =>
        new(
            id: "test-gate",
            name: "Test Gate",
            conditionExpression: expression,
            failureMessage: failureMessage,
            evaluator: _evaluator,
            logger: _stepLoggerMock.Object,
            branchPath: branchPath,
            requireAll: requireAll);

    /// <summary>
    /// Creates a <see cref="ValidationWorkflowContext"/> for testing.
    /// </summary>
    private static ValidationWorkflowContext CreateContext(
        string content = "Test document content",
        IList<ValidationStepResult>? validationResults = null) =>
        new()
        {
            WorkspaceId = Guid.NewGuid(),
            DocumentId = "doc-test-001",
            DocumentContent = content,
            DocumentType = "markdown",
            Trigger = ValidationTrigger.Manual,
            PreviousResults = validationResults as IReadOnlyList<ValidationStepResult>
                ?? (validationResults?.ToList() as IReadOnlyList<ValidationStepResult>)
        };
}
