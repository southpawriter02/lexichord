// -----------------------------------------------------------------------
// <copyright file="StreamingChatToken.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Represents a single token received during streaming chat completion.
/// </summary>
/// <remarks>
/// <para>
/// This immutable record encapsulates individual tokens as they are streamed from the LLM.
/// Streaming allows the UI to display partial responses in real-time rather than waiting
/// for the complete response.
/// </para>
/// <para>
/// The final token in a stream will have <see cref="IsComplete"/> set to true and
/// will include the <see cref="FinishReason"/> if available from the provider.
/// </para>
/// </remarks>
/// <param name="Token">The token text content. May be empty for completion markers.</param>
/// <param name="IsComplete">Whether this is the final token in the stream.</param>
/// <param name="FinishReason">
/// The reason streaming stopped. Only present on the final token.
/// Common values: "stop", "length", "content_filter".
/// </param>
/// <example>
/// <code>
/// // Consuming a streaming response
/// await foreach (var token in service.StreamAsync(request))
/// {
///     if (!token.IsComplete)
///     {
///         Console.Write(token.Token);
///     }
///     else
///     {
///         Console.WriteLine();
///         Console.WriteLine($"Completed: {token.FinishReason}");
///     }
/// }
///
/// // Building complete response from tokens
/// var builder = new StringBuilder();
/// await foreach (var token in service.StreamAsync(request))
/// {
///     builder.Append(token.Token);
/// }
/// var fullResponse = builder.ToString();
/// </code>
/// </example>
public record StreamingChatToken(
    string Token,
    bool IsComplete = false,
    string? FinishReason = null)
{
    /// <summary>
    /// Gets the token text content.
    /// </summary>
    /// <value>The token text. Never null but may be empty for completion markers.</value>
    public string Token { get; init; } = Token ?? throw new ArgumentNullException(nameof(Token));

    /// <summary>
    /// Creates a completion marker token indicating the stream has ended.
    /// </summary>
    /// <param name="finishReason">The reason streaming stopped.</param>
    /// <returns>A new <see cref="StreamingChatToken"/> marked as complete.</returns>
    /// <remarks>
    /// This factory method creates the final token that signals the end of a stream.
    /// Provider implementations should call this when the stream terminates.
    /// </remarks>
    /// <example>
    /// <code>
    /// // In a provider implementation
    /// if (receivedDoneSignal)
    /// {
    ///     yield return StreamingChatToken.Complete("stop");
    /// }
    /// </code>
    /// </example>
    public static StreamingChatToken Complete(string? finishReason = null)
        => new(string.Empty, IsComplete: true, FinishReason: finishReason);

    /// <summary>
    /// Creates a content token with the specified text.
    /// </summary>
    /// <param name="token">The token text content.</param>
    /// <returns>A new <see cref="StreamingChatToken"/> with the specified content.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="token"/> is null.</exception>
    /// <remarks>
    /// This factory method creates a standard content token for streaming.
    /// </remarks>
    public static StreamingChatToken Content(string token)
    {
        ArgumentNullException.ThrowIfNull(token, nameof(token));
        return new StreamingChatToken(token);
    }

    /// <summary>
    /// Gets whether this token contains content (non-empty text).
    /// </summary>
    /// <value>True if <see cref="Token"/> is not empty; otherwise, false.</value>
    public bool HasContent => !string.IsNullOrEmpty(Token);
}
