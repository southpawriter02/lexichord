// =============================================================================
// File: ValidationIcon.cs
// Project: Lexichord.Abstractions
// Description: Icon enum for citation validation status display.
// =============================================================================
// LOGIC: Maps validation outcomes to visual indicators for the citation UI.
//   CheckMark = all verified, Warning = minor issues, Error = significant
//   issues, Question = validation incomplete or inconclusive.
//
// v0.6.6h: Entity Citation Renderer (CKVS Phase 3b)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Icon indicator for citation validation status.
/// </summary>
/// <remarks>
/// <para>
/// Used by <see cref="CitationMarkup.Icon"/> to communicate the overall
/// validation state of cited entities. Rendered as Unicode symbols in
/// the citation panel.
/// </para>
/// <para>
/// <b>Mapping:</b>
/// <list type="bullet">
///   <item><see cref="CheckMark"/> — ✓ (all content verified).</item>
///   <item><see cref="Warning"/> — ⚠ (minor issues detected).</item>
///   <item><see cref="Error"/> — ✗ (significant issues detected).</item>
///   <item><see cref="Question"/> — ? (validation incomplete).</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6h as part of the Entity Citation Renderer.
/// </para>
/// </remarks>
public enum ValidationIcon
{
    /// <summary>
    /// All content verified against the knowledge graph. Rendered as ✓.
    /// </summary>
    CheckMark,

    /// <summary>
    /// Minor issues detected. Rendered as ⚠.
    /// </summary>
    Warning,

    /// <summary>
    /// Significant issues detected. Rendered as ✗.
    /// </summary>
    Error,

    /// <summary>
    /// Validation incomplete or inconclusive. Rendered as ?.
    /// </summary>
    Question
}
