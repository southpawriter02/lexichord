// <copyright file="UpdateTrigger.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Identifies what triggered a chart update in the Resonance Dashboard.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.5c - Used by <see cref="IResonanceUpdateService"/> to communicate
/// the source of update events to subscribers.</para>
/// <list type="bullet">
///   <item>Debounced triggers: <see cref="ReadabilityAnalyzed"/>, <see cref="VoiceAnalysisCompleted"/></item>
///   <item>Immediate triggers: <see cref="ProfileChanged"/>, <see cref="ForceUpdate"/></item>
/// </list>
/// </remarks>
public enum UpdateTrigger
{
    /// <summary>
    /// Chart update triggered by readability analysis completion.
    /// </summary>
    /// <remarks>LOGIC: Debounced (300ms) to prevent rapid updates during typing.</remarks>
    ReadabilityAnalyzed,

    /// <summary>
    /// Chart update triggered by voice analysis completion.
    /// </summary>
    /// <remarks>LOGIC: Debounced (300ms) to prevent rapid updates during typing.</remarks>
    VoiceAnalysisCompleted,

    /// <summary>
    /// Chart update triggered by active profile change.
    /// </summary>
    /// <remarks>LOGIC: Immediate dispatch - user expects instant feedback on profile switch.</remarks>
    ProfileChanged,

    /// <summary>
    /// Chart update triggered by explicit refresh request.
    /// </summary>
    /// <remarks>LOGIC: Immediate dispatch via <see cref="IResonanceUpdateService.ForceUpdateAsync"/>.</remarks>
    ForceUpdate,

    /// <summary>
    /// Chart update triggered during dashboard initialization.
    /// </summary>
    /// <remarks>LOGIC: Immediate dispatch when dashboard first loads.</remarks>
    Initialization
}
