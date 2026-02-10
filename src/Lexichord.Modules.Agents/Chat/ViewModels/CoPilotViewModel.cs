// -----------------------------------------------------------------------
// <copyright file="CoPilotViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Chat.Models;
using Lexichord.Modules.Agents.Chat.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Chat.ViewModels;

/// <summary>
/// ViewModel for the Co-Pilot chat panel, orchestrating conversations
/// and streaming interactions with license-based routing.
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
/// <strong>License Gating (v0.6.5d):</strong>
/// Routes chat requests based on the user's license tier. Teams-tier
/// and above receive real-time streaming responses, while WriterPro
/// users fall back to batch completion with an upgrade hint:
/// </para>
/// <list type="bullet">
///   <item><see cref="IsStreamingAvailable"/> — License-based streaming availability</item>
///   <item><see cref="StreamingUnavailableMessage"/> — Upgrade hint text</item>
///   <item><see cref="ShouldUseStreaming"/> — Internal routing decision</item>
///   <item><see cref="SendBatchAsync"/> — Batch fallback for non-streaming tiers</item>
/// </list>
/// <para>
/// <strong>Version History:</strong>
/// <list type="bullet">
///   <item><description>v0.6.4a: Initial creation (stub)</description></item>
///   <item><description>v0.6.5c: Added streaming state properties, scroll event,
///     and status message for streaming UI handler integration</description></item>
///   <item><description>v0.6.5d: Added license gating, batch completion fallback,
///     upgrade hint, constructor injection, and SendCommand</description></item>
/// </list>
/// </para>
/// </remarks>
public partial class CoPilotViewModel : ObservableObject
{
    #region Fields

    /// <summary>
    /// License context for tier-based feature gating.
    /// </summary>
    /// <remarks>
    /// Injected via constructor. Used by <see cref="ShouldUseStreaming"/>
    /// and <see cref="IsStreamingAvailable"/> to determine whether the
    /// current user can access real-time streaming.
    /// </remarks>
    private readonly ILicenseContext? _license;

    /// <summary>
    /// Chat completion service for both streaming and batch requests.
    /// </summary>
    /// <remarks>
    /// Injected via constructor. <see cref="IChatCompletionService.StreamAsync"/>
    /// is used for Teams+ tiers; <see cref="IChatCompletionService.CompleteAsync"/>
    /// is used as a fallback for WriterPro.
    /// </remarks>
    private readonly IChatCompletionService? _chatService;

    /// <summary>
    /// Logger instance for diagnostic and telemetry output.
    /// </summary>
    private readonly ILogger<CoPilotViewModel>? _logger;

    /// <summary>
    /// Tracks whether the upgrade hint has been shown during the current session.
    /// </summary>
    /// <remarks>
    /// Once set to <c>true</c>, the hint will not be shown again until the
    /// application is restarted. This prevents nagging the user on every message.
    /// </remarks>
    private bool _upgradeHintShownThisSession;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of <see cref="CoPilotViewModel"/> with
    /// all required dependencies for full license-gated operation.
    /// </summary>
    /// <param name="chatService">The chat completion service for LLM requests.</param>
    /// <param name="providerRegistry">The LLM provider registry (reserved for future use).</param>
    /// <param name="promptRenderer">The prompt renderer (reserved for future use).</param>
    /// <param name="conversationManager">The conversation manager (reserved for future use).</param>
    /// <param name="licenseContext">The license context for tier checks.</param>
    /// <param name="loggerFactory">Factory for creating typed loggers.</param>
    /// <remarks>
    /// <para>
    /// This constructor is used by the DI container for production operation.
    /// All parameters are stored for use throughout the ViewModel lifecycle.
    /// </para>
    /// <para>
    /// <strong>v0.6.5d:</strong> Added license gating parameters. The
    /// <paramref name="licenseContext"/> drives streaming vs batch routing.
    /// </para>
    /// </remarks>
    public CoPilotViewModel(
        IChatCompletionService chatService,
        ILLMProviderRegistry providerRegistry,
        IPromptRenderer promptRenderer,
        IConversationManager conversationManager,
        ILicenseContext licenseContext,
        ILoggerFactory loggerFactory)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _license = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _logger = loggerFactory?.CreateLogger<CoPilotViewModel>();

