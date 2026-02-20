// -----------------------------------------------------------------------
// <copyright file="ExpressionEvaluator.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;
using DynamicExpresso;

namespace Lexichord.Modules.Agents.Workflows;

/// <summary>
/// Expression evaluator using DynamicExpresso for safe evaluation.
/// </summary>
/// <remarks>
/// <para>
/// Provides sandboxed expression evaluation for workflow step conditions. Uses
/// DynamicExpresso as the underlying interpreter, which provides a safe execution
/// environment without access to reflection, file system, or network resources.
/// </para>
/// <para>
/// Expressions can reference workflow variables by name and use the following
/// built-in functions:
/// </para>
/// <list type="bullet">
///   <item><description><c>count(items)</c> — Returns the count of an enumerable</description></item>
///   <item><description><c>any(items)</c> — Returns true if the enumerable has any elements</description></item>
///   <item><description><c>isEmpty(str)</c> — Returns true if the string is null or empty</description></item>
///   <item><description><c>contains(str, substr)</c> — Returns true if str contains substr</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.7b as part of the Workflow Engine.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var evaluator = new ExpressionEvaluator();
/// var variables = new Dictionary&lt;string, object&gt;
/// {
///     ["_previousStepSuccess"] = true,
///     ["wordCount"] = 150
/// };
///
/// // Boolean evaluation
/// bool result = evaluator.Evaluate&lt;bool&gt;("wordCount > 100", variables);
/// // result = true
///
/// // Validation
/// bool isValid = evaluator.IsValid("x > 5", out string? error);
/// // isValid = true, error = null
/// </code>
/// </example>
/// <seealso cref="IExpressionEvaluator"/>
/// <seealso cref="ExpressionEvaluationException"/>
internal class ExpressionEvaluator : IExpressionEvaluator
{
    // LOGIC: The DynamicExpresso Interpreter instance. Configured once at construction
    // with built-in functions and reused for all evaluations. Thread-safe for reads
    // since we only call Eval with parameters (no mutable state modification).
    private readonly Interpreter _interpreter;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionEvaluator"/> class.
    /// Configures the DynamicExpresso interpreter with built-in helper functions.
    /// </summary>
    public ExpressionEvaluator()
    {
        // LOGIC: Create a sandboxed interpreter with no reflection access.
        _interpreter = new Interpreter();

        // LOGIC: Register common utility functions available in all expressions.
        // These provide convenient access to collection and string operations
        // without requiring users to know C# syntax for LINQ methods.
        _interpreter.SetFunction("count", (Func<IEnumerable<object>, int>)(items => items.Count()));
        _interpreter.SetFunction("any", (Func<IEnumerable<object>, bool>)(items => items.Any()));
        _interpreter.SetFunction("isEmpty", (Func<string, bool>)(s => string.IsNullOrEmpty(s)));
        _interpreter.SetFunction("contains", (Func<string, string, bool>)((s, sub) => s.Contains(sub)));
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Converts workflow variables into DynamicExpresso Parameter objects,
    /// evaluates the expression, and converts the result to the requested type.
    /// Wraps all DynamicExpresso exceptions in <see cref="ExpressionEvaluationException"/>.
    /// </remarks>
    public T Evaluate<T>(string expression, IReadOnlyDictionary<string, object> variables)
    {
        try
        {
            // LOGIC: Build parameters from the variable dictionary.
            // Each variable becomes a named parameter accessible in the expression.
            var parameters = variables
                .Select(kv => new Parameter(kv.Key, kv.Value))
                .ToArray();

            // LOGIC: Evaluate the expression with the provided parameters.
            var result = _interpreter.Eval(expression, parameters);

            // LOGIC: Convert result to the requested type.
            // This handles cases like bool from comparison operators.
            return (T)Convert.ChangeType(result, typeof(T));
        }
        catch (Exception ex)
        {
            throw new ExpressionEvaluationException(
                $"Failed to evaluate expression: {expression}", ex);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Attempts to parse the expression without evaluating it. This catches
    /// syntax errors early during pre-flight validation without requiring variable values.
    /// Note: This cannot validate variable references since they are runtime-dependent.
    /// </remarks>
    public bool IsValid(string expression, out string? errorMessage)
    {
        try
        {
            // LOGIC: Parse validates syntax without requiring variable bindings.
            _interpreter.Parse(expression);
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Uses regex to extract all identifiers (sequences of letters, digits,
    /// and underscores starting with a letter or underscore) from the expression.
    /// Filters out reserved words (true, false, null) and built-in function names
    /// to return only user-defined variable references.
    /// </remarks>
    public IReadOnlyList<string> GetReferencedVariables(string expression)
    {
        var variables = new HashSet<string>();

        // LOGIC: Match all identifier-like tokens in the expression.
        // This is a simple heuristic — it may include false positives for
        // identifiers that appear inside string literals, but that's acceptable
        // for the purpose of pre-flight variable validation.
        var matches = Regex.Matches(expression, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\b");
        foreach (Match match in matches)
        {
            var name = match.Groups[1].Value;

            // LOGIC: Filter out language keywords and built-in function names
            // since these are not user-provided variables.
            if (!IsReservedWord(name))
                variables.Add(name);
        }

        return variables.ToList();
    }

    /// <summary>
    /// Checks whether a name is a reserved word or built-in function.
    /// </summary>
    /// <param name="name">The identifier to check.</param>
    /// <returns><c>true</c> if the name is reserved; <c>false</c> if it's a user variable.</returns>
    /// <remarks>
    /// LOGIC: Reserved words include C# boolean/null literals and the built-in
    /// function names registered in the constructor.
    /// </remarks>
    private static bool IsReservedWord(string name) =>
        name is "true" or "false" or "null" or "and" or "or" or "not"
            or "count" or "any" or "isEmpty" or "contains";
}
