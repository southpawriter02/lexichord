// =============================================================================
// File: CitationValidationStatus.cs
// Project: Lexichord.Abstractions
// Description: Enum defining the possible outcomes of citation validation.
// =============================================================================
// LOGIC: Defines four validation states for citation freshness checking.
//   - Valid: Source file unchanged since indexing. Citation is current.
//   - Stale: Source file has been modified since indexing. Content may differ.
//   - Missing: Source file no longer exists at the specified path.
//   - Error: Validation could not complete (e.g., permission denied, I/O error).
//   - Used by CitationValidationResult to communicate validation outcome.
//   - Used by StaleIndicatorViewModel to determine UI display state.
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Status of citation validation against the source file's current state.
/// </summary>
/// <remarks>
/// <para>
/// Each status value represents a distinct outcome of comparing a
/// <see cref="Citation"/>'s <see cref="Citation.IndexedAt"/> timestamp
/// against the source file's current modification time.
/// </para>
/// <para>
/// <b>Decision Flow:</b>
/// <list type="number">
///   <item><description>File exists? No → <see cref="Missing"/>.</description></item>
///   <item><description>File accessible? No → <see cref="Error"/>.</description></item>
///   <item><description>ModifiedAt &gt; IndexedAt? Yes → <see cref="Stale"/>.</description></item>
///   <item><description>Otherwise → <see cref="Valid"/>.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.2c as part of the Citation Engine (Stale Detection).
/// </para>
/// </remarks>
public enum CitationValidationStatus
{
    /// <summary>
    /// Source file unchanged since indexing. Citation is current.
    /// </summary>
    /// <remarks>
    /// LOGIC: The file's <c>LastWriteTimeUtc</c> is less than or equal to the
    /// citation's <see cref="Citation.IndexedAt"/> timestamp, meaning the content
    /// has not been modified since the document was last indexed. The citation
    /// accurately reflects the current file content.
    /// </remarks>
    Valid,

    /// <summary>
    /// Source file has been modified since indexing. Content may differ.
    /// </summary>
    /// <remarks>
    /// LOGIC: The file's <c>LastWriteTimeUtc</c> exceeds the citation's
    /// <see cref="Citation.IndexedAt"/> timestamp. The chunk content retrieved
    /// during search may no longer match the current file content. The user
    /// should re-verify the citation or re-index the document.
    /// <para>
    /// <b>UI Indicator:</b> Warning icon (⚠️) with "Source may have changed" tooltip.
    /// </para>
    /// </remarks>
    Stale,

    /// <summary>
    /// Source file no longer exists at the specified path.
    /// </summary>
    /// <remarks>
    /// LOGIC: The file at <see cref="Citation.DocumentPath"/> was not found.
    /// This may indicate the file was deleted, moved, or renamed. The citation
    /// cannot be verified and should be treated as invalid.
    /// <para>
    /// <b>UI Indicator:</b> Error icon (❌) with "Source file not found" tooltip.
    /// </para>
    /// </remarks>
    Missing,

    /// <summary>
    /// Validation could not complete due to an error.
    /// </summary>
    /// <remarks>
    /// LOGIC: An exception occurred while attempting to access the file
    /// (e.g., <see cref="UnauthorizedAccessException"/>, <see cref="IOException"/>).
    /// The citation's freshness status is unknown. The error message is captured
    /// in <see cref="CitationValidationResult.ErrorMessage"/>.
    /// </remarks>
    Error
}
