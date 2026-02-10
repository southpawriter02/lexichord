// =============================================================================
// File: IHallucinationDetector.cs
// Project: Lexichord.Abstractions
// Description: Interface for detecting hallucinations in LLM-generated content.
// =============================================================================
// LOGIC: The hallucination detector compares entity mentions and property
//   values in generated content against the knowledge context. Entities not
//   in context are flagged as UnknownEntity; contradictory property values
//   are flagged as ContradictoryValue.
//
// v0.6.6g: Post-Generation Validator (CKVS Phase 3b)
// Dependencies: HallucinationFinding (v0.6.6g), KnowledgeContext (v0.6.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Detects potential hallucinations in LLM-generated content.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Compares generated content against the
/// <see cref="KnowledgeContext"/> to find claims not supported by
/// the knowledge graph. Used by the
/// <see cref="IPostGenerationValidator"/> as step 5 in the
/// validation pipeline.
/// </para>
/// <para>
/// <b>Detection Strategies:</b>
/// <list type="number">
///   <item><description>
///     <b>Unknown Entity:</b> Entity-like mentions in content that
///     do not appear in <see cref="KnowledgeContext.Entities"/> names
///     or property values.
///   </description></item>
///   <item><description>
///     <b>Contradictory Value:</b> Entity property values in content
///     that differ from the values in context.
///   </description></item>
/// </list>
/// </para>
/// <para>
/// <b>License:</b> Teams tier required for hallucination detection.
/// Feature code: KG-066g.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6g as part of the Post-Generation Validator.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class ContentValidator(IHallucinationDetector detector)
/// {
///     public async Task CheckAsync(string content, KnowledgeContext ctx)
///     {
///         var findings = await detector.DetectAsync(content, ctx);
///         foreach (var f in findings)
///         {
///             Console.WriteLine($"[{f.Type}] {f.ClaimText} (confidence: {f.Confidence:P0})");
///         }
///     }
/// }
/// </code>
/// </example>
public interface IHallucinationDetector
{
    /// <summary>
    /// Detects claims in content not supported by the knowledge context.
    /// </summary>
    /// <param name="content">The LLM-generated content to check.</param>
    /// <param name="context">
    /// The knowledge context used during generation, containing the
    /// entities and property values to validate against.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="HallucinationFinding"/> instances, one per
    /// detected hallucination. Empty list if no hallucinations found.
    /// </returns>
    Task<IReadOnlyList<HallucinationFinding>> DetectAsync(
        string content,
        KnowledgeContext context,
        CancellationToken ct = default);
}
