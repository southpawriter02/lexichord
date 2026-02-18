// -----------------------------------------------------------------------
// <copyright file="ComparisonOptions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Immutable record for document comparison configuration (v0.7.6d).
//   Provides options to control filtering, grouping, and output of comparisons.
//
//   Options:
//     - SignificanceThreshold: Minimum score to include changes (default 0.2)
//     - IncludeFormattingChanges: Whether to show formatting-only changes (default false)
//     - GroupBySection: Group changes by document section (default true)
//     - MaxChanges: Maximum number of changes to return (default 20)
//     - FocusSections: Limit analysis to specific sections (default null = all)
//     - IncludeTextDiff: Include raw text diff (default false)
//     - IdentifyRelatedChanges: Link related changes (default true)
//     - OriginalVersionLabel/NewVersionLabel: Labels for versions
//
//   Introduced in: v0.7.6d as part of Document Comparison
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.DocumentComparison;

/// <summary>
/// Configuration options for document comparison operations.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This immutable record provides configuration options that control
/// how document comparisons are performed, what changes are included in the results,
/// and how the output is formatted.
/// </para>
/// <para>
/// <b>Default Behavior:</b>
/// With default options, comparisons will:
/// <list type="bullet">
/// <item><description>Include all changes with significance >= 0.2 (excludes trivial)</description></item>
/// <item><description>Exclude formatting-only changes</description></item>
/// <item><description>Group changes by document section</description></item>
/// <item><description>Return at most 20 changes</description></item>
/// <item><description>Analyze all sections</description></item>
/// <item><description>Identify and link related changes</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Validation:</b>
/// Use <see cref="Validate"/> to verify option values are within valid ranges.
/// Invalid values will throw <see cref="ArgumentException"/> with descriptive messages.
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
/// // Use defaults
/// var result = await comparer.CompareContentAsync(original, updated);
///
/// // Custom options
/// var options = new ComparisonOptions
/// {
///     SignificanceThreshold = 0.5,  // Only important+ changes
///     MaxChanges = 10,
///     OriginalVersionLabel = "v1.0",
///     NewVersionLabel = "v1.1"
/// };
/// var result = await comparer.CompareContentAsync(original, updated, options);
///
/// // Focus on specific sections
/// var options = new ComparisonOptions
/// {
///     FocusSections = new[] { "Requirements", "Timeline" }
/// };
/// </code>
/// </example>
public record ComparisonOptions
{
    /// <summary>
    /// Default comparison options.
    /// </summary>
    /// <value>
    /// A shared <see cref="ComparisonOptions"/> instance with all default values.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Static default instance to avoid repeated allocations.
    /// </remarks>
    public static ComparisonOptions Default { get; } = new();

    /// <summary>
    /// Gets the minimum significance score to include a change.
    /// </summary>
    /// <value>
    /// A value from 0.0 to 1.0 representing the minimum significance threshold.
    /// Changes below this threshold are filtered out.
    /// Default is 0.2 (shows all but trivial changes).
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Set to 0.0 to include all changes including trivial ones.
    /// Set to 0.3 to exclude Low significance changes.
    /// Set to 0.6 to show only High and Critical changes.
    /// Set to 0.8 to show only Critical changes.
    /// </remarks>
    public double SignificanceThreshold { get; init; } = 0.2;

    /// <summary>
    /// Gets whether to include formatting-only changes.
    /// </summary>
    /// <value>
    /// <c>true</c> to include <see cref="ChangeCategory.Formatting"/> changes;
    /// <c>false</c> to filter them out.
    /// Default is <c>false</c> (focus on semantic changes).
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Formatting changes (heading levels, list styles, code block formatting)
    /// don't affect document meaning and are often noise. Enable this option when
    /// reviewing formatting or style guide compliance.
    /// </remarks>
    public bool IncludeFormattingChanges { get; init; } = false;

    /// <summary>
    /// Gets whether to group changes by section in the output.
    /// </summary>
    /// <value>
    /// <c>true</c> to order changes by section, then by significance within each section;
    /// <c>false</c> to order purely by significance.
    /// Default is <c>true</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Section grouping helps users understand changes in context.
    /// Disable for a pure significance-based view where the most important changes
    /// appear first regardless of location.
    /// </remarks>
    public bool GroupBySection { get; init; } = true;

    /// <summary>
    /// Gets the maximum number of changes to report.
    /// </summary>
    /// <value>
    /// The maximum number of <see cref="DocumentChange"/> items to include in the result.
    /// Changes beyond this limit are summarized.
    /// Default is 20. Range: 1-100.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Limits output size for large documents with many changes.
    /// The summary text will indicate if additional changes were detected but not listed.
    /// </remarks>
    public int MaxChanges { get; init; } = 20;

