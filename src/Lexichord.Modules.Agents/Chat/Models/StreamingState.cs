// -----------------------------------------------------------------------
// <copyright file="StreamingState.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Chat.Models;

/// <summary>
/// Represents the lifecycle state of a streaming operation.
/// </summary>
/// <remarks>
/// <para>
/// This enumeration tracks the current phase of a streaming LLM request,
/// from initial connection through completion or cancellation. The state
/// is used to:
/// </para>
/// <list type="bullet">
///   <item>Control UI element visibility (typing indicator, cancel button)</item>
///   <item>Enable/disable user input</item>
///   <item>Manage resource cleanup</item>
///   <item>Track operation telemetry</item>
/// </list>
/// <para>
/// State transitions are unidirectional: Idle → Connecting → Streaming →
/// (Completed|Cancelled|Error) → Idle.
/// </para>
/// </remarks>
public enum StreamingState
{
    /// <summary>
    /// No streaming operation in progress.
    /// </summary>
    /// <remarks>
    /// The default state. User input is enabled, and the send button
    /// is available. This is the initial state and the state returned
    /// to after any stream ends.
    /// </remarks>
    Idle = 0,

    /// <summary>
    /// Establishing connection to the LLM provider.
    /// </summary>
    /// <remarks>
    /// The request has been sent, but no tokens have been received yet.
    /// The typing indicator should be displayed during this phase.
    /// Typical duration: 100ms - 2000ms depending on provider and model.
    /// </remarks>
    Connecting = 1,

    /// <summary>
    /// Actively receiving tokens from the stream.
    /// </summary>
    /// <remarks>
    /// At least one token has been received, and more are expected.
    /// The cancel button should be visible, and the UI should be
    /// updating with new content. The typing indicator is hidden.
    /// </remarks>
    Streaming = 2,

    /// <summary>
    /// Stream completed successfully.
    /// </summary>
    /// <remarks>
    /// The stream end signal was received ([DONE] or equivalent).
    /// The complete message is now displayed. This is a transient
    /// state that immediately transitions to Idle.
    /// </remarks>
    Completed = 3,

    /// <summary>
    /// Stream was cancelled by the user.
    /// </summary>
    /// <remarks>
    /// The user clicked the cancel button or navigated away. Partial
    /// content is preserved. This is a transient state that
    /// immediately transitions to Idle.
    /// </remarks>
    Cancelled = 4,

    /// <summary>
    /// Stream failed due to an error.
    /// </summary>
    /// <remarks>
    /// A network error, parse error, or provider error occurred.
    /// An error message is displayed. Partial content may be
    /// preserved if available. This state may require user
    /// acknowledgment before transitioning to Idle.
    /// </remarks>
    Error = 5
}

/// <summary>
/// Extension methods for <see cref="StreamingState"/>.
/// </summary>
public static class StreamingStateExtensions
{
    /// <summary>
    /// Indicates whether the state represents an active streaming operation.
    /// </summary>
    /// <param name="state">The streaming state.</param>
    /// <returns>True if streaming is in progress (Connecting or Streaming).</returns>
    public static bool IsActive(this StreamingState state) =>
        state is StreamingState.Connecting or StreamingState.Streaming;

    /// <summary>
    /// Indicates whether the state represents a terminal (final) state.
    /// </summary>
    /// <param name="state">The streaming state.</param>
    /// <returns>True if the operation has ended (Completed, Cancelled, or Error).</returns>
    public static bool IsTerminal(this StreamingState state) =>
        state is StreamingState.Completed or StreamingState.Cancelled or StreamingState.Error;

    /// <summary>
    /// Indicates whether the cancel button should be visible.
    /// </summary>
    /// <param name="state">The streaming state.</param>
    /// <returns>True if cancel is available (Connecting or Streaming).</returns>
    public static bool CanCancel(this StreamingState state) =>
        state.IsActive();

    /// <summary>
    /// Indicates whether the typing indicator should be shown.
    /// </summary>
    /// <param name="state">The streaming state.</param>
    /// <returns>True if typing indicator should be visible (Connecting only).</returns>
    public static bool ShowTypingIndicator(this StreamingState state) =>
        state == StreamingState.Connecting;

    /// <summary>
    /// Indicates whether user input should be enabled.
    /// </summary>
    /// <param name="state">The streaming state.</param>
    /// <returns>True if input is allowed (any non-active state).</returns>
    public static bool InputEnabled(this StreamingState state) =>
        !state.IsActive();
}
