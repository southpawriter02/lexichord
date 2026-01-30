namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Represents a quick-fix action for a style violation.
/// </summary>
/// <remarks>
/// LOGIC: QuickFixAction encapsulates the data needed to apply a fix:
/// - ViolationId links back to the violation being fixed
/// - Title provides user-visible text for the context menu
/// - ReplacementText is the suggested replacement
/// - StartOffset and Length define the range to replace
///
/// Design Decision: Immutable record ensures thread-safety when
/// passed between UI thread and background operations.
///
/// Version: v0.2.4d
/// </remarks>
public record QuickFixAction
{
    /// <summary>
    /// The ID of the violation this fix addresses.
    /// </summary>
    public required string ViolationId { get; init; }

    /// <summary>
    /// Display title for the context menu item.
    /// </summary>
    /// <remarks>
    /// LOGIC: Formatted as "Replace 'X' with 'Y'" or similar user-friendly text.
    /// </remarks>
    public required string Title { get; init; }

    /// <summary>
    /// The text to replace the violation with.
    /// </summary>
    public required string ReplacementText { get; init; }

    /// <summary>
    /// Character offset where the replacement starts (0-indexed).
    /// </summary>
    public required int StartOffset { get; init; }

    /// <summary>
    /// Length of text to replace.
    /// </summary>
    public required int Length { get; init; }

    /// <summary>
    /// Gets the end offset (exclusive).
    /// </summary>
    public int EndOffset => StartOffset + Length;
}