    /// <summary>
    /// Gets the specific sections to focus analysis on.
    /// </summary>
    /// <value>
    /// A list of section names to analyze, or <c>null</c> to analyze all sections.
    /// Default is <c>null</c> (all sections).
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> When set, only changes in the specified sections are analyzed
    /// and returned. Useful for targeted review of specific document areas.
    /// Section names should match document headings.
    /// </remarks>
    public IReadOnlyList<string>? FocusSections { get; init; }

    /// <summary>
    /// Gets whether to include the full text diff alongside semantic analysis.
    /// </summary>
    /// <value>
    /// <c>true</c> to include a unified diff format string in the result;
    /// <c>false</c> to omit it.
    /// Default is <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> The text diff is generated by DiffPlex and provides a
    /// traditional line-by-line view of changes. Useful for detailed review
    /// or when semantic analysis may miss subtle changes.
    /// </remarks>
    public bool IncludeTextDiff { get; init; } = false;

    /// <summary>
    /// Gets whether to identify and link related changes.
    /// </summary>
    /// <value>
    /// <c>true</c> to analyze changes for semantic relationships and populate
    /// <see cref="DocumentChange.RelatedChangeIndices"/>;
    /// <c>false</c> to skip relationship analysis.
    /// Default is <c>true</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Related changes are changes that are semantically connected,
    /// such as a terminology rename that appears in multiple locations. Linking
    /// them helps users understand the full scope of certain changes.
    /// </remarks>
    public bool IdentifyRelatedChanges { get; init; } = true;

    /// <summary>
    /// Gets the label for the original (older) version.
    /// </summary>
    /// <value>
    /// A human-readable label such as "v1.0", "Jan 15", or "HEAD~1";
    /// or <c>null</c> if no label is specified.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Used in the UI header and summary text to identify the
    /// original version being compared.
    /// </remarks>
    public string? OriginalVersionLabel { get; init; }

    /// <summary>
    /// Gets the label for the new (current) version.
    /// </summary>
    /// <value>
    /// A human-readable label such as "v1.1", "Jan 27", or "HEAD";
    /// or <c>null</c> if no label is specified.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Used in the UI header and summary text to identify the
    /// new version being compared.
    /// </remarks>
    public string? NewVersionLabel { get; init; }

    /// <summary>
    /// Gets the maximum tokens to use for LLM response.
    /// </summary>
    /// <value>
    /// The maximum number of tokens for the LLM completion.
    /// Default is 4096.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Higher values allow for more detailed analysis of complex
    /// documents with many changes. Reduce for faster responses on simple comparisons.
    /// </remarks>
    public int MaxResponseTokens { get; init; } = 4096;

    /// <summary>
    /// Validates this options instance and throws if any values are invalid.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when any option value is outside its valid range.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Validates:
    /// <list type="bullet">
    /// <item><description><see cref="SignificanceThreshold"/>: Must be between 0.0 and 1.0.</description></item>
    /// <item><description><see cref="MaxChanges"/>: Must be between 1 and 100.</description></item>
    /// <item><description><see cref="MaxResponseTokens"/>: Must be between 256 and 16384.</description></item>
    /// </list>
    /// </remarks>
    public void Validate()
    {
        if (SignificanceThreshold is < 0.0 or > 1.0)
        {
            throw new ArgumentException(
                $"SignificanceThreshold must be between 0.0 and 1.0, but was {SignificanceThreshold}.",
                nameof(SignificanceThreshold));
        }

        if (MaxChanges is < 1 or > 100)
        {
            throw new ArgumentException(
                $"MaxChanges must be between 1 and 100, but was {MaxChanges}.",
                nameof(MaxChanges));
        }

        if (MaxResponseTokens is < 256 or > 16384)
        {
            throw new ArgumentException(
                $"MaxResponseTokens must be between 256 and 16384, but was {MaxResponseTokens}.",
                nameof(MaxResponseTokens));
        }
    }

    /// <summary>
    /// Creates a copy of this options with updated version labels.
    /// </summary>
    /// <param name="originalLabel">Label for the original version.</param>
    /// <param name="newLabel">Label for the new version.</param>
    /// <returns>A new <see cref="ComparisonOptions"/> with the specified labels.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience method for setting labels without using "with" syntax.
    /// </remarks>
    public ComparisonOptions WithLabels(string? originalLabel, string? newLabel) =>
        this with
        {
            OriginalVersionLabel = originalLabel,
            NewVersionLabel = newLabel
        };
}
