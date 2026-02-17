// =============================================================================
// File: UnifiedIssue.cs
// Project: Lexichord.Abstractions
// Description: Normalized representation of validation findings from any source.
// =============================================================================
// LOGIC: Provides a unified model for issues from Style Linter, Grammar Linter,
//   and CKVS Validation Engine. This enables the Tuning Agent to process all
//   issues uniformly regardless of their source.
//
// v0.7.5e: Unified Issue Model (Unified Validation Feature)
// =============================================================================

using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;

namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Normalized representation of a validation issue from any source (Style Linter,
/// Grammar Linter, or CKVS Validation Engine).
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This record provides a unified model for the Tuning Agent to process
/// issues from multiple sources without source-specific handling. Key normalized fields:
/// <list type="bullet">
///   <item><description>Severity: Mapped to <see cref="UnifiedSeverity"/> from source-specific enums</description></item>
///   <item><description>Category: Mapped to <see cref="IssueCategory"/> from source-specific categories</description></item>
///   <item><description>Location: <see cref="TextSpan"/> for precise editor positioning</description></item>
///   <item><description>Fixes: List of <see cref="UnifiedFix"/> with confidence scores</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Relationship to v0.6.5j UnifiedFinding:</b> This type coexists with
/// <see cref="Knowledge.Validation.Integration.UnifiedFinding"/> which is used for
/// CKVS validation result aggregation. UnifiedIssue (v0.7.5e) is designed for the
/// Tuning Agent's fix workflow with location-aware fixes and source preservation.
/// </para>
/// <para>
/// <b>Factory Methods:</b> Use <see cref="UnifiedIssueFactory"/> to create instances
/// from <see cref="StyleDeviation"/>, <see cref="StyleViolation"/>, or
/// <see cref="UnifiedFinding"/>.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This record is immutable and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5e as part of the Unified Issue Model feature.
/// </para>
/// </remarks>
/// <param name="IssueId">
/// Unique identifier for this issue instance. Used for tracking through the
/// fix workflow and for deduplication.
/// </param>
/// <param name="SourceId">
/// Original identifier from the source system (rule ID, validation code, etc.).
/// Preserved for diagnostics, filtering, and linking back to source definitions.
/// </param>
/// <param name="Category">
/// Unified category for grouping and filtering in the UI.
/// </param>
/// <param name="Severity">
/// Unified severity level, normalized from source-specific severity enums.
/// </param>
/// <param name="Message">
/// Human-readable description of the issue, suitable for display in the UI.
/// </param>
/// <param name="Location">
/// Text span indicating where the issue occurs in the document.
/// </param>
/// <param name="OriginalText">
/// The text at the issue location, for display and fix verification.
/// </param>
/// <param name="Fixes">
/// List of available fixes for this issue, sorted by confidence (highest first).
/// May be empty if no automatic fix is available.
/// </param>
/// <param name="SourceType">
/// String identifying the source system: "StyleLinter", "GrammarLinter", "Validation".
/// </param>
/// <param name="OriginalSource">
/// The original source object (StyleViolation, ValidationFinding, etc.) preserved
/// for advanced operations that need source-specific data. May be null.
/// </param>
public record UnifiedIssue(
    Guid IssueId,
    string SourceId,
    IssueCategory Category,
    UnifiedSeverity Severity,
    string Message,
    TextSpan Location,
    string? OriginalText,
    IReadOnlyList<UnifiedFix> Fixes,
    string? SourceType,
    object? OriginalSource)
{
    /// <summary>
    /// Gets whether this issue has any available fixes.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Returns true if <see cref="Fixes"/> contains at least one fix
    /// that is not of type <see cref="FixType.NoFix"/>.
    /// </remarks>
    public bool HasFixes => Fixes.Any(f => f.Type != FixType.NoFix);

    /// <summary>
    /// Gets the best (highest confidence) fix for this issue.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Returns the first fix in the list (assumed to be sorted by confidence)
    /// that is not of type <see cref="FixType.NoFix"/>, or null if no fixes are available.
    /// </remarks>
    public UnifiedFix? BestFix => Fixes.FirstOrDefault(f => f.Type != FixType.NoFix);

    /// <summary>
    /// Gets whether the best fix is high-confidence (>= 0.8).
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Used to identify issues that are candidates for bulk acceptance
    /// in the Tuning Agent review panel.
    /// </remarks>
    public bool HasHighConfidenceFix => BestFix?.IsHighConfidence == true;

    /// <summary>
    /// Gets whether this issue can be automatically fixed.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Returns true if the best fix exists and has <see cref="UnifiedFix.CanAutoApply"/>
    /// set to true.
    /// </remarks>
    public bool CanAutoFix => BestFix?.CanAutoApply == true;

    /// <summary>
    /// Gets the severity as a numeric value for sorting (lower = more severe).
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Error=0 is most severe, Hint=3 is least severe.
    /// Used for sorting issues by severity in descending order.
    /// </remarks>
    public int SeverityOrder => (int)Severity;

    /// <summary>
    /// Gets whether this issue originates from the Style Linter.
    /// </summary>
    public bool IsFromStyleLinter => SourceType == "StyleLinter";

    /// <summary>
    /// Gets whether this issue originates from the Grammar Linter.
    /// </summary>
    public bool IsFromGrammarLinter => SourceType == "GrammarLinter";

    /// <summary>
    /// Gets whether this issue originates from CKVS Validation.
    /// </summary>
    public bool IsFromValidation => SourceType == "Validation";

    /// <summary>
    /// Creates a copy of this issue with updated fixes.
    /// </summary>
    /// <param name="newFixes">The new list of fixes.</param>
    /// <returns>A new <see cref="UnifiedIssue"/> with the updated fixes.</returns>
    public UnifiedIssue WithFixes(IReadOnlyList<UnifiedFix> newFixes) =>
        this with { Fixes = newFixes };

    /// <summary>
    /// Creates a copy of this issue with an additional fix.
    /// </summary>
    /// <param name="fix">The fix to add.</param>
    /// <returns>A new <see cref="UnifiedIssue"/> with the fix added.</returns>
    public UnifiedIssue WithAdditionalFix(UnifiedFix fix) =>
        this with { Fixes = Fixes.Append(fix).OrderByDescending(f => f.Confidence).ToList() };

    /// <summary>
    /// Creates an empty issue placeholder.
    /// </summary>
    /// <returns>An empty <see cref="UnifiedIssue"/> instance.</returns>
    public static UnifiedIssue Empty { get; } = new(
        Guid.Empty,
        string.Empty,
        IssueCategory.Custom,
        UnifiedSeverity.Info,
        string.Empty,
        TextSpan.Empty,
        null,
        Array.Empty<UnifiedFix>(),
        null,
        null);
}
