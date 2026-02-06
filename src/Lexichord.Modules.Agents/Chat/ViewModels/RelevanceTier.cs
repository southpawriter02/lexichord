// -----------------------------------------------------------------------
// <copyright file="RelevanceTier.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Chat.ViewModels;

/// <summary>
/// Defines relevance tiers for RAG chunk classification.
/// </summary>
/// <remarks>
/// <para>
/// Relevance tiers provide a human-readable classification of RAG chunk
/// relevance scores. Each tier corresponds to a range of the 0.0-1.0
/// relevance score returned by the semantic search service.
/// </para>
/// <para>
/// The tiers are used for:
/// </para>
/// <list type="bullet">
///   <item><description>Visual indicators in the context panel UI</description></item>
///   <item><description>Color coding of relevance badges</description></item>
///   <item><description>Accessibility descriptions for screen readers</description></item>
/// </list>
/// </remarks>
/// <seealso cref="RagChunkContextItem"/>
public enum RelevanceTier
{
    /// <summary>
    /// Very high relevance (≥90%). Highly relevant to the current context.
    /// </summary>
    /// <remarks>
    /// Chunks in this tier are almost certainly useful for the current
    /// query and should be prioritized in context assembly.
    /// </remarks>
    VeryHigh,

    /// <summary>
    /// High relevance (75-89%). Strongly relevant to the current context.
    /// </summary>
    /// <remarks>
    /// Chunks in this tier are very likely to be useful and should
    /// generally be included in context unless budget is constrained.
    /// </remarks>
    High,

    /// <summary>
    /// Medium relevance (50-74%). Moderately relevant to the current context.
    /// </summary>
    /// <remarks>
    /// Chunks in this tier may provide useful supporting information
    /// but are not as directly relevant as higher tiers.
    /// </remarks>
    Medium,

    /// <summary>
    /// Low relevance (25-49%). Marginally relevant to the current context.
    /// </summary>
    /// <remarks>
    /// Chunks in this tier have limited relevance and should only be
    /// included when higher-relevance content is unavailable.
    /// </remarks>
    Low,

    /// <summary>
    /// Very low relevance (&lt;25%). Minimally relevant to the current context.
    /// </summary>
    /// <remarks>
    /// Chunks in this tier are unlikely to be useful and may add noise
    /// to the context. Consider filtering these out.
    /// </remarks>
    VeryLow
}
