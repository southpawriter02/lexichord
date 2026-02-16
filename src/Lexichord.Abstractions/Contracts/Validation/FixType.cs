// =============================================================================
// File: FixType.cs
// Project: Lexichord.Abstractions
// Description: Types of fixes that can be applied to resolve validation issues.
// =============================================================================
// LOGIC: Categorizes fix operations by their effect on the document text.
//   This enables the fix application logic to handle different fix types
//   appropriately and allows the UI to display relevant information.
//
// v0.7.5e: Unified Issue Model (Unified Validation Feature)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Types of fixes that can be applied to resolve validation issues.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Fix types categorize the kind of text modification required:
/// <list type="bullet">
///   <item><description>Replacement: Swap OldText for NewText at a specific location</description></item>
///   <item><description>Insertion: Add NewText at a position (no text removed)</description></item>
///   <item><description>Deletion: Remove text at a position (no text added)</description></item>
///   <item><description>Rewrite: AI-generated rewrite that may restructure content</description></item>
///   <item><description>NoFix: Issue cannot be automatically fixed</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Application Logic:</b>
/// <list type="bullet">
///   <item><description>Replacement/Rewrite: Delete OldText, insert NewText</description></item>
///   <item><description>Insertion: Insert NewText at Location.Start</description></item>
///   <item><description>Deletion: Delete text from Location.Start to Location.End</description></item>
///   <item><description>NoFix: Display issue only, no automatic fix available</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5e as part of the Unified Issue Model feature.
/// </para>
/// </remarks>
public enum FixType
{
    /// <summary>
    /// Replace existing text with new text at the specified location.
    /// </summary>
    /// <remarks>
    /// The most common fix type. Removes text at <see cref="UnifiedFix.Location"/>
    /// and inserts <see cref="UnifiedFix.NewText"/> in its place.
    /// Both OldText and NewText must be non-null.
    /// </remarks>
    Replacement = 0,

    /// <summary>
    /// Insert new text at the specified position without removing existing text.
    /// </summary>
    /// <remarks>
    /// Used for adding missing content. Inserts <see cref="UnifiedFix.NewText"/>
    /// at <see cref="UnifiedFix.Location"/>.<see cref="Editor.TextSpan.Start"/>.
    /// OldText should be null or empty; NewText must be non-null.
    /// </remarks>
    Insertion = 1,

    /// <summary>
    /// Delete text at the specified location without inserting replacement text.
    /// </summary>
    /// <remarks>
    /// Used for removing extraneous content. Deletes text at
    /// <see cref="UnifiedFix.Location"/> without inserting replacement.
    /// OldText should be non-null; NewText should be null or empty.
    /// </remarks>
    Deletion = 2,

    /// <summary>
    /// AI-generated rewrite that may restructure content significantly.
    /// </summary>
    /// <remarks>
    /// Used by the Tuning Agent for complex fixes where simple replacement
    /// isn't sufficient. The <see cref="UnifiedFix.NewText"/> may differ
    /// substantially from <see cref="UnifiedFix.OldText"/> in structure
    /// or length. Requires user review before application.
    /// </remarks>
    Rewrite = 3,

    /// <summary>
    /// No automatic fix is available for this issue.
    /// </summary>
    /// <remarks>
    /// Used for issues that require manual intervention, such as structural
    /// reorganization or complex semantic changes that cannot be automated.
    /// <see cref="UnifiedFix.CanAutoApply"/> should be <c>false</c>.
    /// </remarks>
    NoFix = 4
}
