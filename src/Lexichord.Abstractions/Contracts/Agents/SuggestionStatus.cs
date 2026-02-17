// -----------------------------------------------------------------------
// <copyright file="SuggestionStatus.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.Agents;

/// <summary>
/// Status of a fix suggestion in the review workflow.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Tracks the lifecycle state of each suggestion as the user
/// reviews them in the Tuning Panel. Transitions are unidirectional:
/// <c>Pending</c> → one of <c>Accepted</c>, <c>Rejected</c>, <c>Modified</c>, or <c>Skipped</c>.
/// </para>
/// <para>
/// <b>Usage:</b>
/// <list type="bullet">
///   <item><description><see cref="Pending"/> — Default state, awaiting user action</description></item>
///   <item><description><see cref="Accepted"/> — Fix applied to document as-is</description></item>
///   <item><description><see cref="Rejected"/> — Fix dismissed, document unchanged</description></item>
///   <item><description><see cref="Modified"/> — User edited the fix before applying</description></item>
///   <item><description><see cref="Skipped"/> — Deferred for later review, no feedback recorded</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5c as part of the Accept/Reject UI feature.
/// </para>
/// </remarks>
/// <seealso cref="SuggestionFilter"/>
public enum SuggestionStatus
{
    /// <summary>
    /// Suggestion is awaiting user review. This is the default initial state.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Suggestion was accepted and the fix was applied to the document.
    /// </summary>
    /// <remarks>
    /// The suggested text was applied as-is via the editor service.
    /// A <c>SuggestionAcceptedEvent</c> is published for analytics and learning loop.
    /// </remarks>
    Accepted = 1,

    /// <summary>
    /// Suggestion was rejected by the user; the document remains unchanged.
    /// </summary>
    /// <remarks>
    /// A <c>SuggestionRejectedEvent</c> is published for analytics and learning loop.
    /// </remarks>
    Rejected = 2,

    /// <summary>
    /// Suggestion was modified by the user before being applied.
    /// </summary>
    /// <remarks>
    /// The user edited the suggested text, and the modified version was applied.
    /// A <c>SuggestionAcceptedEvent</c> with <c>IsModified = true</c> is published.
    /// </remarks>
    Modified = 3,

    /// <summary>
    /// Suggestion was skipped without recording feedback.
    /// </summary>
    /// <remarks>
    /// No MediatR event is published. The suggestion can be revisited later
    /// by changing the filter to show all suggestions.
    /// </remarks>
    Skipped = 4
}
