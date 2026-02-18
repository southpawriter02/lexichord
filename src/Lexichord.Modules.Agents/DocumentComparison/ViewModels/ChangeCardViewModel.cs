// -----------------------------------------------------------------------
// <copyright file="ChangeCardViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: ViewModel for individual change cards in the Comparison View (v0.7.6d).
//   Wraps a DocumentChange with display properties for UI binding:
//   - CategoryIcon: Icon name based on category
//   - CategoryColor: Theme color based on category
//   - SignificanceColor: Color based on significance level
//   - IsExpanded: Toggle for showing full diff
//
//   Introduced in: v0.7.6d
// -----------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using Lexichord.Abstractions.Agents.DocumentComparison;

namespace Lexichord.Modules.Agents.DocumentComparison.ViewModels;

/// <summary>
/// ViewModel for displaying an individual change in the comparison view.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This ViewModel wraps a <see cref="DocumentChange"/> with computed
/// display properties for UI binding. Each change card shows:
/// <list type="bullet">
/// <item><description>Category with icon and color coding</description></item>
/// <item><description>Section location</description></item>
/// <item><description>Human-readable description</description></item>
/// <item><description>Significance score and level</description></item>
/// <item><description>Expandable diff view with original/new text</description></item>
/// <item><description>Impact note for high-significance changes</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Lifetime:</b> Created by <see cref="ComparisonViewModel"/> for each change.
/// </para>
/// <para><b>Introduced in:</b> v0.7.6d as part of the Document Comparison feature.</para>
/// </remarks>
/// <seealso cref="DocumentChange"/>
/// <seealso cref="ComparisonViewModel"/>
public sealed partial class ChangeCardViewModel : ObservableObject
{
    /// <summary>
    /// Gets the underlying document change.
    /// </summary>
    public DocumentChange Change { get; }

    /// <summary>
    /// Gets or sets whether the diff view is expanded.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpandCollapseIcon))]
    private bool _isExpanded;

    /// <summary>
    /// Initializes a new instance of <see cref="ChangeCardViewModel"/>.
    /// </summary>
    /// <param name="change">The document change to wrap.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="change"/> is null.</exception>
    public ChangeCardViewModel(DocumentChange change)
    {
        Change = change ?? throw new ArgumentNullException(nameof(change));
    }

    // ── Display Properties ───────────────────────────────────────────────

    /// <summary>
    /// Gets the category of the change.
    /// </summary>
    public ChangeCategory Category => Change.Category;

    /// <summary>
    /// Gets the section where the change occurred.
    /// </summary>
    public string? Section => Change.Section;

    /// <summary>
    /// Gets the human-readable description.
    /// </summary>
    public string Description => Change.Description;

    /// <summary>
    /// Gets the significance score (0.0 to 1.0).
    /// </summary>
    public double Significance => Change.Significance;

    /// <summary>
    /// Gets the significance level (Critical, High, Medium, Low).
    /// </summary>
    public ChangeSignificance SignificanceLevel => Change.SignificanceLevel;

    /// <summary>
    /// Gets the original text before the change.
    /// </summary>
    public string? OriginalText => Change.OriginalText;

    /// <summary>
    /// Gets the new text after the change.
    /// </summary>
    public string? NewText => Change.NewText;

    /// <summary>
    /// Gets the impact note explaining why the change matters.
    /// </summary>
    public string? Impact => Change.Impact;

    /// <summary>
    /// Gets whether there is original text to display.
    /// </summary>
    public bool HasOriginalText => Change.HasOriginalText;

    /// <summary>
    /// Gets whether there is new text to display.
    /// </summary>
    public bool HasNewText => Change.HasNewText;

    /// <summary>
    /// Gets whether there is impact information to display.
    /// </summary>
    public bool HasImpact => Change.HasImpact;

    /// <summary>
    /// Gets whether the diff can be expanded (has text to show).
    /// </summary>
    public bool CanExpand => HasOriginalText || HasNewText;

    // ── Icon and Color Properties ────────────────────────────────────────

    /// <summary>
    /// Gets the icon name for this category.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Returns icon resource names from the theme:
    /// <list type="bullet">
    /// <item><description>Added: "Plus" (green)</description></item>
    /// <item><description>Removed: "Minus" (red)</description></item>
    /// <item><description>Modified: "Edit" (orange)</description></item>
    /// <item><description>Restructured: "Arrow" (blue)</description></item>
    /// <item><description>Clarified: "Lightbulb" (blue)</description></item>
    /// <item><description>Formatting: "Brush" (gray)</description></item>
    /// <item><description>Correction: "Check" (red)</description></item>
    /// <item><description>Terminology: "Tag" (purple)</description></item>
    /// </list>
    /// </remarks>
    public string CategoryIcon => Category switch
    {
        ChangeCategory.Added => "Plus",
        ChangeCategory.Removed => "Minus",
        ChangeCategory.Modified => "Edit",
        ChangeCategory.Restructured => "ArrowRight",
        ChangeCategory.Clarified => "Lightbulb",
        ChangeCategory.Formatting => "Brush",
        ChangeCategory.Correction => "Checkmark",
        ChangeCategory.Terminology => "Tag",
        _ => "Edit"
    };

