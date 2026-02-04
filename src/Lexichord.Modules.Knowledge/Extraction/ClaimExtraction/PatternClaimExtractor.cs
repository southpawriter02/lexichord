// =============================================================================
// File: PatternClaimExtractor.cs
// Project: Lexichord.Modules.Knowledge
// Description: Extracts claims using configurable patterns.
// =============================================================================
// LOGIC: Matches text against regex and template patterns to extract claims.
//   Supports named capture groups for subject and object identification.
//
// v0.5.6g: Claim Extractor (Knowledge Graph Claim Extraction Pipeline)
// =============================================================================

using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts.Knowledge.ClaimExtraction;
using Lexichord.Abstractions.Contracts.Knowledge.Claims;
using Lexichord.Abstractions.Contracts.Knowledge.Parsing;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Extraction.ClaimExtraction;

/// <summary>
/// Extracts claims using configurable patterns.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Uses regex and template patterns to identify claim
/// structures in text. Supports {SUBJECT} and {OBJECT} placeholders
/// that are converted to named capture groups.
/// </para>
/// <para>
/// <b>Pattern Types Supported:</b>
/// <list type="bullet">
/// <item><description>Regex: Full regex with named groups.</description></item>
/// <item><description>Template: Simple patterns with placeholders.</description></item>
/// </list>
/// </para>
/// </remarks>
public class PatternClaimExtractor : IClaimExtractor
{
    private readonly List<CompiledPattern> _patterns = new();
    private readonly ILogger<PatternClaimExtractor> _logger;

    /// <inheritdoc/>
    public string Name => "PatternExtractor";

