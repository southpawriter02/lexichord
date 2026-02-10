// =============================================================================
// File: KnowledgePrompt.cs
// Project: Lexichord.Abstractions
// Description: A fully rendered knowledge-aware prompt ready for LLM submission.
// =============================================================================
// LOGIC: This is the output of IKnowledgePromptBuilder.BuildPrompt(). It
//   contains the rendered system and user prompts with knowledge context
//   already injected, plus metadata about which entities and axioms were
//   included for traceability.
//
// v0.6.6i: Knowledge-Aware Prompts (CKVS Phase 3b)
// Dependencies: None (pure data record)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// A fully rendered prompt ready for LLM submission, with knowledge context injected.
/// </summary>
/// <remarks>
/// <para>
/// Produced by <see cref="IKnowledgePromptBuilder.BuildPrompt"/>. Contains
/// the finalized system and user prompts with all knowledge context variables
/// substituted, along with metadata tracking which entities and axioms
/// contributed to the prompt.
/// </para>
/// <para>
/// <b>Traceability:</b> The <see cref="IncludedEntityIds"/> and
/// <see cref="IncludedAxiomIds"/> lists enable downstream components
/// (e.g., citation renderer, post-generation validator) to know exactly
/// which knowledge was available to the LLM.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6i as part of Knowledge-Aware Prompts.
/// </para>
/// </remarks>
public record KnowledgePrompt
{
    /// <summary>
    /// Rendered system prompt with grounding rules and axiom context.
    /// </summary>
    public required string SystemPrompt { get; init; }

    /// <summary>
    /// Rendered user prompt with knowledge entities, relationships, and the user's request.
    /// </summary>
    public required string UserPrompt { get; init; }

    /// <summary>
    /// Estimated token count for the complete prompt (system + user).
    /// </summary>
    /// <remarks>
    /// LOGIC: Rough estimate using character count / 4. Sufficient for
    /// budget checking but not tokenizer-accurate.
    /// </remarks>
    public int EstimatedTokens { get; init; }

    /// <summary>
    /// ID of the template used to generate this prompt.
    /// </summary>
    public string? TemplateId { get; init; }

    /// <summary>
    /// IDs of knowledge entities included in the prompt context.
    /// </summary>
    /// <value>Used for citation and validation traceability.</value>
    public IReadOnlyList<Guid> IncludedEntityIds { get; init; } = [];

    /// <summary>
    /// IDs of axioms included in the system prompt.
    /// </summary>
    /// <value>Used for axiom compliance traceability.</value>
    public IReadOnlyList<string> IncludedAxiomIds { get; init; } = [];
}
