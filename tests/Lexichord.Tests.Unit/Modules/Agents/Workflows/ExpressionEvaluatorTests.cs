// -----------------------------------------------------------------------
// <copyright file="ExpressionEvaluatorTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.Agents.Workflows;

namespace Lexichord.Tests.Unit.Modules.Agents.Workflows;

/// <summary>
/// Unit tests for <see cref="ExpressionEvaluator"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tests the DynamicExpresso-based expression evaluator including simple comparisons,
/// variable access, error handling, syntax validation, and variable reference extraction.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-v0.7.7b §6.2
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Version", "v0.7.7b")]
public class ExpressionEvaluatorTests
{
    private readonly ExpressionEvaluator _sut;

    public ExpressionEvaluatorTests()
    {
        _sut = new ExpressionEvaluator();
    }

    // ── Test 1: Simple Comparison ─────────────────────────────────────────

    /// <summary>
    /// Verifies that a simple arithmetic comparison expression evaluates correctly.
    /// </summary>
    [Fact]
    public void Evaluate_SimpleComparison_ReturnsCorrectResult()
    {
        // Arrange
        var variables = new Dictionary<string, object>
        {
            ["x"] = 10
        };

        // Act
        var result = _sut.Evaluate<bool>("x > 5", variables);

        // Assert
        result.Should().BeTrue();
    }

    // ── Test 2: Variable Access ───────────────────────────────────────────

    /// <summary>
    /// Verifies that expression evaluation resolves variables from the context.
    /// </summary>
    [Fact]
    public void Evaluate_VariableAccess_ResolvesFromContext()
    {
        // Arrange
        var variables = new Dictionary<string, object>
        {
            ["_previousStepSuccess"] = true,
            ["wordCount"] = 150
        };

        // Act
        var result = _sut.Evaluate<bool>("_previousStepSuccess == true && wordCount > 100", variables);

        // Assert
        result.Should().BeTrue();
    }

    // ── Test 3: Invalid Expression ────────────────────────────────────────

    /// <summary>
    /// Verifies that evaluating an invalid expression throws ExpressionEvaluationException.
    /// </summary>
    [Fact]
    public void Evaluate_InvalidExpression_ThrowsException()
    {
        // Arrange
        var variables = new Dictionary<string, object>();

        // Act
        var act = () => _sut.Evaluate<bool>(">>>invalid<<<", variables);

        // Assert
        act.Should().Throw<ExpressionEvaluationException>();
    }

    // ── Test 4: IsValid ───────────────────────────────────────────────────

    /// <summary>
    /// Verifies that IsValid returns true for syntactically correct expressions.
    /// </summary>
    [Fact]
    public void IsValid_ValidExpression_ReturnsTrue()
    {
        // Act
        var result = _sut.IsValid("1 + 2", out var errorMessage);

        // Assert
        result.Should().BeTrue();
        errorMessage.Should().BeNull();
    }

    // ── Test 5: GetReferencedVariables ─────────────────────────────────────

    /// <summary>
    /// Verifies that GetReferencedVariables extracts identifiers and filters
    /// out reserved words.
    /// </summary>
    [Fact]
    public void GetReferencedVariables_ReturnsIdentifiers()
    {
        // Act
        var variables = _sut.GetReferencedVariables("wordCount > 100 && _previousStepSuccess == true");

        // Assert
        variables.Should().Contain("wordCount");
        variables.Should().Contain("_previousStepSuccess");
        variables.Should().NotContain("true");
    }
}
