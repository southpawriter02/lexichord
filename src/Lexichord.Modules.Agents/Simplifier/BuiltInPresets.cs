// -----------------------------------------------------------------------
// <copyright file="BuiltInPresets.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.Simplifier;

namespace Lexichord.Modules.Agents.Simplifier;

/// <summary>
/// Provides the built-in audience presets for the Simplifier Agent.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Built-in presets provide pre-configured readability targets for
/// common audience types. These presets are always available to all users regardless
/// of license tier. Users with WriterPro or Teams tier can also create custom presets.
/// </para>
/// <para>
/// <b>Preset Definitions:</b>
/// <list type="bullet">
///   <item><description>
///     <b>General Public (Grade 8, 20 words):</b> Targets broad accessibility.
///     Most news articles and public communications aim for this level.
///     Avoid jargon to ensure clarity for all readers.
///   </description></item>
///   <item><description>
///     <b>Technical Audience (Grade 12, 25 words):</b> Targets educated professionals.
///     Technical documentation, academic papers, and industry reports use this level.
///     Don't avoid jargon; instead, explain technical terms in context.
///   </description></item>
///   <item><description>
///     <b>Executive Summary (Grade 10, 18 words):</b> Targets busy decision-makers.
///     Concise, action-oriented language with clear conclusions.
///     Avoid jargon to ensure quick comprehension.
///   </description></item>
///   <item><description>
///     <b>International/ESL (Grade 6, 15 words):</b> Targets non-native English speakers.
///     Simple vocabulary, short sentences, active voice preferred.
///     Avoid jargon and idioms that don't translate well.
///   </description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4a as part of the Simplifier Agent Readability Target Service.
/// </para>
/// </remarks>
/// <seealso cref="AudiencePreset"/>
/// <seealso cref="IReadabilityTargetService"/>
public static class BuiltInPresets
{
    /// <summary>
    /// Preset ID for General Public audience.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Used for referencing the preset by ID in API calls and settings.
    /// </remarks>
    public const string GeneralPublicId = "general-public";

    /// <summary>
    /// Preset ID for Technical audience.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Used for referencing the preset by ID in API calls and settings.
    /// </remarks>
    public const string TechnicalId = "technical";

    /// <summary>
    /// Preset ID for Executive audience.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Used for referencing the preset by ID in API calls and settings.
    /// </remarks>
    public const string ExecutiveId = "executive";

    /// <summary>
    /// Preset ID for International/ESL audience.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Used for referencing the preset by ID in API calls and settings.
    /// </remarks>
    public const string InternationalId = "international";

