// =============================================================================
// File: ClaimExtractionService.cs
// Project: Lexichord.Modules.Knowledge
// Description: Main claim extraction service orchestrating extractors.
// =============================================================================
// LOGIC: Coordinates parsing, extraction, entity linking, confidence scoring,
//   and deduplication to produce final claims from text.
//
// v0.5.6g: Claim Extractor (Knowledge Graph Claim Extraction Pipeline)
// Dependencies: ISentenceParser (v0.5.6f), Claim (v0.5.6e)
// =============================================================================

using System.Diagnostics;
using Lexichord.Abstractions.Contracts.Knowledge.ClaimExtraction;
using Lexichord.Abstractions.Contracts.Knowledge.Claims;
using Lexichord.Abstractions.Contracts.Knowledge.Parsing;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Extraction.ClaimExtraction;

/// <summary>
/// Main claim extraction service orchestrating extractors.
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
/// <item><description>Parse text into sentences.</description></item>
/// <item><description>Run each extractor on each sentence.</description></item>
/// <item><description>Score and filter by confidence.</description></item>
/// <item><description>Deduplicate semantically equivalent claims.</description></item>
/// </list>
/// </para>
/// </remarks>
public class ClaimExtractionService : IClaimExtractionService
{
    private readonly ISentenceParser _parser;
    private readonly IEnumerable<IClaimExtractor> _extractors;
    private readonly ClaimDeduplicator _deduplicator;
    private readonly ConfidenceScorer _confidenceScorer;
    private readonly ILogger<ClaimExtractionService> _logger;

    // Mutable stats for aggregation
    private int _totalClaims;
    private int _totalSentences;
    private int _duplicatesRemoved;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClaimExtractionService"/> class.
    /// </summary>
    public ClaimExtractionService(
        ISentenceParser parser,
        IEnumerable<IClaimExtractor> extractors,
        ClaimDeduplicator deduplicator,
        ConfidenceScorer confidenceScorer,
        ILogger<ClaimExtractionService> logger)
    {
        _parser = parser;
        _extractors = extractors;
        _deduplicator = deduplicator;
        _confidenceScorer = confidenceScorer;
        _logger = logger;

        _logger.LogInformation(
            "ClaimExtractionService initialized with {Count} extractors",
            _extractors.Count());
    }

    /// <inheritdoc/>
    public async Task<ClaimExtractionResult> ExtractClaimsAsync(
        string text,
        IReadOnlyList<LinkedEntity> linkedEntities,
        ClaimExtractionContext context,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return ClaimExtractionResult.Empty;
        }

        var sw = Stopwatch.StartNew();
        _logger.LogDebug("Starting claim extraction for document {DocumentId}", context.DocumentId);

        // Parse text into sentences
        var parseResult = await _parser.ParseAsync(text, new ParseOptions
        {
            Language = context.Language,
            IncludeDependencies = context.UseDependencyExtraction,
            IncludeSRL = true
        }, ct);

        var allClaims = new List<Claim>();
        var failedSentences = new Dictionary<int, string>();
        int sentenceIndex = 0;

        foreach (var sentence in parseResult.Sentences)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var sentenceClaims = await ExtractFromSentenceAsync(
                    sentence, linkedEntities, context, ct);

