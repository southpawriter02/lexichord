// -----------------------------------------------------------------------
// <copyright file="LineRange.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Immutable record representing a range of line numbers (v0.7.6d).
//   Used to track where changes occurred in the original and new documents.
//
//   Properties:
//     - Start: First line number (1-based)
//     - End: Last line number (1-based)
//     - LineCount: Computed property (End - Start + 1)
//
//   Validation ensures Start > 0 and End >= Start.
//
//   Introduced in: v0.7.6d as part of Document Comparison
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.DocumentComparison;

/// <summary>
/// Represents a range of line numbers in a document.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This immutable record tracks line number ranges for document changes.
/// Line numbers are 1-based (first line is line 1) to match standard editor conventions.
/// </para>
/// <para>
/// <b>Usage:</b>
/// <list type="bullet">
/// <item><description><see cref="DocumentChange.OriginalLineRange"/>: Lines affected in the original document.</description></item>
/// <item><description><see cref="DocumentChange.NewLineRange"/>: Lines affected in the new document.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Validation:</b>
/// <list type="bullet">
/// <item><description><see cref="Start"/> must be greater than 0 (line numbers are 1-based).</description></item>
/// <item><description><see cref="End"/> must be greater than or equal to <see cref="Start"/>.</description></item>
/// </list>
/// Use <see cref="IsValid"/> to check validity or <see cref="Validate"/> to throw on invalid state.
/// </para>
/// <para>
/// <b>Thread safety:</b> This is an immutable record and is inherently thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6d as part of the Document Comparison feature.
/// </para>
/// </remarks>
/// <param name="Start">
/// The first line number in the range (1-based). Must be greater than 0.
/// </param>
/// <param name="End">
/// The last line number in the range (1-based). Must be greater than or equal to <paramref name="Start"/>.
/// </param>
/// <example>
/// <code>
/// // Single line change
/// var singleLine = new LineRange(42, 42);
/// Console.WriteLine(singleLine.ToString()); // "line 42"
/// Console.WriteLine(singleLine.LineCount);  // 1
///
/// // Multi-line change
/// var multiLine = new LineRange(10, 15);
/// Console.WriteLine(multiLine.ToString()); // "lines 10-15"
/// Console.WriteLine(multiLine.LineCount);  // 6
///
/// // Validation
/// var range = new LineRange(5, 10);
/// range.Validate(); // No exception
///
/// var invalid = new LineRange(10, 5);
/// invalid.Validate(); // Throws ArgumentException
/// </code>
/// </example>
public record LineRange(int Start, int End)
{
    /// <summary>
    /// Gets the number of lines in this range (inclusive).
    /// </summary>
    /// <value>
    /// The count of lines from <see cref="Start"/> to <see cref="End"/> inclusive.
    /// Always at least 1 for valid ranges.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Calculated as <c>End - Start + 1</c> since both endpoints are inclusive.
    /// For example, range 10-15 contains 6 lines (10, 11, 12, 13, 14, 15).
    /// </remarks>
    public int LineCount => End - Start + 1;

    /// <summary>
    /// Gets whether this line range spans a single line.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Start"/> equals <see cref="End"/>; otherwise <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Used for display formatting - single line ranges display as
    /// "line N" instead of "lines N-N".
    /// </remarks>
    public bool IsSingleLine => Start == End;

    /// <summary>
    /// Gets whether this line range has valid values.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Start"/> is greater than 0 and <see cref="End"/> is
    /// greater than or equal to <see cref="Start"/>; otherwise <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Line numbers are 1-based, so Start must be at least 1.
    /// End must be at least equal to Start to represent a valid range.
    /// </remarks>
    public bool IsValid => Start > 0 && End >= Start;

    /// <summary>
    /// Validates this line range and throws if invalid.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="Start"/> is less than 1 or <see cref="End"/> is less than <see cref="Start"/>.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Provides explicit validation with descriptive error messages.
    /// Call this method to enforce invariants when the range is created from external input.
    /// </remarks>
    public void Validate()
    {
        if (Start < 1)
        {
            throw new ArgumentException(
                $"Start line must be at least 1 (1-based line numbers), but was {Start}.",
                nameof(Start));
        }

        if (End < Start)
        {
            throw new ArgumentException(
                $"End line ({End}) must be greater than or equal to start line ({Start}).",
                nameof(End));
        }
    }

    /// <summary>
    /// Checks whether a specific line number falls within this range.
    /// </summary>
    /// <param name="lineNumber">The line number to check (1-based).</param>
    /// <returns>
    /// <c>true</c> if <paramref name="lineNumber"/> is between <see cref="Start"/>
    /// and <see cref="End"/> (inclusive); otherwise <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <b>LOGIC:</b> Simple range containment check. Both endpoints are inclusive.
    /// </remarks>
    public bool Contains(int lineNumber) => lineNumber >= Start && lineNumber <= End;

    /// <summary>
    /// Checks whether this range overlaps with another range.
    /// </summary>
    /// <param name="other">The other range to check for overlap.</param>
    /// <returns>
    /// <c>true</c> if the ranges share at least one line number; otherwise <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <b>LOGIC:</b> Two ranges overlap if the start of one is within the other,
    /// or vice versa. This is equivalent to: Start &lt;= other.End AND End &gt;= other.Start.
    /// </remarks>
    public bool Overlaps(LineRange other) => Start <= other.End && End >= other.Start;

    /// <summary>
    /// Returns a human-readable string representation of this line range.
    /// </summary>
    /// <returns>
    /// <c>"line N"</c> for single-line ranges, or <c>"lines N-M"</c> for multi-line ranges.
    /// </returns>
    /// <remarks>
    /// <b>LOGIC:</b> Provides a natural language description suitable for UI display
    /// and log messages.
    /// </remarks>
    public override string ToString() =>
        IsSingleLine ? $"line {Start}" : $"lines {Start}-{End}";
}
