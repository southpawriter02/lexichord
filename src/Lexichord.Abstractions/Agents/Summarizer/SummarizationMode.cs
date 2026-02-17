// -----------------------------------------------------------------------
// <copyright file="SummarizationMode.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Defines the available summarization output formats (v0.7.6a).
//   Each mode produces a distinct summary style optimized for specific use
//   cases. The mode is selected either explicitly via SummarizationOptions
//   or inferred from natural language commands by SummarizerAgent.ParseCommand.
//
//   Mode-to-prompt mapping:
//     Abstract      → formal academic prose (150-300 words)
//     TLDR          → single short paragraph (50-100 words)
//     BulletPoints  → "•" list with N items (configurable via MaxItems)
//     KeyTakeaways  → numbered "**Takeaway N:**" items with explanations
//     Executive     → business-oriented prose (100-200 words)
//     Custom        → user-defined prompt (requires CustomPrompt in options)
//
//   Introduced in: v0.7.6a
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Summarizer;

/// <summary>
/// Defines the available summarization output formats.
/// Each mode produces a distinct summary style optimized for specific use cases.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="SummarizationMode"/> enum drives both prompt construction
/// and response parsing in the Summarizer Agent. Each mode maps to a dedicated section
/// in the <c>specialist-summarizer.yaml</c> prompt template with tailored instructions
/// for tone, structure, and formatting.
/// </para>
/// <para>
/// <b>Default Mode:</b> <see cref="BulletPoints"/> is the default when no specific mode
/// is detected from the user's command (e.g., "Summarize this").
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6a as part of the Summarizer Agent Summarization Modes.
/// </para>
/// </remarks>
/// <seealso cref="SummarizationOptions"/>
/// <seealso cref="SummarizationResult"/>
/// <seealso cref="ISummarizerAgent"/>
public enum SummarizationMode
{
    /// <summary>
    /// Academic-style abstract suitable for papers and formal documents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Output:</b> 150-300 words in prose format.
    /// </para>
    /// <para>
    /// <b>Structure:</b> Purpose, methodology/approach, findings, implications.
    /// </para>
    /// <para>
    /// <b>Tone:</b> Formal, third-person, passive voice acceptable.
    /// </para>
    /// <para>
    /// <b>Trigger keywords:</b> "abstract", "academic", "paper".
    /// </para>
    /// </remarks>
    Abstract,

    /// <summary>
    /// Quick "Too Long; Didn't Read" summary for fast consumption.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Output:</b> 50-100 words in a single paragraph.
    /// </para>
    /// <para>
    /// <b>Focus:</b> Core takeaway in first sentence, essential context following.
    /// </para>
    /// <para>
    /// <b>Tone:</b> Direct and informative, slightly informal acceptable.
    /// </para>
    /// <para>
    /// <b>Trigger keywords:</b> "tldr", "tl;dr", "too long didn't read".
    /// </para>
    /// </remarks>
    TLDR,

    /// <summary>
    /// Bullet-point list of key points.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Output:</b> 3-7 items (configurable via <see cref="SummarizationOptions.MaxItems"/>).
    /// </para>
    /// <para>
    /// <b>Format:</b> Each bullet is one sentence, starts with action verb or key noun.
    /// Uses "•" as the bullet marker.
    /// </para>
    /// <para>
    /// <b>Default mode:</b> This is the fallback mode when no specific mode is detected
    /// from the user's natural language command.
    /// </para>
    /// <para>
    /// <b>Trigger keywords:</b> "bullet", "point", "list", or any generic "summarize" command.
    /// </para>
    /// </remarks>
    BulletPoints,

    /// <summary>
    /// Numbered actionable insights with explanations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Output:</b> 3-7 takeaways (configurable via <see cref="SummarizationOptions.MaxItems"/>).
    /// </para>
    /// <para>
    /// <b>Format:</b> "**Takeaway N:** [insight]" followed by 1-2 sentence explanation.
    /// </para>
    /// <para>
    /// <b>Focus:</b> Practical, actionable insights that can be immediately applied.
    /// </para>
    /// <para>
    /// <b>Trigger keywords:</b> "takeaway", "insight", "learning".
    /// </para>
    /// </remarks>
    KeyTakeaways,

    /// <summary>
    /// Executive summary for stakeholders and decision-makers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Output:</b> 100-200 words in business prose.
    /// </para>
    /// <para>
    /// <b>Structure:</b> Context, key findings, risks/opportunities, recommended actions.
    /// </para>
    /// <para>
    /// <b>Tone:</b> Professional, confident, action-oriented.
    /// </para>
    /// <para>
    /// <b>Trigger keywords:</b> "executive", "stakeholder", "management", "board".
    /// </para>
    /// </remarks>
    Executive,

    /// <summary>
    /// Custom format defined by user's own instructions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Output:</b> Varies based on <see cref="SummarizationOptions.CustomPrompt"/>.
    /// </para>
    /// <para>
    /// <b>Requires:</b> <see cref="SummarizationOptions.CustomPrompt"/> must be populated.
    /// <see cref="SummarizationOptions.Validate"/> will throw <see cref="System.ArgumentException"/>
    /// if <see cref="SummarizationOptions.CustomPrompt"/> is null or whitespace when this mode is selected.
    /// </para>
    /// </remarks>
    Custom
}
