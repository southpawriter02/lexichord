// -----------------------------------------------------------------------
// <copyright file="ChatMessageViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using Lexichord.Abstractions.Contracts.LLM;

namespace Lexichord.Modules.Agents.Chat.ViewModels;

/// <summary>
/// ViewModel representing a single chat message in the Co-Pilot panel.
/// </summary>
/// <remarks>
/// <para>
/// This ViewModel wraps a chat message for display in the UI, providing
/// observable properties for data binding and streaming-specific state.
/// During streaming, the <see cref="Content"/> property is updated
/// progressively by the <see cref="Services.StreamingChatHandler"/>.
/// </para>
/// <para>
/// <strong>Version History:</strong>
/// <list type="bullet">
///   <item><description>v0.6.4b: Initial creation (stub)</description></item>
///   <item><description>v0.6.5c: Added streaming properties (<see cref="IsStreaming"/>,
///     <see cref="HasError"/>, <see cref="TokenCount"/>)</description></item>
/// </list>
/// </para>
/// </remarks>
public partial class ChatMessageViewModel : ObservableObject
{
    /// <summary>
    /// Gets or sets the unique identifier for this message.
    /// </summary>
    [ObservableProperty]
    private Guid _messageId;

    /// <summary>
    /// Gets or sets the role of the message sender (User, Assistant, System).
    /// </summary>
    [ObservableProperty]
    private ChatRole _role;

    /// <summary>
    /// Gets or sets the text content of the message.
    /// </summary>
    /// <remarks>
    /// During streaming, this is updated progressively by the
    /// <see cref="Services.StreamingChatHandler"/> as tokens are flushed
    /// from the buffer. After streaming completes, it contains the
    /// full response text.
    /// </remarks>
    [ObservableProperty]
    private string _content = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when this message was created.
    /// </summary>
    [ObservableProperty]
    private DateTime _timestamp;

    /// <summary>
    /// Gets or sets whether this message is currently being streamed.
    /// </summary>
    /// <remarks>
    /// When true, the message content is still being received from the LLM.
    /// The UI may display a streaming indicator (e.g., blinking cursor)
    /// while this is true.
    /// </remarks>
    [ObservableProperty]
    private bool _isStreaming;

    /// <summary>
    /// Gets or sets whether an error occurred during streaming of this message.
    /// </summary>
    /// <remarks>
    /// When true, the message may contain partial content from before the
    /// error occurred. The UI should display an error indicator.
    /// </remarks>
    [ObservableProperty]
    private bool _hasError;

    /// <summary>
    /// Gets or sets the token count for this message (set on stream completion).
    /// </summary>
    /// <remarks>
    /// This is populated from the <see cref="Abstractions.Contracts.LLM.ChatResponse.CompletionTokens"/>
    /// when the stream completes. It is null during streaming or if the
    /// provider did not report token usage.
    /// </remarks>
    [ObservableProperty]
    private int? _tokenCount;
}
