// -----------------------------------------------------------------------
// <copyright file="SimplificationChangeViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using Lexichord.Abstractions.Agents.Simplifier;

namespace Lexichord.Modules.Agents.Simplifier;

/// <summary>
/// ViewModel wrapper for a <see cref="SimplificationChange"/> with UI state.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This ViewModel wraps a <see cref="SimplificationChange"/> record
/// to add mutable UI state such as selection and expansion status. It exposes
/// computed display properties for the change type and provides the underlying
/// change data for the preview diff UI.
/// </para>
/// <para>
/// <b>Selection State:</b>
/// By default, all changes are selected (<see cref="IsSelected"/> = <c>true</c>).
/// Users can toggle selection to include or exclude changes from acceptance.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4c as part of the Simplifier Agent Preview/Diff UI.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Creating a change ViewModel
/// var change = new SimplificationChange(
///     OriginalText: "utilize",
///     SimplifiedText: "use",
///     ChangeType: SimplificationChangeType.WordSimplification,
///     Explanation: "Replaced complex word with simpler alternative");
///
/// var viewModel = new SimplificationChangeViewModel(change, index: 0);
///
/// // Toggling selection
/// viewModel.IsSelected = !viewModel.IsSelected;
///
/// // Getting display text
/// Console.WriteLine(viewModel.ChangeTypeDisplay); // "Word Simplified"
/// Console.WriteLine(viewModel.ChangeTypeIcon);    // "SimplifyIcon"
/// </code>
/// </example>
/// <seealso cref="SimplificationChange"/>
/// <seealso cref="SimplificationPreviewViewModel"/>
public sealed partial class SimplificationChangeViewModel : ObservableObject
{
    private readonly SimplificationChange _change;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimplificationChangeViewModel"/> class.
    /// </summary>
    /// <param name="change">The underlying simplification change record.</param>
    /// <param name="index">The zero-based index of this change in the changes list.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="change"/> is <c>null</c>.
    /// </exception>
    public SimplificationChangeViewModel(SimplificationChange change, int index)
    {
        _change = change ?? throw new ArgumentNullException(nameof(change));
        Index = index;
    }

    #region Observable Properties

