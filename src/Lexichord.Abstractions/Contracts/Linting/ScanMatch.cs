namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Represents a pattern match from the Scanner, enriched with rule context.
/// </summary>
/// <remarks>
/// LOGIC: Bridge between IScannerService (which returns PatternMatchSpan)
/// and IViolationAggregator (which needs full rule context to build violations).
///
/// The Scanner produces raw match positions; this record adds:
/// - The matched text extracted from content
/// - The full StyleRule for message building and severity
/// - Optional capture groups from regex patterns
///
/// Version: v0.2.3d
/// </remarks>
/// <param name="RuleId">The ID of the rule that matched.</param>
/// <param name="StartOffset">Character offset where the match starts (0-indexed).</param>
/// <param name="Length">Length of the matched text.</param>
/// <param name="MatchedText">The text that was matched.</param>
/// <param name="Rule">The full style rule for message and severity context.</param>
/// <param name="CaptureGroups">Named capture groups from regex patterns (if any).</param>
public sealed record ScanMatch(
    string RuleId,
    int StartOffset,
    int Length,
    string MatchedText,
    StyleRule Rule,
    IReadOnlyDictionary<string, string>? CaptureGroups = null)
{
    /// <summary>
    /// Gets the end offset (exclusive).
    /// </summary>
    public int EndOffset => StartOffset + Length;

    /// <summary>
    /// Creates a ScanMatch from a PatternMatchSpan and rule context.
    /// </summary>
    /// <param name="span">The match span from the scanner.</param>
    /// <param name="content">The document content for text extraction.</param>
    /// <param name="rule">The rule that matched.</param>
    /// <param name="captureGroups">Optional capture groups.</param>
    /// <returns>A fully populated ScanMatch.</returns>
    /// <remarks>
    /// LOGIC: Factory method to simplify creation from scanner output.
    /// Extracts matched text from content using span positions.
    /// </remarks>
    public static ScanMatch FromSpan(
        PatternMatchSpan span,
        string content,
        StyleRule rule,
        IReadOnlyDictionary<string, string>? captureGroups = null)
    {
        var matchedText = span.StartOffset >= 0 && span.EndOffset <= content.Length
            ? content.Substring(span.StartOffset, span.Length)
            : string.Empty;

        return new ScanMatch(
            RuleId: rule.Id,
            StartOffset: span.StartOffset,
            Length: span.Length,
            MatchedText: matchedText,
            Rule: rule,
            CaptureGroups: captureGroups);
    }
}
