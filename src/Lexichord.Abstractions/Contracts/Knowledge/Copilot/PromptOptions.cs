// =============================================================================
// File: PromptOptions.cs
// Project: Lexichord.Abstractions
// Description: Configuration options for building knowledge-aware prompts.
// =============================================================================
// LOGIC: Controls which template to use, how much context to include, the
//   output format for context data, and the grounding strictness level.
//   The ContextFormat enum is reused from v0.6.6e (KnowledgeContextOptions).
//
// v0.6.6i: Knowledge-Aware Prompts (CKVS Phase 3b)
// Dependencies:
//   - ContextFormat (v0.6.6e)
//   - GroundingLevel (v0.6.6i)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Options for building a knowledge-aware prompt.
/// </summary>
/// <remarks>
/// <para>
/// Configures the prompt building process, including template selection,
/// context inclusion flags, formatting, and grounding strictness. When
/// <see cref="TemplateId"/> is <c>null</c>, the default
/// <c>"copilot-knowledge-aware"</c> template is used.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6i as part of Knowledge-Aware Prompts.
/// </para>
/// </remarks>
public record PromptOptions
{
    /// <summary>
    /// Template ID to use for prompt generation.
    /// </summary>
    /// <value>
    /// When <c>null</c>, the default <c>"copilot-knowledge-aware"</c> template is used.
    /// Must match a registered template ID if specified.
    /// </value>
    public string? TemplateId { get; init; }

    /// <summary>
    /// Maximum number of tokens to allocate for knowledge context.
    /// </summary>
    /// <value>Defaults to <c>2000</c> tokens.</value>
    public int MaxContextTokens { get; init; } = 2000;

    /// <summary>
    /// Whether to include axiom rules in the system prompt.
    /// </summary>
    /// <value>Defaults to <c>true</c>.</value>
    public bool IncludeAxioms { get; init; } = true;

    /// <summary>
    /// Whether to include entity relationships in the user prompt.
    /// </summary>
    /// <value>Defaults to <c>true</c>.</value>
    public bool IncludeRelationships { get; init; } = true;

    /// <summary>
    /// Output format for knowledge context data in the prompt.
    /// </summary>
    /// <value>Defaults to <see cref="ContextFormat.Yaml"/>.</value>
    public ContextFormat ContextFormat { get; init; } = ContextFormat.Yaml;

    /// <summary>
    /// Strictness level for grounding the LLM's output in the knowledge context.
    /// </summary>
    /// <value>Defaults to <see cref="GroundingLevel.Moderate"/>.</value>
    public GroundingLevel GroundingLevel { get; init; } = GroundingLevel.Moderate;

    /// <summary>
    /// Additional free-text instructions to append to the system prompt.
    /// </summary>
    /// <value>When <c>null</c> or empty, no additional instructions are included.</value>
    public string? AdditionalInstructions { get; init; }
}
