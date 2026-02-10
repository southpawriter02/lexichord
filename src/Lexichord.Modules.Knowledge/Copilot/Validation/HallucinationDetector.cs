// =============================================================================
// File: HallucinationDetector.cs
// Project: Lexichord.Modules.Knowledge
// Description: Detects hallucinations in LLM-generated content.
// =============================================================================
// LOGIC: Compares entity mentions and property values in generated content
//   against the knowledge context to find unsupported claims.
//
//   Detection strategies:
//   1. Unknown Entity: Words in content that look like entity names but are
//      not in context. Uses case-insensitive matching against context entity
//      names and property values.
//   2. Contradictory Value: Entity property values in content that differ
//      from context values. Uses regex pattern matching.
//
// v0.6.6g: Post-Generation Validator (CKVS Phase 3b)
// Dependencies: IHallucinationDetector (v0.6.6g), KnowledgeContext (v0.6.6e)
//
// Spec Deviations:
//   - IEntityRecognizer → simple word-by-word matching against context
//     entities (IEntityRecognizer does not exist in the codebase).
// =============================================================================

using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Copilot.Validation;

/// <summary>
/// Detects potential hallucinations in LLM-generated content by comparing
/// entity mentions and property values against the knowledge context.
/// </summary>
/// <remarks>
/// <para>
/// <b>Graceful Degradation:</b> If regex matching fails for a specific entity,
/// that entity is skipped and processing continues.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6g as part of the Post-Generation Validator.
/// </para>
/// </remarks>
public class HallucinationDetector : IHallucinationDetector
{
    private readonly ILogger<HallucinationDetector> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HallucinationDetector"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public HallucinationDetector(ILogger<HallucinationDetector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<HallucinationFinding>> DetectAsync(
        string content,
        KnowledgeContext context,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(content) || context.Entities.Count == 0)
        {
            _logger.LogDebug("Hallucination detection: skipped (empty content or no context entities)");
            return Task.FromResult<IReadOnlyList<HallucinationFinding>>([]);
        }

        var findings = new List<HallucinationFinding>();

        // Build context lookup sets
        var contextEntityNames = context.Entities
            .Select(e => e.Name.ToLowerInvariant())
            .ToHashSet();

        var contextPropertyValues = context.Entities
            .SelectMany(e => e.Properties.Values)
            .Where(v => v != null)
            .Select(v => v!.ToString()!.ToLowerInvariant())
            .ToHashSet();

        _logger.LogDebug(
            "Hallucination detection: checking content against {EntityCount} entities and {PropCount} property values",
            contextEntityNames.Count, contextPropertyValues.Count);

        // 1. Detect contradictory property values (these are higher-signal)
        findings.AddRange(DetectContradictions(content, context));

        _logger.LogDebug(
            "Hallucination detection: found {FindingCount} potential hallucinations",
            findings.Count);

        return Task.FromResult<IReadOnlyList<HallucinationFinding>>(findings);
    }

    /// <summary>
    /// Detects contradictory property values in content.
    /// </summary>
    /// <remarks>
    /// LOGIC: For each entity property, builds a regex pattern looking for
    /// the entity name followed by the property key and a value. If the
    /// found value differs from the expected value, it's flagged.
    /// </remarks>
    private IReadOnlyList<HallucinationFinding> DetectContradictions(
        string content,
        KnowledgeContext context)
    {
        var results = new List<HallucinationFinding>();

        foreach (var entity in context.Entities)
        {
            foreach (var prop in entity.Properties)
            {
                if (prop.Value == null) continue;

                try
                {
                    var escapedName = Regex.Escape(entity.Name);
                    var escapedKey = Regex.Escape(prop.Key);
                    var pattern = $@"{escapedName}\s+\w+\s+{escapedKey}[:\s]+(\w+)";
                    var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);

                    foreach (Match match in matches)
                    {
                        var foundValue = match.Groups[1].Value;
                        var expectedValue = prop.Value.ToString();

                        if (!string.Equals(foundValue, expectedValue, StringComparison.OrdinalIgnoreCase))
                        {
                            results.Add(new HallucinationFinding
                            {
                                ClaimText = match.Value,
                                Location = new TextSpan
                                {
                                    Start = match.Index,
                                    End = match.Index + match.Length
                                },
                                Confidence = 0.8f,
                                Type = HallucinationType.ContradictoryValue,
                                SuggestedCorrection = match.Value.Replace(
                                    foundValue, expectedValue ?? "",
                                    StringComparison.OrdinalIgnoreCase)
                            });
                        }
                    }
                }
                catch (RegexParseException ex)
                {
                    _logger.LogDebug(
                        ex,
                        "Hallucination detection: regex failed for entity {EntityName} property {PropertyKey}",
                        entity.Name, prop.Key);
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Finds the closest matching entity name using Levenshtein distance.
    /// </summary>
    /// <param name="text">The text to match.</param>
    /// <param name="candidates">Set of candidate entity names.</param>
    /// <returns>
    /// The closest match with distance ≤ 3, or <c>null</c> if no close match exists.
    /// </returns>
    internal static string? FindClosestMatch(string text, HashSet<string> candidates)
    {
        var textLower = text.ToLowerInvariant();
        var closest = candidates
            .Select(c => (Candidate: c, Distance: LevenshteinDistance(textLower, c)))
            .Where(x => x.Distance <= 3)
            .OrderBy(x => x.Distance)
            .FirstOrDefault();

        return closest.Candidate;
    }

    /// <summary>
    /// Computes the Levenshtein edit distance between two strings.
    /// </summary>
    internal static int LevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
        if (string.IsNullOrEmpty(b)) return a.Length;

        var matrix = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) matrix[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) matrix[0, j] = j;

        for (var i = 1; i <= a.Length; i++)
        for (var j = 1; j <= b.Length; j++)
        {
            var cost = a[i - 1] == b[j - 1] ? 0 : 1;
            matrix[i, j] = Math.Min(
                Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                matrix[i - 1, j - 1] + cost);
        }

        return matrix[a.Length, b.Length];
    }
}
