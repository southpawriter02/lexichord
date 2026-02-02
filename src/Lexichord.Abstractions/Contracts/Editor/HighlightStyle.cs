// =============================================================================
// File: HighlightStyle.cs
// Project: Lexichord.Abstractions
// Description: Enumeration of text highlight styles for editor rendering.
// =============================================================================
// LOGIC: Defines the visual styles available for temporary text highlighting
//   in the editor. Used by navigation services to indicate different types
//   of highlighted content (search results, errors, warnings, references).
// =============================================================================
// VERSION: v0.4.6c (Source Navigation)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Editor;

/// <summary>
/// Styles for text highlighting in the editor.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="HighlightStyle"/> defines the visual appearance of temporary
/// text highlights applied during navigation operations. Each style maps
/// to a distinct color scheme in the editor's rendering layer.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.6c as part of Source Navigation.
/// </para>
/// </remarks>
public enum HighlightStyle
{
    /// <summary>
    /// Standard search result highlight (yellow background).
    /// </summary>
    /// <remarks>
    /// LOGIC: Used when navigating from search results to source documents.
    /// Provides a warm yellow background to indicate the matched text span.
    /// </remarks>
    SearchResult,

    /// <summary>
    /// Error highlight (red underline).
    /// </summary>
    /// <remarks>
    /// LOGIC: Used for error indicators such as compilation errors or
    /// critical style violations.
    /// </remarks>
    Error,

    /// <summary>
    /// Warning highlight (orange underline).
    /// </summary>
    /// <remarks>
    /// LOGIC: Used for warning indicators such as style suggestions
    /// or potential issues.
    /// </remarks>
    Warning,

    /// <summary>
    /// Reference highlight (blue background).
    /// </summary>
    /// <remarks>
    /// LOGIC: Used for cross-reference highlights such as go-to-definition
    /// or find-all-references results.
    /// </remarks>
    Reference
}
