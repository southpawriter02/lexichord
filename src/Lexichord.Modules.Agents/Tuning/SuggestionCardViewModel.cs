// -----------------------------------------------------------------------
// <copyright file="SuggestionCardViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts.Agents;

namespace Lexichord.Modules.Agents.Tuning;

/// <summary>
/// ViewModel wrapper for a <see cref="StyleDeviation"/> and its associated
/// <see cref="FixSuggestion"/> with mutable UI state for review.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This ViewModel wraps the immutable <see cref="StyleDeviation"/> and
/// <see cref="FixSuggestion"/> records to add mutable UI state such as expansion,
/// review status, and alternative selection. It exposes computed display properties
/// for the suggestion card UI.
/// </para>
/// <para>
/// <b>Lifecycle:</b>
/// Created by <see cref="TuningPanelViewModel"/> during scan/generation flow.
/// Not DI-registered — instances are created manually.
/// </para>
/// <para>
/// <b>Suggestion Updates:</b>
/// When a suggestion is regenerated via <see cref="UpdateSuggestion"/>, the underlying
/// <see cref="FixSuggestion"/> is replaced and all computed properties are re-notified.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5c as part of the Accept/Reject UI feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var card = new SuggestionCardViewModel(deviation, suggestion);
/// card.IsExpanded = true;
///
/// // After accepting
/// card.Status = SuggestionStatus.Accepted;
/// card.IsReviewed = true;
///
/// // After regeneration
/// card.UpdateSuggestion(newSuggestion);
/// </code>
/// </example>
/// <seealso cref="TuningPanelViewModel"/>
/// <seealso cref="StyleDeviation"/>
/// <seealso cref="FixSuggestion"/>
public sealed partial class SuggestionCardViewModel : ObservableObject
{
    // ── Immutable State ──────────────────────────────────────────────────
    private readonly StyleDeviation _deviation;
    private FixSuggestion _suggestion;

    /// <summary>
    /// Initializes a new instance of the <see cref="SuggestionCardViewModel"/> class.
    /// </summary>
    /// <param name="deviation">The style deviation detected in the document.</param>
    /// <param name="suggestion">The AI-generated fix suggestion for the deviation.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="deviation"/> or <paramref name="suggestion"/> is null.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Captures references to the deviation and suggestion for
    /// display in the suggestion card. The card starts in <see cref="SuggestionStatus.Pending"/>
    /// state with the expanded panel collapsed.
    /// </remarks>
    public SuggestionCardViewModel(StyleDeviation deviation, FixSuggestion suggestion)
    {
        ArgumentNullException.ThrowIfNull(deviation);
        ArgumentNullException.ThrowIfNull(suggestion);

        _deviation = deviation;
        _suggestion = suggestion;
    }

    // ── Exposed Immutable Data ───────────────────────────────────────────

    /// <summary>
    /// Gets the underlying style deviation.
    /// </summary>
    public StyleDeviation Deviation => _deviation;

    /// <summary>
    /// Gets the current fix suggestion (may be updated via regeneration).
    /// </summary>
    public FixSuggestion Suggestion => _suggestion;

    // ── Observable UI State ──────────────────────────────────────────────

    /// <summary>
    /// Whether the suggestion card is expanded to show full details.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Toggled by clicking the card header or pressing Enter/Space.
    /// Only one card should typically be expanded at a time, managed by
    /// <see cref="TuningPanelViewModel.SelectSuggestion"/>.
    /// </remarks>
    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>
    /// Whether the user has reviewed this suggestion (any action taken).
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Set to <c>true</c> when the status transitions from
    /// <see cref="SuggestionStatus.Pending"/> to any other state.
    /// Used for progress tracking.
    /// </remarks>
    [ObservableProperty]
    private bool _isReviewed;

    /// <summary>
    /// The current review status of this suggestion.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Transitions from <see cref="SuggestionStatus.Pending"/> to one of
    /// Accepted, Rejected, Modified, or Skipped. Drives visual styling (green/red/yellow)
    /// and filter behavior.
    /// </remarks>
    [ObservableProperty]
    private SuggestionStatus _status = SuggestionStatus.Pending;

    /// <summary>
    /// The user-modified text, set when the suggestion is edited before applying.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Only populated when <see cref="Status"/> is
    /// <see cref="SuggestionStatus.Modified"/>. Stored for learning loop feedback.
    /// </remarks>
    [ObservableProperty]
    private string? _modifiedText;

    /// <summary>
    /// Whether the alternatives list is expanded.
    /// </summary>
    [ObservableProperty]
    private bool _showAlternatives;

    /// <summary>
    /// The currently selected alternative suggestion, if any.
    /// </summary>
    [ObservableProperty]
    private AlternativeSuggestion? _selectedAlternative;

    /// <summary>
    /// Whether this suggestion is currently being regenerated.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Set to <c>true</c> during regeneration API call.
    /// Used to show a loading indicator on the suggestion card.
    /// </remarks>
    [ObservableProperty]
    private bool _isRegenerating;

