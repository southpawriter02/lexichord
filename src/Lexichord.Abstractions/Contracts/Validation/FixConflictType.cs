// =============================================================================
// File: FixConflictType.cs
// Project: Lexichord.Abstractions
// Description: Types and severity levels for fix conflicts in the fix workflow.
// =============================================================================
// LOGIC: Categorizes detected conflicts between fixes and assigns severity
//   levels to guide conflict resolution strategies.
//
// v0.7.5h: Combined Fix Workflow (Unified Validation Feature)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Types of conflicts that can occur between fixes.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="IUnifiedFixWorkflow"/> detects these conflict types
/// before applying fixes to prevent document corruption:
/// <list type="bullet">
///   <item><description><see cref="OverlappingPositions"/>: Text spans overlap</description></item>
///   <item><description><see cref="ContradictorySuggestions"/>: Same location, different fixes</description></item>
///   <item><description><see cref="DependentFixes"/>: One fix depends on another</description></item>
///   <item><description><see cref="CreatesNewIssue"/>: Fix would create new validation error</description></item>
///   <item><description><see cref="InvalidLocation"/>: Fix location out of bounds</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Enum values are immutable and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5h as part of the Combined Fix Workflow feature.
/// </para>
/// </remarks>
/// <seealso cref="FixConflictCase"/>
/// <seealso cref="FixConflictSeverity"/>
public enum FixConflictType
{
    /// <summary>
    /// Two fixes target overlapping text regions.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Detected via <see cref="Editor.TextSpan.OverlapsWith"/>. Applying
    /// both fixes would corrupt the document because offset calculations become invalid
    /// when text ranges overlap.
    /// </remarks>
    OverlappingPositions = 0,

    /// <summary>
    /// Fixes suggest contradictory changes to the same location.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Two or more fixes target the exact same text range but propose
    /// different replacement text. Only one can be applied.
    /// </remarks>
    ContradictorySuggestions = 1,

    /// <summary>
    /// One fix depends on another fix's successful application.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Detected when Style and Grammar fixes target nearby locations
    /// (within 200 characters). A Grammar fix may become invalid if a Style fix
    /// changes the surrounding text. Category ordering (Knowledge → Grammar → Style)
    /// mitigates this, but the dependency is still tracked.
    /// </remarks>
    DependentFixes = 2,

    /// <summary>
    /// Applying the fix would create a new validation error.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Detected during re-validation after fix application. The fix
    /// resolves one issue but introduces a different one.
    /// </remarks>
    CreatesNewIssue = 3,

    /// <summary>
    /// The fix location is invalid or out of document bounds.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> The fix's <see cref="Editor.TextSpan"/> references a position
    /// beyond the document's length or has negative coordinates.
    /// </remarks>
    InvalidLocation = 4
}

/// <summary>
/// Severity level of a detected fix conflict.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Determines how urgently a conflict needs to be addressed:
/// <list type="bullet">
///   <item><description><see cref="Info"/>: Informational, can be safely ignored</description></item>
///   <item><description><see cref="Warning"/>: Should be reviewed but won't corrupt document</description></item>
///   <item><description><see cref="Error"/>: Prevents fix application, must be resolved</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Enum values are immutable and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5h as part of the Combined Fix Workflow feature.
/// </para>
/// </remarks>
/// <seealso cref="FixConflictCase"/>
/// <seealso cref="FixConflictType"/>
public enum FixConflictSeverity
{
    /// <summary>
    /// Conflict noted but can be safely ignored.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Used for dependency relationships that are mitigated by
    /// category ordering.
    /// </remarks>
    Info = 0,

    /// <summary>
    /// Conflict should be reviewed but doesn't prevent fix application.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Used for dependent fixes and potential new-issue creation.
    /// </remarks>
    Warning = 1,

    /// <summary>
    /// Conflict prevents fix application and must be resolved.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Used for overlapping positions and contradictory suggestions.
    /// These conflicts would corrupt the document if ignored.
    /// </remarks>
    Error = 2
}
