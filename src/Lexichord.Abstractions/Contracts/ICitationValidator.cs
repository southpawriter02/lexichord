// =============================================================================
// File: ICitationValidator.cs
// Project: Lexichord.Abstractions
// Description: Interface for validating citations against current file state.
// =============================================================================
// LOGIC: Defines the contract for citation freshness validation.
//   - ValidateAsync: Validates a single citation by comparing file modification
//     timestamp against the citation's IndexedAt value.
//   - ValidateBatchAsync: Validates multiple citations in parallel with throttling.
//   - ValidateIfLicensedAsync: License-gated wrapper that returns null for
//     unlicensed users (Core tier), enabling UI to hide stale indicators.
//   - Implementations should publish CitationValidationFailedEvent for stale/missing.
//   - Thread-safe: all methods are async and support CancellationToken.
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Validates citations against current file state to detect staleness.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ICitationValidator"/> is the primary contract for citation freshness
/// validation. It compares a <see cref="Citation"/>'s <see cref="Citation.IndexedAt"/>
/// timestamp against the source file's current modification time to determine if
/// the cited content may have changed.
/// </para>
/// <para>
/// <b>Validation Strategy:</b>
/// <list type="number">
///   <item><description>Check if file exists at <see cref="Citation.DocumentPath"/>.</description></item>
///   <item><description>Compare file's <c>LastWriteTimeUtc</c> with <see cref="Citation.IndexedAt"/>.</description></item>
///   <item><description>If modified after indexing, return <see cref="CitationValidationStatus.Stale"/>.</description></item>
///   <item><description>If unchanged, return <see cref="CitationValidationStatus.Valid"/>.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>License Gating:</b> Stale detection is a Writer Pro feature. Use
/// <see cref="ValidateIfLicensedAsync"/> to respect license tier — it returns
/// <c>null</c> for unlicensed users, allowing the UI to hide stale indicators.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe for concurrent use
/// across multiple search result items.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.2c as part of the Citation Engine (Stale Detection).
/// </para>
/// </remarks>
public interface ICitationValidator
{
    /// <summary>
    /// Validates a single citation against its source file.
    /// </summary>
    /// <param name="citation">
    /// The citation to validate. Must not be <c>null</c>.
    /// </param>
    /// <param name="ct">
    /// Cancellation token for the async operation.
    /// </param>
    /// <returns>
    /// A <see cref="CitationValidationResult"/> containing the validation status,
    /// the file's current modification timestamp, and any error details.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="citation"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method accesses the file system to check existence and modification time.
    /// File I/O errors (e.g., <see cref="UnauthorizedAccessException"/>,
    /// <see cref="IOException"/>) are caught and returned as
    /// <see cref="CitationValidationStatus.Error"/> results rather than thrown.
    /// </para>
    /// <para>
    /// A <see cref="Lexichord.Abstractions.Events.CitationValidationFailedEvent"/>
    /// is published via MediatR for <see cref="CitationValidationStatus.Stale"/>
    /// and <see cref="CitationValidationStatus.Missing"/> results.
    /// </para>
    /// </remarks>
    Task<CitationValidationResult> ValidateAsync(
        Citation citation,
        CancellationToken ct = default);

    /// <summary>
    /// Validates multiple citations in parallel with throttled concurrency.
    /// </summary>
    /// <param name="citations">
    /// The citations to validate. Must not be <c>null</c>.
    /// </param>
    /// <param name="ct">
    /// Cancellation token for the async operation.
    /// </param>
    /// <returns>
    /// A list of <see cref="CitationValidationResult"/> instances in the same
    /// order as the input <paramref name="citations"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="citations"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Batch validation executes up to 10 validations concurrently using a
    /// <see cref="SemaphoreSlim"/> for throttling. This prevents excessive
    /// file system I/O when validating large result sets.
    /// </para>
    /// <para>
    /// <b>Performance Target:</b> Batch of 20 validations in &lt; 200ms.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<CitationValidationResult>> ValidateBatchAsync(
        IEnumerable<Citation> citations,
        CancellationToken ct = default);

    /// <summary>
    /// Validates a citation only if the user has a valid license for stale detection.
    /// </summary>
    /// <param name="citation">
    /// The citation to validate. Must not be <c>null</c>.
    /// </param>
    /// <param name="ct">
    /// Cancellation token for the async operation.
    /// </param>
    /// <returns>
    /// A <see cref="CitationValidationResult"/> if the user is licensed for citation
    /// validation; <c>null</c> if the feature is not enabled for the current license tier.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method checks <c>ILicenseContext.IsFeatureEnabled</c> with the
    /// <c>FeatureCodes.CitationValidation</c> feature code before performing
    /// validation. Core tier users receive <c>null</c>, which signals the UI
    /// to hide stale indicators entirely.
    /// </para>
    /// <para>
    /// <b>License Requirement:</b> Writer Pro or higher.
    /// </para>
    /// </remarks>
    Task<CitationValidationResult?> ValidateIfLicensedAsync(
        Citation citation,
        CancellationToken ct = default);
}
