// -----------------------------------------------------------------------
// <copyright file="ChangeCategory.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Enumeration defining the 8 categories of document changes (v0.7.6d).
//   Each category represents a distinct type of modification that can be
//   detected when comparing two document versions.
//
//   Categories:
//     - Added: New content that didn't exist before
//     - Removed: Content that was deleted entirely
//     - Modified: Content changed in place
//     - Restructured: Content moved to a different location
//     - Clarified: Better explanation without changing meaning
//     - Formatting: Style changes only (headings, lists, etc.)
//     - Correction: Factual fixes or error corrections
//     - Terminology: Vocabulary or naming changes
//
//   Introduced in: v0.7.6d as part of Document Comparison
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.DocumentComparison;

/// <summary>
/// Categories of document changes detected during version comparison.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Each category represents a distinct type of modification that can be
/// detected when comparing two document versions. The LLM semantic analysis assigns
/// one of these categories to each detected change based on the nature of the modification.
/// </para>
/// <para>
/// <b>Category Selection:</b>
/// <list type="bullet">
/// <item><description><see cref="Added"/>: Use when entirely new content appears that didn't exist before.</description></item>
/// <item><description><see cref="Removed"/>: Use when existing content is deleted entirely.</description></item>
/// <item><description><see cref="Modified"/>: Use when content is changed in place (same location, different text).</description></item>
/// <item><description><see cref="Restructured"/>: Use when content is moved to a different location.</description></item>
/// <item><description><see cref="Clarified"/>: Use when explanations are improved without changing core meaning.</description></item>
/// <item><description><see cref="Formatting"/>: Use for style-only changes (headings, lists, code blocks).</description></item>
/// <item><description><see cref="Correction"/>: Use for factual fixes, typo corrections, or error fixes.</description></item>
/// <item><description><see cref="Terminology"/>: Use for vocabulary changes or naming convention updates.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>UI Display:</b>
/// Each category has an associated icon and color in the UI:
/// <list type="bullet">
/// <item><description><see cref="Added"/>: Green + icon</description></item>
/// <item><description><see cref="Removed"/>: Red - icon</description></item>
/// <item><description><see cref="Modified"/>: Orange ~ icon</description></item>
/// <item><description><see cref="Restructured"/>: Blue arrow icon</description></item>
/// <item><description><see cref="Clarified"/>: Blue lightbulb icon</description></item>
/// <item><description><see cref="Formatting"/>: Gray paint icon</description></item>
/// <item><description><see cref="Correction"/>: Red checkmark icon</description></item>
/// <item><description><see cref="Terminology"/>: Purple tag icon</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6d as part of the Document Comparison feature.
/// </para>
/// </remarks>
public enum ChangeCategory
{
    /// <summary>
    /// New content was added that didn't exist before.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Examples:</b> New sections, new paragraphs, new list items, new code blocks.
    /// </para>
    /// <para>
    /// <b>Text Properties:</b> <c>OriginalText</c> will be <c>null</c>;
    /// <c>NewText</c> will contain the added content.
    /// </para>
    /// </remarks>
    Added = 0,

    /// <summary>
    /// Existing content was removed entirely.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Examples:</b> Deleted sections, removed paragraphs, removed list items.
    /// </para>
    /// <para>
    /// <b>Text Properties:</b> <c>OriginalText</c> will contain the removed content;
    /// <c>NewText</c> will be <c>null</c>.
    /// </para>
    /// </remarks>
    Removed = 1,

    /// <summary>
    /// Content was modified while staying in the same location.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Examples:</b> Rephrased sentences, updated facts, changed values, reworded paragraphs.
    /// </para>
    /// <para>
    /// <b>Text Properties:</b> Both <c>OriginalText</c> and <c>NewText</c> will be populated.
    /// </para>
    /// </remarks>
    Modified = 2,

    /// <summary>
    /// Content was moved to a different location.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Examples:</b> Reordered sections, moved paragraphs between sections, reorganized lists.
    /// </para>
    /// <para>
    /// <b>Text Properties:</b> Both line ranges will be populated showing old and new positions.
    /// </para>
    /// </remarks>
    Restructured = 3,

    /// <summary>
    /// Content was clarified without changing core meaning.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Examples:</b> Better explanations, additional context, examples added, elaborations.
    /// </para>
    /// <para>
    /// <b>Text Properties:</b> Both <c>OriginalText</c> and <c>NewText</c> will be populated.
    /// </para>
    /// </remarks>
    Clarified = 4,

    /// <summary>
    /// Only formatting or style changes, no semantic change.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Examples:</b> Heading level changes, list formatting, code block formatting,
    /// whitespace adjustments, markdown syntax changes.
    /// </para>
    /// <para>
    /// <b>Filtering:</b> These changes are excluded by default unless
    /// <see cref="ComparisonOptions.IncludeFormattingChanges"/> is <c>true</c>.
    /// </para>
    /// </remarks>
    Formatting = 5,

    /// <summary>
    /// Factual corrections or error fixes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Examples:</b> Fixed typos, corrected data, updated outdated information,
    /// fixed broken links, corrected code samples.
    /// </para>
    /// <para>
    /// <b>Text Properties:</b> Both <c>OriginalText</c> and <c>NewText</c> will be populated.
    /// </para>
    /// </remarks>
    Correction = 6,

    /// <summary>
    /// Terminology or vocabulary changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Examples:</b> Renamed concepts, updated naming conventions, changed branding,
    /// updated product names, revised technical terminology.
    /// </para>
    /// <para>
    /// <b>Text Properties:</b> Both <c>OriginalText</c> and <c>NewText</c> will be populated.
    /// </para>
    /// </remarks>
    Terminology = 7
}
