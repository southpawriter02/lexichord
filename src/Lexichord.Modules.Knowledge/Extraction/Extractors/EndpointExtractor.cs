// =============================================================================
// File: EndpointExtractor.cs
// Project: Lexichord.Modules.Knowledge
// Description: Extracts API endpoint mentions from text using regex patterns.
// =============================================================================
// LOGIC: Identifies API endpoint patterns in text using three detection
//   strategies with decreasing confidence levels:
//   1. HTTP method + path (e.g., "GET /users/{id}") → confidence 1.0
//   2. Code block definitions (e.g., "endpoint: /path") → confidence 0.9
//   3. Standalone paths (e.g., "/users") → confidence 0.7
//
// False positive filtering:
//   - File paths with extensions (e.g., /etc/config.json)
//   - Unix system paths (/home/, /usr/, /etc/, /var/)
//   - Very short paths (< 4 characters)
//
// Path normalization:
//   - Strip query parameters
//   - Ensure leading slash
//   - Remove trailing slash (unless root "/")
//   - Lowercase for canonical form
//
// v0.4.5g: Entity Abstraction Layer (CKVS Phase 1)
// Dependencies: IEntityExtractor, EntityMention, ExtractionContext (v0.4.5g)
// =============================================================================

using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Knowledge.Extraction.Extractors;

/// <summary>
/// Extracts API endpoint mentions from text using regex pattern matching.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="EndpointExtractor"/> identifies API endpoint patterns in
/// technical documentation and code. It uses three detection strategies with
/// decreasing confidence levels to balance precision and recall.
/// </para>
/// <para>
/// <b>Detection Patterns:</b>
/// <list type="number">
///   <item><description><b>Method + Path</b> (confidence 1.0): Matches explicit HTTP method and path
///   combinations like <c>GET /users/{id}</c>, <c>POST /api/v1/orders</c>.</description></item>
///   <item><description><b>Code Block Definition</b> (confidence 0.9): Matches endpoint definitions
///   in code-like syntax such as <c>endpoint: /api/orders</c>, <c>url = "/users"</c>.</description></item>
///   <item><description><b>Standalone Path</b> (confidence 0.7): Matches standalone API paths
///   like <c>/users</c> when they appear without an HTTP method.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>False Positive Filtering:</b> The extractor skips file paths (containing
/// extensions like <c>.json</c>), Unix system paths (<c>/home/</c>, <c>/usr/</c>),
/// and very short paths (fewer than 4 characters) that are unlikely to be API
/// endpoints.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5g as part of the Entity Abstraction Layer.
/// </para>
/// </remarks>
internal sealed class EndpointExtractor : IEntityExtractor
{
    /// <inheritdoc/>
    public IReadOnlyList<string> SupportedTypes => new[] { "Endpoint" };

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Priority 100 (high). Endpoints are the most specific entity type
    /// and should be detected first to prevent other extractors from claiming
    /// the same text spans.
    /// </remarks>
    public int Priority => 100;

    // =========================================================================
    // Regex Patterns
    // =========================================================================

