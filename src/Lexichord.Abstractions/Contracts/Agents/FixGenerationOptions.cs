// -----------------------------------------------------------------------
// <copyright file="FixGenerationOptions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.Agents;

/// <summary>
/// Options for controlling fix generation behavior in <see cref="IFixSuggestionGenerator"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> These options allow callers to customize fix generation:
/// <list type="bullet">
///   <item><description>Number of alternative suggestions to generate</description></item>
///   <item><description>Confidence thresholds for accepting suggestions</description></item>
///   <item><description>Validation and explanation behavior</description></item>
///   <item><description>Tone and voice preferences</description></item>
///   <item><description>Parallelism settings for batch processing</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> This record is immutable and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5b as part of the Automatic Fix Suggestions feature.
/// </para>
/// </remarks>
public record FixGenerationOptions
{
    /// <summary>
    /// Maximum number of alternative suggestions to generate.
    /// </summary>
    /// <value>Default: 2.</value>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> The LLM is instructed to provide up to this many alternative
    /// fix suggestions in addition to the primary suggestion. Alternatives provide
    /// users with choices when the "best" fix is ambiguous.
    /// </para>
    /// <para>
    /// <b>Trade-offs:</b> More alternatives increase token usage and generation time.
    /// </para>
    /// </remarks>
    public int MaxAlternatives { get; init; } = 2;

    /// <summary>
    /// Whether to include detailed explanations for each suggestion.
    /// </summary>
    /// <value>Default: true.</value>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> When <c>true</c>, the LLM provides an explanation of what
    /// was changed and why. Explanations help users understand the fix and
    /// make informed decisions about whether to accept it.
    /// </para>
    /// <para>
    /// <b>Trade-offs:</b> Disabling explanations reduces token usage but makes
    /// fixes harder to evaluate.
    /// </para>
    /// </remarks>
    public bool IncludeExplanations { get; init; } = true;

    /// <summary>
    /// Minimum confidence threshold for suggestions.
    /// </summary>
    /// <value>Default: 0.7.</value>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Suggestions with confidence below this threshold are
    /// flagged as low-confidence in the UI. Users should review these more
    /// carefully before applying.
    /// </para>
    /// <para>
    /// <b>Range:</b> 0.0 to 1.0.
    /// </para>
    /// </remarks>
    public double MinConfidence { get; init; } = 0.7;

    /// <summary>
    /// Whether to validate fixes against the linter before returning.
    /// </summary>
    /// <value>Default: true.</value>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> When <c>true</c>, each generated fix is validated by
    /// re-linting the fixed text to ensure:
    /// <list type="bullet">
    ///   <item><description>The original violation is resolved</description></item>
    ///   <item><description>No new violations are introduced</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Trade-offs:</b> Validation adds latency but significantly improves
    /// fix quality and user confidence.
    /// </para>
    /// </remarks>
    public bool ValidateFixes { get; init; } = true;

    /// <summary>
    /// Custom prompt overrides for specific violation categories.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Allows category-specific prompt customization. Keys are
    /// category names (e.g., "Terminology", "Grammar"), values are prompt
    /// fragments to inject into the generation prompt.
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<string, string>? CategoryPromptOverrides { get; init; }

    /// <summary>
    /// User's preferred tone for rewrites.
    /// </summary>
    /// <value>Default: <see cref="TonePreference.Neutral"/>.</value>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Controls the writing style of generated fixes:
    /// <list type="bullet">
    ///   <item><description><see cref="TonePreference.Neutral"/> — Preserve original tone</description></item>
    ///   <item><description><see cref="TonePreference.Formal"/> — Professional language</description></item>
    ///   <item><description><see cref="TonePreference.Casual"/> — Conversational language</description></item>
    ///   <item><description><see cref="TonePreference.Technical"/> — Precise technical language</description></item>
    ///   <item><description><see cref="TonePreference.Simplified"/> — Simple, accessible language</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public TonePreference Tone { get; init; } = TonePreference.Neutral;

    /// <summary>
    /// Whether to strongly preserve the author's voice in rewrites.
    /// </summary>
    /// <value>Default: true.</value>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> When <c>true</c>, the LLM is instructed to keep sentence
    /// structure similar and minimize stylistic changes while fixing the violation.
    /// This is important for creative writing and content with a distinctive voice.
    /// </para>
    /// </remarks>
    public bool PreserveVoice { get; init; } = true;

    /// <summary>
    /// Maximum parallel LLM requests for batch processing.
    /// </summary>
    /// <value>Default: 5.</value>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Controls concurrency when generating fixes for multiple
    /// deviations via <see cref="IFixSuggestionGenerator.GenerateFixesAsync"/>.
    /// Higher values increase throughput but may hit rate limits.
    /// </para>
    /// </remarks>
    public int MaxParallelism { get; init; } = 5;

    /// <summary>
    /// Whether to use learning context to enhance prompts.
    /// </summary>
    /// <value>Default: true.</value>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> When <c>true</c> and <see cref="Contracts.Agents.ILearningLoopService"/>
    /// is available (v0.7.5d), prompts are enhanced with:
    /// <list type="bullet">
    ///   <item><description>Patterns previously accepted by the user</description></item>
    ///   <item><description>Patterns previously rejected by the user</description></item>
    ///   <item><description>Rule-specific guidance from learning history</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public bool UseLearningContext { get; init; } = true;

    /// <summary>
    /// Default options instance.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Provides a shared default instance to avoid allocations
    /// when callers don't need custom options.
    /// </remarks>
    public static FixGenerationOptions Default { get; } = new();
}
