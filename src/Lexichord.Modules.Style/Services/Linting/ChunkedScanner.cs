using System.Diagnostics;
using System.Runtime.CompilerServices;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services.Linting;

/// <summary>
/// Scans large documents in chunks with viewport priority.
/// </summary>
/// <remarks>
/// LOGIC: ChunkedScanner splits large documents into manageable chunks
/// for progressive scanning. It prioritizes viewport-overlapping chunks
/// to provide immediate feedback for visible content.
///
/// Chunking Strategy:
/// - Documents under 1MB are scanned as a single chunk
/// - Larger documents are split into ~100KB chunks
/// - Chunks are aligned to line boundaries
/// - Viewport chunks are scanned first
/// - All chunks overlap by 100 chars to catch boundary violations
///
/// Thread Safety:
/// - ScanChunkedAsync can be called from any thread
/// - Results are yielded as they complete
/// - Cancellation is respected between chunks
///
/// Version: v0.2.7d
/// </remarks>
public sealed class ChunkedScanner
{
    private readonly IScannerService _scannerService;
    private readonly IPerformanceMonitor _performanceMonitor;
    private readonly ILogger<ChunkedScanner> _logger;

    /// <summary>
    /// Default chunk size in bytes.
    /// </summary>
    private const int DefaultChunkSizeBytes = 100 * 1024; // 100KB

    /// <summary>
    /// Threshold above which documents are chunked.
    /// </summary>
    private const int ChunkingThresholdBytes = 1024 * 1024; // 1MB

