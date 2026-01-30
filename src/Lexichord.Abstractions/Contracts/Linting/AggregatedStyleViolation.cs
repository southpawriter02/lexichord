namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Represents a fully enriched style violation with position and context.
/// </summary>
/// <remarks>
/// LOGIC: This is the output of IViolationAggregator.Aggregate().
/// It extends the raw match data with:
/// - Unique violation ID for stable references
/// - 1-based line and column numbers for editor display
/// - Expanded message with placeholder substitution
/// - All rule metadata (severity, category, suggestion)
///
/// Design Decision: Named "AggregatedStyleViolation" to distinguish from
/// the simpler StyleViolation in StyleDomainTypes.cs (v0.2.1b).
///
/// Threading: Immutable record, safe for concurrent access.
///
/// Version: v0.2.3d
/// </remarks>
public record AggregatedStyleViolation
{
    /// <summary>
    /// Unique identifier for this violation (stable across re-scans if same position/rule).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The document this violation belongs to.
    /// </summary>
    public required string DocumentId { get; init; }

    /// <summary>
    /// The ID of the rule that was violated.
    /// </summary>
    public required string RuleId { get; init; }

    /// <summary>
    /// Character offset where the violation starts (0-indexed).
    /// </summary>
    public required int StartOffset { get; init; }

    /// <summary>
    /// Length of the violating text in characters.
    /// </summary>
    public required int Length { get; init; }

    /// <summary>
    /// Line number where the violation starts (1-indexed).
    /// </summary>
    public required int Line { get; init; }

    /// <summary>
    /// Column number where the violation starts (1-indexed).
    /// </summary>
    public required int Column { get; init; }

    /// <summary>
    /// Line number where the violation ends (1-indexed).
    /// </summary>
    public required int EndLine { get; init; }

    /// <summary>
    /// Column number where the violation ends (1-indexed).
    /// </summary>
    public required int EndColumn { get; init; }

    /// <summary>
    /// The text that triggered the violation.
    /// </summary>
    public required string ViolatingText { get; init; }

    /// <summary>
    /// Human-readable message describing the violation.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Severity level of the violation.
    /// </summary>
    public required ViolationSeverity Severity { get; init; }

    /// <summary>
    /// Optional suggested replacement text.
    /// </summary>
    public string? Suggestion { get; init; }

    /// <summary>
    /// Category of the violated rule.
    /// </summary>
    public required RuleCategory Category { get; init; }

    /// <summary>
    /// Gets the end offset (exclusive).
    /// </summary>
    public int EndOffset => StartOffset + Length;

    /// <summary>
    /// Gets whether this violation has a suggestion.
    /// </summary>
    public bool HasSuggestion => !string.IsNullOrEmpty(Suggestion);

    /// <summary>
    /// Creates a copy with the start offset adjusted by delta.
    /// </summary>
    /// <param name="delta">Amount to add to start offset.</param>
    /// <returns>A new violation with adjusted position.</returns>
    /// <remarks>
    /// LOGIC: Useful when applying edits that shift document positions.
    /// </remarks>
    public AggregatedStyleViolation WithOffset(int delta) =>
        this with { StartOffset = StartOffset + delta };

    /// <summary>
    /// Checks if this violation overlaps with another.
    /// </summary>
    /// <param name="other">The other violation to check.</param>
    /// <returns>True if the violations share any character positions.</returns>
    /// <remarks>
    /// LOGIC: Used for deduplication. Two ranges [a,b) and [c,d) overlap
    /// if a < d AND c < b.
    /// </remarks>
    public bool OverlapsWith(AggregatedStyleViolation other) =>
        StartOffset < other.EndOffset && other.StartOffset < EndOffset;

    /// <summary>
    /// Checks if this violation contains a specific character offset.
    /// </summary>
    /// <param name="offset">The character offset to check.</param>
    /// <returns>True if the offset is within this violation's range.</returns>
    /// <remarks>
    /// LOGIC: Used by GetViolationAt for hover/click lookup.
    /// </remarks>
    public bool ContainsOffset(int offset) =>
        offset >= StartOffset && offset < EndOffset;
}
