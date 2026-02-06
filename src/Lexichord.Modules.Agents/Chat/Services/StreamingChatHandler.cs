// -----------------------------------------------------------------------
// <copyright file="StreamingChatHandler.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Text;
using Avalonia.Threading;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Chat.Abstractions;
using Lexichord.Modules.Agents.Chat.Models;
using Lexichord.Modules.Agents.Chat.ViewModels;
using Microsoft.Extensions.Logging;

// Disambiguate: use the v0.6.5a StreamingChatToken from Chat.Models,
// not the v0.6.1a version from Abstractions.Contracts.LLM.
using StreamingChatToken = Lexichord.Modules.Agents.Chat.Models.StreamingChatToken;

namespace Lexichord.Modules.Agents.Chat.Services;

/// <summary>
/// Handles streaming chat tokens for progressive UI updates.
/// </summary>
/// <remarks>
/// <para>
/// This handler receives tokens from the <see cref="ISSEParser"/> and manages
/// their display in the chat interface. Key responsibilities include:
/// </para>
/// <list type="bullet">
///   <item>Buffering tokens to reduce UI update frequency</item>
///   <item>Dispatching updates to the UI thread safely</item>
///   <item>Managing auto-scroll behavior</item>
///   <item>Handling stream lifecycle (start, progress, complete, error)</item>
/// </list>
/// <para>
/// The handler uses a 50ms throttle timer to batch updates, ensuring smooth
/// animation even with rapid token arrival. This approach reduces layout
/// recalculations from potentially hundreds per second to approximately 20.
/// </para>
/// <para>
/// <strong>Lifecycle:</strong> Each instance is tied to a single streaming session.
/// The <see cref="CoPilotViewModel"/> creates a new handler for each request and
/// disposes it when the stream ends. This class is NOT registered in DI.
/// </para>
/// <para>
/// <strong>Thread Safety:</strong> The <see cref="_contentBuffer"/> is protected
/// by <see cref="_bufferLock"/> to safely handle concurrent access from the
/// SSE parser thread (via <see cref="OnTokenReceived"/>) and the timer
/// thread (via <see cref="OnUpdateTimerTick"/>).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var handler = new StreamingChatHandler(viewModel, logger);
/// await handler.StartStreamingAsync();
///
/// await foreach (var token in sseParser.ParseSSEStreamAsync(stream, "OpenAI"))
/// {
///     await handler.OnTokenReceived(token);
/// }
///
/// await handler.OnStreamComplete(fullResponse);
/// handler.Dispose();
/// </code>
/// </example>
/// <seealso cref="IStreamingChatHandler"/>
/// <seealso cref="CoPilotViewModel"/>
/// <seealso cref="StreamingChatToken"/>
public sealed class StreamingChatHandler : IStreamingChatHandler, IDisposable
{
    #region Constants

    /// <summary>
    /// Interval between UI updates in milliseconds.
    /// </summary>
    /// <remarks>
    /// 50ms provides approximately 20 updates/second, which appears fluid
    /// while avoiding excessive CPU usage. Adjust based on user feedback.
    /// </remarks>
    private const int UpdateIntervalMs = 50;

    /// <summary>
    /// Maximum tokens to buffer before forcing an update.
    /// </summary>
    /// <remarks>
    /// Even if the timer hasn't fired, flush the buffer when it reaches
    /// this size to prevent excessive memory usage on slow UI threads.
    /// </remarks>
    private const int MaxBufferTokens = 20;

    #endregion

    #region Dependencies

    private readonly CoPilotViewModel _viewModel;
    private readonly ILogger<StreamingChatHandler> _logger;

    /// <summary>
    /// Delegate for dispatching actions to the UI thread.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="Dispatcher.UIThread"/>.<see cref="Dispatcher.InvokeAsync(Action)"/>.
    /// Overridable in unit tests via <c>InternalsVisibleTo</c> to avoid requiring
    /// a live Avalonia dispatcher.
    /// </remarks>
    internal Func<Action, Task> DispatchAction { get; set; } =
        async action => await Dispatcher.UIThread.InvokeAsync(action);

    #endregion

    #region State

    private readonly StringBuilder _contentBuffer = new();
    private readonly object _bufferLock = new();
    private readonly Timer _updateTimer;

