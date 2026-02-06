// -----------------------------------------------------------------------
// <copyright file="IStreamingChatHandler.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Chat.Models;

// Disambiguate: use the v0.6.5a StreamingChatToken from Chat.Models,
// not the v0.6.1a version from Abstractions.Contracts.LLM.
using StreamingChatToken = Lexichord.Modules.Agents.Chat.Models.StreamingChatToken;

namespace Lexichord.Modules.Agents.Chat.Abstractions;

/// <summary>
/// Contract for handling streaming chat token events.
/// </summary>
/// <remarks>
/// <para>
/// This interface defines the callback methods invoked during streaming
/// LLM responses. Implementations typically update the UI progressively
/// as tokens arrive, handle errors gracefully, and finalize the message
/// when the stream completes.
/// </para>
/// <para>
/// Implementations should be prepared to receive tokens rapidly (potentially
/// hundreds per second) and should implement appropriate throttling or
/// buffering strategies to maintain UI responsiveness.
/// </para>
/// <para>
/// All methods are asynchronous to allow implementations to perform
/// async operations (e.g., UI thread dispatching) without blocking the
/// SSE parser thread.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class StreamingChatHandler : IStreamingChatHandler
/// {
///     private readonly StringBuilder _buffer = new();
///
///     public async Task OnTokenReceived(StreamingChatToken token)
///     {
///         _buffer.Append(token.Text);
///         await UpdateUIAsync(_buffer.ToString());
///     }
///
///     public async Task OnStreamComplete(ChatResponse fullResponse)
///     {
///         await FinalizeMessageAsync(fullResponse);
///     }
///
///     public async Task OnStreamError(Exception error)
///     {
///         await ShowErrorAsync(error.Message);
///     }
/// }
/// </code>
/// </example>
public interface IStreamingChatHandler
{
    /// <summary>
    /// Called when a new token is received from the stream.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is invoked for each token parsed from the SSE stream.
    /// Implementations should append the token's text to the message being
    /// built and optionally update the UI (with appropriate throttling).
    /// </para>
    /// <para>
    /// The method may be called very frequently. Implementations should
    /// avoid blocking operations and consider batching UI updates.
    /// </para>
    /// </remarks>
    /// <param name="token">The received token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task OnTokenReceived(StreamingChatToken token);

    /// <summary>
    /// Called when the stream completes successfully.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is invoked once when the stream ends normally. The
    /// <paramref name="fullResponse"/> contains the complete assembled
    /// response, including any metadata provided by the LLM.
    /// </para>
    /// <para>
    /// Implementations should finalize the message display, update
    /// conversation state, and perform any necessary cleanup.
    /// </para>
    /// </remarks>
    /// <param name="fullResponse">The complete response with metadata.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task OnStreamComplete(ChatResponse fullResponse);

    /// <summary>
    /// Called when an error occurs during streaming.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is invoked when the stream fails due to network errors,
    /// parse errors, or provider errors. Implementations should display
    /// an appropriate error message and preserve any partial content.
    /// </para>
    /// <para>
    /// After this method is called, no further callbacks will be invoked
    /// for this stream. The handler should reset its state for potential
    /// retry attempts.
    /// </para>
    /// </remarks>
    /// <param name="error">The exception that occurred.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task OnStreamError(Exception error);
}