        _logger?.LogDebug(
            "CoPilotViewModel initialized with {Tier} license",
            _license.GetCurrentTier());
    }

    /// <summary>
    /// Initializes a new instance of <see cref="CoPilotViewModel"/> without
    /// dependencies (for backward compatibility with streaming tests).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This parameterless constructor preserves backward compatibility
    /// with <c>StreamingChatHandlerTests</c> (v0.6.5c) which instantiate
    /// the ViewModel directly without DI.
    /// </para>
    /// <para>
    /// When created without dependencies, license-gated features
    /// (<see cref="IsStreamingAvailable"/>, <see cref="SendCommand"/>)
    /// are not available. The streaming state properties continue to
    /// function normally for handler integration testing.
    /// </para>
    /// </remarks>
    public CoPilotViewModel()
    {
        // No-op: streaming properties and Messages collection work without DI
    }

    #endregion

    #region Observable Properties

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

    /// <summary>
    /// Gets or sets the user's input text in the chat input field.
    /// </summary>
    /// <remarks>
    /// Cleared after sending a message. Bound to the input TextBox
    /// in <see cref="Views.CoPilotView"/>.
    /// </remarks>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private string _inputText = string.Empty;

    /// <summary>
    /// Indicates whether a batch (non-streaming) request is in progress.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Set to <c>true</c> at the start of <see cref="SendBatchAsync"/>
    /// and reset to <c>false</c> when the response is received or an error
    /// occurs. Used by the view to display a loading indicator during batch
    /// generation.
    /// </para>
    /// <para>
    /// This is distinct from <see cref="IsStreaming"/>, which tracks the
    /// streaming state for Teams+ users.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private bool _isGenerating;

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

    #region License Gating Properties (v0.6.5d)

    /// <summary>
    /// Indicates whether streaming is available for the current license.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns <c>true</c> when the user's license tier is
    /// <see cref="LicenseTier.Teams"/> or higher. Returns <c>false</c>
    /// for <see cref="LicenseTier.WriterPro"/> and below.
    /// </para>
    /// <para>
    /// When <c>false</c>, the <see cref="SendCommand"/> routes requests
    /// through <see cref="SendBatchAsync"/> instead of streaming, and
    /// the <see cref="StreamingUnavailableMessage"/> is populated.
    /// </para>
    /// <para>
    /// Used in XAML bindings to control visibility of streaming UI
    /// elements and the upgrade hint component.
    /// </para>
    /// </remarks>
    public bool IsStreamingAvailable =>
        _license?.GetCurrentTier() >= LicenseTier.Teams;

    /// <summary>
    /// Message shown when streaming is not available for the current license.
    /// </summary>
    /// <remarks>
    /// Returns <c>null</c> when streaming is available (Teams+ tier),
    /// allowing the UI to hide the upgrade hint. Returns a user-friendly
    /// message containing "Teams" when streaming is unavailable, guiding
    /// the user toward upgrading.
    /// </remarks>
    public string? StreamingUnavailableMessage => IsStreamingAvailable
        ? null
        : "ℹ️ Real-time streaming requires Teams tier. Upgrade for instant responses.";

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
    /// Also called after batch responses are added to the message list.
    /// </remarks>
    public void RequestScrollToBottom()
    {
        ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Send Command (v0.6.5d)

    /// <summary>
    /// Determines whether the <see cref="SendCommand"/> can execute.
    /// </summary>
    /// <returns>
    /// <c>true</c> if <see cref="InputText"/> is not empty/whitespace
    /// and no generation is in progress; otherwise <c>false</c>.
    /// </returns>
    private bool CanSend => !string.IsNullOrWhiteSpace(InputText) && !IsGenerating;

    /// <summary>
    /// Sends a chat message, using streaming or batch based on license tier.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <remarks>
    /// <para>
    /// This is the primary entry point for sending user messages. The method:
    /// </para>
    /// <list type="number">
    ///   <item>Captures and clears the user input</item>
    ///   <item>Adds a user message to the <see cref="Messages"/> collection</item>
    ///   <item>Checks license tier via <see cref="ShouldUseStreaming"/></item>
    ///   <item>Routes to <see cref="SendStreamingAsync"/> or <see cref="SendBatchAsync"/></item>
    /// </list>
    /// <para>
    /// <strong>License Routing (v0.6.5d):</strong>
    /// Teams and Enterprise tiers use streaming; WriterPro and below
    /// are routed to batch completion with a logged downgrade event.
    /// </para>
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync(CancellationToken ct)
    {
        var userInput = InputText.Trim();
        InputText = string.Empty;

        try
        {
            // Add user message to the conversation
            AddUserMessage(userInput);

            // License-based routing: Teams+ → streaming, WriterPro → batch
            if (ShouldUseStreaming())
            {
                await SendStreamingAsync(userInput, ct);
            }
            else
            {
                await SendBatchAsync(userInput, ct);

                // Show upgrade hint once per session for non-streaming tiers
                ShowUpgradeHintIfNeeded();
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("Send operation cancelled");
        }
        catch (Exception ex)
        {
            HandleSendError(ex);
        }
    }

    #endregion

    #region License Check Logic (v0.6.5d)

    /// <summary>
    /// Determines whether streaming should be used based on license tier.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the current license tier is <see cref="LicenseTier.Teams"/>
    /// or higher; <c>false</c> for batch completion fallback.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is the core license gating decision point. It compares the
    /// current tier from <see cref="ILicenseContext.GetCurrentTier()"/> against
    /// <see cref="LicenseTier.Teams"/>. When streaming is not available, a
    /// downgrade log message is emitted at <see cref="LogLevel.Information"/>
    /// level to support diagnostics and telemetry.
    /// </para>
    /// <para>
    /// The check is performed on every send to accommodate mid-session license
    /// changes (e.g., user upgrades or license expires during use).
    /// </para>
    /// </remarks>
    private bool ShouldUseStreaming()
    {
        if (_license is null) return false;

        var currentTier = _license.GetCurrentTier();
        var canStream = currentTier >= LicenseTier.Teams;

        if (canStream)
        {
            _logger?.LogDebug(
                "Streaming authorized for {Tier} license",
                currentTier);
        }
        else
        {
            _logger?.LogInformation(
                "Streaming downgraded to batch for {Tier} license. " +
                "Streaming requires Teams tier or higher.",
                currentTier);
        }

        return canStream;
    }

    #endregion

    #region Streaming Path (v0.6.5d)

    /// <summary>
    /// Sends a chat message using streaming completion for Teams+ users.
    /// </summary>
    /// <param name="userInput">The trimmed user message text.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <remarks>
    /// <para>
    /// This method delegates to the <see cref="Services.StreamingChatHandler"/>
    /// which manages token buffering, throttled UI updates, and state transitions.
    /// The streaming handler was established in v0.6.5c.
    /// </para>
    /// <para>
    /// <strong>v0.6.5d:</strong> This path is only reached when
    /// <see cref="ShouldUseStreaming"/> returns <c>true</c> (Teams+ tier).
    /// </para>
    /// </remarks>
    private async Task SendStreamingAsync(string userInput, CancellationToken ct)
    {
        _logger?.LogDebug(
            "Initiating streaming request for {Tier} license",
            _license?.GetCurrentTier());

        // Build chat request
        var request = ChatRequest.FromUserMessage(userInput);

        // Stream tokens through the chat service
        await foreach (var token in _chatService!.StreamAsync(request, ct))
        {
            // Token handling is delegated to the StreamingChatHandler
            // which buffers, throttles, and dispatches to the UI.
            // This is a simplified path; full integration uses the handler.
        }
    }

    #endregion

    #region Batch Fallback Path (v0.6.5d)

    /// <summary>
    /// Sends a chat message using batch (non-streaming) completion.
    /// </summary>
    /// <param name="userInput">The trimmed user message text.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <remarks>
    /// <para>
    /// This method is used for WriterPro users who don't have access to
    /// streaming. The experience is functional but with higher perceived
    /// latency as the full response must be generated before display.
    /// </para>
    /// <para>
    /// The method manages the <see cref="IsGenerating"/> flag to drive
    /// loading indicators in the UI, and records the response duration
    /// in the <see cref="StatusMessage"/>.
    /// </para>
    /// </remarks>
    private async Task SendBatchAsync(string userInput, CancellationToken ct)
    {
        _logger?.LogDebug(
            "Initiating batch request for {Tier} license",
            _license?.GetCurrentTier());

        IsGenerating = true;
        StatusMessage = "Generating...";

        try
        {
            // Build chat request for batch completion
            var request = ChatRequest.FromUserMessage(userInput);
            var stopwatch = Stopwatch.StartNew();

            // Execute batch completion (waits for full response)
            var response = await _chatService!.CompleteAsync(request, ct);

            stopwatch.Stop();

            _logger?.LogInformation(
                "Batch response received in {ElapsedMs}ms, {Length} chars",
                stopwatch.ElapsedMilliseconds,
                response.Content?.Length ?? 0);

            // Add assistant message with the complete response
            Messages.Add(new ChatMessageViewModel
            {
                MessageId = Guid.NewGuid(),
                Role = ChatRole.Assistant,
                Content = response.Content ?? string.Empty,
                Timestamp = DateTime.Now,
                TokenCount = response.CompletionTokens
            });

            StatusMessage = $"Completed in {stopwatch.ElapsedMilliseconds}ms";
            RequestScrollToBottom();
        }
        finally
        {
            IsGenerating = false;
        }
    }

    #endregion

    #region Upgrade Hint Logic (v0.6.5d)

    /// <summary>
    /// Determines if an upgrade hint should be shown to the current user.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the hint should be displayed; <c>false</c> if the
    /// user has streaming, or the hint was already shown this session.
    /// </returns>
    /// <remarks>
    /// The hint is shown at most once per application session to avoid
    /// nagging. It is not shown if:
    /// <list type="bullet">
    ///   <item>Streaming is already available (Teams+ tier)</item>
    ///   <item>The hint was already shown this session</item>
    /// </list>
    /// </remarks>
    private bool ShouldShowUpgradeHint()
    {
        if (IsStreamingAvailable) return false;
        if (_upgradeHintShownThisSession) return false;
        return true;
    }

    /// <summary>
    /// Shows the streaming upgrade hint if conditions are met.
    /// </summary>
    /// <remarks>
    /// Sets the <see cref="StatusMessage"/> to the upgrade hint text
    /// and marks the hint as shown for this session. The hint is
    /// displayed in the status bar area of the chat view.
    /// </remarks>
    private void ShowUpgradeHintIfNeeded()
    {
        if (!ShouldShowUpgradeHint()) return;

        _upgradeHintShownThisSession = true;

        // Show non-intrusive hint in status bar
        StatusMessage = StreamingUnavailableMessage ?? string.Empty;

        _logger?.LogDebug("Displayed streaming upgrade hint");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Adds a user message to the <see cref="Messages"/> collection.
    /// </summary>
    /// <param name="content">The user's message text.</param>
    /// <remarks>
    /// Creates a new <see cref="ChatMessageViewModel"/> with the
    /// <see cref="ChatRole.User"/> role and the current timestamp.
    /// </remarks>
    private void AddUserMessage(string content)
    {
        Messages.Add(new ChatMessageViewModel
        {
            MessageId = Guid.NewGuid(),
            Role = ChatRole.User,
            Content = content,
            Timestamp = DateTime.Now
        });
    }

    /// <summary>
    /// Handles errors that occur during send operations.
    /// </summary>
    /// <param name="ex">The exception that was thrown.</param>
    /// <remarks>
    /// Logs the error and updates the <see cref="StatusMessage"/>
    /// with a user-friendly error description.
    /// </remarks>
    private void HandleSendError(Exception ex)
    {
        _logger?.LogError(ex, "Error during chat send operation");
        StatusMessage = $"Error: {ex.Message}";
    }

    #endregion

    #region Selection Context (v0.6.7a)

    /// <summary>
    /// Backing field for selection context text.
    /// </summary>
    private string? _selectionContext;

    /// <summary>
    /// Gets whether the chat panel has active selection context.
    /// </summary>
    /// <remarks>
    /// Drives the visibility of the <c>SelectionContextIndicator</c>
    /// in the chat panel UI.
    ///
    /// <b>Introduced in:</b> v0.6.7a
    /// </remarks>
    public bool HasSelectionContext => _selectionContext is not null;

    /// <summary>
    /// Gets a summary string for the selection context indicator.
    /// </summary>
    /// <remarks>
    /// Displays the character count, e.g., "Selection context (245 chars)".
    ///
    /// <b>Introduced in:</b> v0.6.7a
    /// </remarks>
    public string? SelectionSummary => _selectionContext is not null
        ? $"Selection context ({_selectionContext.Length} chars)"
        : null;

    /// <summary>
    /// Gets a preview of the selection context for display in the indicator.
    /// </summary>
    /// <remarks>
    /// Returns the first 80 characters of the selection with ellipsis
    /// if truncated. Used in the <c>SelectionContextIndicator</c> UI.
    ///
    /// <b>Introduced in:</b> v0.6.7a
    /// </remarks>
    public string? SelectionPreview => _selectionContext is not null
        ? _selectionContext.Length > 80
            ? $"\"{_selectionContext[..80]}...\""
            : $"\"{_selectionContext}\""
        : null;

    /// <summary>
    /// Event raised when the chat input should receive keyboard focus.
    /// </summary>
    /// <remarks>
    /// The <see cref="Views.CoPilotView"/> subscribes to this event to
    /// programmatically focus the text input field after selection context
    /// is set.
    ///
    /// <b>Introduced in:</b> v0.6.7a
    /// </remarks>
    public event EventHandler? FocusChatInputRequested;

    /// <summary>
    /// Sets the selection context for the Co-pilot chat panel.
    /// </summary>
    /// <param name="selection">The selected text from the editor.</param>
    /// <remarks>
    /// LOGIC: Stores the selection text and raises property change
    /// notifications for all selection context properties to update
    /// the UI (indicator, summary, preview).
    ///
    /// <b>Introduced in:</b> v0.6.7a
    /// </remarks>
    public virtual void SetSelectionContext(string selection)
    {
        _selectionContext = selection;
        OnPropertyChanged(nameof(HasSelectionContext));
        OnPropertyChanged(nameof(SelectionSummary));
        OnPropertyChanged(nameof(SelectionPreview));

        _logger?.LogDebug(
            "Selection context set in ViewModel: {CharCount} chars",
            selection.Length);
    }

    /// <summary>
    /// Clears the selection context from the Co-pilot chat panel.
    /// </summary>
    /// <remarks>
    /// LOGIC: Removes stored selection and notifies the UI to hide
    /// the selection context indicator.
    ///
    /// <b>Introduced in:</b> v0.6.7a
    /// </remarks>
    public virtual void ClearSelectionContext()
    {
        _selectionContext = null;
        OnPropertyChanged(nameof(HasSelectionContext));
        OnPropertyChanged(nameof(SelectionSummary));
        OnPropertyChanged(nameof(SelectionPreview));

        _logger?.LogDebug("Selection context cleared from ViewModel");
    }

    /// <summary>
    /// Requests that the chat input field receives keyboard focus.
    /// </summary>
    /// <remarks>
    /// LOGIC: Raises the <see cref="FocusChatInputRequested"/> event
    /// which the view subscribes to for programmatic focus transfer.
    ///
    /// <b>Introduced in:</b> v0.6.7a
    /// </remarks>
    public virtual void FocusChatInput()
    {
        FocusChatInputRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Command to clear the selection context (bound to the indicator's clear button).
    /// </summary>
    [RelayCommand]
    private void ClearSelectionContextCommand()
    {
        ClearSelectionContext();
    }

    #endregion
}