    // ── Computed Display Properties ──────────────────────────────────────

    /// <summary>
    /// Gets the name of the violated rule for display.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Delegates to <see cref="StyleDeviation.ViolatedRule"/> name.
    /// Returns "Unknown Rule" if the rule reference is unavailable.
    /// </remarks>
    public string RuleName => Deviation.ViolatedRule?.Name ?? "Unknown Rule";

    /// <summary>
    /// Gets the category of the violated rule for grouping.
    /// </summary>
    public string RuleCategory => Deviation.Category;

    /// <summary>
    /// Gets the priority level for display and sorting.
    /// </summary>
    public DeviationPriority Priority => Deviation.Priority;

    /// <summary>
    /// Gets the AI confidence score (0.0 to 1.0).
    /// </summary>
    public double Confidence => Suggestion.Confidence;

    /// <summary>
    /// Gets the quality/semantic preservation score (0.0 to 1.0).
    /// </summary>
    public double QualityScore => Suggestion.QualityScore;

    /// <summary>
    /// Gets whether this suggestion meets the high-confidence threshold.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Delegates to <see cref="FixSuggestion.IsHighConfidence"/>
    /// (confidence ≥ 0.9, quality ≥ 0.9, validated).
    /// </remarks>
    public bool IsHighConfidence => Suggestion.IsHighConfidence;

    /// <summary>
    /// Gets the diff between original and suggested text.
    /// </summary>
    public TextDiff Diff => Suggestion.Diff;

    /// <summary>
    /// Gets the original text with the style violation.
    /// </summary>
    public string OriginalText => Deviation.OriginalText;

    /// <summary>
    /// Gets the AI-suggested replacement text.
    /// </summary>
    public string SuggestedText => Suggestion.SuggestedText;

    /// <summary>
    /// Gets the human-readable explanation of the fix.
    /// </summary>
    public string Explanation => Suggestion.Explanation;

    /// <summary>
    /// Gets the list of alternative suggestions, if available.
    /// </summary>
    public IReadOnlyList<AlternativeSuggestion>? Alternatives => Suggestion.Alternatives;

    /// <summary>
    /// Gets whether alternative suggestions are available.
    /// </summary>
    public bool HasAlternatives => Alternatives?.Count > 0;

    /// <summary>
    /// Gets the confidence score formatted as a percentage string.
    /// </summary>
    /// <example>"92%"</example>
    public string ConfidenceDisplay => $"{Confidence * 100:F0}%";

    /// <summary>
    /// Gets the quality score formatted as a percentage string.
    /// </summary>
    /// <example>"95%"</example>
    public string QualityDisplay => $"{QualityScore * 100:F0}%";

    /// <summary>
    /// Gets a human-readable priority label.
    /// </summary>
    public string PriorityDisplay => Priority switch
    {
        DeviationPriority.Critical => "Critical",
        DeviationPriority.High => "High",
        DeviationPriority.Normal => "Normal",
        DeviationPriority.Low => "Low",
        _ => "Unknown"
    };

    // ── Commands ─────────────────────────────────────────────────────────

    /// <summary>
    /// Toggles the alternatives list expansion.
    /// </summary>
    [RelayCommand]
    private void ToggleAlternatives()
    {
        ShowAlternatives = !ShowAlternatives;
    }

    // ── Public Methods ───────────────────────────────────────────────────

    /// <summary>
    /// Updates the underlying suggestion after regeneration.
    /// </summary>
    /// <param name="newSuggestion">The regenerated fix suggestion.</param>
    /// <remarks>
    /// <b>LOGIC:</b> Replaces the internal <see cref="FixSuggestion"/> reference
    /// and raises property change notifications for all computed properties
    /// that depend on it. Called by <see cref="TuningPanelViewModel.RegenerateSuggestionAsync"/>
    /// after receiving a new suggestion from the generator.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="newSuggestion"/> is null.
    /// </exception>
    public void UpdateSuggestion(FixSuggestion newSuggestion)
    {
        ArgumentNullException.ThrowIfNull(newSuggestion);

        _suggestion = newSuggestion;

        // LOGIC: Notify all computed properties that depend on the suggestion.
        OnPropertyChanged(nameof(Suggestion));
        OnPropertyChanged(nameof(Confidence));
        OnPropertyChanged(nameof(QualityScore));
        OnPropertyChanged(nameof(IsHighConfidence));
        OnPropertyChanged(nameof(Diff));
        OnPropertyChanged(nameof(SuggestedText));
        OnPropertyChanged(nameof(Explanation));
        OnPropertyChanged(nameof(Alternatives));
        OnPropertyChanged(nameof(HasAlternatives));
        OnPropertyChanged(nameof(ConfidenceDisplay));
        OnPropertyChanged(nameof(QualityDisplay));
    }
}
