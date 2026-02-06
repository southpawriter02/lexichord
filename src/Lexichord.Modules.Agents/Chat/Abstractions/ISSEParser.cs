// -----------------------------------------------------------------------
// <copyright file="ISSEParser.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Modules.Agents.Chat.Models;

namespace Lexichord.Modules.Agents.Chat.Abstractions;

/// <summary>
/// Defines the contract for parsing Server-Sent Events (SSE) streams from LLM
/// providers into asynchronous sequences of <see cref="StreamingChatToken"/>.
/// </summary>
/// <remarks>
/// <para>
/// This service sits between the raw HTTP response stream and the streaming UI handler.
/// It understands the JSON payload format of each supported provider (OpenAI, Anthropic)
/// and extracts token content, yielding <see cref="StreamingChatToken"/> instances that
/// the <see cref="IStreamingChatHandler"/> can consume for progressive display.
/// </para>
/// <para>
/// <strong>Architecture (v0.6.5b):</strong>
/// </para>
/// <code>
/// HTTP Stream → ISSEParser → IAsyncEnumerable&lt;StreamingChatToken&gt; → IStreamingChatHandler → UI
/// </code>
/// <para>
/// <strong>Supported Providers:</strong>
/// </para>
/// <list type="bullet">
///   <item><description>
///     <strong>OpenAI</strong> — Parses <c>choices[0].delta.content</c> from each SSE data line.
///     Stream terminates on <c>[DONE]</c> marker.
///   </description></item>
///   <item><description>
///     <strong>Anthropic</strong> — Parses <c>delta.text</c> from <c>content_block_delta</c> events.
///     Stream terminates on <c>message_stop</c> event type.
///   </description></item>
/// </list>
/// <para>
/// <strong>Error Handling:</strong> Malformed JSON lines are logged and skipped to
/// maintain stream continuity. Unsupported providers throw
/// <see cref="NotSupportedException"/>. A null stream throws
/// <see cref="ArgumentNullException"/>.
/// </para>
/// <para>
/// <strong>Thread Safety:</strong> Implementations should be stateless and safe for
/// concurrent use, supporting singleton DI registration.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Typical usage within an LLM connector:
/// await foreach (var token in sseParser.ParseSSEStreamAsync(responseStream, "OpenAI", ct))
/// {
///     if (token.HasContent)
///     {
///         await handler.OnTokenReceived(token);
///     }
///
///     if (token.IsComplete)
///     {
///         break;
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="StreamingChatToken"/>
/// <seealso cref="IStreamingChatHandler"/>
public interface ISSEParser
{
    /// <summary>
    /// Parses an SSE stream from an LLM provider and yields tokens asynchronously.
    /// </summary>
    /// <param name="responseStream">
    /// The HTTP response stream containing SSE-formatted data. The stream is consumed
    /// line-by-line and disposed when parsing completes.
    /// </param>
    /// <param name="provider">
    /// The LLM provider identifier (e.g., "OpenAI", "Anthropic") used to determine
    /// the JSON parsing strategy. Matching is case-insensitive.
    /// </param>
    /// <param name="ct">
    /// Cancellation token for aborting the parse operation. When triggered, the
    /// enumeration stops gracefully without throwing.
    /// </param>
    /// <returns>
    /// An <see cref="IAsyncEnumerable{T}"/> of <see cref="StreamingChatToken"/> instances.
    /// The final token in the sequence will have <see cref="StreamingChatToken.IsComplete"/>
    /// set to <c>true</c> if the stream terminated normally.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="responseStream"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="provider"/> is not a recognized provider name.
    /// </exception>
    IAsyncEnumerable<StreamingChatToken> ParseSSEStreamAsync(
        Stream responseStream,
        string provider,
        CancellationToken ct = default);
}
