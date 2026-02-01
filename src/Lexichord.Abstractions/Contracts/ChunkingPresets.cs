// =============================================================================
// File: ChunkingPresets.cs
// Project: Lexichord.Abstractions
// Description: Predefined chunking configurations for common use cases,
//              providing ready-to-use ChunkingOptions instances.
// =============================================================================
// LOGIC: Static class with four preset configurations optimized for different
//   document types and retrieval strategies.
//   - HighPrecision: Small chunks (500 chars) for FAQ-style content.
//   - Balanced: Delegates to ChunkingOptions.Default (1000 chars).
//   - HighContext: Large chunks (2000 chars) for technical documentation.
//   - Code: Disables word boundaries, preserves whitespace for code content.
//   - All presets pass ChunkingOptions.Validate().
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Predefined chunking configurations for common use cases.
/// </summary>
/// <remarks>
/// <para>
/// Each preset provides a validated <see cref="ChunkingOptions"/> instance
/// tuned for a specific document type or retrieval strategy. Use these
/// presets as starting points, or create custom <see cref="ChunkingOptions"/>
/// for specialized requirements.
/// </para>
/// <para>
/// <b>Selection Guide:</b>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="HighPrecision"/>: FAQ content, short documents, high recall requirements.</description></item>
///   <item><description><see cref="Balanced"/>: General documents, articles, blog posts.</description></item>
///   <item><description><see cref="HighContext"/>: Technical documentation, long-form content needing broad context.</description></item>
///   <item><description><see cref="Code"/>: Source code, configuration files, structured content.</description></item>
/// </list>
/// </remarks>
public static class ChunkingPresets
{
    /// <summary>
    /// Small chunks for high-precision retrieval.
    /// Best for: FAQ-style content, short documents.
    /// </summary>
    /// <value>
    /// <see cref="ChunkingOptions"/> with <c>TargetSize = 500</c>,
    /// <c>Overlap = 50</c>, <c>MinSize = 100</c>, <c>MaxSize = 1000</c>.
    /// </value>
    public static ChunkingOptions HighPrecision { get; } = new()
    {
        TargetSize = 500,
        Overlap = 50,
        MinSize = 100,
        MaxSize = 1000
    };

    /// <summary>
    /// Default balanced configuration.
    /// Best for: General documents, articles, blog posts.
    /// </summary>
    /// <value>
    /// Equivalent to <see cref="ChunkingOptions.Default"/>.
    /// </value>
    public static ChunkingOptions Balanced { get; } = ChunkingOptions.Default;

    /// <summary>
    /// Large chunks for context-heavy retrieval.
    /// Best for: Technical documentation, long-form content.
    /// </summary>
    /// <value>
    /// <see cref="ChunkingOptions"/> with <c>TargetSize = 2000</c>,
    /// <c>Overlap = 200</c>, <c>MinSize = 500</c>, <c>MaxSize = 4000</c>.
    /// </value>
    public static ChunkingOptions HighContext { get; } = new()
    {
        TargetSize = 2000,
        Overlap = 200,
        MinSize = 500,
        MaxSize = 4000
    };

    /// <summary>
    /// Configuration optimized for code or structured content.
    /// Uses smaller overlap and allows larger chunks.
    /// </summary>
    /// <value>
    /// <see cref="ChunkingOptions"/> with <c>TargetSize = 1500</c>,
    /// <c>Overlap = 50</c>, <c>MinSize = 200</c>, <c>MaxSize = 3000</c>,
    /// <c>RespectWordBoundaries = false</c>, <c>PreserveWhitespace = true</c>.
    /// </value>
    public static ChunkingOptions Code { get; } = new()
    {
        TargetSize = 1500,
        Overlap = 50,
        MinSize = 200,
        MaxSize = 3000,
        RespectWordBoundaries = false,
        PreserveWhitespace = true
    };
}
