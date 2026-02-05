// ═══════════════════════════════════════════════════════════════════════════════
// LEXICHORD - Agent Chat Panel (Critical Missing Component)
// Chat interface for AI writing agent interactions
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Lexichord.Host.Controls;

/// <summary>
/// Chat panel for interacting with AI writing agents.
/// </summary>
/// <remarks>
/// SPEC: SCREEN_WIREFRAMES.md Section 2.3 - The Ensemble (Active Session Panel)
/// This was identified as a critical missing component during the GUI audit.
///
/// Features:
/// - Message history with user/agent distinction
/// - Quick action buttons for common operations
/// - Action buttons on agent messages (Apply, Diff, Regenerate)
/// - Input field with send button
/// - Agent typing indicator
/// - Session controls (Stop, Clear, Export)
/// </remarks>
public partial class AgentChatPanel : UserControl
{
    // ─────────────────────────────────────────────────────────────────────────
    // Styled Properties
    // ─────────────────────────────────────────────────────────────────────────

    public static readonly StyledProperty<string> AgentNameProperty =
        AvaloniaProperty.Register<AgentChatPanel, string>(nameof(AgentName), "The Editor");

    public string AgentName
    {
        get => GetValue(AgentNameProperty);
        set => SetValue(AgentNameProperty, value);
    }

    public static readonly StyledProperty<string> AgentIconProperty =
        AvaloniaProperty.Register<AgentChatPanel, string>(nameof(AgentIcon), "🎭");

    public string AgentIcon
    {
        get => GetValue(AgentIconProperty);
        set => SetValue(AgentIconProperty, value);
    }

    public static readonly StyledProperty<string> AgentStatusProperty =
        AvaloniaProperty.Register<AgentChatPanel, string>(nameof(AgentStatus), "Ready to help");

    public string AgentStatus
    {
        get => GetValue(AgentStatusProperty);
        set => SetValue(AgentStatusProperty, value);
    }

    public static readonly StyledProperty<bool> IsTypingProperty =
        AvaloniaProperty.Register<AgentChatPanel, bool>(nameof(IsTyping), false);

    public bool IsTyping
    {
        get => GetValue(IsTypingProperty);
        set => SetValue(IsTypingProperty, value);
    }

    public static readonly StyledProperty<string> InputTextProperty =
        AvaloniaProperty.Register<AgentChatPanel, string>(nameof(InputText), string.Empty);

    public string InputText
    {
        get => GetValue(InputTextProperty);
        set => SetValue(InputTextProperty, value);
    }

    public static readonly StyledProperty<bool> ShowQuickActionsProperty =
        AvaloniaProperty.Register<AgentChatPanel, bool>(nameof(ShowQuickActions), true);

    public bool ShowQuickActions
    {
        get => GetValue(ShowQuickActionsProperty);
        set => SetValue(ShowQuickActionsProperty, value);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Computed Properties
    // ─────────────────────────────────────────────────────────────────────────

    public bool CanSend => !string.IsNullOrWhiteSpace(InputText) && !IsTyping;

    public static readonly DirectProperty<AgentChatPanel, bool> CanSendProperty =
        AvaloniaProperty.RegisterDirect<AgentChatPanel, bool>(
            nameof(CanSend), o => o.CanSend);

    // ─────────────────────────────────────────────────────────────────────────
    // Collections
    // ─────────────────────────────────────────────────────────────────────────

    public ObservableCollection<ChatMessage> Messages { get; } = [];

    // ─────────────────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────────────────

    public AgentChatPanel()
    {
        InitializeComponent();
        DataContext = this;

        // Update CanSend when dependencies change
        this.GetObservable(InputTextProperty).Subscribe(_ =>
            RaisePropertyChanged(CanSendProperty, default, CanSend));
        this.GetObservable(IsTypingProperty).Subscribe(_ =>
        {
            RaisePropertyChanged(CanSendProperty, default, CanSend);
            AgentStatus = IsTyping ? "Typing..." : "Ready to help";
        });

        // Add welcome message
        AddWelcomeMessage();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Event Handlers
    // ─────────────────────────────────────────────────────────────────────────

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            if (CanSend)
            {
                SendCommand.Execute(null);
            }
            e.Handled = true;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText))
            return;

        var userMessage = InputText;
        InputText = string.Empty;

        // Add user message
        Messages.Add(new ChatMessage
        {
            IsUser = true,
            Content = userMessage,
            SenderName = "You",
            Timestamp = DateTime.Now,
            TextColor = Brushes.White,
            TimestampAlignment = HorizontalAlignment.Right
        });

