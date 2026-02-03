// =============================================================================
// File: SnippetService.cs
// Project: Lexichord.Modules.RAG
// Description: Service for extracting contextual snippets from text chunks.
// =============================================================================
// LOGIC: Implements snippet extraction with query highlighting.
//   - Uses IQueryAnalyzer to extract keywords from queries.
//   - Finds exact and fuzzy matches in chunk content.
//   - Calculates match density to center snippets on relevant regions.
//   - Respects sentence boundaries when configured.
//   - Merges overlapping highlights for clean rendering.
// =============================================================================
// VERSION: v0.5.6a (Snippet Extraction)
// DEPENDENCIES:
//   - IQueryAnalyzer (v0.5.4a)
//   - ISentenceBoundaryDetector (v0.5.6c / placeholder)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Extracts contextual snippets from text chunks with query highlighting.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SnippetService"/> implements <see cref="ISnippetService"/> to provide
/// snippet extraction for the Answer Preview feature. Snippets are centered on
/// regions with the highest density of query matches.
/// </para>
/// <para>
/// <b>Algorithm Overview:</b>
/// <list type="number">
///   <item><description>Extract keywords from query via <see cref="IQueryAnalyzer"/></description></item>
///   <item><description>Find all keyword matches in chunk content</description></item>
///   <item><description>Calculate match density across a sliding window</description></item>
///   <item><description>Center snippet on highest density region</description></item>
///   <item><description>Expand to sentence boundaries if configured</description></item>
///   <item><description>Apply max length constraints and build highlights</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> This implementation is thread-safe as it uses only
/// local variables and injected stateless dependencies.
/// </para>
/// <para>
/// <b>Performance:</b> Single snippet extraction typically completes in &lt; 5ms
/// for content under 10KB.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6a as part of The Answer Preview feature.
/// </para>
/// </remarks>
public sealed class SnippetService : ISnippetService
{
    private readonly IQueryAnalyzer _queryAnalyzer;
    private readonly ISentenceBoundaryDetector _sentenceDetector;
    private readonly ILogger<SnippetService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SnippetService"/>.
    /// </summary>
    /// <param name="queryAnalyzer">Analyzer for extracting keywords from queries.</param>
    /// <param name="sentenceDetector">Detector for finding sentence boundaries.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is <c>null</c>.
    /// </exception>
    public SnippetService(
        IQueryAnalyzer queryAnalyzer,
        ISentenceBoundaryDetector sentenceDetector,
        ILogger<SnippetService> logger)
    {
        _queryAnalyzer = queryAnalyzer ?? throw new ArgumentNullException(nameof(queryAnalyzer));
        _sentenceDetector = sentenceDetector ?? throw new ArgumentNullException(nameof(sentenceDetector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Snippet ExtractSnippet(TextChunk chunk, string query, SnippetOptions options)
    {
        var content = chunk.Content;

        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogDebug("Empty chunk content, returning Snippet.Empty");
            return Snippet.Empty;
        }

        var keywords = _queryAnalyzer.Analyze(query).Keywords;
        var matches = FindMatches(content, keywords, options);

        _logger.LogDebug(
            "Extracting snippet: {MatchCount} matches found for {KeywordCount} keywords",
            matches.Count, keywords.Count);

        if (matches.Count == 0)
        {
            return CreateFallbackSnippet(content, options);
        }

        return ExtractSnippetFromMatches(content, matches, options);
    }

    /// <inheritdoc />
    public IReadOnlyList<Snippet> ExtractMultipleSnippets(
        TextChunk chunk,
        string query,
        SnippetOptions options,
        int maxSnippets = 3)
    {
        var content = chunk.Content;

        if (string.IsNullOrWhiteSpace(content))
        {
            return new[] { Snippet.Empty };
        }

        var keywords = _queryAnalyzer.Analyze(query).Keywords;
        var matches = FindMatches(content, keywords, options);

        if (matches.Count == 0)
        {
            return new[] { CreateFallbackSnippet(content, options) };
        }

        // LOGIC: Cluster matches by proximity and extract snippets from top clusters.
        var clusters = ClusterMatches(matches, clusterThreshold: 100);
        var snippets = new List<Snippet>();
        var usedRanges = new List<(int Start, int End)>();

        foreach (var cluster in clusters
            .OrderByDescending(c => c.Matches.Count)
            .Take(maxSnippets))
        {
            var snippet = ExtractSnippetFromMatches(content, cluster.Matches, options);

            // LOGIC: Check for overlap with existing snippets.
            var range = (snippet.StartOffset, snippet.StartOffset + snippet.Length);
            if (!usedRanges.Any(r => Overlaps(r, range)))
            {
                snippets.Add(snippet);
                usedRanges.Add(range);
            }
        }

        _logger.LogDebug(
            "Extracted {SnippetCount} non-overlapping snippets from {ClusterCount} clusters",
            snippets.Count, clusters.Count);

        return snippets;
    }

    /// <inheritdoc />
    public IDictionary<Guid, Snippet> ExtractBatch(
        IEnumerable<TextChunk> chunks,
        string query,
        SnippetOptions options)
    {
        // LOGIC: Analyze query once and apply to all chunks.
        var keywords = _queryAnalyzer.Analyze(query).Keywords;
        var result = new Dictionary<Guid, Snippet>();

        foreach (var chunk in chunks)
        {
            // LOGIC: Generate a deterministic ID from chunk content and index.
            // This provides a stable key for deduplication and caching.
            var chunkId = GenerateDeterministicId(chunk);

            if (string.IsNullOrWhiteSpace(chunk.Content))
            {
                result[chunkId] = Snippet.Empty;
                continue;
            }

            var matches = FindMatches(chunk.Content, keywords, options);
            var snippet = matches.Count > 0
                ? ExtractSnippetFromMatches(chunk.Content, matches, options)
                : CreateFallbackSnippet(chunk.Content, options);

            result[chunkId] = snippet;
        }

        _logger.LogDebug("Batch extraction completed: {ChunkCount} chunks processed", result.Count);

        return result;
    }

    /// <summary>
    /// Generates a deterministic GUID from chunk content and metadata.
    /// </summary>
    private static Guid GenerateDeterministicId(TextChunk chunk)
    {
        // LOGIC: Create a stable ID from content hash + position.
        // This enables consistent keying across calls without external ID infrastructure.
        var hash = chunk.Content.GetHashCode() ^ chunk.StartOffset ^ chunk.EndOffset ^ chunk.Metadata.Index;
        var bytes = new byte[16];
        BitConverter.GetBytes(hash).CopyTo(bytes, 0);
        BitConverter.GetBytes(chunk.StartOffset).CopyTo(bytes, 4);
        BitConverter.GetBytes(chunk.EndOffset).CopyTo(bytes, 8);
        BitConverter.GetBytes(chunk.Metadata.Index).CopyTo(bytes, 12);
        return new Guid(bytes);
    }

    #region Private Methods

    /// <summary>
    /// Finds all keyword matches in content.
    /// </summary>
    private List<MatchInfo> FindMatches(
        string content,
        IReadOnlyList<string> keywords,
        SnippetOptions options)
    {
        var matches = new List<MatchInfo>();

        foreach (var keyword in keywords.Where(k => k.Length >= options.MinMatchLength))
        {
            // LOGIC: Find all exact matches (case-insensitive).
            var index = 0;
            while ((index = content.IndexOf(keyword, index, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                matches.Add(new MatchInfo(index, keyword.Length, HighlightType.QueryMatch));
                index += keyword.Length;
            }

            // LOGIC: Find fuzzy matches if enabled.
            if (options.IncludeFuzzyMatches)
            {
                matches.AddRange(FindFuzzyMatches(content, keyword));
            }
        }

        return MergeOverlappingMatches(matches.OrderBy(m => m.Position).ToList());
    }

    /// <summary>
    /// Finds fuzzy matches for a keyword (basic implementation).
    /// </summary>
    private static IEnumerable<MatchInfo> FindFuzzyMatches(string content, string keyword)
    {
        // LOGIC: Simple plural/suffix matching as a basic fuzzy implementation.
        // Full fuzzy matching with edit distance will be enhanced in future versions.
        var variants = new[]
        {
            keyword + "s",
            keyword + "es",
            keyword + "ing",
            keyword + "ed"
        };

        foreach (var variant in variants.Where(v => v.Length >= 3))
        {
            var index = 0;
            while ((index = content.IndexOf(variant, index, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                yield return new MatchInfo(index, variant.Length, HighlightType.FuzzyMatch);
                index += variant.Length;
            }
        }
    }

    /// <summary>
    /// Merges overlapping matches, preferring more specific types.
    /// </summary>
    private static List<MatchInfo> MergeOverlappingMatches(List<MatchInfo> sorted)
    {
        if (sorted.Count <= 1) return sorted;

        var result = new List<MatchInfo> { sorted[0] };

        for (var i = 1; i < sorted.Count; i++)
        {
            var last = result[^1];
            var current = sorted[i];

            if (current.Position < last.Position + last.Length)
            {
                // LOGIC: Overlapping - merge and prefer more specific type.
                var newEnd = Math.Max(last.Position + last.Length, current.Position + current.Length);
                var newType = last.Type < current.Type ? last.Type : current.Type;
                result[^1] = new MatchInfo(last.Position, newEnd - last.Position, newType);
            }
            else
            {
                result.Add(current);
            }
        }

        return result;
    }

    /// <summary>
    /// Extracts a snippet from the given matches.
    /// </summary>
    private Snippet ExtractSnippetFromMatches(
        string content,
        List<MatchInfo> matches,
        SnippetOptions options)
    {
        // LOGIC: Find highest density region.
        var (centerPos, _) = FindHighestDensityPosition(content, matches);

        // LOGIC: Calculate initial bounds centered on density peak.
        var halfLength = options.MaxLength / 2;
        var start = Math.Max(0, centerPos - halfLength);
        var end = Math.Min(content.Length, centerPos + halfLength);

        // LOGIC: Expand to sentence boundaries if requested.
        if (options.RespectSentenceBoundaries)
        {
            start = _sentenceDetector.FindSentenceStart(content, start);
            end = _sentenceDetector.FindSentenceEnd(content, end);
        }

        // LOGIC: Apply max length constraint.
        var (finalStart, finalEnd, truncStart, truncEnd) =
            ApplyMaxLength(content, start, end, centerPos, options.MaxLength);

        // LOGIC: Build snippet text and highlights.
        var text = BuildSnippetText(content, finalStart, finalEnd, truncStart, truncEnd);
        var highlights = BuildHighlights(matches, finalStart, finalEnd, truncStart);

        _logger.LogDebug(
            "Snippet bounds: {Start}-{End}, truncated: {TruncStart}/{TruncEnd}",
            finalStart, finalEnd, truncStart, truncEnd);

        return new Snippet(text, highlights, finalStart, truncStart, truncEnd);
    }

    /// <summary>
    /// Finds the position with highest match density.
    /// </summary>
    private static (int Position, int Score) FindHighestDensityPosition(
        string content,
        List<MatchInfo> matches)
    {
        const int windowSize = 100;
        const int step = 10;

        var bestPos = matches.Count > 0 ? matches[0].Position : 0;
        var bestScore = 0;

        for (var pos = 0; pos < content.Length; pos += step)
        {
            var windowEnd = pos + windowSize;
            var score = matches.Count(m =>
                m.Position >= pos && m.Position < windowEnd);

            // LOGIC: Weight by match type (exact = 2, fuzzy = 1).
            score += matches
                .Where(m => m.Position >= pos && m.Position < windowEnd && m.Type == HighlightType.QueryMatch)
                .Count();

            if (score > bestScore)
            {
                bestScore = score;
                bestPos = pos + (windowSize / 2);
            }
        }

        return (Math.Min(bestPos, content.Length), bestScore);
    }

    /// <summary>
    /// Applies max length constraint to bounds.
    /// </summary>
    private static (int Start, int End, bool TruncStart, bool TruncEnd) ApplyMaxLength(
        string content,
        int start,
        int end,
        int centerPos,
        int maxLength)
    {
        var currentLength = end - start;
        var truncStart = start > 0;
        var truncEnd = end < content.Length;

        if (currentLength <= maxLength)
        {
            return (start, end, truncStart, truncEnd);
        }

        // LOGIC: Shrink from both ends, keeping center visible.
        var excess = currentLength - maxLength;
        var shrinkStart = excess / 2;
        var shrinkEnd = excess - shrinkStart;

        start = Math.Min(start + shrinkStart, centerPos);
        end = Math.Max(end - shrinkEnd, centerPos);

        return (start, end, truncStart || start > 0, truncEnd || end < content.Length);
    }

    /// <summary>
    /// Builds snippet text with truncation markers.
    /// </summary>
    private static string BuildSnippetText(
        string content,
        int start,
        int end,
        bool truncStart,
        bool truncEnd)
    {
        var text = content[start..end];

        if (truncStart)
        {
            text = "..." + text.TrimStart();
        }

        if (truncEnd)
        {
            text = text.TrimEnd() + "...";
        }

        return text;
    }

    /// <summary>
    /// Builds highlight spans adjusted for snippet offset.
    /// </summary>
    private static IReadOnlyList<HighlightSpan> BuildHighlights(
        List<MatchInfo> matches,
        int snippetStart,
        int snippetEnd,
        bool truncStart)
    {
        var offset = truncStart ? 3 : 0; // "..." prefix

        return matches
            .Where(m => m.Position >= snippetStart && m.Position + m.Length <= snippetEnd)
            .Select(m => new HighlightSpan(
                m.Position - snippetStart + offset,
                m.Length,
                m.Type))
            .ToList();
    }

    /// <summary>
    /// Creates a fallback snippet when no matches are found.
    /// </summary>
    private Snippet CreateFallbackSnippet(string content, SnippetOptions options)
    {
        var length = Math.Min(content.Length, options.MaxLength);
        var text = content[..length];
        var truncated = length < content.Length;

        _logger.LogWarning("No matches found in chunk, using fallback snippet");

        if (truncated)
        {
            text = text.TrimEnd() + "...";
        }

        return new Snippet(
            text,
            Array.Empty<HighlightSpan>(),
            StartOffset: 0,
            IsTruncatedStart: false,
            IsTruncatedEnd: truncated);
    }

    /// <summary>
    /// Clusters matches by proximity.
    /// </summary>
    private static List<MatchCluster> ClusterMatches(List<MatchInfo> matches, int clusterThreshold)
    {
        if (matches.Count == 0) return new List<MatchCluster>();

        var clusters = new List<MatchCluster>();
        var currentCluster = new List<MatchInfo> { matches[0] };

        for (var i = 1; i < matches.Count; i++)
        {
            var prev = matches[i - 1];
            var curr = matches[i];

            if (curr.Position - (prev.Position + prev.Length) <= clusterThreshold)
            {
                currentCluster.Add(curr);
            }
            else
            {
                clusters.Add(CreateCluster(currentCluster));
                currentCluster = new List<MatchInfo> { curr };
            }
        }

        clusters.Add(CreateCluster(currentCluster));

        return clusters;
    }

    /// <summary>
    /// Creates a cluster with calculated center position.
    /// </summary>
    private static MatchCluster CreateCluster(List<MatchInfo> matches)
    {
        var start = matches.Min(m => m.Position);
        var end = matches.Max(m => m.Position + m.Length);
        var center = start + ((end - start) / 2);
        return new MatchCluster(matches, center);
    }

    /// <summary>
    /// Checks if two ranges overlap.
    /// </summary>
    private static bool Overlaps((int Start, int End) a, (int Start, int End) b) =>
        a.Start < b.End && b.Start < a.End;

    #endregion

    #region Private Types

    /// <summary>
    /// Information about a match in content.
    /// </summary>
    private record MatchInfo(int Position, int Length, HighlightType Type);

    /// <summary>
    /// A cluster of matches grouped by proximity.
    /// </summary>
    private record MatchCluster(List<MatchInfo> Matches, int CenterPosition);

    #endregion
}