    /// <summary>
    /// Initializes a new instance of the <see cref="PatternClaimExtractor"/> class.
    /// </summary>
    /// <param name="patterns">The patterns to use for extraction.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public PatternClaimExtractor(
        IEnumerable<ExtractionPattern> patterns,
        ILogger<PatternClaimExtractor> logger)
    {
        _logger = logger;

        foreach (var pattern in patterns
            .Where(p => p.IsEnabled && p.Type != PatternType.Dependency)
            .OrderByDescending(p => p.Priority))
        {
            try
            {
                _patterns.Add(new CompiledPattern(pattern));
                _logger.LogDebug("Compiled pattern: {PatternId} ({Type})", pattern.Id, pattern.Type);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to compile pattern {PatternId}", pattern.Id);
            }
        }

        _logger.LogInformation("PatternClaimExtractor initialized with {Count} patterns", _patterns.Count);
    }

    /// <inheritdoc/>
    public IReadOnlyList<ExtractedClaim> Extract(
        ParsedSentence sentence,
        IReadOnlyList<LinkedEntity> entities,
        ClaimExtractionContext context)
    {
        if (!context.UsePatterns)
        {
            return Array.Empty<ExtractedClaim>();
        }

        var claims = new List<ExtractedClaim>();

        foreach (var compiledPattern in _patterns)
        {
            // Check predicate filter
            if (context.PredicateFilter != null &&
                !context.PredicateFilter.Contains(compiledPattern.Pattern.Predicate))
            {
                continue;
            }

            var matches = compiledPattern.Match(sentence.Text);

            foreach (var match in matches)
            {
                // Find matching entities for subject and object
                var subjectEntity = FindEntityInSpan(entities, match.SubjectSpan, sentence);
                var objectEntity = compiledPattern.Pattern.ObjectIsLiteral
                    ? null
                    : FindEntityInSpan(entities, match.ObjectSpan, sentence);

                // Parse literal value if needed
                object? literalValue = null;
                if (compiledPattern.Pattern.ObjectIsLiteral)
                {
                    literalValue = ParseLiteral(
                        match.ObjectSpan.Text,
                        compiledPattern.Pattern.LiteralType);
                }

                claims.Add(new ExtractedClaim
                {
                    SubjectSpan = match.SubjectSpan,
                    ObjectSpan = match.ObjectSpan,
                    Predicate = compiledPattern.Pattern.Predicate,
                    Method = ClaimExtractionMethod.PatternRule,
                    PatternId = compiledPattern.Pattern.Id,
                    RawConfidence = compiledPattern.Pattern.BaseConfidence,
                    Sentence = sentence,
                    SubjectEntity = subjectEntity,
                    ObjectEntity = objectEntity,
                    LiteralValue = literalValue,
                    LiteralType = compiledPattern.Pattern.LiteralType
                });

                _logger.LogDebug(
                    "Pattern {PatternId} matched: {Subject} {Predicate} {Object}",
                    compiledPattern.Pattern.Id,
                    match.SubjectSpan.Text,
                    compiledPattern.Pattern.Predicate,
                    match.ObjectSpan.Text);
            }
        }

        return claims;
    }

    /// <summary>
    /// Finds a linked entity that overlaps with the given span.
    /// </summary>
    private LinkedEntity? FindEntityInSpan(
        IReadOnlyList<LinkedEntity> entities,
        TextSpan span,
        ParsedSentence sentence)
    {
        var absoluteStart = sentence.StartOffset + span.StartOffset;
        var absoluteEnd = sentence.StartOffset + span.EndOffset;

        return entities.FirstOrDefault(e =>
            e.Mention.StartOffset >= absoluteStart &&
            e.Mention.EndOffset <= absoluteEnd);
    }

    /// <summary>
    /// Parses a literal value to the specified type.
    /// </summary>
    private object? ParseLiteral(string text, string? literalType)
    {
        text = text.Trim();

        return literalType?.ToLowerInvariant() switch
        {
            "int" when int.TryParse(text, out var i) => i,
            "float" when float.TryParse(text, out var f) => f,
            "bool" => text.ToLowerInvariant() is "true" or "yes" or "deprecated",
            _ => text
        };
    }

    /// <summary>
    /// A compiled pattern ready for matching.
    /// </summary>
    private class CompiledPattern
    {
        public ExtractionPattern Pattern { get; }
        private readonly Regex? _regex;

        public CompiledPattern(ExtractionPattern pattern)
        {
            Pattern = pattern;

            if (pattern.Type == PatternType.Regex && pattern.RegexPattern != null)
            {
                _regex = new Regex(
                    pattern.RegexPattern,
                    RegexOptions.IgnoreCase | RegexOptions.Compiled,
                    TimeSpan.FromMilliseconds(100));
            }
            else if (pattern.Type == PatternType.Template && pattern.Template != null)
            {
                // Convert template to regex
                var regexPattern = Regex.Escape(pattern.Template)
                    .Replace("\\{SUBJECT\\}", "(?<subject>.+?)")
                    .Replace("\\{OBJECT\\}", "(?<object>.+?)");
                _regex = new Regex(
                    regexPattern,
                    RegexOptions.IgnoreCase | RegexOptions.Compiled,
                    TimeSpan.FromMilliseconds(100));
            }
        }

        public IEnumerable<PatternMatch> Match(string text)
        {
            if (_regex == null) yield break;

            MatchCollection matches;
            try
            {
                matches = _regex.Matches(text);
            }
            catch (RegexMatchTimeoutException)
            {
                yield break;
            }

            foreach (Match match in matches)
            {
                var subjectGroup = match.Groups["subject"];
                var objectGroup = match.Groups["object"];

                if (subjectGroup.Success && objectGroup.Success)
                {
                    yield return new PatternMatch
                    {
                        SubjectSpan = new TextSpan
                        {
                            Text = subjectGroup.Value.Trim(),
                            StartOffset = subjectGroup.Index,
                            EndOffset = subjectGroup.Index + subjectGroup.Length
                        },
                        ObjectSpan = new TextSpan
                        {
                            Text = objectGroup.Value.Trim(),
                            StartOffset = objectGroup.Index,
                            EndOffset = objectGroup.Index + objectGroup.Length
                        }
                    };
                }
            }
        }
    }

    /// <summary>
    /// A pattern match result.
    /// </summary>
    private record PatternMatch
    {
        public required TextSpan SubjectSpan { get; init; }
        public required TextSpan ObjectSpan { get; init; }
    }
}
