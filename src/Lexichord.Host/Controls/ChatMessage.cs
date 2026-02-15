using System;
using Avalonia.Layout;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Lexichord.Host.Controls;

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
