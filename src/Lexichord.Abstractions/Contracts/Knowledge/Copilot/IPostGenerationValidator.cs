// =============================================================================
// File: IPostGenerationValidator.cs
// Project: Lexichord.Abstractions
// Description: Interface for validating LLM-generated content against the KG.
// =============================================================================
// LOGIC: The post-generation validator is the quality gate between LLM
//   output and user-facing presentation. It extracts claims from generated
//   content, validates them against the knowledge graph, detects
//   hallucinations, and optionally auto-fixes content.
//
// v0.6.6g: Post-Generation Validator (CKVS Phase 3b)
// Dependencies: PostValidationResult (v0.6.6g), KnowledgeContext (v0.6.6e),
//               AgentRequest (v0.6.6a)
// =============================================================================

using Lexichord.Abstractions.Agents;

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Validates LLM-generated content against the knowledge graph.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> The post-generation validator is the quality gate between
/// LLM output and user-facing presentation. It catches hallucinations,
/// inconsistencies, and axiom violations in generated output.
/// </para>
/// <para>
/// <b>Validation Pipeline:</b>
/// <list type="number">
///   <item><description>Entity verification — match entity names from content
///     against <see cref="KnowledgeContext.Entities"/>.</description></item>
///   <item><description>Claim extraction via
///     <see cref="IClaimExtractionService"/>.</description></item>
///   <item><description>Claim validation via
///     <see cref="IValidationEngine"/>.</description></item>
///   <item><description>Hallucination detection via
///     <see cref="IHallucinationDetector"/>.</description></item>
///   <item><description>Score computation and status determination.</description></item>
///   <item><description>Fix generation and user message.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>License:</b>
/// <list type="bullet">
///   <item>WriterPro — basic validation (entity + claim).</item>
///   <item>Teams — full validation including hallucination detection.</item>
///   <item>Enterprise — full validation with auto-fix.</item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe. Multiple
/// callers may invoke validation concurrently.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6g as part of the Post-Generation Validator.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class CopilotPipeline(IPostGenerationValidator validator)
/// {
///     public async Task&lt;string&gt; ProcessAsync(
///         string generated, KnowledgeContext ctx, AgentRequest req)
///     {
///         var result = await validator.ValidateAndFixAsync(
///             generated, ctx, req);
///
///         if (result.CorrectedContent != null)
///             return result.CorrectedContent;
///
///         if (!result.IsValid)
///             logger.LogWarning("Issues: {Msg}", result.UserMessage);
///
///         return generated;
///     }
/// }
/// </code>
/// </example>
public interface IPostGenerationValidator
{
    /// <summary>
    /// Validates generated content against the knowledge graph.
    /// </summary>
    /// <param name="generatedContent">The LLM-generated text to validate.</param>
    /// <param name="context">
    /// The knowledge context used during generation, providing entities,
    /// relationships, axioms, and claims for validation.
    /// </param>
    /// <param name="originalRequest">
    /// The original <see cref="AgentRequest"/> that triggered generation.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="PostValidationResult"/> with findings, hallucinations,
    /// score, and a user-facing message. <see cref="PostValidationResult.CorrectedContent"/>
    /// is always <c>null</c> (use <see cref="ValidateAndFixAsync"/> for auto-fix).
    /// </returns>
    Task<PostValidationResult> ValidateAsync(
        string generatedContent,
        KnowledgeContext context,
        AgentRequest originalRequest,
        CancellationToken ct = default);

    /// <summary>
    /// Validates and optionally auto-fixes generated content.
    /// </summary>
    /// <param name="generatedContent">The LLM-generated text to validate.</param>
    /// <param name="context">The knowledge context.</param>
    /// <param name="originalRequest">The original agent request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="PostValidationResult"/>. If auto-fixable issues were found,
    /// <see cref="PostValidationResult.CorrectedContent"/> contains the corrected
    /// text; otherwise it is <c>null</c>.
    /// </returns>
    /// <remarks>
    /// LOGIC: Calls <see cref="ValidateAsync"/> first, then applies fixes
    /// where <see cref="ValidationFix.CanAutoApply"/> is <c>true</c> and
    /// <see cref="ValidationFix.ReplaceSpan"/> is non-null. Requires
    /// Enterprise license tier for auto-fix.
    /// </remarks>
    Task<PostValidationResult> ValidateAndFixAsync(
        string generatedContent,
        KnowledgeContext context,
        AgentRequest originalRequest,
        CancellationToken ct = default);
}
