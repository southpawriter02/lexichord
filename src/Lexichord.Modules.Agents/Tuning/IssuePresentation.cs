// =============================================================================
// File: IssuePresentation.cs
// Project: Lexichord.Modules.Agents
// Description: Per-issue UI state wrapper for the Unified Issues Panel.
// =============================================================================
// LOGIC: Wraps a UnifiedIssue record to add mutable UI state and computed
//   display properties for rendering individual issue items in the panel.
//
// v0.7.5g: Unified Issues Panel
// =============================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;
using Lexichord.Abstractions.Contracts.Validation;

namespace Lexichord.Modules.Agents.Tuning;

/// <summary>
/// Presentation model for a single <see cref="UnifiedIssue"/> with mutable UI state.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This class wraps the immutable <see cref="UnifiedIssue"/> record
/// to add mutable UI state for the Unified Issues Panel. It provides:
/// </para>
/// <list type="bullet">
///   <item><description>Expansion state for detail view toggle</description></item>
///   <item><description>Suppression state for dismissed issues</description></item>
///   <item><description>Computed display properties for UI binding</description></item>
///   <item><description>Commands for common issue actions</description></item>
/// </list>
/// <para>
/// <b>Lifecycle:</b>
/// Created by <see cref="IssuePresentationGroup"/> or <see cref="UnifiedIssuesPanelViewModel"/>
/// when populating the issues list. Not DI-registered — instances are created manually.
/// </para>
/// <para>
/// <b>Display Properties:</b>
/// The class provides computed properties that format issue data for UI display:
/// <list type="bullet">
///   <item><description><see cref="CategoryLabel"/>: Human-readable category name</description></item>
///   <item><description><see cref="SeverityLabel"/>: Human-readable severity name</description></item>
///   <item><description><see cref="LocationDisplay"/>: Formatted position string</description></item>
///   <item><description><see cref="SourceDisplay"/>: Validator name for attribution</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is designed for UI thread access only.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5g as part of the Unified Issues Panel feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var presentation = new IssuePresentation(unifiedIssue);
///
/// // Expand to show details
/// presentation.IsExpanded = true;
///
/// // Check if fixable
/// if (presentation.CanAutoApply)
/// {
///     // Apply fix...
/// }
///
/// // Dismiss the issue
/// presentation.IsSuppressed = true;
/// </code>
/// </example>
/// <seealso cref="UnifiedIssue"/>
/// <seealso cref="IssuePresentationGroup"/>
/// <seealso cref="UnifiedIssuesPanelViewModel"/>
public sealed partial class IssuePresentation : ObservableObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IssuePresentation"/> class.
    /// </summary>
    /// <param name="issue">The unified issue to wrap.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="issue"/> is null.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Captures the issue reference for display. The issue starts
    /// with collapsed details (<see cref="IsExpanded"/> = false) and active state
    /// (<see cref="IsSuppressed"/> = false).
    /// </remarks>
    public IssuePresentation(UnifiedIssue issue)
    {
        ArgumentNullException.ThrowIfNull(issue);
        Issue = issue;
    }

    // ── The Underlying Issue ────────────────────────────────────────────

    /// <summary>
    /// Gets the underlying unified issue.
    /// </summary>
    /// <value>
    /// The <see cref="UnifiedIssue"/> record being presented.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> The issue is immutable; this property provides access to
    /// all original issue data including severity, category, location, message,
    /// and available fixes.
    /// </remarks>
    public UnifiedIssue Issue { get; }

    // ── Observable UI State ─────────────────────────────────────────────

    /// <summary>
    /// Gets or sets whether the issue details are expanded.
    /// </summary>
    /// <value>
    /// <c>true</c> if the detail section is visible; <c>false</c> if collapsed.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> When expanded, shows additional details like suggested fix,
    /// original text, and validator attribution. Toggled by clicking the issue
    /// row or pressing Enter/Space.
    /// </remarks>
    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>
    /// Gets or sets whether the issue is suppressed (dismissed).
    /// </summary>
    /// <value>
    /// <c>true</c> if the user has dismissed this issue; <c>false</c> if active.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Suppressed issues remain in the list but are visually
    /// grayed out. They are not included in the active count and are skipped
    /// during bulk fix operations.
    /// </remarks>
    [ObservableProperty]
    private bool _isSuppressed;

    /// <summary>
    /// Gets or sets whether the issue has been fixed.
    /// </summary>
    /// <value>
    /// <c>true</c> if a fix has been applied to this issue; <c>false</c> otherwise.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Set to <c>true</c> after successfully applying a fix.
    /// Fixed issues may be styled differently or removed on next validation refresh.
    /// </remarks>
    [ObservableProperty]
    private bool _isFixed;

    /// <summary>
    /// Gets or sets whether this issue is currently selected.
    /// </summary>
    /// <value>
    /// <c>true</c> if this is the currently focused issue; <c>false</c> otherwise.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Used for keyboard navigation highlighting. Only one issue
    /// should be selected at a time across all groups.
    /// </remarks>
    [ObservableProperty]
    private bool _isSelected;

    // ── Computed Display Properties — Issue Info ────────────────────────

    /// <summary>
    /// Gets the human-readable category label.
    /// </summary>
    /// <value>
    /// "Style", "Grammar", "Knowledge", "Structure", or "Custom".
    /// </value>
    public string CategoryLabel => Issue.Category switch
    {
        IssueCategory.Style => "Style",
        IssueCategory.Grammar => "Grammar",
        IssueCategory.Knowledge => "Knowledge",
        IssueCategory.Structure => "Structure",
        IssueCategory.Custom => "Custom",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets the human-readable severity label.
    /// </summary>
    /// <value>
    /// "Error", "Warning", "Info", or "Hint".
    /// </value>
    public string SeverityLabel => Issue.Severity switch
    {
        UnifiedSeverity.Error => "Error",
        UnifiedSeverity.Warning => "Warning",
        UnifiedSeverity.Info => "Info",
        UnifiedSeverity.Hint => "Hint",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets the location display string.
    /// </summary>
    /// <value>
    /// A string like "Position 123" or "Chars 100-150".
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> The <see cref="Abstractions.Contracts.Editor.TextSpan"/> stores
    /// character offsets (Start, Length), not line/column numbers. For detailed
    /// location display, the UI layer may need to convert to line/column using
    /// document content.
    /// </remarks>
    public string LocationDisplay
    {
        get
        {
            var start = Issue.Location.Start;
            var length = Issue.Location.Length;

            return length > 0
                ? $"Chars {start}-{start + length}"
                : $"Position {start}";
        }
    }

    /// <summary>
    /// Gets the source validator name for attribution.
    /// </summary>
    /// <value>
    /// "Style Linter", "Grammar Linter", "Validation Engine", or "Unknown".
    /// </value>
    public string SourceDisplay => Issue.SourceType switch
    {
        "StyleLinter" => "Style Linter",
        "GrammarLinter" => "Grammar Linter",
        "Validation" => "Validation Engine",
        _ => Issue.SourceType ?? "Unknown"
    };

    /// <summary>
    /// Gets the source rule or validation code identifier.
    /// </summary>
    /// <value>
    /// The <see cref="UnifiedIssue.SourceId"/> (e.g., "TERM-001", "PASSIVE_VOICE").
    /// </value>
    public string SourceId => Issue.SourceId ?? string.Empty;

    /// <summary>
    /// Gets the issue message.
    /// </summary>
    /// <value>
    /// The human-readable description from <see cref="UnifiedIssue.Message"/>.
    /// </value>
    public string Message => Issue.Message;

    /// <summary>
    /// Gets the original text at the issue location.
    /// </summary>
    /// <value>
    /// The text that would be replaced by a fix, or empty string if unavailable.
    /// </value>
    public string OriginalText => Issue.OriginalText ?? string.Empty;

    // ── Computed Display Properties — Fix Info ──────────────────────────

    /// <summary>
    /// Gets whether this issue has at least one available fix.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="UnifiedIssue.HasFixes"/> is true.
    /// </value>
    public bool HasFix => Issue.HasFixes;

    /// <summary>
    /// Gets whether the best fix can be applied automatically.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="UnifiedIssue.CanAutoFix"/> is true.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Only issues with <c>CanAutoApply == true</c> are included
    /// in bulk fix operations. The "Apply Fix" button is enabled only when this is true.
    /// </remarks>
    public bool CanAutoApply => Issue.CanAutoFix;

    /// <summary>
    /// Gets whether the best fix has high confidence (>= 0.8).
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="UnifiedIssue.HasHighConfidenceFix"/> is true.
    /// </value>
    public bool HasHighConfidenceFix => Issue.HasHighConfidenceFix;

    /// <summary>
    /// Gets the best fix description.
    /// </summary>
    /// <value>
    /// The description from <see cref="UnifiedIssue.BestFix"/>, or empty string.
    /// </value>
    public string FixDescription => Issue.BestFix?.Description ?? string.Empty;

    /// <summary>
    /// Gets the suggested replacement text.
    /// </summary>
    /// <value>
    /// The <see cref="UnifiedFix.NewText"/> from the best fix, or empty string.
    /// </value>
    public string SuggestedText => Issue.BestFix?.NewText ?? string.Empty;

    /// <summary>
    /// Gets the text that would be replaced by the fix.
    /// </summary>
    /// <value>
    /// The <see cref="UnifiedFix.OldText"/> from the best fix, or empty string.
    /// </value>
    public string FixOriginalText => Issue.BestFix?.OldText ?? string.Empty;

    /// <summary>
    /// Gets the confidence score of the best fix as a percentage string.
    /// </summary>
    /// <value>
    /// A string like "85%" or empty if no fix available.
    /// </value>
    public string ConfidenceDisplay =>
        Issue.BestFix is not null
            ? $"{Issue.BestFix.Confidence * 100:F0}%"
            : string.Empty;

    /// <summary>
    /// Gets the fix type label for display.
    /// </summary>
    /// <value>
    /// "Replace", "Insert", "Delete", "Rewrite", or "No Fix".
    /// </value>
    public string FixTypeLabel => Issue.BestFix?.Type switch
    {
        FixType.Replacement => "Replace",
        FixType.Insertion => "Insert",
        FixType.Deletion => "Delete",
        FixType.Rewrite => "Rewrite",
        FixType.NoFix => "No Fix",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets the number of available fixes.
    /// </summary>
    /// <value>
    /// The count of fixes in <see cref="UnifiedIssue.Fixes"/>.
    /// </value>
    public int FixCount => Issue.Fixes.Count;

    /// <summary>
    /// Gets whether multiple fixes are available.
    /// </summary>
    /// <value>
    /// <c>true</c> if more than one fix is available.
    /// </value>
    public bool HasMultipleFixes => Issue.Fixes.Count > 1;

    // ── Computed Properties — Status ────────────────────────────────────

    /// <summary>
    /// Gets whether this issue is actionable (not suppressed, not fixed).
    /// </summary>
    /// <value>
    /// <c>true</c> if the issue can still be acted upon.
    /// </value>
    public bool IsActionable => !IsSuppressed && !IsFixed;

    /// <summary>
    /// Gets the visual opacity for the issue row.
    /// </summary>
    /// <value>
    /// 1.0 for active issues, 0.5 for suppressed or fixed issues.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Provides a binding target for row opacity styling.
    /// </remarks>
    public double DisplayOpacity => (IsSuppressed || IsFixed) ? 0.5 : 1.0;

    // ── Commands ────────────────────────────────────────────────────────

    /// <summary>
    /// Toggles the expansion state of the issue details.
    /// </summary>
    [RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
    }

    /// <summary>
    /// Toggles the suppressed state of the issue.
    /// </summary>
    [RelayCommand]
    private void ToggleSuppressed()
    {
        IsSuppressed = !IsSuppressed;

        // LOGIC: Notify computed properties that depend on suppressed state.
        OnPropertyChanged(nameof(IsActionable));
        OnPropertyChanged(nameof(DisplayOpacity));
    }

    // ── Public Methods ──────────────────────────────────────────────────

    /// <summary>
    /// Marks the issue as fixed.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Called after successfully applying a fix. Updates the
    /// visual state and notifies dependent properties.
    /// </remarks>
    public void MarkAsFixed()
    {
        IsFixed = true;
        OnPropertyChanged(nameof(IsActionable));
        OnPropertyChanged(nameof(DisplayOpacity));
    }

    /// <summary>
    /// Notifies all computed properties that depend on the underlying issue.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Call this if the underlying issue could have changed
    /// (though UnifiedIssue is immutable, this is here for future flexibility).
    /// </remarks>
    public void NotifyIssueChanged()
    {
        OnPropertyChanged(nameof(Issue));
        OnPropertyChanged(nameof(CategoryLabel));
        OnPropertyChanged(nameof(SeverityLabel));
        OnPropertyChanged(nameof(LocationDisplay));
        OnPropertyChanged(nameof(SourceDisplay));
        OnPropertyChanged(nameof(SourceId));
        OnPropertyChanged(nameof(Message));
        OnPropertyChanged(nameof(OriginalText));
        OnPropertyChanged(nameof(HasFix));
        OnPropertyChanged(nameof(CanAutoApply));
        OnPropertyChanged(nameof(HasHighConfidenceFix));
        OnPropertyChanged(nameof(FixDescription));
        OnPropertyChanged(nameof(SuggestedText));
        OnPropertyChanged(nameof(FixOriginalText));
        OnPropertyChanged(nameof(ConfidenceDisplay));
        OnPropertyChanged(nameof(FixTypeLabel));
        OnPropertyChanged(nameof(FixCount));
        OnPropertyChanged(nameof(HasMultipleFixes));
    }
}
