// -----------------------------------------------------------------------
// <copyright file="SummarizationOptions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Configuration options for summarization requests (v0.7.6a).
//   Combines mode selection with output constraints and style preferences.
//   Options are either constructed manually or inferred from natural language
//   commands by SummarizerAgent.ParseCommand.
//
//   Validation rules:
//     - MaxItems must be between 1 and 10
//     - TargetWordCount (if set) must be between 10 and 1000
//     - Custom mode requires non-empty CustomPrompt
//     - MaxResponseTokens has no explicit validation (uses reasonable default)
//
//   Default configuration:
//     Mode = BulletPoints, MaxItems = 5, MaxResponseTokens = 2048,
//     PreserveTechnicalTerms = true, IncludeSectionSummaries = false
//
//   Introduced in: v0.7.6a
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Summarizer;

/// <summary>
/// Configuration options for summarization requests.
/// Combines mode selection with output constraints and style preferences.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Options can be constructed directly or parsed from natural language
/// commands via <see cref="ISummarizerAgent.ParseCommand"/>. The <see cref="Validate"/>
/// method enforces constraints before processing begins.
/// </para>
/// <para>
/// <b>Thread safety:</b> This is an immutable record and is inherently thread-safe.
/// Use <c>with</c> expressions to create modified copies.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6a as part of the Summarizer Agent Summarization Modes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create options for 3 bullet points targeting developers
/// var options = new SummarizationOptions
/// {
///     Mode = SummarizationMode.BulletPoints,
///     MaxItems = 3,
///     TargetAudience = "developers",
///     PreserveTechnicalTerms = true
/// };
///
/// // Validate before use
/// options.Validate();
///
/// // Parse from natural language
/// var parsed = summarizerAgent.ParseCommand("Summarize in 5 bullets for managers");
/// </code>
/// </example>
/// <seealso cref="SummarizationMode"/>
/// <seealso cref="SummarizationResult"/>
/// <seealso cref="ISummarizerAgent"/>
public record SummarizationOptions
{
    /// <summary>
    /// Gets the output format mode.
    /// </summary>
    /// <value>
    /// The <see cref="SummarizationMode"/> to use for generating the summary.
    /// Default: <see cref="SummarizationMode.BulletPoints"/> (most common use case).
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> The mode drives prompt construction in the specialist-summarizer template
    /// and determines how the response is parsed (prose vs. list extraction).
    /// </remarks>
    public SummarizationMode Mode { get; init; } = SummarizationMode.BulletPoints;

    /// <summary>
    /// Gets the maximum number of items for list-based modes.
    /// </summary>
    /// <value>
    /// Maximum items for <see cref="SummarizationMode.BulletPoints"/> and
    /// <see cref="SummarizationMode.KeyTakeaways"/> modes. Ignored for prose modes
    /// (<see cref="SummarizationMode.Abstract"/>, <see cref="SummarizationMode.TLDR"/>,
    /// <see cref="SummarizationMode.Executive"/>). Range: 1-10. Default: 5.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Embedded into the prompt as <c>{{max_items}}</c>. The LLM is instructed
    /// to generate exactly this many items. Clamped to 1-10 by <see cref="Validate"/>.
    /// </remarks>
    public int MaxItems { get; init; } = 5;

    /// <summary>
    /// Gets the target word count for prose modes.
    /// </summary>
    /// <value>
    /// Target word count for <see cref="SummarizationMode.Abstract"/>,
    /// <see cref="SummarizationMode.TLDR"/>, and <see cref="SummarizationMode.Executive"/>.
    /// <c>null</c> uses mode-specific defaults (Abstract: 200, TLDR: 75, Executive: 150).
    /// Ignored for list modes. Range: 10-1000.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Embedded into the prompt as <c>{{target_word_count}}</c>.
    /// When null, the agent uses default values based on the mode.
    /// </remarks>
    public int? TargetWordCount { get; init; }

