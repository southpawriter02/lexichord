// -----------------------------------------------------------------------
// <copyright file="SuggestionAcceptedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using MediatR;

namespace Lexichord.Abstractions.Contracts.Agents.Events;

/// <summary>
/// Published when the user accepts a fix suggestion in the Tuning Panel.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This event is published by the <c>TuningPanelViewModel</c>
/// when a suggestion is accepted (applied as-is) or modified (user-edited then applied).
/// Subscribers can use this event for:
/// </para>
/// <list type="bullet">
///   <item><description>Analytics tracking of suggestion acceptance rates</description></item>
///   <item><description>Learning loop feedback recording (v0.7.5d)</description></item>
///   <item><description>Triggering follow-up actions (e.g., re-scanning)</description></item>
///   <item><description>Logging for audit trails</description></item>
/// </list>
/// <para>
/// <b>Modified Suggestions:</b>
/// When <see cref="IsModified"/> is <c>true</c>, the user edited the suggestion
/// before applying. The <see cref="ModifiedText"/> property contains the
/// user's version. This data is valuable for the learning loop to understand
/// user preferences.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5c as part of the Accept/Reject UI feature.
/// </para>
/// </remarks>
/// <param name="Deviation">The style deviation that was addressed.</param>
/// <param name="Suggestion">The fix suggestion that was accepted.</param>
/// <param name="ModifiedText">
/// The user-modified text, if the suggestion was edited before applying.
/// <c>null</c> when the suggestion was accepted as-is.
/// </param>
/// <param name="IsModified">
/// Whether the user modified the suggested text before accepting.
/// </param>
/// <param name="Timestamp">When the acceptance occurred.</param>
/// <example>
/// <code>
/// // Publishing after accepting a suggestion as-is
/// await _mediator.Publish(SuggestionAcceptedEvent.Create(deviation, suggestion));
///
/// // Publishing after accepting a modified suggestion
/// await _mediator.Publish(SuggestionAcceptedEvent.CreateModified(deviation, suggestion, "edited text"));
/// </code>
/// </example>
/// <seealso cref="SuggestionRejectedEvent"/>
/// <seealso cref="StyleDeviation"/>
/// <seealso cref="FixSuggestion"/>
public record SuggestionAcceptedEvent(
    StyleDeviation Deviation,
    FixSuggestion Suggestion,
    string? ModifiedText,
    bool IsModified,
    DateTime Timestamp) : INotification
{
    /// <summary>
    /// Gets the text that was actually applied to the document.
    /// </summary>
    /// <value>
    /// <see cref="ModifiedText"/> if the suggestion was modified;
    /// otherwise, <see cref="FixSuggestion.SuggestedText"/>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience property for consumers who need the final applied text
    /// without checking <see cref="IsModified"/>.
    /// </remarks>
    public string AppliedText => ModifiedText ?? Suggestion.SuggestedText;

    /// <summary>
    /// Creates a new acceptance event for a suggestion accepted as-is.
    /// </summary>
    /// <param name="deviation">The style deviation that was addressed.</param>
    /// <param name="suggestion">The fix suggestion that was accepted.</param>
    /// <returns>A new <see cref="SuggestionAcceptedEvent"/> with the current timestamp.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for the common case where the user accepts
    /// the AI suggestion without modification.
    /// </remarks>
    public static SuggestionAcceptedEvent Create(
        StyleDeviation deviation,
        FixSuggestion suggestion) =>
        new(deviation, suggestion, ModifiedText: null, IsModified: false, DateTime.UtcNow);

    /// <summary>
    /// Creates a new acceptance event for a modified suggestion.
    /// </summary>
    /// <param name="deviation">The style deviation that was addressed.</param>
    /// <param name="suggestion">The original fix suggestion.</param>
    /// <param name="modifiedText">The user-edited text that was applied.</param>
    /// <returns>A new <see cref="SuggestionAcceptedEvent"/> with <see cref="IsModified"/> set to <c>true</c>.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for when the user edits the suggestion before applying.
    /// The <paramref name="modifiedText"/> is stored for learning loop analysis.
    /// </remarks>
    public static SuggestionAcceptedEvent CreateModified(
        StyleDeviation deviation,
        FixSuggestion suggestion,
        string modifiedText) =>
        new(deviation, suggestion, modifiedText, IsModified: true, DateTime.UtcNow);
}
