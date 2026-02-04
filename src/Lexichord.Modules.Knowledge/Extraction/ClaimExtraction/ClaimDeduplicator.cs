// =============================================================================
// File: ClaimDeduplicator.cs
// Project: Lexichord.Modules.Knowledge
// Description: Deduplicates semantically equivalent claims.
// =============================================================================
// LOGIC: Removes duplicate claims based on subject, predicate, and object
//   equivalence. Keeps the highest-confidence version of each unique claim.
//
// v0.5.6g: Claim Extractor (Knowledge Graph Claim Extraction Pipeline)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Claims;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Extraction.ClaimExtraction;

/// <summary>
/// Deduplicates semantically equivalent claims.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> When multiple extractors or patterns match the same
/// underlying claim, this component keeps only the highest-confidence version.
/// </para>
/// <para>
/// <b>Equivalence Criteria:</b>
/// <list type="bullet">
/// <item><description>Same subject surface form (case-insensitive)</description></item>
/// <item><description>Same predicate</description></item>
/// <item><description>Same object surface form or literal value</description></item>
/// </list>
/// </para>
/// </remarks>
public class ClaimDeduplicator
{
    private readonly ILogger<ClaimDeduplicator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClaimDeduplicator"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    public ClaimDeduplicator(ILogger<ClaimDeduplicator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Deduplicates a list of claims.
    /// </summary>
    /// <param name="claims">The claims to deduplicate.</param>
    /// <returns>Deduplicated claims, keeping highest confidence for each.</returns>
    public List<Claim> Deduplicate(List<Claim> claims)
    {
        if (claims.Count <= 1)
        {
            return claims;
        }

        var grouped = claims
            .GroupBy(c => GetClaimKey(c))
            .Select(g => g.OrderByDescending(c => c.Confidence).First())
            .ToList();

        var removed = claims.Count - grouped.Count;
        if (removed > 0)
        {
            _logger.LogDebug(
                "Deduplicated {RemovedCount} claims ({BeforeCount} -> {AfterCount})",
                removed, claims.Count, grouped.Count);
        }

        return grouped;
    }

    /// <summary>
    /// Generates a unique key for a claim based on its semantic content.
    /// </summary>
    /// <param name="claim">The claim to key.</param>
    /// <returns>A string key representing the claim's unique content.</returns>
    private static string GetClaimKey(Claim claim)
    {
        var subject = NormalizeSurfaceForm(claim.Subject.SurfaceForm);
        var predicate = claim.Predicate;
        var objectKey = claim.Object.Type == ClaimObjectType.Literal
            ? $"lit:{claim.Object.LiteralValue}"
            : $"ent:{NormalizeSurfaceForm(claim.Object.Entity?.SurfaceForm ?? "")}";

        return $"{subject}|{predicate}|{objectKey}";
    }

    /// <summary>
    /// Normalizes a surface form for comparison.
    /// </summary>
    private static string NormalizeSurfaceForm(string surfaceForm)
    {
        return surfaceForm
            .ToLowerInvariant()
            .Trim()
            .Replace("the ", "")
            .Replace("a ", "")
            .Replace("an ", "");
    }
}