    /// <summary>
    /// Overlap between chunks to catch boundary violations.
    /// </summary>
    private const int ChunkOverlap = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChunkedScanner"/> class.
    /// </summary>
    /// <param name="scannerService">Scanner service for pattern matching.</param>
    /// <param name="performanceMonitor">Performance monitor for tracking.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public ChunkedScanner(
        IScannerService scannerService,
        IPerformanceMonitor performanceMonitor,
        ILogger<ChunkedScanner> logger)
    {
        _scannerService = scannerService ?? throw new ArgumentNullException(nameof(scannerService));
        _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Scans a document in chunks, yielding results progressively.
    /// </summary>
    /// <param name="content">Document content to scan.</param>
    /// <param name="rules">Style rules to apply.</param>
    /// <param name="viewportStart">Start offset of the visible viewport.</param>
    /// <param name="viewportEnd">End offset of the visible viewport.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async stream of chunk scan results.</returns>
    /// <remarks>
    /// LOGIC: Yields ChunkScanResult objects as each chunk is scanned.
    /// Viewport chunks are yielded first, followed by non-viewport chunks.
    /// </remarks>
    public async IAsyncEnumerable<ChunkScanResult> ScanChunkedAsync(
        string content,
        IReadOnlyList<StyleRule> rules,
        int viewportStart,
        int viewportEnd,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (content.Length < ChunkingThresholdBytes)
        {
            _logger.LogDebug("Document under 1MB, using single scan");

            // LOGIC: Small documents scan in one pass
            yield return await ScanSingleChunkAsync(
                content, rules, 0, content.Length,
                isViewport: true, chunkIndex: 0, totalChunks: 1,
                cancellationToken);
            yield break;
        }

        // LOGIC: Create chunks aligned to line boundaries
        var chunks = CreateChunks(content, viewportStart, viewportEnd);

        _logger.LogDebug(
            "Document chunked: {Count} chunks of ~{Size}KB each",
            chunks.Count,
            DefaultChunkSizeBytes / 1024);

        // LOGIC: Sort chunks: viewport first, then by position
        var orderedChunks = chunks
            .OrderByDescending(c => c.IsViewport)
            .ThenBy(c => c.StartOffset)
            .ToList();

        for (var i = 0; i < orderedChunks.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chunk = orderedChunks[i];

            _logger.LogDebug(
                "Scanning chunk {Index}/{Total} (viewport: {IsViewport})",
                i + 1,
                orderedChunks.Count,
                chunk.IsViewport);

            yield return await ScanSingleChunkAsync(
                content, rules,
                chunk.StartOffset, chunk.EndOffset,
                chunk.IsViewport, i, orderedChunks.Count,
                cancellationToken);
        }
    }

    /// <summary>
    /// Creates line-aligned chunks for a document.
    /// </summary>
    private List<ChunkInfo> CreateChunks(string content, int viewportStart, int viewportEnd)
    {
        var chunks = new List<ChunkInfo>();
        var position = 0;

        while (position < content.Length)
        {
            // LOGIC: Calculate end of this chunk
            var targetEnd = Math.Min(position + DefaultChunkSizeBytes, content.Length);

            // LOGIC: Align to line boundary (find next newline)
            var actualEnd = targetEnd;
            if (actualEnd < content.Length)
            {
                var newlinePos = content.IndexOf('\n', targetEnd);
                if (newlinePos >= 0 && newlinePos < targetEnd + 1000)
                {
                    actualEnd = newlinePos + 1;
                }
            }

            // LOGIC: Check if chunk overlaps viewport
            var chunkStart = position;
            var chunkEnd = actualEnd;
            var overlapsViewport = chunkEnd > viewportStart && chunkStart < viewportEnd;

            chunks.Add(new ChunkInfo(chunkStart, chunkEnd, overlapsViewport));

            // LOGIC: Move to next chunk with overlap
            position = Math.Max(actualEnd - ChunkOverlap, position + 1);
        }

        return chunks;
    }

    /// <summary>
    /// Scans a single chunk of the document.
    /// </summary>
    private async Task<ChunkScanResult> ScanSingleChunkAsync(
        string content,
        IReadOnlyList<StyleRule> rules,
        int startOffset,
        int endOffset,
        bool isViewport,
        int chunkIndex,
        int totalChunks,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        // LOGIC: Extract chunk content
        var chunkContent = content.Substring(startOffset, endOffset - startOffset);

        // LOGIC: Scan the chunk using batch scanner
        var violations = new List<StyleViolation>();
        var scanResults = await _scannerService.ScanBatchAsync(chunkContent, rules, cancellationToken);

        // LOGIC: Convert scanner results to style violations with adjusted positions
        foreach (var scanResult in scanResults)
        {
            var rule = rules.FirstOrDefault(r => r.Id == scanResult.RuleId);
            if (rule is null)
                continue;

            foreach (var match in scanResult.Matches)
            {
                var absoluteStart = match.StartOffset + startOffset;
                var absoluteEnd = absoluteStart + match.Length;
                var matchedText = content.Substring(absoluteStart, match.Length);

                // LOGIC: Compute line/column positions
                var (startLine, startColumn) = ComputePosition(content, absoluteStart);
                var (endLine, endColumn) = ComputePosition(content, absoluteEnd);

                violations.Add(new StyleViolation(
                    Rule: rule,
                    Message: $"{rule.Name}: {rule.Description}",
                    StartOffset: absoluteStart,
                    EndOffset: absoluteEnd,
                    StartLine: startLine,
                    StartColumn: startColumn,
                    EndLine: endLine,
                    EndColumn: endColumn,
                    MatchedText: matchedText,
                    Suggestion: rule.Suggestion,
                    Severity: rule.DefaultSeverity));
            }
        }

        stopwatch.Stop();

        // LOGIC: Record timing for adaptive debounce
        _performanceMonitor.RecordOperation($"chunk_{chunkIndex}", stopwatch.Elapsed);

        return new ChunkScanResult(
            chunkIndex,
            totalChunks,
            startOffset,
            endOffset,
            isViewport,
            violations,
            stopwatch.Elapsed);
    }

    /// <summary>
    /// Computes line and column from character offset.
    /// </summary>
    private static (int Line, int Column) ComputePosition(string content, int offset)
    {
        var line = 1;
        var column = 1;

        for (var i = 0; i < offset && i < content.Length; i++)
        {
            if (content[i] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
        }

        return (line, column);
    }

    /// <summary>
    /// Information about a chunk.
    /// </summary>
    private readonly record struct ChunkInfo(int StartOffset, int EndOffset, bool IsViewport);
}
