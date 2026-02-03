// =============================================================================
// File: CitationSettingsKeys.cs
// Project: Lexichord.Abstractions
// Description: Constants for citation-related user preference keys stored via
//              ISystemSettingsRepository.
// =============================================================================
// LOGIC: Defines well-known settings keys for citation preferences (v0.5.2b).
//   - DefaultStyle: The user's preferred citation format (Inline/Footnote/Markdown).
//   - IncludeLineNumbers: Whether to include line number references in citations.
//   - UseRelativePaths: Whether to use workspace-relative paths in citations.
//   - Keys follow the "Citation.{Setting}" naming convention consistent with
//     existing keys like "Search.DefaultMode" (v0.5.1d).
//   - Values are persisted via ISystemSettingsRepository (v0.0.5d) and retrieved
//     by CitationFormatterRegistry (v0.5.2b) at runtime.
// =============================================================================

namespace Lexichord.Abstractions.Constants;

/// <summary>
/// Constants for citation-related user preference keys.
/// </summary>
/// <remarks>
/// <para>
/// These keys are used with <see cref="Contracts.ISystemSettingsRepository"/> to persist
/// and retrieve citation formatting preferences. The settings are managed by
/// <c>CitationFormatterRegistry</c> (v0.5.2b) and consumed by the citation
/// formatting pipeline.
/// </para>
/// <para>
/// <b>Key Naming Convention:</b> All citation keys follow the <c>Citation.{Setting}</c>
/// pattern, consistent with the existing <c>Search.DefaultMode</c> key (v0.5.1d).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.2b as part of the Citation Engine.
/// </para>
/// </remarks>
public static class CitationSettingsKeys
{
    /// <summary>
    /// User's preferred default citation style.
    /// </summary>
    /// <remarks>
    /// LOGIC: Stored as the string representation of <see cref="Contracts.CitationStyle"/>:
    /// <c>"Inline"</c>, <c>"Footnote"</c>, or <c>"Markdown"</c>.
    /// Default value is <c>"Inline"</c> when not set.
    /// <para>
    /// Read by <c>CitationFormatterRegistry.GetPreferredStyle()</c> and written by
    /// <c>CitationFormatterRegistry.SetPreferredStyleAsync()</c>.
    /// </para>
    /// </remarks>
    public const string DefaultStyle = "Citation.DefaultStyle";

    /// <summary>
    /// Whether to include line numbers in citations.
    /// </summary>
    /// <remarks>
    /// LOGIC: Stored as a boolean string (<c>"true"</c> or <c>"false"</c>).
    /// Default value is <c>true</c> when not set.
    /// <para>
    /// When disabled, formatters omit the <c>:line</c> (Footnote) and
    /// <c>#Lline</c> (Markdown) suffixes even when a line number is available.
    /// </para>
    /// </remarks>
    public const string IncludeLineNumbers = "Citation.IncludeLineNumbers";

    /// <summary>
    /// Whether to use relative paths in citations.
    /// </summary>
    /// <remarks>
    /// LOGIC: Stored as a boolean string (<c>"true"</c> or <c>"false"</c>).
    /// Default value is <c>false</c> when not set.
    /// <para>
    /// When enabled and a workspace root is available, formatters use
    /// <see cref="Contracts.Citation.RelativePath"/> instead of
    /// <see cref="Contracts.Citation.DocumentPath"/> for more concise output.
    /// </para>
    /// </remarks>
    public const string UseRelativePaths = "Citation.UseRelativePaths";
}
