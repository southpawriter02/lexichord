// ═══════════════════════════════════════════════════════════════════════════
// LEXICHORD - IssueCard Control
// Displays style suggestions with severity and action capability
// ═══════════════════════════════════════════════════════════════════════════

using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;

namespace Lexichord.Host.Controls;

/// <summary>
/// Severity levels for style issues.
/// </summary>
public enum IssueSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Card control displaying a style issue with original text, suggestion,
/// category, and an action button to apply the fix.
/// </summary>
public partial class IssueCard : UserControl
{
    // ─────────────────────────────────────────────────────────────────────────
    // Styled Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The original text that has an issue.
    /// </summary>
    public static readonly StyledProperty<string> OriginalTextProperty =
        AvaloniaProperty.Register<IssueCard, string>(nameof(OriginalText), string.Empty);

    public string OriginalText
    {
        get => GetValue(OriginalTextProperty);
        set => SetValue(OriginalTextProperty, value);
    }

    /// <summary>
    /// The suggested replacement text.
    /// </summary>
    public static readonly StyledProperty<string?> SuggestionTextProperty =
        AvaloniaProperty.Register<IssueCard, string?>(nameof(SuggestionText));

    public string? SuggestionText
    {
        get => GetValue(SuggestionTextProperty);
        set => SetValue(SuggestionTextProperty, value);
    }

    /// <summary>
    /// Category of the style issue (e.g., "Clarity", "Conciseness").
    /// </summary>
    public static readonly StyledProperty<string> CategoryProperty =
        AvaloniaProperty.Register<IssueCard, string>(nameof(Category), "General");

    public string Category
    {
        get => GetValue(CategoryProperty);
        set => SetValue(CategoryProperty, value);
    }

    /// <summary>
    /// Severity level of the issue.
    /// </summary>
    public static readonly StyledProperty<IssueSeverity> SeverityProperty =
        AvaloniaProperty.Register<IssueCard, IssueSeverity>(nameof(Severity), IssueSeverity.Warning);

    public IssueSeverity Severity
    {
        get => GetValue(SeverityProperty);
        set => SetValue(SeverityProperty, value);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Computed Properties
    // ─────────────────────────────────────────────────────────────────────────

    public bool HasSuggestion => !string.IsNullOrEmpty(SuggestionText);
    public bool IsError => Severity == IssueSeverity.Error;
    public bool IsWarning => Severity == IssueSeverity.Warning;
    public bool IsInfo => Severity == IssueSeverity.Info;

    // ─────────────────────────────────────────────────────────────────────────
    // Events
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Raised when the user clicks the "Apply Fix" button.
    /// </summary>
    public event EventHandler<IssueFixEventArgs>? FixApplied;

    // ─────────────────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────────────────

    public IssueCard()
    {
        InitializeComponent();
        DataContext = this;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void ApplyFix()
    {
        if (HasSuggestion)
        {
            FixApplied?.Invoke(this, new IssueFixEventArgs(OriginalText, SuggestionText!));
        }
    }
}

/// <summary>
/// Event args for when a fix is applied.
/// </summary>
public class IssueFixEventArgs : EventArgs
{
    public string OriginalText { get; }
    public string ReplacementText { get; }

    public IssueFixEventArgs(string original, string replacement)
    {
        OriginalText = original;
        ReplacementText = replacement;
    }
}
