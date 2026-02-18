// -----------------------------------------------------------------------
// <copyright file="ComparisonResult.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Immutable record representing the result of a document comparison (v0.7.6d).
//   Contains the summary, detected changes, metrics, and usage information.
//
//   Properties:
//     - OriginalPath/NewPath: Paths to compared documents
//     - OriginalLabel/NewLabel: Version labels
//     - Summary: Natural language summary of changes
//     - Changes: List of DocumentChange records
//     - ChangeMagnitude: Overall change score 0.0-1.0
//     - OriginalWordCount/NewWordCount: Word counts
//     - WordCountDelta: Net change in words (computed)
//     - AdditionCount/DeletionCount/ModificationCount: Counts by category (computed)
//     - AffectedSections: Sections with significant changes
//     - Usage: Token usage metrics
//     - ComparedAt: Timestamp
//     - AreIdentical: Whether documents are the same (computed)
//     - Success/ErrorMessage: Result status
//     - TextDiff: Optional raw diff output
//
//   Factory methods:
//     - Failed(): Creates a failure result with error message
//
//   Introduced in: v0.7.6d as part of Document Comparison
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents;

namespace Lexichord.Abstractions.Agents.DocumentComparison;

/// <summary>
/// Result of comparing two document versions.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This immutable record contains all information about a completed
/// document comparison, including the detected changes, summary, metrics, and
/// usage information. It also supports failure states with error messages.
/// </para>
/// <para>
/// <b>Success vs Failure:</b>
/// <list type="bullet">
/// <item><description>Successful results have <see cref="Success"/> = <c>true</c> and populated change data.</description></item>
/// <item><description>Failed results have <see cref="Success"/> = <c>false</c>, an <see cref="ErrorMessage"/>, and empty collections.</description></item>
/// </list>
/// Use the <see cref="Failed"/> factory method to create failure results.
/// </para>
/// <para>
/// <b>Computed Properties:</b>
/// Several properties are computed from the <see cref="Changes"/> collection:
/// <list type="bullet">
/// <item><description><see cref="WordCountDelta"/>: <c>NewWordCount - OriginalWordCount</c></description></item>
/// <item><description><see cref="AdditionCount"/>: Count of <see cref="ChangeCategory.Added"/> changes</description></item>
/// <item><description><see cref="DeletionCount"/>: Count of <see cref="ChangeCategory.Removed"/> changes</description></item>
/// <item><description><see cref="ModificationCount"/>: Count of <see cref="ChangeCategory.Modified"/> changes</description></item>
/// <item><description><see cref="AreIdentical"/>: <c>true</c> if magnitude is 0 and no changes exist</description></item>
/// </list>
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
/// // Successful comparison result
/// var result = await comparer.CompareContentAsync(original, updated);
///
/// if (result.Success)
/// {
///     Console.WriteLine($"Summary: {result.Summary}");
///     Console.WriteLine($"Change magnitude: {result.ChangeMagnitude:P0}");
///     Console.WriteLine($"Additions: {result.AdditionCount}");
///     Console.WriteLine($"Deletions: {result.DeletionCount}");
///     Console.WriteLine($"Modifications: {result.ModificationCount}");
///
///     foreach (var change in result.Changes)
///     {
///         Console.WriteLine($"- [{change.Category}] {change.Description}");
///     }
/// }
/// else
/// {
///     Console.WriteLine($"Comparison failed: {result.ErrorMessage}");
/// }
/// </code>
/// </example>
public record ComparisonResult
{
    /// <summary>
    /// Gets the path to the original (older) document.
    /// </summary>
    /// <value>
    /// The file path to the original document, or a descriptive string for content comparisons.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> For file-based comparisons, this is the actual file path.
    /// For content-based comparisons, this may be a placeholder like "[content]".
    /// </remarks>
    public required string OriginalPath { get; init; }

    /// <summary>
    /// Gets the path to the new (current) document.
    /// </summary>
    /// <value>
    /// The file path to the new document, or a descriptive string for content comparisons.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> For file-based comparisons, this is the actual file path.
    /// For content-based comparisons, this may be a placeholder like "[content]".
    /// </remarks>
    public required string NewPath { get; init; }