    /// <summary>
    /// Gets or sets a value indicating whether this change is selected for acceptance.
    /// </summary>
    /// <value>
    /// <c>true</c> if the change is selected; otherwise, <c>false</c>.
    /// Defaults to <c>true</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Selected changes will be applied when the user clicks
    /// "Accept All" or "Accept Selected". Changes can be toggled individually
    /// via the checkbox in the changes list.
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAccept))]
    private bool _isSelected = true;

    /// <summary>
    /// Gets or sets a value indicating whether this change's details are expanded.
    /// </summary>
    /// <value>
    /// <c>true</c> if the change details are expanded; otherwise, <c>false</c>.
    /// Defaults to <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> When expanded, the full explanation and confidence score
    /// are shown in the changes list UI.
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpandCollapseText))]
    [NotifyPropertyChangedFor(nameof(ExpandCollapseChevron))]
    private bool _isExpanded;

    /// <summary>
    /// Gets or sets a value indicating whether this change is currently highlighted.
    /// </summary>
    /// <value>
    /// <c>true</c> if the change is highlighted (e.g., on hover); otherwise, <c>false</c>.
    /// Defaults to <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Used to coordinate highlighting between the diff view
    /// and the changes list. When a change is highlighted, the corresponding
    /// text in the diff view is also highlighted.
    /// </remarks>
    [ObservableProperty]
    private bool _isHighlighted;

    #endregion

    #region Read-Only Properties

    /// <summary>
    /// Gets the zero-based index of this change in the changes list.
    /// </summary>
    /// <value>The index of this change.</value>
    /// <remarks>
    /// <b>LOGIC:</b> Used for tracking which changes are selected when
    /// publishing <see cref="Events.SimplificationAcceptedEvent"/>.
    /// </remarks>
    public int Index { get; }

    /// <summary>
    /// Gets the underlying simplification change record.
    /// </summary>
    /// <value>The original <see cref="SimplificationChange"/> data.</value>
    public SimplificationChange Change => _change;

    /// <summary>
    /// Gets the original text before simplification.
    /// </summary>
    /// <value>The text that was changed.</value>
    public string OriginalText => _change.OriginalText;

    /// <summary>
    /// Gets the simplified text after transformation.
    /// </summary>
    /// <value>The replacement text.</value>
    public string SimplifiedText => _change.SimplifiedText;

    /// <summary>
    /// Gets the type of change that was applied.
    /// </summary>
    /// <value>The <see cref="SimplificationChangeType"/> enum value.</value>
    public SimplificationChangeType ChangeType => _change.ChangeType;

    /// <summary>
    /// Gets the explanation for why this change improves readability.
    /// </summary>
    /// <value>A human-readable explanation from the LLM.</value>
    public string Explanation => _change.Explanation;

    /// <summary>
    /// Gets the confidence score for this change.
    /// </summary>
    /// <value>A value from 0.0 to 1.0 indicating confidence.</value>
    public double Confidence => _change.Confidence;

    /// <summary>
    /// Gets the location of this change in the original text.
    /// </summary>
    /// <value>The <see cref="TextLocation"/> or <c>null</c> if not available.</value>
    public TextLocation? Location => _change.Location;

    /// <summary>
    /// Gets a value indicating whether this change reduced the text length.
    /// </summary>
    /// <value>
    /// <c>true</c> if the simplified text is shorter than the original;
    /// otherwise, <c>false</c>.
    /// </value>
    public bool IsReduction => _change.IsReduction;

    /// <summary>
    /// Gets the length difference between the original and simplified text.
    /// </summary>
    /// <value>
    /// A positive value indicates the text was shortened;
    /// a negative value indicates it was lengthened.
    /// </value>
    public int LengthDifference => _change.LengthDifference;

    /// <summary>
    /// Gets a value indicating whether this change can be accepted.
    /// </summary>
    /// <value>
    /// <c>true</c> if the change is selected; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> A change can only be accepted if it is selected.
    /// This property is used to enable/disable accept buttons.
    /// </remarks>
    public bool CanAccept => IsSelected;

    #endregion

    #region Display Properties

    /// <summary>
    /// Gets the display name for the change type.
    /// </summary>
    /// <value>A human-readable string describing the change type.</value>
    /// <remarks>
    /// <b>LOGIC:</b> Converts the <see cref="SimplificationChangeType"/> enum
    /// to a user-friendly display string for the UI.
    /// </remarks>
    public string ChangeTypeDisplay => ChangeType switch
    {
        SimplificationChangeType.SentenceSplit => "Sentence Split",
        SimplificationChangeType.JargonReplacement => "Jargon Replaced",
        SimplificationChangeType.PassiveToActive => "Active Voice",
        SimplificationChangeType.WordSimplification => "Word Simplified",
        SimplificationChangeType.ClauseReduction => "Clause Reduced",
        SimplificationChangeType.TransitionAdded => "Transition Added",
        SimplificationChangeType.RedundancyRemoved => "Redundancy Removed",
        SimplificationChangeType.Combined => "Combined Changes",
        _ => "Change"
    };

    /// <summary>
    /// Gets the icon resource key for the change type.
    /// </summary>
    /// <value>A string key referencing the icon in theme resources.</value>
    /// <remarks>
    /// <b>LOGIC:</b> Each change type has a corresponding icon to help
    /// users quickly identify the type of change at a glance.
    /// </remarks>
    public string ChangeTypeIcon => ChangeType switch
    {
        SimplificationChangeType.SentenceSplit => "SplitIcon",
        SimplificationChangeType.JargonReplacement => "BookOpenIcon",
        SimplificationChangeType.PassiveToActive => "ArrowRightIcon",
        SimplificationChangeType.WordSimplification => "TypeIcon",
        SimplificationChangeType.ClauseReduction => "MinimizeIcon",
        SimplificationChangeType.TransitionAdded => "LinkIcon",
        SimplificationChangeType.RedundancyRemoved => "TrashIcon",
        SimplificationChangeType.Combined => "EditIcon",
        _ => "EditIcon"
    };

    /// <summary>
    /// Gets the CSS-style color class for the change type badge.
    /// </summary>
    /// <value>A string representing the badge color class.</value>
    /// <remarks>
    /// <b>LOGIC:</b> Different change types are color-coded to help
    /// users quickly categorize changes:
    /// <list type="bullet">
    ///   <item><description>Blue — Structural changes (sentence split, clause reduction)</description></item>
    ///   <item><description>Green — Voice changes (passive to active)</description></item>
    ///   <item><description>Orange — Vocabulary changes (jargon, word simplification)</description></item>
    ///   <item><description>Purple — Flow changes (transitions, redundancy)</description></item>
    /// </list>
    /// </remarks>
    public string ChangeTypeBadgeClass => ChangeType switch
    {
        SimplificationChangeType.SentenceSplit => "badge-blue",
        SimplificationChangeType.ClauseReduction => "badge-blue",
        SimplificationChangeType.PassiveToActive => "badge-green",
        SimplificationChangeType.JargonReplacement => "badge-orange",
        SimplificationChangeType.WordSimplification => "badge-orange",
        SimplificationChangeType.TransitionAdded => "badge-purple",
        SimplificationChangeType.RedundancyRemoved => "badge-purple",
        SimplificationChangeType.Combined => "badge-gray",
        _ => "badge-gray"
    };

    /// <summary>
    /// Gets a truncated preview of the original text for compact display.
    /// </summary>
    /// <value>The original text truncated to 50 characters with ellipsis.</value>
    public string OriginalTextPreview => TruncateText(OriginalText, 50);

    /// <summary>
    /// Gets a truncated preview of the simplified text for compact display.
    /// </summary>
    /// <value>The simplified text truncated to 50 characters with ellipsis.</value>
    public string SimplifiedTextPreview => TruncateText(SimplifiedText, 50);

    /// <summary>
    /// Gets the text for the expand/collapse button.
    /// </summary>
    /// <value>"Hide explanation" when expanded, "Show explanation" when collapsed.</value>
    /// <remarks>
    /// <b>LOGIC:</b> Provides a computed display string that changes based on
    /// the <see cref="IsExpanded"/> state for use in the UI button.
    /// </remarks>
    public string ExpandCollapseText => IsExpanded ? "Hide explanation" : "Show explanation";

    /// <summary>
    /// Gets the chevron character for the expand/collapse button.
    /// </summary>
    /// <value>"▲" when expanded, "▼" when collapsed.</value>
    public string ExpandCollapseChevron => IsExpanded ? "▲" : "▼";

    /// <summary>
    /// Gets the color for the length difference indicator.
    /// </summary>
    /// <value>Green color when text was reduced, red when lengthened.</value>
    /// <remarks>
    /// <b>LOGIC:</b> Provides visual feedback about whether the change
    /// made the text shorter (good) or longer (may need review).
    /// </remarks>
    public string LengthDifferenceColor => IsReduction ? "#22C55E" : "#EF4444";

    #endregion

    #region Private Methods

    /// <summary>
    /// Truncates text to a maximum length with ellipsis.
    /// </summary>
    /// <param name="text">The text to truncate.</param>
    /// <param name="maxLength">The maximum length including ellipsis.</param>
    /// <returns>The truncated text with ellipsis if needed.</returns>
    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
        {
            return text;
        }

        return string.Concat(text.AsSpan(0, maxLength - 3), "...");
    }

    #endregion
}
