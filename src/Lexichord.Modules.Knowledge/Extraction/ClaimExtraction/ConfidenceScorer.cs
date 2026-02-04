// =============================================================================
// File: ConfidenceScorer.cs
// Project: Lexichord.Modules.Knowledge
// Description: Scores and adjusts confidence for extracted claims.
// =============================================================================
// LOGIC: Adjusts raw extraction confidence based on factors like entity
//   resolution, pattern confidence, and extraction method.
//
// v0.5.6g: Claim Extractor (Knowledge Graph Claim Extraction Pipeline)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.ClaimExtraction;
using Lexichord.Abstractions.Contracts.Knowledge.Claims;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Extraction.ClaimExtraction;

/// <summary>
/// Scores and adjusts confidence for extracted claims.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Adjusts raw extraction confidence based on multiple
/// factors to produce a final confidence score for claims.
/// </para>
/// <para>
/// <b>Scoring Factors:</b>
/// <list type="bullet">
/// <item><description>Base pattern confidence</description></item>
/// <item><description>Entity resolution success (+0.1 if both resolved)</description></item>
/// <item><description>Extraction method (dependency-based slightly lower)</description></item>
/// <item><description>Sentence complexity (longer sentences reduce confidence)</description></item>
/// </list>
/// </para>
/// </remarks>
public class ConfidenceScorer
{
    private readonly ILogger<ConfidenceScorer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfidenceScorer"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    public ConfidenceScorer(ILogger<ConfidenceScorer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Scores an extracted claim.
    /// </summary>
    /// <param name="claim">The extracted claim to score.</param>
    /// <returns>Adjusted confidence score between 0 and 1.</returns>
    public float Score(ExtractedClaim claim)
    {
        var score = claim.RawConfidence;

        // Boost for resolved entities
        if (claim.SubjectEntity?.IsResolved == true)
        {
            score += 0.05f;
        }

        if (claim.ObjectEntity?.IsResolved == true || claim.IsLiteral)
        {
            score += 0.05f;
        }

        // Penalty for dependency extraction (slightly less reliable)
        if (claim.Method == ClaimExtractionMethod.DependencyParsing)
        {
            score -= 0.05f;
        }

        // Penalty for very long sentences (more room for error)
        var sentenceLength = claim.Sentence.Tokens.Count;
        if (sentenceLength > 30)
        {
            score -= 0.1f;
        }
        else if (sentenceLength > 20)
        {
            score -= 0.05f;
        }

        // Clamp to valid range
        score = Math.Clamp(score, 0f, 1f);

        _logger.LogDebug(
            "Scored claim {Predicate}: raw={RawConfidence:F2} -> adjusted={Score:F2}",
            claim.Predicate, claim.RawConfidence, score);

        return score;
    }
}
