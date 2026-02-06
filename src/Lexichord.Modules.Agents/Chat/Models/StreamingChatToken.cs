// -----------------------------------------------------------------------
// <copyright file="StreamingChatToken.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Chat.Models;

/// <summary>
/// Represents a single token received during streaming LLM responses.
/// </summary>
/// <remarks>
/// <para>
/// This record is used throughout the streaming infrastructure to represent
/// individual chunks of text as they arrive from the LLM provider. Each token
/// carries metadata about its position in the stream and completion status.
/// </para>
/// <para>
/// The record is immutable by design, ensuring thread-safety when tokens are
/// passed between the SSE parser and UI handler components.
/// </para>
/// </remarks>
/// <param name="Text">
/// The text content of this token. May be a single character, word, or phrase
/// depending on the LLM provider's tokenization. Can be empty for control tokens.
/// </param>
/// <param name="Index">
/// Zero-based index of this token in the current stream. Used for ordering
/// and debugging purposes. Monotonically increasing within a stream.
/// </param>
/// <param name="IsComplete">
/// True if this is the final token in the stream, indicating the LLM has
/// finished generating the response. The <see cref="Text"/> field may be
/// empty when this is true.
/// </param>
/// <param name="FinishReason">
/// Optional reason for stream completion. Common values include:
/// <list type="bullet">
///   <item><c>"stop"</c> — Normal completion, model finished naturally</item>
///   <item><c>"length"</c> — Token limit reached</item>
///   <item><c>"content_filter"</c> — Content filtered by provider</item>
///   <item><c>"function_call"</c> — Model invoked a function (future)</item>
///   <item><c>null</c> — Streaming in progress, not yet complete</item>
/// </list>
/// </param>
/// <example>
/// <code>
/// // Example token during streaming
/// var token1 = new StreamingChatToken("Hello", 0, false, null);
///
/// // Example final token
/// var finalToken = new StreamingChatToken("", 42, true, "stop");
///
/// // Example token with content and completion
/// var lastContent = new StreamingChatToken("!", 41, false, null);
/// </code>
/// </example>
public record StreamingChatToken(
    string Text,
    int Index,
    bool IsComplete,
    string? FinishReason
)
{
    /// <summary>
    /// Creates a content token at the specified index.
    /// </summary>
    /// <param name="text">The token text content.</param>
    /// <param name="index">The token index in the stream.</param>
    /// <returns>A new <see cref="StreamingChatToken"/> for content.</returns>
    public static StreamingChatToken Content(string text, int index) =>
        new(text, index, false, null);

    /// <summary>
    /// Creates a completion token with the specified finish reason.
    /// </summary>
    /// <param name="index">The final token index.</param>
    /// <param name="finishReason">The reason for stream completion.</param>
    /// <returns>A new <see cref="StreamingChatToken"/> marking stream end.</returns>
    public static StreamingChatToken Complete(int index, string finishReason = "stop") =>
        new(string.Empty, index, true, finishReason);

    /// <summary>
    /// Indicates whether this token contains displayable text content.
    /// </summary>
    public bool HasContent => !string.IsNullOrEmpty(Text);
}