    private ChatMessageViewModel? _currentMessage;
    private int _tokenCount;
    private int _bufferedTokenCount;
    private Stopwatch? _streamStopwatch;
    private bool _disposed;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of <see cref="StreamingChatHandler"/>.
    /// </summary>
    /// <param name="viewModel">The parent ViewModel to update.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="viewModel"/> or <paramref name="logger"/> is null.
    /// </exception>
    public StreamingChatHandler(
        CoPilotViewModel viewModel,
        ILogger<StreamingChatHandler> logger)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _updateTimer = new Timer(
            callback: OnUpdateTimerTick,
            state: null,
            dueTime: Timeout.Infinite,
            period: Timeout.Infinite);

        _logger.LogDebug("StreamingChatHandler created");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Prepares the handler for a new streaming session.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call this before initiating the SSE stream to set up the UI state.
    /// Creates the assistant message placeholder and starts the timer.
    /// </para>
    /// <para>
    /// This method transitions the <see cref="CoPilotViewModel.StreamingState"/>
    /// to <see cref="StreamingState.Connecting"/> and begins the 50ms update timer.
    /// </para>
    /// </remarks>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the handler has been disposed.</exception>
    public async Task StartStreamingAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _logger.LogDebug("Starting streaming session");

        // Reset state
        _contentBuffer.Clear();
        _tokenCount = 0;
        _bufferedTokenCount = 0;
        _streamStopwatch = Stopwatch.StartNew();

        // Create placeholder message for assistant response
        _currentMessage = new ChatMessageViewModel
        {
            MessageId = Guid.NewGuid(),
            Role = ChatRole.Assistant,
            Content = string.Empty,
            Timestamp = DateTime.Now,
            IsStreaming = true
        };

        // Add to messages collection on UI thread
        await DispatchAsync(() =>
        {
            _viewModel.Messages.Add(_currentMessage);
            _viewModel.StreamingState = StreamingState.Connecting;
        });

        // Start the update timer
        _updateTimer.Change(UpdateIntervalMs, UpdateIntervalMs);