        // Simulate agent typing
        IsTyping = true;
        await Task.Delay(1500); // Simulated delay

        // Add agent response
        Messages.Add(new ChatMessage
        {
            IsUser = false,
            Content = GenerateAgentResponse(userMessage),
            SenderName = AgentName,
            Timestamp = DateTime.Now,
            TextColor = Application.Current?.FindResource("TextPrimaryBrush") as IBrush ?? Brushes.White,
            TimestampAlignment = HorizontalAlignment.Left,
            HasActions = true,
            CanApply = true,
            HasDiff = true
        });

        IsTyping = false;
        ShowQuickActions = true;

        // Scroll to bottom
        ScrollToBottom();
    }

    [RelayCommand]
    private void Stop()
    {
        IsTyping = false;
    }

    [RelayCommand]
    private void Clear()
    {
        Messages.Clear();
        AddWelcomeMessage();
    }

    [RelayCommand]
    private void Export()
    {
        // TODO: Implement chat export
    }

    [RelayCommand]
    private async Task QuickActionAsync(string action)
    {
        var prompt = action switch
        {
            "improve" => "Please improve my writing with better clarity and flow.",
            "summarize" => "Summarize the selected text concisely.",
            "tone" => "Adjust the tone to be more professional.",
            "expand" => "Expand on the ideas in this text with more detail.",
            "concise" => "Make this text more concise while keeping the key points.",
            _ => action
        };

        InputText = prompt;
        await SendAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private Methods
    // ─────────────────────────────────────────────────────────────────────────

    private void AddWelcomeMessage()
    {
        Messages.Add(new ChatMessage
        {
            IsUser = false,
            Content = $"Hello! I'm {AgentName}, your AI writing assistant. I can help you improve your writing, adjust tone, expand ideas, or create summaries. Select some text in your document or type a request below.",
            SenderName = AgentName,
            Timestamp = DateTime.Now,
            TextColor = Application.Current?.FindResource("TextPrimaryBrush") as IBrush ?? Brushes.White,
            TimestampAlignment = HorizontalAlignment.Left,
            HasActions = false
        });
    }

    private static string GenerateAgentResponse(string userMessage)
    {
        // This is a placeholder - actual implementation would call the LLM service
        if (userMessage.Contains("improve", StringComparison.OrdinalIgnoreCase))
        {
            return "I've analyzed your text and have some suggestions to improve clarity and flow. The main improvements include:\n\n• Replacing passive voice constructions with active voice\n• Breaking up long sentences for better readability\n• Using more specific word choices\n\nWould you like me to apply these changes?";
        }

        if (userMessage.Contains("summarize", StringComparison.OrdinalIgnoreCase))
        {
            return "Here's a concise summary of your selected text:\n\nThe document outlines the key principles of effective technical writing, emphasizing clarity, brevity, and audience awareness. It recommends using active voice, short sentences, and consistent terminology throughout.";
        }

        if (userMessage.Contains("tone", StringComparison.OrdinalIgnoreCase))
        {
            return "I've adjusted the tone to be more professional while maintaining your core message. The changes include:\n\n• Replacing casual phrases with formal alternatives\n• Adjusting sentence structure for a more polished feel\n• Ensuring consistent register throughout\n\nClick 'Apply Changes' to update your document.";
        }

        return "I understand your request. Based on my analysis, I have some suggestions that could help improve your text. Would you like me to explain the specific changes, or should I apply them directly to your document?";
    }

    private void ScrollToBottom()
    {
        if (this.FindControl<ScrollViewer>("MessagesScroll") is { } scrollViewer)
        {
            scrollViewer.ScrollToEnd();
        }
    }
}

/// <summary>
/// Represents a chat message in the conversation.
/// </summary>
public partial class ChatMessage : ObservableObject
{
    public bool IsUser { get; set; }
    public string Content { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public IBrush TextColor { get; set; } = Brushes.White;
    public HorizontalAlignment TimestampAlignment { get; set; }

    // Action properties
    public bool HasActions { get; set; }
    public bool CanApply { get; set; }
    public bool HasDiff { get; set; }

    // Code block properties
    public bool HasCodeBlock { get; set; }
    public string CodeBlock { get; set; } = string.Empty;

    [RelayCommand]
    private void Apply()
    {
        // TODO: Apply changes to the document
    }

    [RelayCommand]
    private void ShowDiff()
    {
        // TODO: Show diff view
    }

    [RelayCommand]
    private void Regenerate()
    {
        // TODO: Regenerate this response
    }
}