    /// <summary>
    /// Gets the display symbol for this category (for text-based display).
    /// </summary>
    public string CategorySymbol => Category switch
    {
        ChangeCategory.Added => "+",
        ChangeCategory.Removed => "-",
        ChangeCategory.Modified => "~",
        ChangeCategory.Restructured => "→",
        ChangeCategory.Clarified => "?",
        ChangeCategory.Formatting => "#",
        ChangeCategory.Correction => "!",
        ChangeCategory.Terminology => "@",
        _ => "~"
    };

    /// <summary>
    /// Gets the brush resource key for the category color.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Returns brush resource keys from the theme:
    /// <list type="bullet">
    /// <item><description>Added: Success/Green</description></item>
    /// <item><description>Removed: Error/Red</description></item>
    /// <item><description>Modified: Warning/Orange</description></item>
    /// <item><description>Restructured: Info/Blue</description></item>
    /// <item><description>Clarified: Info/Blue</description></item>
    /// <item><description>Formatting: Tertiary/Gray</description></item>
    /// <item><description>Correction: Error/Red</description></item>
    /// <item><description>Terminology: Accent/Purple</description></item>
    /// </list>
    /// </remarks>
    public string CategoryColorKey => Category switch
    {
        ChangeCategory.Added => "SuccessBrush",
        ChangeCategory.Removed => "ErrorBrush",
        ChangeCategory.Modified => "WarningBrush",
        ChangeCategory.Restructured => "InfoBrush",
        ChangeCategory.Clarified => "InfoBrush",
        ChangeCategory.Formatting => "TertiaryBrush",
        ChangeCategory.Correction => "ErrorBrush",
        ChangeCategory.Terminology => "AccentBrush",
        _ => "WarningBrush"
    };

    /// <summary>
    /// Gets the brush resource key for the significance level color.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Returns brush resource keys matching the UI spec:
    /// <list type="bullet">
    /// <item><description>Critical: Error/Red</description></item>
    /// <item><description>High: Warning/Orange</description></item>
    /// <item><description>Medium: Info/Blue</description></item>
    /// <item><description>Low: Tertiary/Gray</description></item>
    /// </list>
    /// </remarks>
    public string SignificanceColorKey => SignificanceLevel switch
    {
        ChangeSignificance.Critical => "ErrorBrush",
        ChangeSignificance.High => "WarningBrush",
        ChangeSignificance.Medium => "InfoBrush",
        _ => "TertiaryBrush"
    };

    /// <summary>
    /// Gets the display label for the significance level.
    /// </summary>
    public string SignificanceLabel => SignificanceLevel.GetDisplayLabel();

    /// <summary>
    /// Gets the formatted significance score for display.
    /// </summary>
    public string SignificanceDisplay => $"{Significance:F2}";

    /// <summary>
    /// Gets the category display name.
    /// </summary>
    public string CategoryDisplay => Category.ToString().ToUpperInvariant();

    /// <summary>
    /// Gets the header text combining category and section.
    /// </summary>
    public string HeaderText => Section is not null
        ? $"[{CategorySymbol}] {CategoryDisplay} in \"{Section}\""
        : $"[{CategorySymbol}] {CategoryDisplay}";

    // ── Diff Display Properties ─────────────────────────────────────────

    /// <summary>
    /// Gets whether the change card has diff content that can be shown.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Returns <c>true</c> if either original or new text is present.
    /// Used to conditionally show the expand/collapse toggle button.
    /// </remarks>
    public bool HasDiffContent => HasOriginalText || HasNewText;

    /// <summary>
    /// Gets the expand/collapse icon character.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Returns a down arrow when expanded, right arrow when collapsed.
    /// </remarks>
    public string ExpandCollapseIcon => IsExpanded ? "▼" : "▶";

    /// <summary>
    /// Gets whether to show the diff arrow between original and new text.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Only show arrow when both original and new text exist,
    /// indicating a modification rather than pure addition/removal.
    /// </remarks>
    public bool ShowDiffArrow => HasOriginalText && HasNewText;

    /// <summary>
    /// Gets or sets the label for the original version.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Defaults to "Original" but can be customized
    /// (e.g., "HEAD~1", "main", "v1.0") via <see cref="SetVersionLabels"/>.
    /// </remarks>
    public string OriginalVersionLabel { get; private set; } = "Original";

    /// <summary>
    /// Gets or sets the label for the new version.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Defaults to "New" but can be customized
    /// (e.g., "Current", "HEAD", "v2.0") via <see cref="SetVersionLabels"/>.
    /// </remarks>
    public string NewVersionLabel { get; private set; } = "New";

    /// <summary>
    /// Sets custom labels for the original and new versions.
    /// </summary>
    /// <param name="originalLabel">Label for the original version (e.g., "HEAD~1").</param>
    /// <param name="newLabel">Label for the new version (e.g., "Current").</param>
    /// <remarks>
    /// <b>LOGIC:</b> Allows customization of version labels when comparing
    /// git revisions or named document versions.
    /// </remarks>
    public void SetVersionLabels(string originalLabel, string newLabel)
    {
        OriginalVersionLabel = originalLabel ?? "Original";
        NewVersionLabel = newLabel ?? "New";
        OnPropertyChanged(nameof(OriginalVersionLabel));
        OnPropertyChanged(nameof(NewVersionLabel));
    }

    // ── Commands ────────────────────────────────────────────────────────

    /// <summary>
    /// Toggles the expanded state of the diff view.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Bound to the expand/collapse button.
    /// </remarks>
    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
    }
}
