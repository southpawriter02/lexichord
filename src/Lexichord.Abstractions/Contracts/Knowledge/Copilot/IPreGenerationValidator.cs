// =============================================================================
// File: IPreGenerationValidator.cs
// Project: Lexichord.Abstractions
// Description: Interface for validating context and request before LLM generation.
// =============================================================================
// LOGIC: Defines the contract for pre-generation validation. The validator
//   checks knowledge context consistency, request validity, and axiom
//   compliance before sending to the LLM. This prevents the Co-pilot from
//   generating content based on contradictory or invalid context.
//
// v0.6.6f: Pre-Generation Validator (CKVS Phase 3b)
// Dependencies: AgentRequest (v0.6.6a), KnowledgeContext (v0.6.6e),
//               PreValidationResult (v0.6.6f)
// =============================================================================

using Lexichord.Abstractions.Agents;

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Validates context and request before LLM generation.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IPreGenerationValidator"/> is invoked after knowledge
/// context retrieval but before the LLM call. It ensures that:
/// </para>
/// <list type="number">
///   <item>The knowledge context is internally consistent (no conflicts).</item>
///   <item>The user request is valid and meaningful.</item>
///   <item>Context entities comply with applicable axiom rules.</item>
///   <item>The request does not contradict the available context.</item>
/// </list>
/// <para>
/// <b>Error Handling:</b>
/// <list type="bullet">
///   <item>Validation service unavailable → allow generation with warning.</item>
///   <item>Empty context → warning, proceed without knowledge.</item>
///   <item>Blocking issues → return result with error message.</item>
///   <item>Timeout → allow generation, log warning.</item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe. Multiple
/// callers may invoke validation concurrently for different requests.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6f as part of the Pre-Generation Validator.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await validator.ValidateAsync(request, context, ct);
/// if (!result.CanProceed)
/// {
///     logger.LogWarning("Pre-validation blocked: {Message}", result.UserMessage);
///     return; // Do not invoke LLM
/// }
/// // Proceed with LLM generation
/// var response = await llmProvider.GenerateAsync(prompt, ct);
/// </code>
/// </example>
public interface IPreGenerationValidator
{
    /// <summary>
    /// Validates context and request before generation.
    /// </summary>
    /// <param name="request">The user's agent request containing the query.</param>
    /// <param name="context">The knowledge context retrieved for this request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="PreValidationResult"/> containing all found issues
    /// and a proceed/block decision.
    /// </returns>
    /// <remarks>
    /// LOGIC:
    /// <list type="number">
    ///   <item>Check for empty context (warning).</item>
    ///   <item>Run consistency checks via <see cref="IContextConsistencyChecker"/>.</item>
    ///   <item>Run request-context alignment checks.</item>
    ///   <item>Check axiom compliance if axioms are present.</item>
    ///   <item>Aggregate issues and determine CanProceed.</item>
    /// </list>
    /// </remarks>
    Task<PreValidationResult> ValidateAsync(
        AgentRequest request,
        KnowledgeContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Quick check if generation should proceed.
    /// </summary>
    /// <param name="request">The user's agent request.</param>
    /// <param name="context">The knowledge context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <c>true</c> if generation can proceed; <c>false</c> if
    /// blocking issues were found.
    /// </returns>
    /// <remarks>
    /// LOGIC: Delegates to <see cref="ValidateAsync"/> and returns
    /// <see cref="PreValidationResult.CanProceed"/>. Use when only
    /// the proceed/block decision is needed without issue details.
    /// </remarks>
    Task<bool> CanProceedAsync(
        AgentRequest request,
        KnowledgeContext context,
        CancellationToken ct = default);
}
