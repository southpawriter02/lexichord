// =============================================================================
// File: CitationValidationResult.cs
// Project: Lexichord.Abstractions
// Description: Immutable record representing the outcome of citation validation.
// =============================================================================
// LOGIC: Contains all data needed to assess and display citation freshness.
//   - Citation: The citation that was validated.
//   - IsValid: Convenience boolean (true only for Valid status).
//   - Status: Detailed validation status (Valid/Stale/Missing/Error).
//   - CurrentModifiedAt: File's current modification timestamp (null if missing).
//   - ErrorMessage: Error details when Status is Error.
//   - Computed properties provide convenient status checks for UI binding.
//   - StatusMessage provides a user-friendly description for tooltip display.
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Result of validating a citation against its source file's current state.
/// </summary>
/// <remarks>
/// <para>
/// Produced by <see cref="ICitationValidator.ValidateAsync"/> and consumed by
/// the <c>StaleIndicatorViewModel</c> to drive UI display state. Contains
/// both the raw validation data and computed convenience properties for
/// data binding.
/// </para>
/// <para>
/// <b>Immutability:</b> This record is immutable by design. Once validation
/// completes, the result cannot be modified. A new validation must be
/// performed to get an updated result.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.2c as part of the Citation Engine (Stale Detection).
/// </para>
/// </remarks>
/// <param name="Citation">
/// The citation that was validated. Contains the document path, indexing
/// timestamp, and other provenance fields used during validation.
/// </param>
/// <param name="IsValid">
/// <c>true</c> if the source file is unchanged since indexing; <c>false</c>
/// if the citation is stale, the file is missing, or an error occurred.
/// </param>
/// <param name="Status">
/// Detailed validation status providing the specific reason for the result.
/// See <see cref="CitationValidationStatus"/> for possible values.
/// </param>
/// <param name="CurrentModifiedAt">
/// The file's current <c>LastWriteTimeUtc</c> at the time of validation.
/// <c>null</c> when the file is missing or an error prevented retrieval.
/// </param>
/// <param name="ErrorMessage">
/// Error details when <paramref name="Status"/> is <see cref="CitationValidationStatus.Error"/>.
/// <c>null</c> for all other statuses.
/// </param>
public record CitationValidationResult(
    Citation Citation,
    bool IsValid,
    CitationValidationStatus Status,
    DateTime? CurrentModifiedAt,
    string? ErrorMessage)
{
    /// <summary>
    /// Gets whether the citation is stale (source modified after indexing).
    /// </summary>
    /// <remarks>
    /// LOGIC: Convenience property for UI binding. Returns <c>true</c> only when
    /// <see cref="Status"/> is <see cref="CitationValidationStatus.Stale"/>.
    /// </remarks>
    public bool IsStale => Status == CitationValidationStatus.Stale;

    /// <summary>
    /// Gets whether the source file is missing.
    /// </summary>
    /// <remarks>
    /// LOGIC: Convenience property for UI binding. Returns <c>true</c> only when
    /// <see cref="Status"/> is <see cref="CitationValidationStatus.Missing"/>.
    /// </remarks>
    public bool IsMissing => Status == CitationValidationStatus.Missing;

    /// <summary>
    /// Gets whether validation encountered an error.
    /// </summary>
    /// <remarks>
    /// LOGIC: Convenience property for error handling. Returns <c>true</c> only when
    /// <see cref="Status"/> is <see cref="CitationValidationStatus.Error"/>.
    /// When true, <see cref="ErrorMessage"/> contains the error details.
    /// </remarks>
    public bool HasError => Status == CitationValidationStatus.Error;

    /// <summary>
    /// Gets a user-friendly status message suitable for tooltip display.
    /// </summary>
    /// <remarks>
    /// LOGIC: Produces a human-readable message based on the validation status:
    /// <list type="bullet">
    ///   <item><description>Valid: "Citation is current"</description></item>
    ///   <item><description>Stale: "Source modified {date}" (with formatted timestamp)</description></item>
    ///   <item><description>Missing: "Source file not found"</description></item>
    ///   <item><description>Error: The error message, or "Validation failed" if null</description></item>
    /// </list>
    /// </remarks>
    public string StatusMessage => Status switch
    {
        CitationValidationStatus.Valid => "Citation is current",
        CitationValidationStatus.Stale => $"Source modified {FormatModifiedAt()}",
        CitationValidationStatus.Missing => "Source file not found",
        CitationValidationStatus.Error => ErrorMessage ?? "Validation failed",
        _ => "Unknown status"
    };

    /// <summary>
    /// Formats the <see cref="CurrentModifiedAt"/> timestamp for display.
    /// </summary>
    /// <returns>
    /// A general short date/time string, or "recently" if the timestamp is unavailable.
    /// </returns>
    private string FormatModifiedAt() => CurrentModifiedAt?.ToString("g") ?? "recently";
}
