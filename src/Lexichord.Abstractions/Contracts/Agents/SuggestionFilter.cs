// -----------------------------------------------------------------------
// <copyright file="SuggestionFilter.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.Agents;

/// <summary>
/// Filter options for displaying suggestions in the Tuning Panel.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Controls which suggestions are visible in the review list.
/// The <c>TuningPanelViewModel.FilteredSuggestions</c> property uses this enum
/// to filter the full suggestions collection based on the user's selection.
/// </para>
/// <para>
/// <b>Filter Behavior:</b>
/// <list type="bullet">
///   <item><description><see cref="All"/> — Shows all suggestions regardless of status</description></item>
///   <item><description><see cref="Pending"/> — Shows only unreviewed suggestions</description></item>
///   <item><description><see cref="HighConfidence"/> — Shows suggestions where <c>IsHighConfidence</c> is true</description></item>
///   <item><description><see cref="HighPriority"/> — Shows suggestions with priority >= High</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5c as part of the Accept/Reject UI feature.
/// </para>
/// </remarks>
/// <seealso cref="SuggestionStatus"/>
public enum SuggestionFilter
{
    /// <summary>
    /// Show all suggestions regardless of their review status.
    /// </summary>
    All = 0,

    /// <summary>
    /// Show only suggestions with <see cref="SuggestionStatus.Pending"/> status.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Show only suggestions where <see cref="FixSuggestion.IsHighConfidence"/> is true.
    /// </summary>
    /// <remarks>
    /// High confidence requires confidence ≥ 0.9, quality ≥ 0.9, and validated.
    /// </remarks>
    HighConfidence = 2,

    /// <summary>
    /// Show only suggestions with <see cref="DeviationPriority.High"/> or
    /// <see cref="DeviationPriority.Critical"/> priority.
    /// </summary>
    HighPriority = 3
}
