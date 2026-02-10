// =============================================================================
// File: IEntityCitationRenderer.cs
// Project: Lexichord.Abstractions
// Description: Interface for rendering entity citations in Co-pilot responses.
// =============================================================================
// LOGIC: The Entity Citation Renderer is the transparency layer between
//   validated LLM output and the user. It generates citation markup showing
//   which knowledge graph entities informed the response, and provides
//   on-demand detail for individual citations.
//
// v0.6.6h: Entity Citation Renderer (CKVS Phase 3b)
// Dependencies: ValidatedGenerationResult (v0.6.6h), CitationOptions (v0.6.6h),
//               CitationMarkup (v0.6.6h), KnowledgeEntity (v0.4.5e),
//               EntityCitationDetail (v0.6.6h)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Renders entity citations for Co-pilot responses.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Provides transparency into AI-generated content by showing
/// the canonical Knowledge Graph sources backing each claim. The renderer
/// transforms a <see cref="ValidatedGenerationResult"/> into user-facing
/// <see cref="CitationMarkup"/> for display in the citation panel.
/// </para>
/// <para>
/// <b>Key Operations:</b>
/// <list type="number">
///   <item><description><see cref="GenerateCitations"/> — produces the full
///     citation markup for a response, including citations list, validation
///     status, icon, and formatted output.</description></item>
///   <item><description><see cref="GetCitationDetail"/> — provides deep-dive
///     information for a single entity (used properties, derived claims,
///     graph browser link).</description></item>
/// </list>
/// </para>
/// <para>
/// <b>License Gating:</b>
/// <list type="bullet">
///   <item>Core — not available.</item>
///   <item>WriterPro — basic citations (compact format).</item>
///   <item>Teams — full citations + entity details.</item>
///   <item>Enterprise — full + custom formats.</item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe. The renderer
/// is stateless and can be called concurrently for different responses.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6h as part of the Entity Citation Renderer.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class CopilotResponseHandler(IEntityCitationRenderer renderer)
/// {
///     public CitationMarkup RenderCitations(ValidatedGenerationResult result)
///     {
///         var options = new CitationOptions { Format = CitationFormat.Compact };
///         return renderer.GenerateCitations(result, options);
///     }
///
///     public EntityCitationDetail GetDetail(
///         KnowledgeEntity entity, ValidatedGenerationResult result)
///     {
///         return renderer.GetCitationDetail(entity, result);
///     }
/// }
/// </code>
/// </example>
public interface IEntityCitationRenderer
{
    /// <summary>
    /// Generates citation markup for a validated Co-pilot response.
    /// </summary>
    /// <param name="result">
    /// The validated generation result containing the generated content,
    /// source entities, and post-validation findings.
    /// </param>
    /// <param name="options">
    /// Options controlling the citation format, limits, and display preferences.
    /// </param>
    /// <returns>
    /// A <see cref="CitationMarkup"/> with the cited entities, validation
    /// status text, validation icon, and optional formatted output.
    /// </returns>
    /// <remarks>
    /// LOGIC: For each source entity (up to <see cref="CitationOptions.MaxCitations"/>):
    /// <list type="number">
    ///   <item>Check verification status against validation findings.</item>
    ///   <item>Build <see cref="EntityCitation"/> with type icon and display label.</item>
    ///   <item>Optionally group by type and sort alphabetically.</item>
    ///   <item>Determine validation status text and icon from post-validation.</item>
    ///   <item>Format output according to <see cref="CitationOptions.Format"/>.</item>
    /// </list>
    /// </remarks>
    CitationMarkup GenerateCitations(
        ValidatedGenerationResult result,
        CitationOptions options);

    /// <summary>
    /// Gets detailed citation information for a specific entity.
    /// </summary>
    /// <param name="entity">
    /// The <see cref="KnowledgeEntity"/> to get citation details for.
    /// </param>
    /// <param name="result">
    /// The validated generation result providing context for property
    /// usage detection and claim derivation.
    /// </param>
    /// <returns>
    /// An <see cref="EntityCitationDetail"/> with used properties,
    /// cited relationships, derived claims, and a graph browser link.
    /// </returns>
    /// <remarks>
    /// LOGIC: Scans the generated content for each entity property value
    /// (case-insensitive) to determine which properties were used. Extracts
    /// derived claims from the post-validation result by matching the
    /// claim subject entity ID.
    /// </remarks>
    EntityCitationDetail GetCitationDetail(
        KnowledgeEntity entity,
        ValidatedGenerationResult result);
}
