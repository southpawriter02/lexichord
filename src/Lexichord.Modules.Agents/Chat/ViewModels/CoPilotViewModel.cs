// -----------------------------------------------------------------------
// <copyright file="CoPilotViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Lexichord.Modules.Agents.Chat.Models;

namespace Lexichord.Modules.Agents.Chat.ViewModels;

/// <summary>
/// ViewModel for the Co-Pilot chat panel, orchestrating conversations
/// and streaming interactions.
/// </summary>
/// <remarks>
/// <para>
/// This ViewModel manages the chat interface lifecycle, including message
/// display, streaming state, and user input. It serves as the bridge
/// between the <see cref="Services.StreamingChatHandler"/> and the
/// <see cref="Views.CoPilotView"/>.
/// </para>
/// <para>
/// <strong>Streaming Integration (v0.6.5c):</strong>
/// The ViewModel exposes streaming-related properties that are computed
/// from <see cref="StreamingState"/> using the <see cref="StreamingStateExtensions"/>
/// helper methods. These properties drive UI element visibility:
/// </para>
/// <list type="bullet">
///   <item><see cref="IsStreaming"/> — Controls input disable state</item>
///   <item><see cref="CanCancel"/> — Controls cancel button visibility</item>
///   <item><see cref="ShowTypingIndicator"/> — Controls typing dots visibility</item>
///   <item><see cref="InputEnabled"/> — Controls text input enable state</item>
/// </list>
/// <para>
/// <strong>Version History:</strong>
/// <list type="bullet">
///   <item><description>v0.6.4a: Initial creation (stub)</description></item>
///   <item><description>v0.6.5c: Added streaming state properties, scroll event,
///     and status message for streaming UI handler integration</description></item>
/// </list>
/// </para>
/// </remarks>
public partial class CoPilotViewModel : ObservableObject
{
    #region Streaming Properties

    /// <summary>
    /// Current state of the streaming operation.
    /// </summary>
    /// <remarks>
    /// This property drives the computed streaming UI properties via
    /// <see cref="NotifyPropertyChangedForAttribute"/>. When the state
    /// changes, all dependent properties are automatically re-evaluated.
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStreaming))]
    [NotifyPropertyChangedFor(nameof(CanCancel))]
    [NotifyPropertyChangedFor(nameof(ShowTypingIndicator))]
    [NotifyPropertyChangedFor(nameof(InputEnabled))]
    private StreamingState _streamingState = StreamingState.Idle;

    /// <summary>
    /// Gets or sets the status message displayed in the status bar.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    #endregion

    #region Messages Collection

    /// <summary>
    /// Gets or sets the collection of chat messages displayed in the panel.
    /// </summary>
    /// <remarks>
    /// This collection is bound to the messages list in the view. During
    /// streaming, the <see cref="Services.StreamingChatHandler"/> adds a
    /// placeholder message and updates its content progressively.
    /// </remarks>
    public ObservableCollection<ChatMessageViewModel> Messages { get; set; } = [];

    #endregion

    #region Computed Streaming Properties

    /// <summary>
    /// True if streaming is actively in progress.
    /// </summary>
    /// <seealso cref="StreamingStateExtensions.IsActive"/>
    public bool IsStreaming => StreamingState.IsActive();

    /// <summary>
    /// True if the cancel command is available.
    /// </summary>
    /// <seealso cref="StreamingStateExtensions.CanCancel"/>
    public bool CanCancel => StreamingState.CanCancel();

    /// <summary>
    /// True if the typing indicator should be displayed.
    /// </summary>
    /// <seealso cref="StreamingStateExtensions.ShowTypingIndicator"/>
    public bool ShowTypingIndicator => StreamingState.ShowTypingIndicator();

    /// <summary>
    /// True if user input is enabled.
    /// </summary>
    /// <seealso cref="StreamingStateExtensions.InputEnabled"/>
    public bool InputEnabled => StreamingState.InputEnabled();

    #endregion

    #region Streaming Events

    /// <summary>
    /// Event raised when scroll to bottom is requested.
    /// </summary>
    /// <remarks>
    /// The <see cref="Views.CoPilotView"/> subscribes to this event
    /// to scroll the message list to show the latest content during streaming.
    /// </remarks>
    public event EventHandler? ScrollToBottomRequested;

    /// <summary>
    /// Requests the view to scroll to the bottom of the message list.
    /// </summary>
    /// <remarks>
    /// Called by the <see cref="Services.StreamingChatHandler"/> after each
    /// buffer flush to ensure the user sees the latest streaming content.
    /// </remarks>
    public void RequestScrollToBottom()
    {
        ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}