    /// <summary>
    /// Gets the label for the original version.
    /// </summary>
    /// <value>
    /// A human-readable label such as "v1.0" or "Jan 15", or <c>null</c> if not specified.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Copied from <see cref="ComparisonOptions.OriginalVersionLabel"/>.
    /// </remarks>
    public string? OriginalLabel { get; init; }

    /// <summary>
    /// Gets the label for the new version.
    /// </summary>
    /// <value>
    /// A human-readable label such as "v1.1" or "Jan 27", or <c>null</c> if not specified.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Copied from <see cref="ComparisonOptions.NewVersionLabel"/>.
    /// </remarks>
    public string? NewLabel { get; init; }

    /// <summary>
    /// Gets the overall summary of changes in natural language.
    /// </summary>
    /// <value>
    /// A 2-3 sentence description of the major changes, or an error message for failed comparisons.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Generated by the LLM to provide a high-level overview of what changed.
    /// For identical documents, this will indicate "No changes detected."
    /// </remarks>
    public required string Summary { get; init; }

    /// <summary>
    /// Gets the list of individual changes detected.
    /// </summary>
    /// <value>
    /// A list of <see cref="DocumentChange"/> records ordered by significance (highest first),
    /// or an empty list for identical documents or failed comparisons.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> The changes are filtered and ordered according to the
    /// <see cref="ComparisonOptions"/> used. The list may be truncated if
    /// more changes were detected than <see cref="ComparisonOptions.MaxChanges"/>.
    /// </remarks>
    public required IReadOnlyList<DocumentChange> Changes { get; init; }

    /// <summary>
    /// Gets the overall change magnitude.
    /// </summary>
    /// <value>
    /// A value from 0.0 (identical) to 1.0 (completely different) indicating
    /// how much the document has changed overall.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Calculated by the LLM based on the semantic impact of all changes.
    /// A magnitude of 0.0 means identical documents; 1.0 means a complete rewrite.
    /// </remarks>
    public double ChangeMagnitude { get; init; }

    /// <summary>
    /// Gets the word count of the original document.
    /// </summary>
    /// <value>
    /// The number of words in the original document.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Calculated using whitespace splitting. Used to calculate
    /// <see cref="WordCountDelta"/>.
    /// </remarks>
    public int OriginalWordCount { get; init; }

    /// <summary>
    /// Gets the word count of the new document.
    /// </summary>
    /// <value>
    /// The number of words in the new document.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Calculated using whitespace splitting. Used to calculate
    /// <see cref="WordCountDelta"/>.
    /// </remarks>
    public int NewWordCount { get; init; }

    /// <summary>
    /// Gets the net change in word count.
    /// </summary>
    /// <value>
    /// Positive value indicates words were added; negative indicates words were removed.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Computed as <c>NewWordCount - OriginalWordCount</c>.
    /// </remarks>
    public int WordCountDelta => NewWordCount - OriginalWordCount;

    /// <summary>
    /// Gets the number of additions.
    /// </summary>
    /// <value>
    /// The count of changes with <see cref="ChangeCategory.Added"/>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Computed by filtering <see cref="Changes"/> by category.
    /// </remarks>
    public int AdditionCount => Changes.Count(c => c.Category == ChangeCategory.Added);

    /// <summary>
    /// Gets the number of deletions.
    /// </summary>
    /// <value>
    /// The count of changes with <see cref="ChangeCategory.Removed"/>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Computed by filtering <see cref="Changes"/> by category.
    /// </remarks>
    public int DeletionCount => Changes.Count(c => c.Category == ChangeCategory.Removed);

    /// <summary>
    /// Gets the number of modifications.
    /// </summary>
    /// <value>
    /// The count of changes with <see cref="ChangeCategory.Modified"/>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Computed by filtering <see cref="Changes"/> by category.
    /// </remarks>
    public int ModificationCount => Changes.Count(c => c.Category == ChangeCategory.Modified);

    /// <summary>
    /// Gets the sections that were significantly changed.
    /// </summary>
    /// <value>
    /// A list of section names that contain changes, or an empty list for
    /// identical documents or failed comparisons.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Extracted from the detected changes and deduplicated.
    /// Useful for quick navigation to changed areas.
    /// </remarks>
    public required IReadOnlyList<string> AffectedSections { get; init; }

