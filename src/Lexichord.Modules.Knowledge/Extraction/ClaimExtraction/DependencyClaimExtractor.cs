// =============================================================================
// File: DependencyClaimExtractor.cs
// Project: Lexichord.Modules.Knowledge
// Description: Extracts claims from dependency parse trees.
// =============================================================================
// LOGIC: Uses grammatical relations (nsubj, dobj) from dependency parsing
//   to extract claims. Maps verb lemmas to predicates.
//
// v0.5.6g: Claim Extractor (Knowledge Graph Claim Extraction Pipeline)
// Dependencies: ISentenceParser (v0.5.6f), ParsedSentence, DependencyRelations
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.ClaimExtraction;
using Lexichord.Abstractions.Contracts.Knowledge.Claims;
using Lexichord.Abstractions.Contracts.Knowledge.Parsing;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Extraction.ClaimExtraction;

/// <summary>
/// Extracts claims from dependency parse trees.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Uses grammatical structure to identify subject-verb-object
/// patterns and converts them into claims. More robust to paraphrasing
/// than pattern-based extraction.
/// </para>
/// <para>
/// <b>Verb-to-Predicate Mapping:</b>
/// Maps common verbs to standard predicates (e.g., "accept" → ACCEPTS).
/// Unmapped verbs default to HAS_PROPERTY.
/// </para>
/// </remarks>
public class DependencyClaimExtractor : IClaimExtractor
{
    private readonly ILogger<DependencyClaimExtractor> _logger;

    /// <summary>
    /// Maps verb lemmas to claim predicates.
    /// Uses only predicates defined in ClaimPredicate (v0.5.6e).
    /// </summary>
    private static readonly Dictionary<string, string> VerbToPredicateMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["accept"] = ClaimPredicate.ACCEPTS,
        ["return"] = ClaimPredicate.RETURNS,
        ["require"] = ClaimPredicate.REQUIRES,
        ["contain"] = ClaimPredicate.CONTAINS,
        ["have"] = ClaimPredicate.HAS_PROPERTY,
        ["extend"] = ClaimPredicate.EXTENDS,
        ["deprecate"] = ClaimPredicate.IS_DEPRECATED,
        ["call"] = ClaimPredicate.CALLS,
        ["invoke"] = ClaimPredicate.CALLS,
        ["include"] = ClaimPredicate.CONTAINS,
        ["throw"] = ClaimPredicate.THROWS,
        ["reference"] = ClaimPredicate.REFERENCES,
        ["replace"] = ClaimPredicate.REPLACES
    };

    /// <inheritdoc/>
    public string Name => "DependencyExtractor";

    /// <summary>
    /// Initializes a new instance of the <see cref="DependencyClaimExtractor"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    public DependencyClaimExtractor(ILogger<DependencyClaimExtractor> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ExtractedClaim> Extract(
        ParsedSentence sentence,
        IReadOnlyList<LinkedEntity> entities,
        ClaimExtractionContext context)
    {
        if (!context.UseDependencyExtraction)
        {
            return Array.Empty<ExtractedClaim>();
        }

        if (sentence.Dependencies == null || sentence.Dependencies.Count == 0)
        {
            return Array.Empty<ExtractedClaim>();
        }

        var claims = new List<ExtractedClaim>();

        // Find root verb
        var rootVerb = sentence.GetRootVerb();
        if (rootVerb == null)
        {
            _logger.LogDebug("No root verb found in sentence: {Sentence}", sentence.Text.Truncate(50));
            return claims;
        }

        // Map verb to predicate
        var lemma = rootVerb.Lemma?.ToLowerInvariant() ?? rootVerb.Text.ToLowerInvariant();
        if (!VerbToPredicateMap.TryGetValue(lemma, out var predicate))
        {
            predicate = ClaimPredicate.HAS_PROPERTY; // Default fallback
        }

        // Check predicate filter
        if (context.PredicateFilter != null && !context.PredicateFilter.Contains(predicate))
        {
            return claims;
        }

        // Find subject
        var subject = sentence.GetSubject();
        if (subject == null)
        {
            _logger.LogDebug("No subject found for verb '{Verb}'", rootVerb.Text);
            return claims;
        }

        // Find direct object
        var directObject = sentence.GetDirectObject();
        if (directObject == null)
        {
            _logger.LogDebug("No direct object found for verb '{Verb}'", rootVerb.Text);
            return claims;
        }

        // Get full noun phrases
        var subjectPhrase = GetNounPhrase(subject, sentence);
        var objectPhrase = GetNounPhrase(directObject, sentence);

        // Find matching entities
        var subjectEntity = FindMatchingEntity(entities, subjectPhrase);
        var objectEntity = FindMatchingEntity(entities, objectPhrase);

        claims.Add(new ExtractedClaim
        {
            SubjectSpan = new TextSpan
            {
                Text = subjectPhrase,
                StartOffset = subject.StartChar - sentence.StartOffset,
                EndOffset = subject.EndChar - sentence.StartOffset
            },
            ObjectSpan = new TextSpan
            {
                Text = objectPhrase,
                StartOffset = directObject.StartChar - sentence.StartOffset,
                EndOffset = directObject.EndChar - sentence.StartOffset
            },
            Predicate = predicate,
            Method = ClaimExtractionMethod.DependencyParsing,
            RawConfidence = 0.7f, // Lower base confidence for dependency extraction
            Sentence = sentence,
            SubjectEntity = subjectEntity,
            ObjectEntity = objectEntity
        });

        _logger.LogDebug(
            "Dependency extraction: {Subject} {Predicate} {Object}",
            subjectPhrase, predicate, objectPhrase);

        return claims;
    }

    /// <summary>
    /// Gets the full noun phrase for a head token.
    /// </summary>
    private string GetNounPhrase(Token head, ParsedSentence sentence)
    {
        var tokens = new List<Token> { head };

        // Add compound modifiers
        var compounds = sentence.Dependencies?
            .Where(d => d.Head == head && d.Relation == DependencyRelations.COMPOUND)
            .Select(d => d.Dependent)
            .ToList() ?? new List<Token>();

        tokens.AddRange(compounds);

        // Add determiners and adjectives
        var modifiers = sentence.Dependencies?
            .Where(d => d.Head == head &&
                   (d.Relation == DependencyRelations.DET ||
                    d.Relation == DependencyRelations.AMOD))
            .Select(d => d.Dependent)
            .ToList() ?? new List<Token>();

        tokens.AddRange(modifiers);

        // Sort by position and join
        return string.Join(" ", tokens.OrderBy(t => t.Index).Select(t => t.Text));
    }

    /// <summary>
    /// Finds an entity matching the phrase.
    /// </summary>
    private LinkedEntity? FindMatchingEntity(
        IReadOnlyList<LinkedEntity> entities,
        string phrase)
    {
        return entities.FirstOrDefault(e =>
            phrase.Contains(e.Mention.Value, StringComparison.OrdinalIgnoreCase) ||
            e.Mention.Value.Contains(phrase, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Extension methods for string truncation.
/// </summary>
internal static class StringExtensions
{
    /// <summary>
    /// Truncates a string to a maximum length.
    /// </summary>
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}
