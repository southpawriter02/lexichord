// =============================================================================
// File: ParameterExtractor.cs
// Project: Lexichord.Modules.Knowledge
// Description: Extracts API parameter mentions from text using regex patterns.
// =============================================================================
// LOGIC: Identifies API parameter mentions in text using five detection
//   strategies with varying confidence levels:
//   1. Explicit definitions ("parameter limit") → confidence 1.0
//   2. Inline code parameters ("`name` parameter") → confidence 0.9
//   3. Path parameters ({name}) → confidence 0.95
//   4. Query parameters (?name=) → confidence 0.85
//   5. JSON property names → confidence 0.6
//
// Deduplication:
//   - Parameters are tracked by name (case-insensitive).
//   - First detection wins (higher-confidence patterns run first).
//
// Meta-property filtering:
//   - Common JSON meta-properties (type, name, id, etc.) are excluded
//     from the JSON property pattern to reduce noise.
//
// v0.4.5g: Entity Abstraction Layer (CKVS Phase 1)
// Dependencies: IEntityExtractor, EntityMention, ExtractionContext (v0.4.5g)
// =============================================================================

using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Knowledge.Extraction.Extractors;

/// <summary>
/// Extracts API parameter mentions from text using regex pattern matching.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ParameterExtractor"/> identifies API parameter mentions in
/// technical documentation and code using five detection strategies. Parameters
/// are identified from path templates, query strings, explicit definitions,
/// inline code references, and JSON property names.
/// </para>
/// <para>
/// <b>Detection Patterns:</b>
/// <list type="number">
///   <item><description><b>Explicit Definition</b> (confidence 1.0): Matches "parameter <c>limit</c>",
///   "param <c>offset</c> (integer)".</description></item>
///   <item><description><b>Path Parameter</b> (confidence 0.95): Matches template variables
///   like <c>{userId}</c>, <c>{orderId}</c>.</description></item>
///   <item><description><b>Inline Code</b> (confidence 0.9): Matches backtick-quoted names
///   followed by "parameter", e.g., <c>`limit` parameter</c>.</description></item>
///   <item><description><b>Query Parameter</b> (confidence 0.85): Matches query string names
///   like <c>?page=1&amp;limit=20</c>.</description></item>
///   <item><description><b>JSON Property</b> (confidence 0.6): Matches property names in
///   JSON-like syntax, filtered for common meta-properties.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5g as part of the Entity Abstraction Layer.
/// </para>
/// </remarks>
internal sealed class ParameterExtractor : IEntityExtractor
{
    /// <inheritdoc/>
    public IReadOnlyList<string> SupportedTypes => new[] { "Parameter" };

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Priority 90 (medium-high). Parameters are often co-located with
    /// endpoints and should be extracted after endpoints but before general
    /// concepts.
    /// </remarks>
    public int Priority => 90;

    // =========================================================================
    // Regex Patterns
    // =========================================================================

