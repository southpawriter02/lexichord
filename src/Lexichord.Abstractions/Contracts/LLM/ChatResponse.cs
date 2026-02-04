// -----------------------------------------------------------------------
// <copyright file="ChatResponse.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Represents the response from a chat completion request.
/// </summary>
/// <remarks>
/// <para>
/// This immutable record encapsulates the AI model's response along with usage metrics
/// and completion metadata. All properties are set during construction and cannot be modified.
/// </para>
/// <para>
/// The <see cref="TotalTokens"/> property is computed from <see cref="PromptTokens"/>
/// and <see cref="CompletionTokens"/> for convenience in usage tracking.
/// </para>
/// </remarks>
/// <param name="Content">The generated text response from the model.</param>
/// <param name="PromptTokens">Number of tokens in the request (input).</param>
/// <param name="CompletionTokens">Number of tokens in the response (output).</param>
/// <param name="Duration">Time taken to generate the response.</param>
/// <param name="FinishReason">
/// Reason the model stopped generating. Common values include:
/// "stop" (natural completion), "length" (max tokens reached),
/// "content_filter" (content policy violation).
/// </param>
/// <example>
/// <code>
/// // Creating a response (typically done by provider implementations)
/// var response = new ChatResponse(
///     Content: "The capital of France is Paris.",
///     PromptTokens: 15,
///     CompletionTokens: 8,
///     Duration: TimeSpan.FromMilliseconds(250),
///     FinishReason: "stop"
/// );
///
/// // Using the response
/// Console.WriteLine($"Response: {response.Content}");
/// Console.WriteLine($"Total tokens: {response.TotalTokens}");
/// Console.WriteLine($"Generation time: {response.Duration.TotalMilliseconds}ms");
/// </code>
/// </example>
public record ChatResponse(
    string Content,
    int PromptTokens,
    int CompletionTokens,
    TimeSpan Duration,
    string? FinishReason)
{
    /// <summary>
    /// Gets the generated text response from the model.
    /// </summary>
    /// <value>The model's text output. Never null but may be empty.</value>
    public string Content { get; init; } = Content ?? throw new ArgumentNullException(nameof(Content));

    /// <summary>
    /// Gets the number of tokens in the request (input).
    /// </summary>
    /// <value>A non-negative integer representing input token count.</value>
    /// <exception cref="ArgumentOutOfRangeException">Thrown during construction if value is negative.</exception>
    public int PromptTokens { get; init; } = PromptTokens >= 0
        ? PromptTokens
        : throw new ArgumentOutOfRangeException(nameof(PromptTokens), "PromptTokens cannot be negative.");

    /// <summary>
    /// Gets the number of tokens in the response (output).
    /// </summary>
    /// <value>A non-negative integer representing output token count.</value>
    /// <exception cref="ArgumentOutOfRangeException">Thrown during construction if value is negative.</exception>
    public int CompletionTokens { get; init; } = CompletionTokens >= 0
        ? CompletionTokens
        : throw new ArgumentOutOfRangeException(nameof(CompletionTokens), "CompletionTokens cannot be negative.");

    /// <summary>
    /// Gets the total number of tokens used (input + output).
    /// </summary>
    /// <value>The sum of <see cref="PromptTokens"/> and <see cref="CompletionTokens"/>.</value>
    /// <remarks>
    /// This computed property is useful for tracking API usage and costs,
    /// as most LLM providers bill based on total token consumption.
    /// </remarks>
    public int TotalTokens => PromptTokens + CompletionTokens;

    /// <summary>
    /// Gets whether the response completed naturally (reached a stop condition).
    /// </summary>
    /// <value>True if <see cref="FinishReason"/> is "stop"; otherwise, false.</value>
    /// <remarks>
    /// A natural completion indicates the model reached a logical stopping point
    /// rather than being truncated due to token limits or other constraints.
    /// </remarks>
    public bool IsComplete => string.Equals(FinishReason, "stop", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets whether the response was truncated due to reaching the maximum token limit.
    /// </summary>
    /// <value>True if <see cref="FinishReason"/> is "length"; otherwise, false.</value>
    /// <remarks>
    /// When this is true, the response may be incomplete. Consider increasing
    /// <see cref="ChatOptions.MaxTokens"/> or implementing continuation logic.
    /// </remarks>
    public bool IsTruncated => string.Equals(FinishReason, "length", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the tokens per second generation rate.
    /// </summary>
    /// <value>The generation speed in tokens per second, or 0 if duration is zero.</value>
    public double TokensPerSecond => Duration.TotalSeconds > 0
        ? CompletionTokens / Duration.TotalSeconds
        : 0;
}
