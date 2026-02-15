// -----------------------------------------------------------------------
// <copyright file="SimplificationChangeType.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Simplifier;

/// <summary>
/// Categorizes the type of change made during text simplification.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Each simplification change is categorized to help users understand
/// what transformations were applied to their text. This categorization:
/// </para>
/// <list type="bullet">
///   <item><description>Enables filtering changes by type in the UI</description></item>
///   <item><description>Provides learning insights about writing patterns</description></item>
///   <item><description>Supports acceptance/rejection of specific change types</description></item>
///   <item><description>Enables metrics collection by change category</description></item>
/// </list>
/// <para>
/// <b>Response Parsing:</b>
/// The <see cref="Lexichord.Modules.Agents.Simplifier.SimplificationResponseParser"/>
/// extracts change types from the LLM response using case-insensitive matching.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4b as part of the Simplifier Agent Simplification Pipeline.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Parsing a change type from LLM response
/// var changeType = changeTypeString.ToLowerInvariant() switch
/// {
///     "sentence-split" or "sentencesplit" => SimplificationChangeType.SentenceSplit,
///     "jargon-replacement" or "jargonreplacement" => SimplificationChangeType.JargonReplacement,
///     "passive-to-active" or "passivetoactive" => SimplificationChangeType.PassiveToActive,
///     _ => SimplificationChangeType.Combined
/// };
///
/// // Filtering changes by type
/// var jargonChanges = result.Changes
///     .Where(c => c.ChangeType == SimplificationChangeType.JargonReplacement)
///     .ToList();
/// </code>
/// </example>
/// <seealso cref="SimplificationChange"/>
/// <seealso cref="SimplificationResult"/>
public enum SimplificationChangeType
{
    /// <summary>
    /// A long or complex sentence was split into multiple shorter sentences.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Sentence splitting reduces cognitive load by presenting
    /// one idea per sentence. This change type is applied when:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>A sentence exceeds the target <see cref="ReadabilityTarget.MaxSentenceLength"/></description></item>
    ///   <item><description>A sentence contains multiple independent clauses</description></item>
    ///   <item><description>Complex compound-complex structures can be simplified</description></item>
    /// </list>
    /// <para>
    /// <b>Example:</b> "The committee, which had been meeting weekly since January,
    /// finally reached a consensus on the proposal that had been under review for months."
    /// → "The committee met weekly since January. They finally reached a consensus.
    /// The proposal had been under review for months."
    /// </para>
    /// </remarks>
    SentenceSplit = 0,

    /// <summary>
    /// Technical jargon or specialized vocabulary was replaced with plain language.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Jargon replacement improves accessibility for general audiences.
    /// The behavior depends on <see cref="ReadabilityTarget.AvoidJargon"/>:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><c>true</c>: Replace jargon with common alternatives</description></item>
    ///   <item><description><c>false</c>: Keep jargon but add explanations</description></item>
    /// </list>
    /// <para>
    /// <b>Example:</b> "The patient exhibited dyspnea and tachycardia."
    /// → "The patient had difficulty breathing and a fast heartbeat."
    /// </para>
    /// </remarks>
    JargonReplacement = 1,

    /// <summary>
    /// Passive voice was converted to active voice.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Active voice is generally clearer and more direct.
    /// This change type improves readability by:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Making the subject perform the action</description></item>
    ///   <item><description>Reducing word count</description></item>
    ///   <item><description>Improving sentence clarity</description></item>
    /// </list>
    /// <para>
    /// <b>Example:</b> "The report was written by the analyst."
    /// → "The analyst wrote the report."
    /// </para>
    /// </remarks>
    PassiveToActive = 2,

    /// <summary>
    /// Complex words were replaced with simpler alternatives.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Word simplification reduces the number of complex words
    /// (3+ syllables) in the text, directly lowering the Flesch-Kincaid grade level.
    /// Common replacements include:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>"utilize" → "use"</description></item>
    ///   <item><description>"approximately" → "about"</description></item>
    ///   <item><description>"subsequently" → "then"</description></item>
    ///   <item><description>"demonstrate" → "show"</description></item>
    /// </list>
    /// <para>
    /// <b>Example:</b> "We need to facilitate communication between departments."
    /// → "We need to help departments talk to each other."
    /// </para>
    /// </remarks>
    WordSimplification = 3,

    /// <summary>
    /// Complex clauses were simplified or removed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Clause reduction simplifies sentence structure by:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Converting relative clauses to simple adjectives</description></item>
    ///   <item><description>Removing non-essential parenthetical information</description></item>
    ///   <item><description>Simplifying conditional structures</description></item>
    /// </list>
    /// <para>
    /// <b>Example:</b> "The employee, who had been working here for ten years, resigned."
    /// → "The ten-year employee resigned."
    /// </para>
    /// </remarks>
    ClauseReduction = 4,

    /// <summary>
    /// Transitional words or phrases were added for clarity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Adding transitions improves flow and comprehension
    /// by explicitly connecting ideas. Common additions include:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>"First," "Second," "Finally," (sequence)</description></item>
    ///   <item><description>"However," "On the other hand," (contrast)</description></item>
    ///   <item><description>"Therefore," "As a result," (cause-effect)</description></item>
    ///   <item><description>"For example," "In other words," (clarification)</description></item>
    /// </list>
    /// <para>
    /// <b>Example:</b> "The project failed. We learned valuable lessons."
    /// → "The project failed. However, we learned valuable lessons."
    /// </para>
    /// </remarks>
    TransitionAdded = 5,

    /// <summary>
    /// Redundant words or phrases were removed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Redundancy removal tightens prose by eliminating:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Pleonasms ("advance planning" → "planning")</description></item>
    ///   <item><description>Filler phrases ("in order to" → "to")</description></item>
    ///   <item><description>Unnecessary qualifiers ("very unique" → "unique")</description></item>
    ///   <item><description>Repeated information</description></item>
    /// </list>
    /// <para>
    /// <b>Example:</b> "In order to successfully complete the final end result..."
    /// → "To complete the result..."
    /// </para>
    /// </remarks>
    RedundancyRemoved = 6,

    /// <summary>
    /// Multiple change types were combined in a single transformation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Combined changes occur when multiple transformations
    /// are applied to the same text segment simultaneously. This is common
    /// when restructuring complex sentences that require:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Splitting AND word simplification</description></item>
    ///   <item><description>Jargon replacement AND passive-to-active conversion</description></item>
    ///   <item><description>Clause reduction AND transition addition</description></item>
    /// </list>
    /// <para>
    /// <b>Note:</b> This is also the fallback category when the change type
    /// cannot be determined from the LLM response.
    /// </para>
    /// </remarks>
    Combined = 7
}
