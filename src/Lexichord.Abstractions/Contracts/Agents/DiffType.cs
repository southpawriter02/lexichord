// -----------------------------------------------------------------------
// <copyright file="DiffType.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.Agents;

/// <summary>
/// Type of diff operation, indicating whether text was unchanged, added, or deleted.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Used in <see cref="DiffOperation"/> to classify each segment of
/// a text diff. The diff is computed between the original violation text and
/// the suggested fix text using DiffPlex.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5b as part of the Automatic Fix Suggestions feature.
/// </para>
/// </remarks>
public enum DiffType
{
    /// <summary>
    /// Text is unchanged between original and suggested versions.
    /// </summary>
    /// <remarks>
    /// This segment of text appears identically in both the original and
    /// the suggested text. No modification is needed for this portion.
    /// </remarks>
    Unchanged = 0,

    /// <summary>
    /// Text was added in the suggested version.
    /// </summary>
    /// <remarks>
    /// This segment of text appears only in the suggested version and
    /// represents new content that will be inserted.
    /// </remarks>
    Addition = 1,

    /// <summary>
    /// Text was deleted from the original version.
    /// </summary>
    /// <remarks>
    /// This segment of text appears only in the original version and
    /// represents content that will be removed when the fix is applied.
    /// </remarks>
    Deletion = 2
}
