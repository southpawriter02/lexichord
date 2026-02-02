// =============================================================================
// File: ConceptExtractor.cs
// Project: Lexichord.Modules.Knowledge
// Description: Extracts domain concept mentions from text using regex patterns.
// =============================================================================
// LOGIC: Identifies domain concept mentions in text using four detection
//   strategies with varying confidence levels:
//   1. Explicitly defined terms ("called Rate Limiting") → confidence 0.9
//   2. Acronyms with definitions ("API (Application...)") → confidence 0.95
//   3. Glossary-style entries ("**Term**: definition") → confidence 0.85
//   4. Capitalized multi-word terms (discovery mode only) → confidence 0.5
//
// Filtering:
//   - Common English words are excluded (The, This, Note, etc.)
//   - Common phrases are excluded (For Example, In Addition, etc.)
//   - All-uppercase terms without definitions are excluded
//   - Terms shorter than 3 characters are excluded
//
// v0.4.5g: Entity Abstraction Layer (CKVS Phase 1)
// Dependencies: IEntityExtractor, EntityMention, ExtractionContext (v0.4.5g)
// =============================================================================

using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Knowledge.Extraction.Extractors;

/// <summary>
/// Extracts domain concept mentions from text using regex pattern matching.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ConceptExtractor"/> identifies domain-specific terms,
/// definitions, and acronyms in technical documentation. Unlike the
/// <see cref="EndpointExtractor"/> and <see cref="ParameterExtractor"/>,
/// concepts are less structured and require more heuristic filtering to
/// avoid false positives.
/// </para>
/// <para>
/// <b>Detection Patterns:</b>
/// <list type="number">
///   <item><description><b>Defined Terms</b> (confidence 0.9): Matches terms explicitly
///   introduced with phrases like "called", "known as", "termed".</description></item>
///   <item><description><b>Acronyms</b> (confidence 0.95): Matches uppercase abbreviations
///   followed by parenthesized definitions like <c>API (Application Programming Interface)</c>.</description></item>
///   <item><description><b>Glossary Entries</b> (confidence 0.85): Matches definition-style
///   lines like <c>**Rate Limiting**: Controls request frequency</c>.</description></item>
///   <item><description><b>Capitalized Terms</b> (confidence 0.5, discovery mode only): Matches
///   multi-word capitalized terms like <c>Service Mesh</c>.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5g as part of the Entity Abstraction Layer.
/// </para>
/// </remarks>
internal sealed class ConceptExtractor : IEntityExtractor
{
    /// <inheritdoc/>
    public IReadOnlyList<string> SupportedTypes => new[] { "Concept" };

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Priority 50 (medium). Concepts are the most general entity type
    /// and should run after specific extractors (endpoints, parameters) to
    /// avoid claiming text that could be more precisely classified.
    /// </remarks>
    public int Priority => 50;

    // =========================================================================
    // Regex Patterns
    // =========================================================================