                allClaims.AddRange(sentenceClaims);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to extract claims from sentence: {Sentence}",
                    sentence.Text.Truncate(100));
                failedSentences[sentenceIndex] = ex.Message;
            }

            sentenceIndex++;
        }

        // Deduplicate
        var beforeCount = allClaims.Count;
        if (context.DeduplicateClaims)
        {
            allClaims = _deduplicator.Deduplicate(allClaims);
            _duplicatesRemoved += beforeCount - allClaims.Count;
        }

        sw.Stop();

        _totalClaims += allClaims.Count;
        _totalSentences += parseResult.Sentences.Count;

        var sentencesWithClaims = parseResult.Sentences
            .Count(s => allClaims.Any(c => c.Evidence?.Sentence == s.Text));

        var patternMatches = allClaims
            .Count(c => c.Evidence?.ExtractionMethod == ClaimExtractionMethod.PatternRule);

        var dependencyExtractions = allClaims
            .Count(c => c.Evidence?.ExtractionMethod == ClaimExtractionMethod.DependencyParsing);

        var avgConfidence = allClaims.Count > 0
            ? (float)allClaims.Average(c => c.Confidence)
            : 0f;

        _logger.LogInformation(
            "Extracted {ClaimCount} claims from {SentenceCount} sentences in {Duration}ms",
            allClaims.Count, parseResult.Sentences.Count, sw.ElapsedMilliseconds);

        return new ClaimExtractionResult
        {
            Claims = allClaims,
            DocumentId = context.DocumentId,
            Duration = sw.Elapsed,
            Stats = new ClaimExtractionStats
            {
                TotalClaims = allClaims.Count,
                TotalSentences = parseResult.Sentences.Count,
                SentencesWithClaims = sentencesWithClaims,
                PatternMatches = patternMatches,
                DependencyExtractions = dependencyExtractions,
                DuplicatesRemoved = beforeCount - allClaims.Count,
                AverageConfidence = avgConfidence,
                ClaimsByPredicate = allClaims
                    .GroupBy(c => c.Predicate)
                    .ToDictionary(g => g.Key, g => g.Count())
            },
            FailedSentences = failedSentences
        };
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Claim>> ExtractFromSentenceAsync(
        ParsedSentence sentence,
        IReadOnlyList<LinkedEntity> linkedEntities,
        ClaimExtractionContext context,
        CancellationToken ct = default)
    {
        var extractedClaims = new List<ExtractedClaim>();

        // Get entities within this sentence
        var sentenceEntities = linkedEntities
            .Where(e => e.Mention.StartOffset >= sentence.StartOffset &&
                        e.Mention.EndOffset <= sentence.EndOffset)
            .ToList();

        // Run all extractors
        foreach (var extractor in _extractors)
        {
            try
            {
                var extractorClaims = extractor.Extract(sentence, sentenceEntities, context);
                extractedClaims.AddRange(extractorClaims);

                _logger.LogDebug(
                    "Extractor {Name} found {Count} claims",
                    extractor.Name, extractorClaims.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Extractor {Name} failed on sentence",
                    extractor.Name);
            }
        }

        // Limit claims per sentence
        extractedClaims = extractedClaims
            .OrderByDescending(c => c.RawConfidence)
            .Take(context.MaxClaimsPerSentence)
            .ToList();

        // Convert to Claim records
        var finalClaims = new List<Claim>();

        foreach (var extracted in extractedClaims)
        {
            var claim = await ConvertToClaimAsync(extracted, context, ct);
            if (claim.Confidence >= context.MinConfidence)
            {
                finalClaims.Add(claim);
            }
        }

        return finalClaims;
    }

    /// <inheritdoc/>
    public Task ReloadPatternsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Pattern reload requested (not implemented in mock)");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public ClaimExtractionStats GetStats() => new()
    {
        TotalClaims = _totalClaims,
        TotalSentences = _totalSentences,
        DuplicatesRemoved = _duplicatesRemoved
    };

    /// <summary>
    /// Converts an extracted claim to a final Claim record.
    /// </summary>
    private Task<Claim> ConvertToClaimAsync(
        ExtractedClaim extracted,
        ClaimExtractionContext context,
        CancellationToken ct)
    {
        // Create subject entity (always unresolved for now - entity linking is a separate feature)
        var subject = ClaimEntity.Unresolved(
            extracted.SubjectSpan.Text,
            extracted.SubjectEntity?.Mention.EntityType ?? GuessEntityType(extracted.SubjectSpan.Text),
            extracted.SubjectSpan.StartOffset,
            extracted.SubjectSpan.EndOffset);

        // Resolve object
        ClaimObject claimObject;
        if (extracted.LiteralValue != null)
        {
            // Create a literal object
            claimObject = extracted.LiteralType switch
            {
                "int" when extracted.LiteralValue is int i => ClaimObject.FromInt(i),
                "bool" => ClaimObject.FromBool(extracted.LiteralValue is true or "true"),
                _ => ClaimObject.FromString(extracted.LiteralValue?.ToString() ?? "")
            };
        }
        else
        {
            // Create an entity object (always unresolved for now)
            claimObject = ClaimObject.FromEntity(ClaimEntity.Unresolved(
                extracted.ObjectSpan.Text,
                extracted.ObjectEntity?.Mention.EntityType ?? GuessEntityType(extracted.ObjectSpan.Text),
                extracted.ObjectSpan.StartOffset,
                extracted.ObjectSpan.EndOffset));
        }

        // Score confidence
        var confidence = _confidenceScorer.Score(extracted);

        var claim = new Claim
        {
            Subject = subject,
            Predicate = extracted.Predicate,
            Object = claimObject,
            Confidence = confidence,
            DocumentId = context.DocumentId,
            ProjectId = context.ProjectId,
            Evidence = new ClaimEvidence
            {
                Sentence = extracted.Sentence.Text,
                StartOffset = extracted.Sentence.StartOffset,
                EndOffset = extracted.Sentence.EndOffset,
                ExtractionMethod = extracted.Method,
                PatternId = extracted.PatternId
            }
        };

        return Task.FromResult(claim);
    }

    /// <summary>
    /// Guesses entity type based on text heuristics.
    /// </summary>
    private static string GuessEntityType(string text)
    {
        // Simple heuristics for unknown entities
        if (text.StartsWith("/") || text.Contains(" /"))
            return "Endpoint";
        if (text.StartsWith("GET ") || text.StartsWith("POST ") ||
            text.StartsWith("PUT ") || text.StartsWith("DELETE "))
            return "Endpoint";
        if (text.All(c => char.IsLower(c) || c == '_'))
            return "Parameter";
        if (text.StartsWith("HTTP") || int.TryParse(text, out _))
            return "StatusCode";

        return "Concept";
    }
}
