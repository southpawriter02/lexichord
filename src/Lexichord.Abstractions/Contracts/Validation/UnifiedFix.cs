// =============================================================================
// File: UnifiedFix.cs
// Project: Lexichord.Abstractions
// Description: Location-aware fix representation for unified validation issues.
// =============================================================================
// LOGIC: Represents a fix that can be applied to resolve a UnifiedIssue.
//   Unlike the v0.6.5j UnifiedFix (for CKVS integration), this version is
//   location-aware with TextSpan positioning for direct editor application.
//
// v0.7.5e: Unified Issue Model (Unified Validation Feature)
// =============================================================================

using Lexichord.Abstractions.Contracts.Editor;

namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Represents a location-aware fix that can be applied to resolve a <see cref="UnifiedIssue"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This record provides all information needed to apply a fix directly to
/// the editor:
/// <list type="bullet">
///   <item><description>Location: Where in the document the fix applies</description></item>
///   <item><description>OldText: Text to be replaced (for verification)</description></item>
///   <item><description>NewText: Replacement text to insert</description></item>
///   <item><description>Type: How to apply the fix (replacement, insertion, etc.)</description></item>
///   <item><description>Confidence: AI confidence in the fix quality</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Relationship to v0.6.5j UnifiedFix:</b> This type coexists with
/// <see cref="Knowledge.Validation.Integration.UnifiedFix"/> which is used for CKVS
/// validation aggregation. This version (v0.7.5e) adds location awareness via
/// <see cref="TextSpan"/> and separates OldText/NewText for diff display.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This record is immutable and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5e as part of the Unified Issue Model feature.
/// </para>
/// </remarks>
/// <param name="FixId">
/// Unique identifier for this fix instance. Used for tracking fix application
/// and undo/redo operations.
/// </param>
/// <param name="Location">
/// The text span where this fix should be applied. For replacements and deletions,
/// specifies the range to modify. For insertions, Start indicates the insertion point.
/// </param>
/// <param name="OldText">
/// The original text at the fix location. Used for verification before applying
/// the fix and for diff display. Null for insertions.
/// </param>
/// <param name="NewText">
/// The replacement text to insert. Null for deletions.
/// </param>
/// <param name="Type">
/// The type of fix operation (Replacement, Insertion, Deletion, Rewrite, NoFix).
/// </param>
/// <param name="Description">
/// Human-readable description of what this fix does, suitable for display in the UI.
/// </param>
/// <param name="Confidence">
/// Confidence score from 0.0 to 1.0 indicating how certain the AI is that this fix
/// is correct. Higher values indicate higher confidence.
/// </param>
/// <param name="CanAutoApply">
/// Whether this fix can be applied automatically without user review.
/// Should be false for NoFix type and for low-confidence fixes.
/// </param>
public record UnifiedFix(
    Guid FixId,
    TextSpan Location,
    string? OldText,
    string? NewText,
    FixType Type,
    string Description,
    double Confidence,
    bool CanAutoApply)
{
    /// <summary>
    /// Gets whether this fix has high confidence (>= 0.8).
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> High-confidence fixes are candidates for bulk acceptance
    /// in the Tuning Agent review panel.
    /// </remarks>
    public bool IsHighConfidence => Confidence >= 0.8;

    /// <summary>
    /// Gets whether this fix actually modifies the document.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Returns false for NoFix type or when OldText equals NewText.
    /// </remarks>
    public bool HasChanges => Type != FixType.NoFix &&
        !string.Equals(OldText, NewText, StringComparison.Ordinal);

    /// <summary>
    /// Creates a replacement fix.
    /// </summary>
    /// <param name="location">The text span to replace.</param>
    /// <param name="oldText">The original text being replaced.</param>
    /// <param name="newText">The replacement text.</param>
    /// <param name="description">Human-readable fix description.</param>
    /// <param name="confidence">Confidence score (0.0 to 1.0).</param>
    /// <returns>A new <see cref="UnifiedFix"/> configured for replacement.</returns>
    public static UnifiedFix Replacement(
        TextSpan location,
        string oldText,
        string newText,
        string description,
        double confidence = 0.8) =>
        new(
            Guid.NewGuid(),
            location,
            oldText,
            newText,
            FixType.Replacement,
            description,
            confidence,
            CanAutoApply: confidence >= 0.7);

    /// <summary>
    /// Creates an insertion fix.
    /// </summary>
    /// <param name="insertionPoint">The position to insert at (as a zero-length span).</param>
    /// <param name="textToInsert">The text to insert.</param>
    /// <param name="description">Human-readable fix description.</param>
    /// <param name="confidence">Confidence score (0.0 to 1.0).</param>
    /// <returns>A new <see cref="UnifiedFix"/> configured for insertion.</returns>
    public static UnifiedFix Insertion(
        int insertionPoint,
        string textToInsert,
        string description,
        double confidence = 0.8) =>
        new(
            Guid.NewGuid(),
            new TextSpan(insertionPoint, 0),
            OldText: null,
            textToInsert,
            FixType.Insertion,
            description,
            confidence,
            CanAutoApply: confidence >= 0.7);

    /// <summary>
    /// Creates a deletion fix.
    /// </summary>
    /// <param name="location">The text span to delete.</param>
    /// <param name="textToDelete">The text being deleted (for verification).</param>
    /// <param name="description">Human-readable fix description.</param>
    /// <param name="confidence">Confidence score (0.0 to 1.0).</param>
    /// <returns>A new <see cref="UnifiedFix"/> configured for deletion.</returns>
    public static UnifiedFix Deletion(
        TextSpan location,
        string textToDelete,
        string description,
        double confidence = 0.8) =>
        new(
            Guid.NewGuid(),
            location,
            textToDelete,
            NewText: null,
            FixType.Deletion,
            description,
            confidence,
            CanAutoApply: confidence >= 0.7);

    /// <summary>
    /// Creates a rewrite fix.
    /// </summary>
    /// <param name="location">The text span to rewrite.</param>
    /// <param name="originalText">The original text being rewritten.</param>
    /// <param name="rewrittenText">The AI-generated rewrite.</param>
    /// <param name="description">Human-readable fix description.</param>
    /// <param name="confidence">Confidence score (0.0 to 1.0).</param>
    /// <returns>A new <see cref="UnifiedFix"/> configured for rewrite.</returns>
    public static UnifiedFix Rewrite(
        TextSpan location,
        string originalText,
        string rewrittenText,
        string description,
        double confidence = 0.7) =>
        new(
            Guid.NewGuid(),
            location,
            originalText,
            rewrittenText,
            FixType.Rewrite,
            description,
            confidence,
            CanAutoApply: false); // Rewrites always require user review

    /// <summary>
    /// Creates a placeholder indicating no automatic fix is available.
    /// </summary>
    /// <param name="location">The location of the issue.</param>
    /// <param name="originalText">The text at the issue location.</param>
    /// <param name="reason">Explanation of why no fix is available.</param>
    /// <returns>A new <see cref="UnifiedFix"/> indicating no fix.</returns>
    public static UnifiedFix NoFixAvailable(
        TextSpan location,
        string? originalText,
        string reason) =>
        new(
            Guid.NewGuid(),
            location,
            originalText,
            NewText: null,
            FixType.NoFix,
            reason,
            Confidence: 0.0,
            CanAutoApply: false);
}