    /// <summary>
    /// Pattern 1: Explicitly defined terms in prose.
    /// Matches: 'called "Rate Limiting"', 'known as OAuth', 'termed Idempotent'
    /// </summary>
    private static readonly Regex DefinedTermPattern = new(
        @"(?:called|known as|referred to as|termed|defined as)\s+[""*_]?([A-Z][\w\s-]+)[""*_]?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Pattern 2: Acronyms with parenthesized definitions.
    /// Matches: API (Application Programming Interface), REST (Representational State Transfer)
    /// </summary>
    private static readonly Regex AcronymPattern = new(
        @"\b([A-Z]{2,6})\s*\(([^)]+)\)",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Pattern 3: Glossary-style definition entries (multiline).
    /// Matches: **Rate Limiting** - Controls request frequency
    /// Matches: *OAuth*: Authorization protocol
    /// </summary>
    private static readonly Regex GlossaryPattern = new(
        @"^\s*\*?\*?([A-Z][\w\s-]+)\*?\*?\s*[-:]\s+(.+)$",
        RegexOptions.Compiled | RegexOptions.Multiline,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Pattern 4: Capitalized multi-word terms (discovery mode only).
    /// Matches: Service Mesh, Rate Limiting, Access Control
    /// </summary>
    private static readonly Regex CapitalizedTermPattern = new(
        @"\b([A-Z][a-z]+(?:\s+[A-Z][a-z]+)+)\b",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Common English words that should not be treated as concepts.
    /// </summary>
    private static readonly HashSet<string> CommonWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "The", "This", "That", "These", "Those", "Here", "There",
        "What", "When", "Where", "Which", "Who", "How", "Why",
        "Note", "Example", "See", "Also", "Next", "Previous"
    };

    /// <summary>
    /// Common phrases that should not be treated as concepts.
    /// </summary>
    private static readonly HashSet<string> CommonPhrases = new(StringComparer.OrdinalIgnoreCase)
    {
        "For Example", "In Addition", "As Well", "On The Other",
        "New York", "Los Angeles", "San Francisco", "United States"
    };

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// LOGIC: Runs patterns in order of confidence. Capitalized multi-word
    /// terms are only extracted when <see cref="ExtractionContext.DiscoveryMode"/>
    /// is enabled, since they have high false positive rates. All patterns
    /// deduplicate by term name (case-insensitive).
    /// </para>
    /// </remarks>
    public Task<IReadOnlyList<EntityMention>> ExtractAsync(
        string text,
        ExtractionContext context,
        CancellationToken ct = default)
    {
        // LOGIC: Guard against null/empty input.
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult<IReadOnlyList<EntityMention>>(Array.Empty<EntityMention>());
        }

        var mentions = new List<EntityMention>();

        // LOGIC: Track seen concepts to prevent duplicates across patterns.
        var seenConcepts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Pattern 1: Explicitly defined terms (confidence 0.9)
        foreach (Match match in DefinedTermPattern.Matches(text))
        {
            var term = match.Groups[1].Value.Trim();

            if (IsValidConcept(term) && seenConcepts.Add(term))
            {
                mentions.Add(CreateMention(term, null, 0.9f, match, text));
            }
        }

        // Pattern 2: Acronyms with definitions (confidence 0.95)
        foreach (Match match in AcronymPattern.Matches(text))
        {
            var acronym = match.Groups[1].Value;
            var definition = match.Groups[2].Value.Trim();

            if (seenConcepts.Add(acronym))
            {
                mentions.Add(CreateMention(acronym, definition, 0.95f, match, text));
            }
        }

        // Pattern 3: Glossary-style definitions (confidence 0.85)
        foreach (Match match in GlossaryPattern.Matches(text))
        {
            var term = match.Groups[1].Value.Trim();
            var definition = match.Groups[2].Value.Trim();

            if (IsValidConcept(term) && seenConcepts.Add(term))
            {
                mentions.Add(CreateMention(term, definition, 0.85f, match, text));
            }
        }

        // Pattern 4: Capitalized multi-word terms (discovery mode only, confidence 0.5)
        // LOGIC: Only enabled in discovery mode due to high false positive rates.
        // This pattern catches domain terms like "Service Mesh" or "Rate Limiting"
        // that aren't explicitly defined but are capitalized consistently.
        if (context.DiscoveryMode)
        {
            foreach (Match match in CapitalizedTermPattern.Matches(text))
            {
                var term = match.Groups[1].Value.Trim();

                if (IsValidConcept(term) &&
                    !CommonPhrases.Contains(term) &&
                    seenConcepts.Add(term))
                {
                    mentions.Add(CreateMention(term, null, 0.5f, match, text));
                }
            }
        }

        return Task.FromResult<IReadOnlyList<EntityMention>>(mentions);
    }

    // =========================================================================
    // Helper Methods
    // =========================================================================

    /// <summary>
    /// Creates an <see cref="EntityMention"/> for a detected concept.
    /// </summary>
    /// <param name="term">Concept term or acronym.</param>
    /// <param name="definition">Concept definition (if available), or <c>null</c>.</param>
    /// <param name="confidence">Confidence score for this detection.</param>
    /// <param name="match">Regex match that detected the concept.</param>
    /// <param name="text">Source text for snippet extraction.</param>
    /// <returns>A new <see cref="EntityMention"/> for the concept.</returns>
    private static EntityMention CreateMention(
        string term,
        string? definition,
        float confidence,
        Match match,
        string text)
    {
        var properties = new Dictionary<string, object>
        {
            ["name"] = term
        };

        if (!string.IsNullOrEmpty(definition))
        {
            properties["definition"] = definition;
        }

        return new EntityMention
        {
            EntityType = "Concept",
            Value = term,
            NormalizedValue = term.ToLowerInvariant(),
            StartOffset = match.Index,
            EndOffset = match.Index + match.Length,
            Confidence = confidence,
            Properties = properties,
            SourceSnippet = GetSnippet(text, match.Index, 50)
        };
    }

    /// <summary>
    /// Validates whether a term is a valid concept candidate.
    /// </summary>
    /// <param name="term">Term to validate.</param>
    /// <returns><c>true</c> if the term is a valid concept; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// LOGIC: A valid concept must be:
    /// - At least 3 characters long.
    /// - Not all uppercase (likely an acronym without definition).
    /// - Not a common English word.
    /// </remarks>
    internal static bool IsValidConcept(string term)
    {
        // Must be at least 3 characters.
        if (term.Length < 3)
        {
            return false;
        }

        // Must not be all uppercase (likely acronym without definition).
        // Acronyms are handled separately by AcronymPattern.
        if (term.All(c => char.IsUpper(c) || !char.IsLetter(c)))
        {
            return false;
        }

        // Must not be a common word.
        return !CommonWords.Contains(term);
    }

    /// <summary>
    /// Gets a text snippet around a position for context display.
    /// </summary>
    /// <param name="text">Full source text.</param>
    /// <param name="position">Center position for the snippet.</param>
    /// <param name="contextLength">Number of characters before and after to include.</param>
    /// <returns>A trimmed text snippet with newlines replaced by spaces.</returns>
    private static string GetSnippet(string text, int position, int contextLength)
    {
        var start = Math.Max(0, position - contextLength);
        var end = Math.Min(text.Length, position + contextLength);
        return text[start..end].Replace("\n", " ").Trim();
    }
}
