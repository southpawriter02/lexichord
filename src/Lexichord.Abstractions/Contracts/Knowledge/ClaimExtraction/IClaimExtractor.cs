// =============================================================================
// File: IClaimExtractor.cs
// Project: Lexichord.Abstractions
// Description: Interface for individual claim extractors.
// =============================================================================
// LOGIC: Defines the contract for components that extract claims from parsed
//   sentences using specific methods (pattern-based, dependency-based, etc.).
//
// v0.5.6g: Claim Extractor (Knowledge Graph Claim Extraction Pipeline)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Parsing;

namespace Lexichord.Abstractions.Contracts.Knowledge.ClaimExtraction;

/// <summary>
/// Interface for individual claim extractors.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Defines a single extraction strategy. Multiple extractors
/// can be combined in the <see cref="IClaimExtractionService"/> to provide
/// comprehensive claim extraction.
/// </para>
/// <para>
/// <b>Implementations:</b>
/// <list type="bullet">
/// <item><description><b>PatternClaimExtractor:</b> Uses regex/template patterns.</description></item>
/// <item><description><b>DependencyClaimExtractor:</b> Uses dependency parse trees.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6g as part of the Claim Extractor.
/// </para>
/// </remarks>
public interface IClaimExtractor
{
    /// <summary>
    /// Gets the name of this extractor.
    /// </summary>
    /// <value>A descriptive name for logging and debugging.</value>
    string Name { get; }

    /// <summary>
    /// Extracts claims from a parsed sentence.
    /// </summary>
    /// <param name="sentence">The parsed sentence to extract claims from.</param>
    /// <param name="entities">Linked entities in the sentence.</param>
    /// <param name="context">Extraction context with configuration.</param>
    /// <returns>A list of extracted claims (pre-processed).</returns>
    /// <remarks>
    /// This method is synchronous as it operates on already-parsed data.
    /// The returned claims are pre-processed and need to be converted
    /// to final <see cref="Claims.Claim"/> records by the service.
    /// </remarks>
    IReadOnlyList<ExtractedClaim> Extract(
        ParsedSentence sentence,
        IReadOnlyList<LinkedEntity> entities,
        ClaimExtractionContext context);
}
