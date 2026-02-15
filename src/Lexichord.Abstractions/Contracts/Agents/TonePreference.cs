// -----------------------------------------------------------------------
// <copyright file="TonePreference.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.Agents;

/// <summary>
/// Tone preferences for fix suggestions, controlling the style of generated rewrites.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The tone preference influences how the AI generates fix suggestions:
/// <list type="bullet">
///   <item><description>Neutral preserves the original text's tone</description></item>
///   <item><description>Formal uses professional, corporate language</description></item>
///   <item><description>Casual uses conversational, approachable language</description></item>
///   <item><description>Technical uses precise, domain-specific language</description></item>
///   <item><description>Simplified uses simple, accessible language</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5b as part of the Automatic Fix Suggestions feature.
/// </para>
/// </remarks>
public enum TonePreference
{
    /// <summary>
    /// Match the original text's tone without modification.
    /// </summary>
    /// <remarks>
    /// Default option. The AI preserves the existing writing style and tone
    /// while making only the minimal changes needed to fix the violation.
    /// </remarks>
    Neutral = 0,

    /// <summary>
    /// Use formal, professional language in the fix.
    /// </summary>
    /// <remarks>
    /// Appropriate for business documents, official communications, and
    /// professional publications. Uses complete sentences, avoids contractions,
    /// and employs formal vocabulary.
    /// </remarks>
    Formal = 1,

    /// <summary>
    /// Use conversational, approachable language in the fix.
    /// </summary>
    /// <remarks>
    /// Appropriate for blog posts, social media, and informal content.
    /// Uses contractions, shorter sentences, and everyday vocabulary.
    /// </remarks>
    Casual = 2,

    /// <summary>
    /// Use precise, technical language in the fix.
    /// </summary>
    /// <remarks>
    /// Appropriate for technical documentation, scientific papers, and
    /// specialized content. Preserves technical terminology and uses
    /// precise, unambiguous language.
    /// </remarks>
    Technical = 3,

    /// <summary>
    /// Use simple, accessible language in the fix.
    /// </summary>
    /// <remarks>
    /// Appropriate for content targeting broad audiences or non-native speakers.
    /// Uses basic vocabulary, short sentences, and avoids jargon.
    /// </remarks>
    Simplified = 4
}
