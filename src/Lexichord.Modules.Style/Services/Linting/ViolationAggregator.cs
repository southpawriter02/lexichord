using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services.Linting;

/// <summary>
/// Transforms raw scan matches into user-facing style violations.
/// </summary>
/// <remarks>
/// LOGIC: The aggregator is the final stage of the linting pipeline:
/// 1. Receives ScanMatch objects from the orchestrator
/// 2. Calculates line/column positions using PositionIndex
/// 3. Expands message templates
/// 4. Deduplicates overlapping violations (higher severity wins)
/// 5. Sorts by position and applies max limit
/// 6. Caches results for fast retrieval
///
/// Caching: Per-document cache enables GetViolationAt without re-scanning.
/// Thread Safety: Uses ConcurrentDictionary for cache, lock-free reads.
///
/// Version: v0.2.3d
/// </remarks>
public sealed class ViolationAggregator : IViolationAggregator
{
    private readonly ILintingConfiguration _config;
    private readonly ILogger<ViolationAggregator> _logger;
    private readonly ConcurrentDictionary<string, ViolationCache> _caches = new();

    /// <summary>
    /// Initializes a new instance of the ViolationAggregator.
    /// </summary>
    /// <param name="config">Linting configuration.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public ViolationAggregator(
        ILintingConfiguration config,
        ILogger<ViolationAggregator> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IReadOnlyList<AggregatedStyleViolation> Aggregate(
        IEnumerable<ScanMatch> matches,
        string documentId,
        string content)
    {
        ArgumentNullException.ThrowIfNull(matches);
        ArgumentException.ThrowIfNullOrEmpty(documentId);
        ArgumentNullException.ThrowIfNull(content);

        var matchList = matches.ToList();

        // LOGIC: Fast path for empty input
        if (matchList.Count == 0)
        {
            _logger.LogDebug("No matches to aggregate for document {DocumentId}", documentId);
            var emptyCache = GetOrCreateCache(documentId);
            emptyCache.Update([]);
            return [];
        }

        _logger.LogDebug(
            "Aggregating {MatchCount} matches for document {DocumentId}",
            matchList.Count,
            documentId);

        // LOGIC: Build position index for O(log n) lookups
        var positionIndex = new PositionIndex(content);

        // LOGIC: Transform matches to violations
        var violations = matchList
            .Select(match => CreateViolation(match, documentId, positionIndex))
            .ToList();

        _logger.LogDebug(
            "Created {ViolationCount} violations before deduplication",
            violations.Count);

        // LOGIC: Deduplicate overlapping violations
        var deduplicated = DeduplicateViolations(violations);

        _logger.LogDebug(
            "After deduplication: {Count} violations (removed {Removed})",
            deduplicated.Count,
            violations.Count - deduplicated.Count);

        // LOGIC: Sort by position (line, then column)
        var sorted = deduplicated
            .OrderBy(v => v.Line)
            .ThenBy(v => v.Column)
            .ToList();

        // LOGIC: Apply max violations limit
        IReadOnlyList<AggregatedStyleViolation> final;
        if (sorted.Count > _config.MaxViolationsPerDocument)
        {
            _logger.LogWarning(
                "Violations truncated from {Original} to {Limit} for document {DocumentId}",
                sorted.Count,
                _config.MaxViolationsPerDocument,
                documentId);

            final = sorted.Take(_config.MaxViolationsPerDocument).ToList();
        }
        else
        {
            final = sorted;
        }

        // LOGIC: Update cache
        var cache = GetOrCreateCache(documentId);
        cache.Update(final);

        _logger.LogDebug(
            "Aggregation complete for {DocumentId}: {Count} violations",
            documentId,
            final.Count);

        return final;
    }

