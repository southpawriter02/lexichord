using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Style.Services.Linting;

/// <summary>
/// Result from scanning a single chunk of a document.
/// </summary>
/// <param name="ChunkIndex">Zero-based index of this chunk.</param>
/// <param name="TotalChunks">Total number of chunks in the document.</param>
/// <param name="StartOffset">Character offset where this chunk begins.</param>
/// <param name="EndOffset">Character offset where this chunk ends.</param>
/// <param name="IsViewportChunk">Whether this chunk overlaps the current viewport.</param>
/// <param name="Violations">Violations found in this chunk.</param>
/// <param name="ScanDuration">Time taken to scan this chunk.</param>
/// <remarks>
/// LOGIC: ChunkScanResult contains the results of scanning a single
/// chunk of a large document. Violations have absolute positions
/// (adjusted from chunk-relative).
///
/// Version: v0.2.7d
/// </remarks>
public record ChunkScanResult(
    int ChunkIndex,
    int TotalChunks,
    int StartOffset,
    int EndOffset,
    bool IsViewportChunk,
    IReadOnlyList<StyleViolation> Violations,
    TimeSpan ScanDuration
)
{
    /// <summary>
    /// Gets the progress as a percentage of chunks completed.
    /// </summary>
    public double ProgressPercent => (ChunkIndex + 1) * 100.0 / TotalChunks;

    /// <summary>
    /// Gets the size of this chunk in characters.
    /// </summary>
    public int ChunkSize => EndOffset - StartOffset;
}
