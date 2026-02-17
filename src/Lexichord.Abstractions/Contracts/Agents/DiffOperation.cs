// -----------------------------------------------------------------------
// <copyright file="DiffOperation.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.Agents;

/// <summary>
/// Represents a single diff operation in a text comparison, indicating a segment
/// of text that was unchanged, added, or deleted.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> A sequence of <see cref="DiffOperation"/> instances describes the
/// complete transformation from the original text to the suggested fix. Each operation
/// represents a contiguous segment of text with the same change type.
/// </para>
/// <para>
/// <b>Usage:</b> The <see cref="StartIndex"/> and <see cref="Length"/> properties
/// refer to positions in the resulting diff output, not the original text. For
/// deletions, the text exists only in the original; for additions, only in the
/// suggested version.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This record is immutable and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5b as part of the Automatic Fix Suggestions feature.
/// </para>
/// </remarks>
/// <param name="Type">
/// The type of diff operation (unchanged, addition, or deletion).
/// </param>
/// <param name="Text">
/// The text content of this diff segment.
/// </param>
/// <param name="StartIndex">
/// The starting index of this segment in the diff output.
/// </param>
/// <param name="Length">
/// The length of this segment in characters.
/// </param>
public record DiffOperation(
    DiffType Type,
    string Text,
    int StartIndex,
    int Length);
