// -----------------------------------------------------------------------
// <copyright file="BatchSimplificationScope.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Simplifier;

/// <summary>
/// Defines the scope of a batch simplification operation.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="BatchSimplificationScope"/> determines which portions
/// of a document are considered for simplification during a batch operation.
/// The scope affects paragraph enumeration but not skip detection—paragraphs
/// within scope may still be skipped based on <see cref="BatchSimplificationOptions"/>.
/// </para>
/// <para>
/// <b>Scope Hierarchy:</b>
/// <list type="bullet">
///   <item><description><see cref="EntireDocument"/>: All paragraphs in the document</description></item>
///   <item><description><see cref="Selection"/>: Only paragraphs within the user's selection</description></item>
///   <item><description><see cref="CurrentSection"/>: Paragraphs under the current heading</description></item>
///   <item><description><see cref="ComplexParagraphsOnly"/>: Only paragraphs exceeding target grade level</description></item>
/// </list>
/// </para>
/// <para><b>Introduced in:</b> v0.7.4d as part of the Batch Simplification feature.</para>
/// </remarks>
/// <seealso cref="BatchSimplificationOptions"/>
/// <seealso cref="IBatchSimplificationService"/>
public enum BatchSimplificationScope
{
    /// <summary>
    /// Process all paragraphs in the document.
    /// </summary>
    /// <remarks>
    /// This is the default scope. All paragraphs are enumerated and evaluated
    /// against skip conditions defined in <see cref="BatchSimplificationOptions"/>.
    /// </remarks>
    EntireDocument = 0,

    /// <summary>
    /// Process only paragraphs within the current user selection.
    /// </summary>
    /// <remarks>
    /// Paragraphs are considered "within selection" if any part of the paragraph
    /// overlaps with the selection range. Partial paragraph selections are
    /// expanded to include the full paragraph.
    /// </remarks>
    Selection = 1,

    /// <summary>
    /// Process the current section determined by heading structure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A "section" starts at a heading (e.g., <c># Heading</c>, <c>## Subheading</c>)
    /// and continues until the next heading of equal or higher level, or end of document.
    /// </para>
    /// <para>
    /// If the cursor is not within a section (e.g., before the first heading),
    /// the scope falls back to <see cref="EntireDocument"/>.
    /// </para>
    /// </remarks>
    CurrentSection = 2,

    /// <summary>
    /// Process only paragraphs that exceed the target grade level.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This scope pre-filters paragraphs based on their Flesch-Kincaid grade level
    /// compared to the target. Only paragraphs with:
    /// <c>GradeLevel > TargetGradeLevel + GradeLevelTolerance</c>
    /// are included in the batch.
    /// </para>
    /// <para>
    /// This is useful for focusing simplification effort on the most complex
    /// content without processing already-simple paragraphs.
    /// </para>
    /// </remarks>
    ComplexParagraphsOnly = 3
}
