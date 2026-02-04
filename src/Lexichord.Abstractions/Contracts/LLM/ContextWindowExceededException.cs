// -----------------------------------------------------------------------
// <copyright file="ContextWindowExceededException.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Exception thrown when a request would exceed the model's context window.
/// </summary>
/// <remarks>
/// <para>
/// This exception indicates that the total tokens in the request (prompt + expected response)
/// would exceed the target model's context window limit. The request cannot be sent
/// without reducing the prompt size or selecting a model with a larger context window.
/// </para>
/// <para>
/// Use <see cref="RequestedTokens"/> and <see cref="ContextWindow"/> to calculate
/// how much the request needs to be reduced.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// try
/// {
///     var adjustedOptions = options.AdjustForContext(estimate);
/// }
/// catch (ContextWindowExceededException ex)
/// {
///     var overage = ex.RequestedTokens - ex.ContextWindow;
///     Console.WriteLine($"Request exceeds context by {overage} tokens");
///     Console.WriteLine($"Consider using a model with larger context window");
/// }
/// </code>
/// </example>
public class ContextWindowExceededException : ChatCompletionException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContextWindowExceededException"/> class.
    /// </summary>
    /// <param name="requestedTokens">The number of tokens requested.</param>
    /// <param name="contextWindow">The model's context window size.</param>
    public ContextWindowExceededException(int requestedTokens, int contextWindow)
        : base(FormatMessage(requestedTokens, contextWindow))
    {
        RequestedTokens = requestedTokens;
        ContextWindow = contextWindow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextWindowExceededException"/> class
    /// with a custom message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="requestedTokens">The number of tokens requested.</param>
    /// <param name="contextWindow">The model's context window size.</param>
    public ContextWindowExceededException(string message, int requestedTokens, int contextWindow)
        : base(message)
    {
        RequestedTokens = requestedTokens;
        ContextWindow = contextWindow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextWindowExceededException"/> class
    /// with a custom message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="requestedTokens">The number of tokens requested.</param>
    /// <param name="contextWindow">The model's context window size.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public ContextWindowExceededException(
        string message,
        int requestedTokens,
        int contextWindow,
        Exception innerException)
        : base(message, innerException)
    {
        RequestedTokens = requestedTokens;
        ContextWindow = contextWindow;
    }

    /// <summary>
    /// Gets the total number of tokens in the request.
    /// </summary>
    /// <value>The estimated token count that exceeded the context window.</value>
    public int RequestedTokens { get; }

    /// <summary>
    /// Gets the model's context window size.
    /// </summary>
    /// <value>The maximum tokens the model can handle.</value>
    public int ContextWindow { get; }

    /// <summary>
    /// Gets the number of tokens by which the request exceeds the context window.
    /// </summary>
    /// <value>The token overage amount.</value>
    public int Overage => Math.Max(0, RequestedTokens - ContextWindow);

    /// <summary>
    /// Gets the ratio of requested tokens to context window size.
    /// </summary>
    /// <value>A value greater than 1.0 when the context is exceeded.</value>
    public double UtilizationRatio => ContextWindow > 0
        ? (double)RequestedTokens / ContextWindow
        : double.PositiveInfinity;

    /// <summary>
    /// Formats the exception message.
    /// </summary>
    /// <param name="requestedTokens">The requested tokens.</param>
    /// <param name="contextWindow">The context window size.</param>
    /// <returns>A formatted error message.</returns>
    private static string FormatMessage(int requestedTokens, int contextWindow)
    {
        var overage = requestedTokens - contextWindow;
        return $"Request ({requestedTokens:N0} tokens) exceeds model context window ({contextWindow:N0} tokens) by {overage:N0} tokens.";
    }
}