    /// <summary>
    /// Gets the token usage for this comparison.
    /// </summary>
    /// <value>
    /// The <see cref="UsageMetrics"/> tracking prompt tokens, completion tokens, and estimated cost.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Populated from the LLM response. Failed comparisons have <see cref="UsageMetrics.Zero"/>.
    /// </remarks>
    public required UsageMetrics Usage { get; init; }

    /// <summary>
    /// Gets the timestamp when the comparison was performed.
    /// </summary>
    /// <value>
    /// The UTC time when the comparison completed.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Set at the end of the comparison operation.
    /// </remarks>
    public DateTimeOffset ComparedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets whether the documents are identical.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="ChangeMagnitude"/> is 0.0 and <see cref="Changes"/> is empty;
    /// otherwise <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Computed property for quick identical-check without examining all changes.
    /// </remarks>
    public bool AreIdentical => ChangeMagnitude == 0.0 && Changes.Count == 0;

    /// <summary>
    /// Gets whether the comparison was successful.
    /// </summary>
    /// <value>
    /// <c>true</c> if the comparison completed without errors; otherwise <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Failed results have this set to <c>false</c> and include an <see cref="ErrorMessage"/>.
    /// </remarks>
    public bool Success { get; init; } = true;

    /// <summary>
    /// Gets the error message if the comparison failed.
    /// </summary>
    /// <value>
    /// A description of what went wrong, or <c>null</c> for successful comparisons.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Only populated when <see cref="Success"/> is <c>false</c>.
    /// </remarks>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the raw text diff if requested.
    /// </summary>
    /// <value>
    /// A unified diff format string, or <c>null</c> if <see cref="ComparisonOptions.IncludeTextDiff"/>
    /// was <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Generated by DiffPlex when requested. Provides traditional
    /// line-by-line diff view.
    /// </remarks>
    public string? TextDiff { get; init; }

    /// <summary>
    /// Creates a failed comparison result.
    /// </summary>
    /// <param name="originalPath">Path to the original document.</param>
    /// <param name="newPath">Path to the new document.</param>
    /// <param name="errorMessage">Description of the failure.</param>
    /// <param name="originalWordCount">Word count of original document (if known).</param>
    /// <param name="newWordCount">Word count of new document (if known).</param>
    /// <returns>A <see cref="ComparisonResult"/> representing a failed comparison.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for creating consistent failure results.
    /// Sets <see cref="Success"/> to <c>false</c>, provides an error summary,
    /// and uses <see cref="UsageMetrics.Zero"/> for usage.
    /// </remarks>
    /// <example>
    /// <code>
    /// catch (Exception ex)
    /// {
    ///     return ComparisonResult.Failed(
    ///         originalPath,
    ///         newPath,
    ///         $"Comparison failed: {ex.Message}");
    /// }
    /// </code>
    /// </example>
    public static ComparisonResult Failed(
        string originalPath,
        string newPath,
        string errorMessage,
        int originalWordCount = 0,
        int newWordCount = 0) => new()
        {
            OriginalPath = originalPath,
            NewPath = newPath,
            Summary = errorMessage,
            Changes = [],
            AffectedSections = [],
            Usage = UsageMetrics.Zero,
            Success = false,
            ErrorMessage = errorMessage,
            OriginalWordCount = originalWordCount,
            NewWordCount = newWordCount,
            ChangeMagnitude = 0.0,
            ComparedAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Creates a result for identical documents.
    /// </summary>
    /// <param name="originalPath">Path to the original document.</param>
    /// <param name="newPath">Path to the new document.</param>
    /// <param name="wordCount">Word count of both documents.</param>
    /// <returns>A <see cref="ComparisonResult"/> representing identical documents.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for the common case of identical documents.
    /// No LLM call is made for identical documents, so usage is zero.
    /// </remarks>
    public static ComparisonResult Identical(
        string originalPath,
        string newPath,
        int wordCount) => new()
        {
            OriginalPath = originalPath,
            NewPath = newPath,
            Summary = "No changes detected. The documents are identical.",
            Changes = [],
            AffectedSections = [],
            Usage = UsageMetrics.Zero,
            Success = true,
            OriginalWordCount = wordCount,
            NewWordCount = wordCount,
            ChangeMagnitude = 0.0,
            ComparedAt = DateTimeOffset.UtcNow
        };
}
