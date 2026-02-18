// -----------------------------------------------------------------------
// <copyright file="DocumentChange.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Immutable record representing a single change detected between documents (v0.7.6d).
//   Contains all information about one change: category, location, description,
//   significance, original/new text, line ranges, impact, and related changes.
//
//   Properties:
//     - Category: Type of change (Added, Removed, Modified, etc.)
//     - Section: Document section where change occurred
//     - Description: Human-readable description of what changed
//     - Significance: Numeric score 0.0-1.0
//     - SignificanceLevel: Computed enum from score
//     - OriginalText/NewText: Text before and after
//     - OriginalLineRange/NewLineRange: Line positions
//     - Impact: Why this change matters
//     - RelatedChangeIndices: Links to related changes
//
//   Introduced in: v0.7.6d as part of Document Comparison
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.DocumentComparison;

/// <summary>
/// Represents a single change detected between two document versions.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This immutable record contains all information about a detected change,
/// including its category, location, textual content, significance, and relationships
/// to other changes. Changes are identified through a combination of DiffPlex text
/// analysis and LLM semantic understanding.
/// </para>
/// <para>
/// <b>Text Properties:</b>
/// <list type="bullet">
/// <item><description><see cref="Added"/>: <c>OriginalText</c> is <c>null</c>, <c>NewText</c> contains added content.</description></item>
/// <item><description><see cref="ChangeCategory.Removed"/>: <c>OriginalText</c> contains removed content, <c>NewText</c> is <c>null</c>.</description></item>
/// <item><description>Other categories: Both <c>OriginalText</c> and <c>NewText</c> are populated.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Line Ranges:</b>
/// Line ranges indicate where in each document the change occurred. These are
/// populated from DiffPlex analysis and help users navigate to the change location.
/// </para>
/// <para>
/// <b>Related Changes:</b>
/// The <see cref="RelatedChangeIndices"/> property links changes that are semantically
/// related (e.g., a terminology rename across multiple locations). Indices refer to
/// positions in the <see cref="ComparisonResult.Changes"/> list.
/// </para>
/// <para>
/// <b>Thread safety:</b> This is an immutable record and is inherently thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6d as part of the Document Comparison feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Modified content example
/// var change = new DocumentChange
/// {
///     Category = ChangeCategory.Modified,
///     Section = "Timeline",
///     Description = "Changed project launch date from Q1 2026 to Q2 2026",
///     Significance = 0.92,
///     OriginalText = "The project launches in Q1 2026",
///     NewText = "The project launches in Q2 2026",
///     OriginalLineRange = new LineRange(42, 42),
///     NewLineRange = new LineRange(42, 42),
///     Impact = "Affects all downstream planning and stakeholder expectations"
/// };
///
/// Console.WriteLine(change.SignificanceLevel); // Critical
/// Console.WriteLine(change.HasOriginalText);   // true
/// Console.WriteLine(change.HasNewText);        // true
/// </code>
/// </example>
public record DocumentChange
{
    /// <summary>
    /// Gets the category of this change.
    /// </summary>
    /// <value>
    /// One of the <see cref="ChangeCategory"/> values indicating the type of modification.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Assigned by the LLM during semantic analysis based on the nature
    /// of the detected change.
    /// </remarks>
    public ChangeCategory Category { get; init; }

    /// <summary>
    /// Gets the document section or heading where this change occurred.
    /// </summary>
    /// <value>
    /// The name of the section (e.g., "Introduction", "Timeline", "Requirements"),
    /// or <c>null</c> if the change is at the document level or in an untitled section.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> The LLM identifies the nearest heading above the change location.
    /// Uses "Introduction" for content before the first heading and "Conclusion"
    /// for final summary sections.
    /// </remarks>
    public string? Section { get; init; }

    /// <summary>
    /// Gets the human-readable description of this change.
    /// </summary>
    /// <value>
    /// A complete sentence describing what changed.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Generated by the LLM to provide a clear, concise explanation
    /// of the change suitable for display in the UI. Should be actionable and informative.
    /// </remarks>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the significance score of this change (0.0 to 1.0).
    /// </summary>
    /// <value>
    /// A value from 0.0 (trivial) to 1.0 (critical) indicating the importance of this change.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Assigned by the LLM based on the change's impact on the document's
    /// meaning, structure, and purpose. Higher scores indicate more significant changes.
    /// See <see cref="ChangeSignificance"/> for score range definitions.
    /// </remarks>
    public double Significance { get; init; }