    /// <summary>
    /// Gets the General Public audience preset.
    /// </summary>
    /// <value>
    /// An <see cref="AudiencePreset"/> targeting Grade 8 reading level with
    /// 20-word maximum sentences and jargon avoidance.
    /// </value>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Grade 8 is the standard target for public communications.
    /// According to readability research, this level is accessible to approximately
    /// 85% of the U.S. adult population.
    /// </para>
    /// <para>
    /// <b>Use Cases:</b>
    /// <list type="bullet">
    ///   <item><description>News articles and press releases</description></item>
    ///   <item><description>Customer communications and marketing materials</description></item>
    ///   <item><description>Public-facing documentation</description></item>
    ///   <item><description>User guides and help content</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static AudiencePreset GeneralPublic { get; } = new(
        Id: GeneralPublicId,
        Name: "General Public",
        TargetGradeLevel: 8.0,
        MaxSentenceLength: 20,
        AvoidJargon: true,
        Description: "Accessible to a broad audience. Targets Grade 8 reading level, " +
                     "suitable for news articles, customer communications, and public documentation.",
        IsBuiltIn: true);

    /// <summary>
    /// Gets the Technical Audience preset.
    /// </summary>
    /// <value>
    /// An <see cref="AudiencePreset"/> targeting Grade 12 reading level with
    /// 25-word maximum sentences and jargon explanation (not avoidance).
    /// </value>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Grade 12 is appropriate for educated professionals who expect
    /// technical precision. Jargon is not avoided but explained in context to
    /// maintain accuracy while ensuring comprehension.
    /// </para>
    /// <para>
    /// <b>Use Cases:</b>
    /// <list type="bullet">
    ///   <item><description>Technical documentation and API references</description></item>
    ///   <item><description>Academic papers and research reports</description></item>
    ///   <item><description>Industry white papers</description></item>
    ///   <item><description>Professional training materials</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static AudiencePreset Technical { get; } = new(
        Id: TechnicalId,
        Name: "Technical Audience",
        TargetGradeLevel: 12.0,
        MaxSentenceLength: 25,
        AvoidJargon: false,
        Description: "For educated professionals expecting technical precision. " +
                     "Explain technical terms in context rather than avoiding them.",
        IsBuiltIn: true);

    /// <summary>
    /// Gets the Executive Summary preset.
    /// </summary>
    /// <value>
    /// An <see cref="AudiencePreset"/> targeting Grade 10 reading level with
    /// 18-word maximum sentences and jargon avoidance.
    /// </value>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Executives need quick, clear communication with actionable
    /// insights. Shorter sentences ensure key points aren't buried. Jargon is
    /// avoided to ensure rapid comprehension by busy decision-makers.
    /// </para>
    /// <para>
    /// <b>Use Cases:</b>
    /// <list type="bullet">
    ///   <item><description>Executive summaries and board reports</description></item>
    ///   <item><description>Business proposals and recommendations</description></item>
    ///   <item><description>Status updates for leadership</description></item>
    ///   <item><description>Strategic planning documents</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static AudiencePreset Executive { get; } = new(
        Id: ExecutiveId,
        Name: "Executive Summary",
        TargetGradeLevel: 10.0,
        MaxSentenceLength: 18,
        AvoidJargon: true,
        Description: "Concise communication for busy decision-makers. " +
                     "Clear, action-oriented language with shorter sentences.",
        IsBuiltIn: true);

    /// <summary>
    /// Gets the International/ESL preset.
    /// </summary>
    /// <value>
    /// An <see cref="AudiencePreset"/> targeting Grade 6 reading level with
    /// 15-word maximum sentences and jargon avoidance.
    /// </value>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Non-native English speakers benefit from simpler vocabulary,
    /// shorter sentences, and avoidance of idioms and cultural references that
    /// don't translate well. Grade 6 provides clarity without being condescending.
    /// </para>
    /// <para>
    /// <b>Use Cases:</b>
    /// <list type="bullet">
    ///   <item><description>International business communications</description></item>
    ///   <item><description>Multilingual documentation</description></item>
    ///   <item><description>Content for global audiences</description></item>
    ///   <item><description>ESL learning materials</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static AudiencePreset International { get; } = new(
        Id: InternationalId,
        Name: "International / ESL",
        TargetGradeLevel: 6.0,
        MaxSentenceLength: 15,
        AvoidJargon: true,
        Description: "For non-native English speakers and global audiences. " +
                     "Simple vocabulary, short sentences, avoids idioms and jargon.",
        IsBuiltIn: true);

    /// <summary>
    /// Gets all built-in presets as a read-only list.
    /// </summary>
    /// <value>
    /// A read-only list containing all four built-in presets in display order:
    /// General Public, Technical, Executive, International.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Order is intentional: General Public first as the most common default,
    /// then Technical for professional users, Executive for business contexts, and
    /// International for global reach.
    /// </remarks>
    public static IReadOnlyList<AudiencePreset> All { get; } = new[]
    {
        GeneralPublic,
        Technical,
        Executive,
        International
    };

    /// <summary>
    /// Gets a built-in preset by its ID.
    /// </summary>
    /// <param name="presetId">The preset ID to look up.</param>
    /// <returns>
    /// The <see cref="AudiencePreset"/> with the specified ID, or <c>null</c> if not found.
    /// </returns>
    /// <remarks>
    /// <b>LOGIC:</b> Case-insensitive lookup for user convenience.
    /// </remarks>
    public static AudiencePreset? GetById(string presetId)
    {
        if (string.IsNullOrWhiteSpace(presetId))
            return null;

        // LOGIC: Case-insensitive comparison for user convenience
        return All.FirstOrDefault(p =>
            string.Equals(p.Id, presetId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines whether the specified ID is a built-in preset.
    /// </summary>
    /// <param name="presetId">The preset ID to check.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="presetId"/> matches a built-in preset ID; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <b>LOGIC:</b> Used to prevent modification or deletion of built-in presets.
    /// </remarks>
    public static bool IsBuiltInId(string presetId)
    {
        return GetById(presetId) != null;
    }
}