    /// <inheritdoc />
    public void ClearViolations(string documentId)
    {
        if (_caches.TryRemove(documentId, out var cache))
        {
            _logger.LogDebug(
                "Cleared violations for document {DocumentId} ({Count} removed)",
                documentId,
                cache.Violations.Count);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<AggregatedStyleViolation> GetViolations(string documentId)
    {
        return _caches.TryGetValue(documentId, out var cache)
            ? cache.Violations
            : [];
    }

    /// <inheritdoc />
    public AggregatedStyleViolation? GetViolation(string documentId, string violationId)
    {
        if (_caches.TryGetValue(documentId, out var cache))
        {
            return cache.ViolationsById.GetValueOrDefault(violationId);
        }

        return null;
    }

    /// <inheritdoc />
    public AggregatedStyleViolation? GetViolationAt(string documentId, int offset)
    {
        if (!_caches.TryGetValue(documentId, out var cache))
        {
            return null;
        }

        // LOGIC: Linear search is acceptable for typical violation counts (<1000)
        // Could optimize with interval tree if needed for very noisy docs
        return cache.Violations.FirstOrDefault(v => v.ContainsOffset(offset));
    }

    /// <inheritdoc />
    public IReadOnlyList<AggregatedStyleViolation> GetViolationsInRange(
        string documentId,
        int startOffset,
        int endOffset)
    {
        if (!_caches.TryGetValue(documentId, out var cache))
        {
            return [];
        }

        // LOGIC: Find all violations that overlap with [startOffset, endOffset)
        return cache.Violations
            .Where(v => v.StartOffset < endOffset && v.EndOffset > startOffset)
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<ViolationSeverity, int> GetViolationCounts(string documentId)
    {
        var counts = new Dictionary<ViolationSeverity, int>
        {
            [ViolationSeverity.Error] = 0,
            [ViolationSeverity.Warning] = 0,
            [ViolationSeverity.Info] = 0,
            [ViolationSeverity.Hint] = 0
        };

        if (_caches.TryGetValue(documentId, out var cache))
        {
            foreach (var violation in cache.Violations)
            {
                counts[violation.Severity]++;
            }
        }

        return counts;
    }

    /// <summary>
    /// Creates a violation from a scan match.
    /// </summary>
    private AggregatedStyleViolation CreateViolation(
        ScanMatch match,
        string documentId,
        PositionIndex positionIndex)
    {
        // LOGIC: Calculate start and end positions
        var (line, column) = positionIndex.GetPosition(match.StartOffset);
        var (endLine, endColumn) = positionIndex.GetPosition(match.EndOffset);

        // LOGIC: Expand message template
        var message = BuildMessage(match);

        // LOGIC: Generate stable ID
        var id = GenerateViolationId(documentId, match);

        return new AggregatedStyleViolation
        {
            Id = id,
            DocumentId = documentId,
            RuleId = match.RuleId,
            StartOffset = match.StartOffset,
            Length = match.Length,
            Line = line,
            Column = column,
            EndLine = endLine,
            EndColumn = endColumn,
            ViolatingText = match.MatchedText,
            Message = message,
            Severity = match.Rule.DefaultSeverity,
            Suggestion = match.Rule.Suggestion,
            Category = match.Rule.Category
        };
    }

    /// <summary>
    /// Builds the violation message from rule template.
    /// </summary>
    private static string BuildMessage(ScanMatch match)
    {
        var rule = match.Rule;

        // LOGIC: Use description if available, otherwise construct default message
        if (!string.IsNullOrEmpty(rule.Description))
        {
            // LOGIC: Check if description has placeholders to expand
            if (MessageTemplateEngine.HasPlaceholders(rule.Description))
            {
                return MessageTemplateEngine.Expand(rule.Description, match);
            }

            return rule.Description;
        }

        // LOGIC: Fallback to name-based message
        return $"{rule.Name}: Found '{match.MatchedText}'";
    }

    /// <summary>
    /// Generates a stable violation ID based on position and rule.
    /// </summary>
    /// <remarks>
    /// LOGIC: ID is deterministic so violations can be tracked across re-scans.
    /// Uses SHA256 hash truncated to 12 hex chars for brevity.
    /// </remarks>
    private static string GenerateViolationId(string documentId, ScanMatch match)
    {
        var input = $"{documentId}:{match.RuleId}:{match.StartOffset}:{match.Length}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash)[..12].ToLowerInvariant();
    }

    /// <summary>
    /// Deduplicates overlapping violations, keeping highest severity.
    /// </summary>
    /// <remarks>
    /// LOGIC: Algorithm:
    /// 1. Sort by StartOffset
    /// 2. Iterate through, tracking current "winning" violation
    /// 3. For overlapping violations, compare severity
    /// 4. Higher severity wins; equal severity keeps earlier start
    /// </remarks>
    private static List<AggregatedStyleViolation> DeduplicateViolations(
        List<AggregatedStyleViolation> violations)
    {
        if (violations.Count <= 1)
        {
            return violations;
        }

        // LOGIC: Sort by start position for overlap detection
        var sorted = violations.OrderBy(v => v.StartOffset).ToList();
        var result = new List<AggregatedStyleViolation>();
        AggregatedStyleViolation? current = null;

        foreach (var violation in sorted)
        {
            if (current is null)
            {
                current = violation;
                continue;
            }

            // LOGIC: Check for overlap
            if (violation.OverlapsWith(current))
            {
                // LOGIC: Keep higher severity violation
                var comparison = CompareSeverity(violation.Severity, current.Severity);
                if (comparison > 0)
                {
                    // New violation is more severe
                    current = violation;
                }
                // If equal or less severe, keep current (earlier position wins)
            }
            else
            {
                // LOGIC: No overlap, emit current and start tracking new violation
                result.Add(current);
                current = violation;
            }
        }

        // LOGIC: Don't forget the last violation
        if (current is not null)
        {
            result.Add(current);
        }

        return result;
    }

    /// <summary>
    /// Compares two severities, returning positive if 'a' is more severe.
    /// </summary>
    /// <remarks>
    /// LOGIC: ViolationSeverity enum values: Error=0, Warning=1, Info=2, Hint=3
    /// Lower value = higher severity, so we invert the comparison.
    /// </remarks>
    private static int CompareSeverity(ViolationSeverity a, ViolationSeverity b)
    {
        // LOGIC: Error > Warning > Info > Hint
        // Lower enum value = higher severity, so invert
        return (int)b - (int)a;
    }

    /// <summary>
    /// Gets or creates the violation cache for a document.
    /// </summary>
    private ViolationCache GetOrCreateCache(string documentId)
    {
        return _caches.GetOrAdd(documentId, _ => new ViolationCache());
    }

    /// <summary>
    /// Per-document violation cache.
    /// </summary>
    /// <remarks>
    /// LOGIC: Stores both list (for ordered access) and dictionary (for ID lookup).
    /// Not thread-safe internally, but used within thread-safe ConcurrentDictionary.
    /// </remarks>
    private sealed class ViolationCache
    {
        private IReadOnlyList<AggregatedStyleViolation> _violations = [];
        private Dictionary<string, AggregatedStyleViolation> _violationsById = new();

        public IReadOnlyList<AggregatedStyleViolation> Violations => _violations;
        public Dictionary<string, AggregatedStyleViolation> ViolationsById => _violationsById;

        public void Update(IReadOnlyList<AggregatedStyleViolation> violations)
        {
            _violations = violations;
            _violationsById = violations.ToDictionary(v => v.Id);
        }
    }
}
