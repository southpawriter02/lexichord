// -----------------------------------------------------------------------
// <copyright file="RewriteIntent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Editor;

/// <summary>
/// The type of rewrite transformation requested.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Each intent maps to a specific prompt template in the Editor Agent.
/// The templates are defined in <c>EditorAgentPromptTemplates.yaml</c> and loaded
/// by the <c>IPromptTemplateRepository</c>.
/// </para>
/// <para>
/// <b>Usage:</b>
/// <list type="bullet">
///   <item><description><see cref="Formal"/> - Transform casual text to professional tone</description></item>
///   <item><description><see cref="Simplified"/> - Reduce complexity for broader audience</description></item>
///   <item><description><see cref="Expanded"/> - Add detail and explanation</description></item>
///   <item><description><see cref="Custom"/> - User-provided transformation instruction</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.3a as part of the Editor Agent feature.
/// </para>
/// </remarks>
/// <seealso cref="RewriteCommandOption"/>
public enum RewriteIntent
{
    /// <summary>
    /// Transform casual text to formal, professional tone.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Uses the <c>editor-rewrite-formal</c> prompt template.
    /// Temperature: 0.3 for consistent, precise corrections.
    /// </para>
    /// <para>
    /// <b>Examples:</b>
    /// <list type="bullet">
    ///   <item><description>"hey whats up" → "Hello, how are you?"</description></item>
    ///   <item><description>"gonna" → "going to"</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    Formal,

    /// <summary>
    /// Simplify text for a broader audience.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Uses the <c>editor-rewrite-simplify</c> prompt template.
    /// Temperature: 0.4 for slight variation in simplification approaches.
    /// </para>
    /// <para>
    /// <b>Guidelines:</b>
    /// <list type="bullet">
    ///   <item><description>Use shorter sentences (aim for 15-20 words max)</description></item>
    ///   <item><description>Replace jargon with plain language</description></item>
    ///   <item><description>Use active voice instead of passive</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    Simplified,

    /// <summary>
    /// Expand text with more detail and explanation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Uses the <c>editor-rewrite-expand</c> prompt template.
    /// Temperature: 0.5 for creative elaboration.
    /// </para>
    /// <para>
    /// <b>Guidelines:</b>
    /// <list type="bullet">
    ///   <item><description>Add relevant details and examples</description></item>
    ///   <item><description>Expand abbreviations and acronyms</description></item>
    ///   <item><description>Include transitional phrases for flow</description></item>
    ///   <item><description>Keep expansion proportional (2-3x original length typically)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    Expanded,

    /// <summary>
    /// Custom transformation with user-provided instruction.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Uses the <c>editor-rewrite-custom</c> prompt template.
    /// Temperature: 0.5 for balanced creativity and precision.
    /// </para>
    /// <para>
    /// <b>Behavior:</b> Opens a dialog for the user to enter a custom
    /// transformation instruction (e.g., "Make this more persuasive").
    /// </para>
    /// </remarks>
    Custom
}
