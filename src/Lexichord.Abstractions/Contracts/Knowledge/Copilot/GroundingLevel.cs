// =============================================================================
// File: GroundingLevel.cs
// Project: Lexichord.Abstractions
// Description: Defines strictness levels for knowledge grounding in prompts.
// =============================================================================
// LOGIC: Controls how strictly the LLM should adhere to the provided knowledge
//   context. Strict mode only allows facts from context; Flexible allows
//   supplementing with general knowledge.
//
// v0.6.6i: Knowledge-Aware Prompts (CKVS Phase 3b)
// Dependencies: None (pure abstraction)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Defines the strictness level for knowledge grounding in LLM prompts.
/// </summary>
/// <remarks>
/// <para>
/// Grounding levels control how tightly the LLM's output must adhere to
/// the provided knowledge context. Higher strictness reduces hallucination
/// risk but may limit the LLM's ability to synthesize or infer.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6i as part of Knowledge-Aware Prompts.
/// </para>
/// </remarks>
public enum GroundingLevel
{
    /// <summary>
    /// Only state facts explicitly present in the knowledge context.
    /// </summary>
    /// <remarks>
    /// The LLM must not make inferences or add information beyond what
    /// is provided. If information is not in the context, the LLM should
    /// indicate it does not have verified information.
    /// </remarks>
    Strict,

    /// <summary>
    /// Prefer facts from the knowledge context but allow reasonable inferences.
    /// </summary>
    /// <remarks>
    /// The LLM may make inferences clearly marked as such and should
    /// indicate the level of confidence when information is uncertain.
    /// This is the default grounding level.
    /// </remarks>
    Moderate,

    /// <summary>
    /// Use the knowledge context as guidance, supplementing with general knowledge.
    /// </summary>
    /// <remarks>
    /// The LLM may supplement context with general knowledge where appropriate
    /// but should prioritize accuracy and maintain consistency with the
    /// knowledge base terminology.
    /// </remarks>
    Flexible
}
