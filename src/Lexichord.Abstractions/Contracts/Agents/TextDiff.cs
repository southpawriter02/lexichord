// -----------------------------------------------------------------------
// <copyright file="TextDiff.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.Agents;

/// <summary>
/// Represents a text diff between original and suggested text, providing
/// multiple representations for display and analysis.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="TextDiff"/> is generated using DiffPlex to compare
/// the original violation text with the AI-suggested fix. It provides:
/// <list type="bullet">
///   <item><description>A list of <see cref="DiffOperation"/> for programmatic access</description></item>
///   <item><description>A unified diff string for console/text display</description></item>
///   <item><description>An optional HTML diff with CSS classes for UI rendering</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> This record is immutable and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5b as part of the Automatic Fix Suggestions feature.
/// </para>
/// </remarks>
public record TextDiff
{
    /// <summary>
    /// List of diff operations describing the transformation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Operations are ordered by their position in the text.
    /// Each operation represents a contiguous segment with the same change type
    /// (unchanged, addition, or deletion).
    /// </para>
    /// </remarks>
    public required IReadOnlyList<DiffOperation> Operations { get; init; }

    /// <summary>
    /// Unified diff format string for text display.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Follows the unified diff format with:
    /// <list type="bullet">
    ///   <item><description>Lines starting with '-' for deletions</description></item>
    ///   <item><description>Lines starting with '+' for additions</description></item>
    ///   <item><description>Lines starting with ' ' for context</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public required string UnifiedDiff { get; init; }

    /// <summary>
    /// HTML representation with highlighting for UI display.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Uses CSS classes for styling:
    /// <list type="bullet">
    ///   <item><description><c>diff-add</c> for additions (green background)</description></item>
    ///   <item><description><c>diff-del</c> for deletions (red background, strikethrough)</description></item>
    ///   <item><description><c>diff-unchanged</c> for unchanged text</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public string? HtmlDiff { get; init; }

    /// <summary>
    /// Gets the number of addition operations in the diff.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Counts operations where <see cref="DiffOperation.Type"/> is
    /// <see cref="DiffType.Addition"/>.
    /// </remarks>
    public int Additions => Operations.Count(o => o.Type == DiffType.Addition);

    /// <summary>
    /// Gets the number of deletion operations in the diff.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Counts operations where <see cref="DiffOperation.Type"/> is
    /// <see cref="DiffType.Deletion"/>.
    /// </remarks>
    public int Deletions => Operations.Count(o => o.Type == DiffType.Deletion);

    /// <summary>
    /// Gets the number of unchanged segments in the diff.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Counts operations where <see cref="DiffOperation.Type"/> is
    /// <see cref="DiffType.Unchanged"/>.
    /// </remarks>
    public int Unchanged => Operations.Count(o => o.Type == DiffType.Unchanged);

    /// <summary>
    /// Gets the total number of characters changed (added or deleted).
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Sum of text lengths for all non-unchanged operations.
    /// This provides a rough measure of how significant the change is.
    /// </remarks>
    public int TotalChanges => Operations
        .Where(o => o.Type != DiffType.Unchanged)
        .Sum(o => o.Text.Length);

    /// <summary>
    /// Gets whether this diff represents any actual changes.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Returns <c>true</c> if there are any additions or deletions,
    /// <c>false</c> if the texts are identical.
    /// </remarks>
    public bool HasChanges => Additions > 0 || Deletions > 0;

    /// <summary>
    /// An empty diff representing no changes.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Used as a default value when diff generation fails or
    /// when creating a failed <see cref="FixSuggestion"/>.
    /// </remarks>
    public static TextDiff Empty => new()
    {
        Operations = Array.Empty<DiffOperation>(),
        UnifiedDiff = string.Empty
    };
}
