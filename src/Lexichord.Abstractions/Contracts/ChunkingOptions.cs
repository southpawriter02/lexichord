// =============================================================================
// File: ChunkingOptions.cs
// Project: Lexichord.Abstractions
// Description: Configuration options for chunking behavior, including target
//              size, overlap, boundaries, and validation constraints.
// =============================================================================
// LOGIC: Non-positional record with init-only properties and sensible defaults.
//   - Default static property provides zero-configuration usage.
//   - Validate() enforces 6 internal consistency constraints:
//     1. TargetSize must be positive.
//     2. Overlap cannot be negative.
//     3. Overlap must be less than TargetSize.
//     4. MinSize cannot be negative.
//     5. MaxSize must be greater than MinSize.
//     6. TargetSize cannot exceed MaxSize.
//   - IncludeEmptyChunks defaults to false, filtering whitespace-only chunks.
//   - RespectWordBoundaries defaults to true, avoiding mid-word splits.
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Configuration options for chunking behavior.
/// All sizes are in characters unless otherwise noted.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ChunkingOptions"/> controls how text is divided into chunks by
/// any <see cref="IChunkingStrategy"/> implementation. Options include target
/// chunk size, overlap between adjacent chunks, minimum and maximum size bounds,
/// and behavioral flags for word boundary respect and whitespace handling.
/// </para>
/// <para>
/// <b>Validation:</b> Call <see cref="Validate"/> before passing options to a
/// chunking strategy to ensure internal consistency. Invalid combinations
/// (e.g., <see cref="Overlap"/> exceeding <see cref="TargetSize"/>) will throw
/// <see cref="ArgumentException"/>.
/// </para>
/// <para>
/// <b>Presets:</b> Use <see cref="ChunkingPresets"/> for common configurations
/// optimized for different use cases (high precision, balanced, high context, code).
/// </para>
/// </remarks>
public record ChunkingOptions
{
    /// <summary>
    /// Default options with sensible defaults for general use.
    /// </summary>
    /// <value>
    /// A <see cref="ChunkingOptions"/> instance with <see cref="TargetSize"/> = 1000,
    /// <see cref="Overlap"/> = 100, <see cref="MinSize"/> = 200,
    /// <see cref="MaxSize"/> = 2000, and word boundary respect enabled.
    /// </value>
    public static ChunkingOptions Default { get; } = new();

    /// <summary>
    /// Target chunk size in characters.
    /// Chunks will be approximately this size, adjusted for boundaries.
    /// </summary>
    /// <value>Default: 1000 characters.</value>
    public int TargetSize { get; init; } = 1000;

    /// <summary>
    /// Number of characters to overlap between consecutive chunks.
    /// Provides context continuity across chunk boundaries.
    /// </summary>
    /// <value>Default: 100 characters.</value>
    public int Overlap { get; init; } = 100;

    /// <summary>
    /// Minimum chunk size before merging with adjacent chunks.
    /// Prevents creation of very small, low-context chunks.
    /// </summary>
    /// <value>Default: 200 characters.</value>
    public int MinSize { get; init; } = 200;

    /// <summary>
    /// Maximum chunk size before forced splitting.
    /// Prevents chunks too large for embedding models.
    /// </summary>
    /// <value>Default: 2000 characters.</value>
    public int MaxSize { get; init; } = 2000;

    /// <summary>
    /// Whether to avoid splitting in the middle of words.
    /// When true, chunk boundaries are adjusted to word edges.
    /// </summary>
    /// <value>Default: <c>true</c>.</value>
    public bool RespectWordBoundaries { get; init; } = true;

    /// <summary>
    /// Whether to preserve leading/trailing whitespace in chunks.
    /// When false, chunks are trimmed.
    /// </summary>
    /// <value>Default: <c>false</c>.</value>
    public bool PreserveWhitespace { get; init; } = false;

    /// <summary>
    /// Whether to include empty or whitespace-only chunks.
    /// When false, empty chunks are filtered out.
    /// </summary>
    /// <value>Default: <c>false</c>.</value>
    public bool IncludeEmptyChunks { get; init; } = false;

    /// <summary>
    /// Validates that the options are internally consistent.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method enforces 6 constraints to ensure the options form a valid
    /// configuration for chunking strategies:
    /// </para>
    /// <list type="number">
    ///   <item><description><see cref="TargetSize"/> must be positive.</description></item>
    ///   <item><description><see cref="Overlap"/> cannot be negative.</description></item>
    ///   <item><description><see cref="Overlap"/> must be less than <see cref="TargetSize"/>.</description></item>
    ///   <item><description><see cref="MinSize"/> cannot be negative.</description></item>
    ///   <item><description><see cref="MaxSize"/> must be greater than <see cref="MinSize"/>.</description></item>
    ///   <item><description><see cref="TargetSize"/> cannot exceed <see cref="MaxSize"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when any constraint is violated.
    /// </exception>
    public void Validate()
    {
        if (TargetSize <= 0)
            throw new ArgumentException("TargetSize must be positive", nameof(TargetSize));

        if (Overlap < 0)
            throw new ArgumentException("Overlap cannot be negative", nameof(Overlap));

        if (Overlap >= TargetSize)
            throw new ArgumentException("Overlap must be less than TargetSize", nameof(Overlap));

        if (MinSize < 0)
            throw new ArgumentException("MinSize cannot be negative", nameof(MinSize));

        if (MaxSize <= MinSize)
            throw new ArgumentException("MaxSize must be greater than MinSize", nameof(MaxSize));

        if (TargetSize > MaxSize)
            throw new ArgumentException("TargetSize cannot exceed MaxSize", nameof(TargetSize));
    }
}
