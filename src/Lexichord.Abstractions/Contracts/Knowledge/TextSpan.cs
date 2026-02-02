// =============================================================================
// File: TextSpan.cs
// Project: Lexichord.Abstractions
// Description: Represents a text span location in a document.
// =============================================================================
// LOGIC: Used by AxiomViolation to pinpoint where a violation occurred in
//   the source document. Provides both character offsets and line/column
//   information for display in editors.
//
// v0.4.6e: Axiom Data Model (CKVS Phase 1b)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge;

/// <summary>
/// Represents a span of text in a document, identified by character offsets
/// and line/column positions.
/// </summary>
/// <remarks>
/// Used by <see cref="AxiomViolation.Location"/> to specify where a violation
/// occurred in the source document, enabling precise error highlighting in editors.
/// </remarks>
public record TextSpan
{
    /// <summary>
    /// Starting character offset (0-based) from the beginning of the document.
    /// </summary>
    public int Start { get; init; }

    /// <summary>
    /// Ending character offset (0-based, exclusive) from the beginning of the document.
    /// </summary>
    public int End { get; init; }

    /// <summary>
    /// Line number where the span starts (1-based).
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// Column number where the span starts (1-based).
    /// </summary>
    public int Column { get; init; }

    /// <summary>
    /// Gets the length of the span in characters.
    /// </summary>
    /// <value>The number of characters covered by this span.</value>
    public int Length => End - Start;
}