        _logger.LogDebug("Streaming session started, timer active");
    }

    /// <summary>
    /// Gets the current accumulated content from the buffer.
    /// </summary>
    /// <returns>The current content string.</returns>
    /// <remarks>
    /// Used by <see cref="CoPilotViewModel"/> to construct the final
    /// <see cref="ChatResponse"/> when the stream completes.
    /// </remarks>
    public string GetCurrentContent()
    {
        lock (_bufferLock)
        {
            return _contentBuffer.ToString();
        }
    }

    #endregion

    #region IStreamingChatHandler Implementation

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Buffers the token's text content and increments counters. On the first
    /// token, transitions the state from <see cref="StreamingState.Connecting"/>
    /// to <see cref="StreamingState.Streaming"/> and logs the time-to-first-token.
    /// </para>
    /// <para>
    /// Completion tokens (<see cref="StreamingChatToken.IsComplete"/> = true)
    /// are logged but otherwise ignored, as stream completion is handled by
    /// <see cref="OnStreamComplete"/>.
    /// </para>
    /// <para>
    /// If the buffer exceeds <see cref="MaxBufferTokens"/>, a forced flush
    /// is triggered immediately to prevent excessive memory usage.
    /// </para>
    /// </remarks>
    public async Task OnTokenReceived(StreamingChatToken token)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (token.IsComplete)
        {
            _logger.LogDebug("Received completion token: {Reason}", token.FinishReason);
            return;
        }

        _tokenCount++;

        // First token: transition from Connecting to Streaming
        if (_tokenCount == 1)
        {
            var timeToFirst = _streamStopwatch?.ElapsedMilliseconds ?? 0;
            _logger.LogDebug("First token received at {ElapsedMs}ms", timeToFirst);

            await DispatchAsync(() =>
            {
                _viewModel.StreamingState = StreamingState.Streaming;
            });
        }

        // Buffer the token
        lock (_bufferLock)
        {
            _contentBuffer.Append(token.Text);
            _bufferedTokenCount++;

            // Force flush if buffer is large
            if (_bufferedTokenCount >= MaxBufferTokens)
            {
                FlushBufferLocked();
            }
        }

        _logger.LogTrace("Token {Index} buffered: '{Text}'", token.Index, token.Text);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Stops the update timer, performs a final buffer flush, and finalizes the
    /// message with the complete response content. Sets the streaming state to
    /// <see cref="StreamingState.Completed"/> and requests a scroll to bottom.
    /// </para>
    /// <para>
    /// After this method completes, <see cref="_currentMessage"/> is set to null
    /// to prevent further updates.
    /// </para>
    /// </remarks>
    public async Task OnStreamComplete(ChatResponse fullResponse)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _streamStopwatch?.Stop();
        _updateTimer.Change(Timeout.Infinite, Timeout.Infinite);

        // Final flush of any remaining buffer
        FlushBuffer();

        _logger.LogInformation(
            "Stream completed: {TokenCount} tokens, {CharCount} chars in {ElapsedMs}ms",
            _tokenCount,
            _contentBuffer.Length,
            _streamStopwatch?.ElapsedMilliseconds ?? 0);

        await DispatchAsync(() =>
        {
            if (_currentMessage is not null)
            {
                _currentMessage.Content = _contentBuffer.ToString();
                _currentMessage.IsStreaming = false;
                _currentMessage.TokenCount = fullResponse.CompletionTokens;
            }

            _viewModel.StreamingState = StreamingState.Completed;
            _viewModel.StatusMessage = $"Completed in {_streamStopwatch?.ElapsedMilliseconds}ms";

            // Ensure scroll to bottom
            _viewModel.RequestScrollToBottom();
        });

        // Reset for next stream
        _currentMessage = null;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Stops the update timer, preserves any partial content by performing a
    /// final flush, and marks the message as having an error. Sets the streaming
    /// state to <see cref="StreamingState.Error"/>.
    /// </para>
    /// <para>
    /// After this method completes, <see cref="_currentMessage"/> is set to null
    /// to prevent further updates.
    /// </para>
    /// </remarks>
    public async Task OnStreamError(Exception error)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _streamStopwatch?.Stop();
        _updateTimer.Change(Timeout.Infinite, Timeout.Infinite);

        _logger.LogError(error, "Stream error after {TokenCount} tokens", _tokenCount);

        // Preserve partial content
        FlushBuffer();

        await DispatchAsync(() =>
        {
            if (_currentMessage is not null)
            {
                _currentMessage.Content = _contentBuffer.ToString();
                _currentMessage.IsStreaming = false;
                _currentMessage.HasError = true;
            }

            _viewModel.StreamingState = StreamingState.Error;
            _viewModel.StatusMessage = $"Error: {error.Message}";
        });

        _currentMessage = null;
    }

    #endregion

    #region Timer Callback

    /// <summary>
    /// Timer callback that triggers periodic buffer flushes.
    /// </summary>
    /// <param name="state">Timer state (unused).</param>
    private void OnUpdateTimerTick(object? state)
    {
        if (_disposed) return;

        FlushBuffer();
    }

    #endregion

    #region Buffer Management

    /// <summary>
    /// Flushes the buffer by acquiring the lock and delegating to <see cref="FlushBufferLocked"/>.
    /// </summary>
    private void FlushBuffer()
    {
        lock (_bufferLock)
        {
            FlushBufferLocked();
        }
    }

    /// <summary>
    /// Flushes buffered content to the UI. Must be called while holding <see cref="_bufferLock"/>.
    /// </summary>
    /// <remarks>
    /// Updates the current message's content on the UI thread with the full
    /// accumulated content from the <see cref="StringBuilder"/>. The buffer
    /// is NOT cleared because we accumulate content — only the buffered token
    /// count is reset to track new tokens since the last flush.
    /// </remarks>
    private void FlushBufferLocked()
    {
        if (_bufferedTokenCount == 0 || _currentMessage is null)
        {
            return;
        }

        var content = _contentBuffer.ToString();
        var count = _bufferedTokenCount;
        _bufferedTokenCount = 0;

        // Don't clear the buffer - we accumulate content

        _logger.LogTrace("Flushing {Count} tokens, total chars: {Chars}", count, content.Length);

        // Dispatch UI update via injectable action (testable without Avalonia)
        _ = DispatchAction(() =>
        {
            if (_currentMessage is not null)
            {
                _currentMessage.Content = content;
                _viewModel.RequestScrollToBottom();
            }
        });
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Dispatches an action to the UI thread asynchronously.
    /// </summary>
    /// <param name="action">The action to execute on the UI thread.</param>
    /// <returns>A task representing the async dispatch operation.</returns>
    private async Task DispatchAsync(Action action)
    {
        await DispatchAction(action);
    }

    #endregion

    #region IDisposable

    /// <inheritdoc />
    /// <remarks>
    /// Disposes the update timer and marks the handler as disposed.
    /// Subsequent calls to any method will throw <see cref="ObjectDisposedException"/>.
    /// This method is safe to call multiple times.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed) return;

        _updateTimer.Dispose();
        _disposed = true;

        _logger.LogDebug("StreamingChatHandler disposed");
    }

    #endregion
}
