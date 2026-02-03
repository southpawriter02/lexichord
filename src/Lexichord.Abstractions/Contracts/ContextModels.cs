// =============================================================================
// File: ContextModels.cs
// Project: Lexichord.Abstractions
// Description: Data contracts for context expansion configuration and results.
// =============================================================================
// LOGIC: ContextOptions configures the expansion window (preceding/following
//   chunk counts, heading inclusion). ExpandedChunk contains the expanded
//   result with before/after chunks and heading breadcrumb.
// =============================================================================
// VERSION: v0.5.3a (Context Expansion Service)
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Configuration options for context expansion.
/// </summary>
/// <param name="PrecedingChunks">
/// Number of chunks to retrieve before the core chunk.
/// Defaults to 1. Maximum is 5.
/// </param>
/// <param name="FollowingChunks">
/// Number of chunks to retrieve after the core chunk.
/// Defaults to 1. Maximum is 5.
/// </param>
/// <param name="IncludeHeadings">
/// Whether to include heading hierarchy breadcrumb.
/// Defaults to true.
/// </param>
/// <remarks>
/// <para>
/// Use <see cref="Validated"/> to ensure values are within allowed ranges.
/// Values outside the range [0, <see cref="MaxChunkWindow"/>] are clamped.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.3a as part of The Context Window feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Default expansion: 1 before, 1 after, with headings
/// var options = new ContextOptions();
///
/// // Custom expansion: 2 before, 3 after, no headings
/// var options = new ContextOptions(
///     PrecedingChunks: 2,
///     FollowingChunks: 3,
///     IncludeHeadings: false);
///
/// // Always validate before use
/// var validated = options.Validated();
/// </code>
/// </example>
public record ContextOptions(
    int PrecedingChunks = 1,
    int FollowingChunks = 1,
    bool IncludeHeadings = true)
{
    /// <summary>
    /// Maximum allowed value for PrecedingChunks and FollowingChunks.
    /// </summary>
    public const int MaxChunkWindow = 5;

    /// <summary>
    /// Validates options and clamps values to allowed range.
    /// </summary>
    /// <returns>A validated ContextOptions instance with clamped values.</returns>
    /// <remarks>
    /// Values less than 0 are clamped to 0.
    /// Values greater than <see cref="MaxChunkWindow"/> are clamped to <see cref="MaxChunkWindow"/>.
    /// </remarks>
    public ContextOptions Validated() => this with
    {
        PrecedingChunks = Math.Clamp(PrecedingChunks, 0, MaxChunkWindow),
        FollowingChunks = Math.Clamp(FollowingChunks, 0, MaxChunkWindow)
    };
}

/// <summary>
/// A chunk with its expanded context.
/// </summary>
/// <param name="Core">
/// The original retrieved chunk. Never null.
/// </param>
/// <param name="Before">
/// Chunks preceding the core chunk, ordered by ascending chunk_index.
/// Empty if no preceding chunks exist or were requested.
/// </param>
/// <param name="After">
/// Chunks following the core chunk, ordered by ascending chunk_index.
/// Empty if no following chunks exist or were requested.
/// </param>
/// <param name="ParentHeading">
/// The immediate parent heading text, if available.
/// Null if no heading context or IncludeHeadings was false.
/// </param>
/// <param name="HeadingBreadcrumb">
/// Full heading hierarchy as breadcrumb trail, from root to immediate parent.
/// Example: ["Authentication", "OAuth", "Token Refresh"]
/// Empty if no heading context or IncludeHeadings was false.
/// </param>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.3a as part of The Context Window feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var expanded = await contextService.ExpandAsync(chunk, options);
///
/// Console.WriteLine($"Before: {expanded.Before.Count} chunks");
/// Console.WriteLine($"Core: {expanded.Core.Content}");
/// Console.WriteLine($"After: {expanded.After.Count} chunks");
/// Console.WriteLine($"Breadcrumb: {expanded.FormatBreadcrumb()}");
/// </code>
/// </example>
public record ExpandedChunk(
    Chunk Core,
    IReadOnlyList<Chunk> Before,
    IReadOnlyList<Chunk> After,
    string? ParentHeading,
    IReadOnlyList<string> HeadingBreadcrumb)
{
    /// <summary>
    /// Whether preceding context is available.
    /// </summary>
    public bool HasBefore => Before.Count > 0;

    /// <summary>
    /// Whether following context is available.
    /// </summary>
    public bool HasAfter => After.Count > 0;

    /// <summary>
    /// Whether heading breadcrumb is available.
    /// </summary>
    public bool HasBreadcrumb => HeadingBreadcrumb.Count > 0;

    /// <summary>
    /// Total number of chunks in the expanded context (including core).
    /// </summary>
    public int TotalChunks => 1 + Before.Count + After.Count;

    /// <summary>
    /// Formats the breadcrumb as a display string.
    /// </summary>
    /// <param name="separator">Separator between heading levels. Default is " > ".</param>
    /// <returns>Formatted breadcrumb string, or empty if no breadcrumb.</returns>
    public string FormatBreadcrumb(string separator = " > ") =>
        string.Join(separator, HeadingBreadcrumb);
}
