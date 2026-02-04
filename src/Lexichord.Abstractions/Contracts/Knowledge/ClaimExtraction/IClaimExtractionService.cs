// =============================================================================
// File: IClaimExtractionService.cs
// Project: Lexichord.Abstractions
// Description: Service interface for extracting claims from text.
// =============================================================================
// LOGIC: Defines the main claim extraction service that orchestrates parsers
//   and extractors to convert text into formal claims.
//
// v0.5.6g: Claim Extractor (Knowledge Graph Claim Extraction Pipeline)
// Dependencies: ISentenceParser (v0.5.6f), Claim (v0.5.6e)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Claims;
using Lexichord.Abstractions.Contracts.Knowledge.Parsing;

namespace Lexichord.Abstractions.Contracts.Knowledge.ClaimExtraction;

/// <summary>
/// Service for extracting claims from text.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> The main entry point for claim extraction. Coordinates
/// sentence parsing, multiple extraction strategies, entity linking,
/// confidence scoring, and deduplication.
/// </para>
/// <para>
/// <b>Extraction Flow:</b>
/// <list type="number">
/// <item><description>Parse text into sentences (using ISentenceParser).</description></item>
/// <item><description>Run each extractor on each sentence.</description></item>
/// <item><description>Link extracted spans to entities.</description></item>
/// <item><description>Score and filter by confidence.</description></item>
/// <item><description>Deduplicate semantically equivalent claims.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>License:</b> Teams tier for full extraction; Enterprise tier for
/// custom patterns. Feature code: KG-056g.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6g as part of the Claim Extraction pipeline.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class DocumentProcessor
/// {
///     private readonly IClaimExtractionService _extractor;
///
///     public async Task ProcessAsync(Document doc)
///     {
///         var context = new ClaimExtractionContext
///         {
///             DocumentId = doc.Id,
///             MinConfidence = 0.7f
///         };
///
///         var result = await _extractor.ExtractClaimsAsync(
///             doc.Content,
///             linkedEntities,
///             context);
///
///         foreach (var claim in result.Claims)
///         {
///             await _store.AddClaimAsync(claim);
///         }
///     }
/// }
/// </code>
/// </example>
public interface IClaimExtractionService
{
    /// <summary>
    /// Extracts claims from document text.
    /// </summary>
    /// <param name="text">The document text to process.</param>
    /// <param name="linkedEntities">Pre-linked entities in the text.</param>
    /// <param name="context">Extraction context with configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Extraction result containing claims and statistics.</returns>
    Task<ClaimExtractionResult> ExtractClaimsAsync(
        string text,
        IReadOnlyList<LinkedEntity> linkedEntities,
        ClaimExtractionContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Extracts claims from a single parsed sentence.
    /// </summary>
    /// <param name="sentence">The parsed sentence.</param>
    /// <param name="linkedEntities">Linked entities in the sentence.</param>
    /// <param name="context">Extraction context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Claims extracted from the sentence.</returns>
    Task<IReadOnlyList<Claim>> ExtractFromSentenceAsync(
        ParsedSentence sentence,
        IReadOnlyList<LinkedEntity> linkedEntities,
        ClaimExtractionContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Reloads extraction patterns from configuration.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the reload operation.</returns>
    Task ReloadPatternsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets extraction statistics.
    /// </summary>
    /// <returns>Aggregate extraction statistics.</returns>
    ClaimExtractionStats GetStats();
}
