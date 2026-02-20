// -----------------------------------------------------------------------
// <copyright file="IExpressionEvaluator.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows;

/// <summary>
/// Evaluates conditional expressions for workflow step conditions.
/// Uses a sandboxed expression evaluator for safety.
/// </summary>
/// <remarks>
/// <para>
/// The expression evaluator provides safe, sandboxed evaluation of conditional
/// expressions used in <see cref="WorkflowStepCondition"/> with
/// <see cref="ConditionType.Expression"/> type. Expressions can reference workflow
/// variables and use built-in functions.
/// </para>
/// <para>
/// <b>Built-in functions:</b>
/// </para>
/// <list type="bullet">
///   <item><description><c>count(items)</c> — Returns the count of an enumerable</description></item>
///   <item><description><c>any(items)</c> — Returns true if the enumerable has any elements</description></item>
///   <item><description><c>isEmpty(str)</c> — Returns true if the string is null or empty</description></item>
///   <item><description><c>contains(str, substr)</c> — Returns true if str contains substr</description></item>
/// </list>
/// <para>
/// <b>Security:</b> Expressions are evaluated in a sandboxed interpreter with no
/// reflection access, no file system access, and no network access.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7b as part of the Workflow Engine.
/// </para>
/// </remarks>
/// <seealso cref="WorkflowStepCondition"/>
/// <seealso cref="ExpressionEvaluationException"/>
public interface IExpressionEvaluator
{
    /// <summary>
    /// Evaluates an expression against the current variable context.
    /// </summary>
    /// <typeparam name="T">Expected result type (typically <see cref="bool"/> for conditions).</typeparam>
    /// <param name="expression">Expression to evaluate (e.g., "_previousStepSuccess == true").</param>
    /// <param name="variables">Variable context providing named values for the expression.</param>
    /// <returns>The evaluation result cast to <typeparamref name="T"/>.</returns>
    /// <exception cref="ExpressionEvaluationException">
    /// Thrown if the expression is invalid, contains undefined variables, or the
    /// result cannot be converted to <typeparamref name="T"/>.
    /// </exception>
    /// <remarks>
    /// LOGIC: Variables are passed as DynamicExpresso parameters. The expression is
    /// parsed and evaluated against these parameters using the sandboxed interpreter.
    /// The result is converted to the requested type via <see cref="Convert.ChangeType"/>.
    /// </remarks>
    T Evaluate<T>(string expression, IReadOnlyDictionary<string, object> variables);

    /// <summary>
    /// Validates that an expression is syntactically correct.
    /// </summary>
    /// <param name="expression">Expression to validate.</param>
    /// <param name="errorMessage">Error message if the expression is invalid; <c>null</c> if valid.</param>
    /// <returns><c>true</c> if the expression can be parsed; <c>false</c> otherwise.</returns>
    /// <remarks>
    /// LOGIC: Attempts to parse the expression without evaluating it. Used by
    /// <see cref="IWorkflowEngine.ValidateExecution"/> for pre-flight validation.
    /// </remarks>
    bool IsValid(string expression, out string? errorMessage);

    /// <summary>
    /// Gets the list of variables referenced in an expression.
    /// </summary>
    /// <param name="expression">Expression to analyze.</param>
    /// <returns>List of variable names referenced in the expression.</returns>
    /// <remarks>
    /// LOGIC: Uses regex to extract identifiers from the expression, filtering out
    /// reserved words (true, false, null) and built-in function names (count, any,
    /// isEmpty, contains).
    /// </remarks>
    IReadOnlyList<string> GetReferencedVariables(string expression);
}