    /// <summary>
    /// Pattern 1: HTTP method + path (highest confidence).
    /// Matches: GET /users, POST /api/v1/orders, DELETE /items/{id}
    /// </summary>
    private static readonly Regex MethodPathPattern = new(
        @"\b(GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS)\s+(\/[\w\-\/\{\}\?=&]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Pattern 2: Code block endpoint definitions (high confidence).
    /// Matches: endpoint: /path, url = "/path", route: /path
    /// </summary>
    private static readonly Regex CodeBlockPathPattern = new(
        @"(?:endpoint|url|path|route)\s*[=:]\s*[""']?(\/[\w\-\/\{\}]+)[""']?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Pattern 3: Standalone paths (medium confidence).
    /// Matches: /users, /api/v1/orders, /items/{id}
    /// Negative lookbehind prevents matching after word characters or hyphens.
    /// </summary>
    private static readonly Regex PathOnlyPattern = new(
        @"(?<![\w\-])(\/[\w\-]+(?:\/[\w\-\{\}]+)*)\b",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Pattern for detecting file extensions (false positive filter).
    /// </summary>
    private static readonly Regex FileExtensionPattern = new(
        @"\.\w{2,4}$",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(50));

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// LOGIC: Runs all three patterns in priority order. Each pattern adds
    /// mentions that are not already covered by a higher-confidence pattern
    /// (checked via path deduplication). False positives are filtered for
    /// standalone paths.
    /// </para>
    /// </remarks>
    public Task<IReadOnlyList<EntityMention>> ExtractAsync(
        string text,
        ExtractionContext context,
        CancellationToken ct = default)
    {
        // LOGIC: Guard against null/empty input — return empty list immediately.
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult<IReadOnlyList<EntityMention>>(Array.Empty<EntityMention>());
        }

        var mentions = new List<EntityMention>();

        // LOGIC: Track seen paths to prevent duplicate mentions across patterns.
        // A path found by a higher-confidence pattern should not be re-reported
        // by a lower-confidence pattern.
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Pattern 1: HTTP method + path (confidence 1.0)
        ExtractMethodPathMentions(text, mentions, seenPaths);

        // Pattern 2: Code block definitions (confidence 0.9)
        ExtractCodeBlockPathMentions(text, mentions, seenPaths);

        // Pattern 3: Standalone paths (confidence 0.7)
        ExtractStandalonePathMentions(text, mentions, seenPaths);

        return Task.FromResult<IReadOnlyList<EntityMention>>(mentions);
    }

    // =========================================================================
    // Pattern Extraction Methods
    // =========================================================================

    /// <summary>
    /// Extracts endpoint mentions matching the HTTP method + path pattern.
    /// </summary>
    /// <param name="text">Source text to analyze.</param>
    /// <param name="mentions">List to add mentions to.</param>
    /// <param name="seenPaths">Set of already-seen paths for deduplication.</param>
    private static void ExtractMethodPathMentions(
        string text,
        List<EntityMention> mentions,
        HashSet<string> seenPaths)
    {
        foreach (Match match in MethodPathPattern.Matches(text))
        {
            var method = match.Groups[1].Value.ToUpperInvariant();
            var path = NormalizePath(match.Groups[2].Value);

            // LOGIC: Track the path to prevent lower-confidence patterns
            // from re-detecting the same endpoint.
            seenPaths.Add(path);

            mentions.Add(new EntityMention
            {
                EntityType = "Endpoint",
                Value = $"{method} {path}",
                NormalizedValue = path,
                StartOffset = match.Index,
                EndOffset = match.Index + match.Length,
                Confidence = 1.0f,
                Properties = new()
                {
                    ["method"] = method,
                    ["path"] = path
                },
                SourceSnippet = GetSnippet(text, match.Index, 50)
            });
        }
    }

    /// <summary>
    /// Extracts endpoint mentions from code block definitions.
    /// </summary>
    /// <param name="text">Source text to analyze.</param>
    /// <param name="mentions">List to add mentions to.</param>
    /// <param name="seenPaths">Set of already-seen paths for deduplication.</param>
    private static void ExtractCodeBlockPathMentions(
        string text,
        List<EntityMention> mentions,
        HashSet<string> seenPaths)
    {
        foreach (Match match in CodeBlockPathPattern.Matches(text))
        {
            var path = NormalizePath(match.Groups[1].Value);

            // LOGIC: Skip paths already found by method+path pattern.
            if (!seenPaths.Add(path))
            {
                continue;
            }

            mentions.Add(new EntityMention
            {
                EntityType = "Endpoint",
                Value = path,
                NormalizedValue = path,
                StartOffset = match.Index,
                EndOffset = match.Index + match.Length,
                Confidence = 0.9f,
                Properties = new() { ["path"] = path },
                SourceSnippet = GetSnippet(text, match.Index, 50)
            });
        }
    }

    /// <summary>
    /// Extracts endpoint mentions from standalone paths.
    /// </summary>
    /// <param name="text">Source text to analyze.</param>
    /// <param name="mentions">List to add mentions to.</param>
    /// <param name="seenPaths">Set of already-seen paths for deduplication.</param>
    private static void ExtractStandalonePathMentions(
        string text,
        List<EntityMention> mentions,
        HashSet<string> seenPaths)
    {
        foreach (Match match in PathOnlyPattern.Matches(text))
        {
            var path = NormalizePath(match.Groups[1].Value);

            // LOGIC: Skip paths already found by higher-confidence patterns.
            if (!seenPaths.Add(path))
            {
                continue;
            }

            // LOGIC: Apply false positive filtering for standalone paths,
            // which are more prone to false matches than method+path patterns.
            if (IsLikelyFalsePositive(path))
            {
                continue;
            }

            mentions.Add(new EntityMention
            {
                EntityType = "Endpoint",
                Value = path,
                NormalizedValue = path,
                StartOffset = match.Index,
                EndOffset = match.Index + match.Length,
                Confidence = 0.7f,
                Properties = new() { ["path"] = path },
                SourceSnippet = GetSnippet(text, match.Index, 50)
            });
        }
    }

    // =========================================================================
    // Helper Methods
    // =========================================================================

    /// <summary>
    /// Normalizes an API path to a canonical form.
    /// </summary>
    /// <param name="path">Raw path string to normalize.</param>
    /// <returns>Normalized path (lowercase, no query params, no trailing slash).</returns>
    /// <remarks>
    /// LOGIC: Normalization steps:
    /// 1. Strip query parameters (everything after '?').
    /// 2. Ensure leading slash.
    /// 3. Remove trailing slash (unless root "/").
    /// 4. Lowercase for case-insensitive deduplication.
    /// </remarks>
    internal static string NormalizePath(string path)
    {
        // Strip query parameters for canonical form.
        var queryIndex = path.IndexOf('?');
        if (queryIndex > 0)
        {
            path = path[..queryIndex];
        }

        // Ensure leading slash.
        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        // Remove trailing slash (unless root "/").
        if (path.Length > 1 && path.EndsWith('/'))
        {
            path = path[..^1];
        }

        return path.ToLowerInvariant();
    }

    /// <summary>
    /// Checks whether a standalone path is likely a false positive.
    /// </summary>
    /// <param name="path">Normalized path to check.</param>
    /// <returns><c>true</c> if the path should be skipped; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// LOGIC: False positive heuristics:
    /// - File paths with extensions (e.g., /etc/config.json)
    /// - Unix system paths (/home/, /usr/, /etc/, /var/)
    /// - Very short paths (fewer than 4 characters, e.g., "/a")
    /// </remarks>
    internal static bool IsLikelyFalsePositive(string path)
    {
        // Skip file paths (contain file extensions).
        if (FileExtensionPattern.IsMatch(path))
        {
            return true;
        }

        // Skip Unix system paths.
        if (path.StartsWith("/home/") ||
            path.StartsWith("/usr/") ||
            path.StartsWith("/etc/") ||
            path.StartsWith("/var/") ||
            path.StartsWith("/tmp/") ||
            path.StartsWith("/bin/") ||
            path.StartsWith("/opt/"))
        {
            return true;
        }

        // Skip very short paths (likely false positives).
        if (path.Length < 4)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets a text snippet around a position for context display.
    /// </summary>
    /// <param name="text">Full source text.</param>
    /// <param name="position">Center position for the snippet.</param>
    /// <param name="contextLength">Number of characters before and after to include.</param>
    /// <returns>A trimmed text snippet with newlines replaced by spaces.</returns>
    internal static string GetSnippet(string text, int position, int contextLength)
    {
        var start = Math.Max(0, position - contextLength);
        var end = Math.Min(text.Length, position + contextLength);
        return text[start..end].Replace("\n", " ").Trim();
    }
}
