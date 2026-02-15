// -----------------------------------------------------------------------
// <copyright file="SimplificationStrategy.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Simplifier;

/// <summary>
/// Defines the simplification intensity level for text transformation.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The simplification strategy controls how aggressively the
/// Simplifier Agent transforms text to achieve the target readability level.
/// Each strategy maps to a specific LLM temperature setting and transformation
/// approach:
/// </para>
/// <list type="bullet">
///   <item><description><b>Conservative (0.3):</b> Minimal changes, preserves meaning closely</description></item>
///   <item><description><b>Balanced (0.4):</b> Moderate changes, balances readability and fidelity</description></item>
///   <item><description><b>Aggressive (0.5):</b> Maximum simplification, may restructure significantly</description></item>
/// </list>
/// <para>
/// <b>Usage Guidelines:</b>
/// <list type="bullet">
///   <item><description>Use <see cref="Conservative"/> for legal, medical, or technical documents where precision is critical</description></item>
///   <item><description>Use <see cref="Balanced"/> for general business or educational content (default)</description></item>
///   <item><description>Use <see cref="Aggressive"/> for maximum accessibility or ESL audiences</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4b as part of the Simplifier Agent Simplification Pipeline.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Creating a request with a specific strategy
/// var request = new SimplificationRequest
/// {
///     OriginalText = complexText,
///     Target = readabilityTarget,
///     Strategy = SimplificationStrategy.Aggressive // Maximum simplification
/// };
///
/// // Strategy affects LLM temperature
/// var temperature = strategy switch
/// {
///     SimplificationStrategy.Conservative => 0.3,
///     SimplificationStrategy.Balanced => 0.4,
///     SimplificationStrategy.Aggressive => 0.5,
///     _ => 0.4
/// };
/// </code>
/// </example>
/// <seealso cref="SimplificationRequest"/>
/// <seealso cref="SimplificationResult"/>
/// <seealso cref="ISimplificationPipeline"/>
public enum SimplificationStrategy
{
    /// <summary>
    /// Minimal changes with high fidelity to original meaning.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Conservative simplification makes the fewest changes necessary
    /// to improve readability. Prioritizes preserving:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Original sentence structure where possible</description></item>
    ///   <item><description>Technical terminology with inline explanations</description></item>
    ///   <item><description>Nuanced meaning and qualifications</description></item>
    /// </list>
    /// <para>
    /// <b>Use Cases:</b> Legal documents, medical instructions, technical specifications,
    /// academic papers, or any content where precise meaning is critical.
    /// </para>
    /// <para>
    /// <b>LLM Temperature:</b> 0.3 (low randomness, high consistency)
    /// </para>
    /// </remarks>
    Conservative = 0,

    /// <summary>
    /// Moderate changes balancing readability and fidelity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Balanced simplification is the default strategy, making reasonable
    /// changes to improve readability while maintaining the core message. May:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Split complex sentences into shorter ones</description></item>
    ///   <item><description>Replace jargon with common alternatives</description></item>
    ///   <item><description>Simplify passive voice to active voice</description></item>
    ///   <item><description>Remove unnecessary qualifications</description></item>
    /// </list>
    /// <para>
    /// <b>Use Cases:</b> Business communications, educational content, blog posts,
    /// marketing materials, or general-audience documentation.
    /// </para>
    /// <para>
    /// <b>LLM Temperature:</b> 0.4 (balanced randomness)
    /// </para>
    /// </remarks>
    Balanced = 1,

    /// <summary>
    /// Maximum simplification with significant restructuring allowed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Aggressive simplification prioritizes readability over fidelity,
    /// making substantial changes to achieve the target grade level. May:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Completely restructure paragraphs</description></item>
    ///   <item><description>Remove all technical jargon</description></item>
    ///   <item><description>Use basic vocabulary exclusively</description></item>
    ///   <item><description>Break down complex ideas into multiple simple statements</description></item>
    ///   <item><description>Add transitional phrases for clarity</description></item>
    /// </list>
    /// <para>
    /// <b>Use Cases:</b> Content for children, ESL/international audiences,
    /// accessibility compliance, or public health communications.
    /// </para>
    /// <para>
    /// <b>LLM Temperature:</b> 0.5 (higher creativity for restructuring)
    /// </para>
    /// </remarks>
    Aggressive = 2
}