    /// <summary>
    /// Gets the custom prompt override for Custom mode.
    /// </summary>
    /// <value>
    /// Custom prompt instructions when <see cref="Mode"/> is <see cref="SummarizationMode.Custom"/>.
    /// Required for Custom mode; ignored otherwise.
    /// Example: "Summarize focusing on security implications"
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Injected directly into the prompt as <c>{{custom_prompt}}</c>.
    /// The agent appends general summarization best practices alongside user instructions.
    /// </remarks>
    public string? CustomPrompt { get; init; }

    /// <summary>
    /// Gets whether to include section-level summaries for long documents.
    /// </summary>
    /// <value>
    /// When <c>true</c>, each major heading gets its own mini-summary before the final summary.
    /// Default: <c>false</c> (single unified summary).
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> When enabled, chunked documents produce per-section summaries that are
    /// included alongside the final combined summary. This increases token usage but provides
    /// more detailed output for long documents.
    /// </remarks>
    public bool IncludeSectionSummaries { get; init; }

    /// <summary>
    /// Gets the target audience for tone and complexity adjustment.
    /// </summary>
    /// <value>
    /// Target audience description. Examples: "software developers", "executives", "general public".
    /// <c>null</c> uses neutral tone suitable for general audience.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Injected into the prompt as <c>{{target_audience}}</c>. The Mustache
    /// conditional <c>{{#target_audience}}</c> activates audience-specific instructions
    /// in the system prompt.
    /// </remarks>
    public string? TargetAudience { get; init; }

    /// <summary>
    /// Gets whether to preserve technical terminology without simplification.
    /// </summary>
    /// <value>
    /// When <c>true</c>, domain-specific terms are retained as-is.
    /// When <c>false</c>, technical terms may be explained or replaced.
    /// Default: <c>true</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Controls the <c>{{#preserve_technical_terms}}</c> / <c>{{^preserve_technical_terms}}</c>
    /// conditional blocks in the prompt template.
    /// </remarks>
    public bool PreserveTechnicalTerms { get; init; } = true;

    /// <summary>
    /// Gets the maximum tokens to allocate for summarization response.
    /// </summary>
    /// <value>
    /// Maximum response tokens. Default: 2048 (sufficient for most summaries).
    /// Increase for <see cref="IncludeSectionSummaries"/> or very long KeyTakeaways.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Passed to <see cref="Abstractions.Contracts.LLM.ChatOptions"/>
    /// when constructing the LLM request. Limits response size to control cost and latency.
    /// </remarks>
    public int MaxResponseTokens { get; init; } = 2048;

    /// <summary>
    /// Validates the options configuration.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when:
    /// <list type="bullet">
    ///   <item><description><see cref="MaxItems"/> is outside the range 1-10</description></item>
    ///   <item><description><see cref="TargetWordCount"/> is outside the range 10-1000</description></item>
    ///   <item><description><see cref="Mode"/> is <see cref="SummarizationMode.Custom"/> and
    ///   <see cref="CustomPrompt"/> is null or whitespace</description></item>
    /// </list>
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Called at the start of <see cref="ISummarizerAgent.SummarizeContentAsync"/>
    /// to fail fast before any LLM invocation. Validation is intentionally strict to prevent
    /// wasted tokens on malformed requests.
    /// </remarks>
    public void Validate()
    {
        // LOGIC: MaxItems range check — prevents empty lists (< 1) or excessively
        // long lists (> 10) that degrade summary quality.
        if (MaxItems < 1 || MaxItems > 10)
        {
            throw new ArgumentException(
                "MaxItems must be between 1 and 10.",
                nameof(MaxItems));
        }

        // LOGIC: TargetWordCount range check — prevents unreasonably short (< 10)
        // or excessively long (> 1000) summaries that defeat the purpose of summarization.
        if (TargetWordCount.HasValue && (TargetWordCount.Value < 10 || TargetWordCount.Value > 1000))
        {
            throw new ArgumentException(
                "TargetWordCount must be between 10 and 1000.",
                nameof(TargetWordCount));
        }

        // LOGIC: Custom mode requires a prompt — without it, the LLM has no instructions
        // for the custom summarization format.
        if (Mode == SummarizationMode.Custom && string.IsNullOrWhiteSpace(CustomPrompt))
        {
            throw new ArgumentException(
                "CustomPrompt is required when Mode is Custom.",
                nameof(CustomPrompt));
        }
    }
}