    /// <summary>
    /// Gets the significance level derived from the score.
    /// </summary>
    /// <value>
    /// The <see cref="ChangeSignificance"/> level corresponding to the <see cref="Significance"/> score.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Computed property that converts the continuous score to a discrete level:
    /// <list type="bullet">
    /// <item><description>&gt;= 0.8: <see cref="ChangeSignificance.Critical"/></description></item>
    /// <item><description>&gt;= 0.6: <see cref="ChangeSignificance.High"/></description></item>
    /// <item><description>&gt;= 0.3: <see cref="ChangeSignificance.Medium"/></description></item>
    /// <item><description>&lt; 0.3: <see cref="ChangeSignificance.Low"/></description></item>
    /// </list>
    /// </remarks>
    public ChangeSignificance SignificanceLevel => ChangeSignificanceExtensions.FromScore(Significance);

    /// <summary>
    /// Gets the original text that was changed or removed.
    /// </summary>
    /// <value>
    /// The text from the original document, or <c>null</c> for <see cref="ChangeCategory.Added"/> changes.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Contains the text before the change was made. For long text,
    /// this may be truncated with an ellipsis indicator.
    /// </remarks>
    public string? OriginalText { get; init; }

    /// <summary>
    /// Gets the new text after the change.
    /// </summary>
    /// <value>
    /// The text in the new document, or <c>null</c> for <see cref="ChangeCategory.Removed"/> changes.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Contains the text after the change was made. For long text,
    /// this may be truncated with an ellipsis indicator.
    /// </remarks>
    public string? NewText { get; init; }

    /// <summary>
    /// Gets the line numbers affected in the original document.
    /// </summary>
    /// <value>
    /// A <see cref="LineRange"/> indicating which lines were affected in the original document,
    /// or <c>null</c> if line information is not available.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Populated from DiffPlex analysis. Useful for navigating to the
    /// change location in the original document.
    /// </remarks>
    public LineRange? OriginalLineRange { get; init; }

    /// <summary>
    /// Gets the line numbers affected in the new document.
    /// </summary>
    /// <value>
    /// A <see cref="LineRange"/> indicating which lines were affected in the new document,
    /// or <c>null</c> if line information is not available.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Populated from DiffPlex analysis. Useful for navigating to the
    /// change location in the new document.
    /// </remarks>
    public LineRange? NewLineRange { get; init; }

    /// <summary>
    /// Gets additional context about why this change matters.
    /// </summary>
    /// <value>
    /// A description of the change's impact on readers, stakeholders, or downstream systems,
    /// or <c>null</c> if no impact information is available.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Generated by the LLM to help users understand the implications
    /// of the change. Particularly valuable for high-significance changes.
    /// </remarks>
    public string? Impact { get; init; }

    /// <summary>
    /// Gets the indices of related changes in the comparison result.
    /// </summary>
    /// <value>
    /// A list of indices into <see cref="ComparisonResult.Changes"/> that identify
    /// semantically related changes, or <c>null</c> if no related changes were identified.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Used to group related changes together in the UI. For example,
    /// a terminology rename might affect multiple locations, which would all be linked.
    /// </remarks>
    public IReadOnlyList<int>? RelatedChangeIndices { get; init; }

    /// <summary>
    /// Gets whether this change has original text content.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="OriginalText"/> is not <c>null</c> or whitespace; otherwise <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience property for UI binding and display logic.
    /// </remarks>
    public bool HasOriginalText => !string.IsNullOrWhiteSpace(OriginalText);

    /// <summary>
    /// Gets whether this change has new text content.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="NewText"/> is not <c>null</c> or whitespace; otherwise <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience property for UI binding and display logic.
    /// </remarks>
    public bool HasNewText => !string.IsNullOrWhiteSpace(NewText);

    /// <summary>
    /// Gets whether this change has impact information.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Impact"/> is not <c>null</c> or whitespace; otherwise <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience property for UI binding and display logic.
    /// </remarks>
    public bool HasImpact => !string.IsNullOrWhiteSpace(Impact);

    /// <summary>
    /// Gets whether this change has related changes.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="RelatedChangeIndices"/> contains at least one index; otherwise <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience property for UI binding and display logic.
    /// </remarks>
    public bool HasRelatedChanges => RelatedChangeIndices is { Count: > 0 };
}