    /// <summary>
    /// Pattern 1: Explicit parameter definitions in prose.
    /// Matches: "parameter limit", "param offset (integer)", "argument userId"
    /// </summary>
    private static readonly Regex ParamDefinitionPattern = new(
        @"(?:parameter|param|argument|arg)\s+[`""]?(\w+)[`""]?\s*(?:\((\w+)\))?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Pattern 2: Inline code parameters.
    /// Matches: "`name` parameter", "`limit` param", "`offset` field"
    /// </summary>
    private static readonly Regex InlineCodeParamPattern = new(
        @"`(\w+)`\s+(?:parameter|param|argument|field)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Pattern 3: Path parameters in URI templates.
    /// Matches: {userId}, {orderId}, {id}
    /// </summary>
    private static readonly Regex PathParamPattern = new(
        @"\{(\w+)\}",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Pattern 4: Query parameters in URLs.
    /// Matches: ?page=, &amp;limit=, ?sort=
    /// </summary>
    private static readonly Regex QueryParamPattern = new(
        @"[?&](\w+)=",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Pattern 5: JSON/code property definitions.
    /// Matches: "email": "test@...", 'name': 'value', "count": 42
    /// </summary>
    private static readonly Regex PropertyPattern = new(
        @"[""'](\w+)[""']\s*:\s*(?:[""']?[\w\s]+[""']?|[\d.]+|true|false|null|\{|\[)",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Common meta-properties to exclude from JSON property detection.
    /// </summary>
    /// <remarks>
    /// LOGIC: These are structural JSON properties that rarely represent API
    /// parameters. Filtering them reduces false positives from the JSON
    /// property pattern.
    /// </remarks>
    private static readonly HashSet<string> CommonMetaProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "type", "name", "id", "version", "description", "title",
        "status", "code", "message", "error", "data", "result",
        "success", "count", "total", "items", "value", "key"
    };

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// LOGIC: Runs all five patterns sequentially. Each pattern adds mentions
    /// for parameter names not already seen (case-insensitive deduplication).
    /// Higher-confidence patterns run first, so their detections take precedence.
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

        // LOGIC: Track seen parameter names to prevent duplicates across patterns.
        var seenParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Pattern 1: Explicit definitions (confidence 1.0)
        foreach (Match match in ParamDefinitionPattern.Matches(text))
        {
            var name = match.Groups[1].Value;
            var type = match.Groups[2].Success ? match.Groups[2].Value : null;

            if (seenParams.Add(name))
            {
                mentions.Add(CreateMention(name, type, "body", 1.0f, match, text));
            }
        }

        // Pattern 2: Inline code parameters (confidence 0.9)
        foreach (Match match in InlineCodeParamPattern.Matches(text))
        {
            var name = match.Groups[1].Value;

            if (seenParams.Add(name))
            {
                mentions.Add(CreateMention(name, null, "body", 0.9f, match, text));
            }
        }

        // Pattern 3: Path parameters (confidence 0.95)
        foreach (Match match in PathParamPattern.Matches(text))
        {
            var name = match.Groups[1].Value;

            if (seenParams.Add(name))
            {
                mentions.Add(CreateMention(name, "string", "path", 0.95f, match, text));
            }
        }

        // Pattern 4: Query parameters (confidence 0.85)
        foreach (Match match in QueryParamPattern.Matches(text))
        {
            var name = match.Groups[1].Value;

            if (seenParams.Add(name))
            {
                mentions.Add(CreateMention(name, null, "query", 0.85f, match, text));
            }
        }

        // Pattern 5: JSON properties (confidence 0.6)
        foreach (Match match in PropertyPattern.Matches(text))
        {
            var name = match.Groups[1].Value;

            // LOGIC: Skip common meta-properties that are unlikely to be
            // API parameters (e.g., "type", "id", "version").
            if (CommonMetaProperties.Contains(name))
            {
                continue;
            }

            if (seenParams.Add(name))
            {
                mentions.Add(CreateMention(name, null, "body", 0.6f, match, text));
            }
        }

        return Task.FromResult<IReadOnlyList<EntityMention>>(mentions);
    }

    // =========================================================================
    // Helper Methods
    // =========================================================================

    /// <summary>
    /// Creates an <see cref="EntityMention"/> for a detected parameter.
    /// </summary>
    /// <param name="name">Parameter name.</param>
    /// <param name="type">Parameter data type (if detected), or <c>null</c>.</param>
    /// <param name="location">Parameter location ("path", "query", "body").</param>
    /// <param name="confidence">Confidence score for this detection.</param>
    /// <param name="match">Regex match that detected the parameter.</param>
    /// <param name="text">Source text for snippet extraction.</param>
    /// <returns>A new <see cref="EntityMention"/> for the parameter.</returns>
    private static EntityMention CreateMention(
        string name,
        string? type,
        string location,
        float confidence,
        Match match,
        string text)
    {
        var properties = new Dictionary<string, object>
        {
            ["name"] = name,
            ["location"] = location
        };

        if (!string.IsNullOrEmpty(type))
        {
            properties["type"] = type;
        }

        return new EntityMention
        {
            EntityType = "Parameter",
            Value = name,
            NormalizedValue = name.ToLowerInvariant(),
            StartOffset = match.Index,
            EndOffset = match.Index + match.Length,
            Confidence = confidence,
            Properties = properties,
            SourceSnippet = GetSnippet(text, match.Index, 30)
        };
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
