// -----------------------------------------------------------------------
// <copyright file="ExpressionEvaluationException.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows;

/// <summary>
/// Exception thrown when expression evaluation fails.
/// </summary>
/// <remarks>
/// <para>
/// This exception wraps failures from the DynamicExpresso interpreter, including
/// syntax errors, undefined variable references, type conversion failures, and
/// evaluation timeout. The inner exception contains the original DynamicExpresso error.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7b as part of the Workflow Engine.
/// </para>
/// </remarks>
/// <seealso cref="IExpressionEvaluator"/>
/// <seealso cref="ExpressionEvaluator"/>
public class ExpressionEvaluationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionEvaluationException"/> class
    /// with a message and inner exception.
    /// </summary>
    /// <param name="message">Description of the evaluation failure.</param>
    /// <param name="inner">The original DynamicExpresso exception.</param>
    public ExpressionEvaluationException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
