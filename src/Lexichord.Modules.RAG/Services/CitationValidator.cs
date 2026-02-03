// =============================================================================
// File: CitationValidator.cs
// Project: Lexichord.Modules.RAG
// Description: Validates citations against current file state for staleness.
// =============================================================================
// LOGIC: Implements ICitationValidator to detect stale and missing citations.
//   - ValidateAsync: Checks file existence and compares LastWriteTimeUtc against
//     the citation's IndexedAt timestamp. Publishes CitationValidationFailedEvent
//     for Stale and Missing results.
//   - ValidateBatchAsync: Validates multiple citations in parallel with throttled
//     concurrency (max 10 concurrent) using SemaphoreSlim.
//   - ValidateIfLicensedAsync: License-gated wrapper using FeatureCodes.CitationValidation.
//     Returns null for unlicensed users (Core tier).
//   - Error handling: Catches UnauthorizedAccessException and IOException, returning
//     Error status rather than throwing.
//   - Thread-safe: All methods are async and stateless. Registered as singleton.
// =============================================================================

using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Validates citations against current file state to detect staleness.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="CitationValidator"/> implements <see cref="ICitationValidator"/> and
/// provides file-based freshness validation for citations. It compares the source file's
/// <c>LastWriteTimeUtc</c> against the <see cref="Citation.IndexedAt"/> timestamp to
/// determine if the cited content may have changed since indexing.
/// </para>
/// <para>
/// <b>Validation Strategy:</b>
/// <list type="number">
///   <item><description>Check if file exists at <see cref="Citation.DocumentPath"/>.</description></item>
///   <item><description>Get file's <c>LastWriteTimeUtc</c> via <see cref="FileInfo"/>.</description></item>
///   <item><description>Compare against <see cref="Citation.IndexedAt"/>.</description></item>
///   <item><description>Publish <see cref="CitationValidationFailedEvent"/> for stale/missing.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>License Gating:</b> Stale detection is a Writer Pro feature. The
/// <see cref="ValidateIfLicensedAsync"/> method checks the license tier before
/// performing validation, returning <c>null</c> for unlicensed users.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This service is registered as a singleton and is thread-safe.
/// All state is local to each method invocation. Batch validation uses a
/// <see cref="SemaphoreSlim"/> to throttle concurrent file I/O.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.2c as part of the Citation Engine (Stale Detection).
/// </para>
/// </remarks>
public sealed class CitationValidator : ICitationValidator
{
    /// <summary>
    /// Maximum number of concurrent file validations during batch operations.
    /// </summary>
    /// <remarks>
    /// LOGIC: Limits parallel file I/O to prevent excessive disk thrashing when
    /// validating large result sets. Value of 10 balances throughput against
    /// file system contention on typical consumer hardware.
    /// </remarks>
    private const int MaxParallelValidations = 10;

    private readonly ILicenseContext _licenseContext;
    private readonly IMediator _mediator;
    private readonly ILogger<CitationValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CitationValidator"/> class.
    /// </summary>
    /// <param name="licenseContext">
    /// License context for checking the <see cref="FeatureCodes.CitationValidation"/>
    /// feature availability. Writer Pro or higher is required for validation.
    /// </param>
    /// <param name="mediator">
    /// MediatR mediator for publishing <see cref="CitationValidationFailedEvent"/>
    /// notifications when citations are stale or missing.
    /// </param>
    /// <param name="logger">
    /// Logger for structured diagnostic output during validation operations.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is <c>null</c>.
    /// </exception>
    public CitationValidator(
        ILicenseContext licenseContext,
        IMediator mediator,
        ILogger<CitationValidator> logger)
    {
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Single citation validation flow:
    /// <list type="number">
    ///   <item><description>Validate citation parameter is not null.</description></item>
    ///   <item><description>Check if file exists at <see cref="Citation.DocumentPath"/>.</description></item>
    ///   <item><description>If missing: log warning, publish event, return Missing status.</description></item>
    ///   <item><description>Get file's <c>LastWriteTimeUtc</c> via <see cref="FileInfo"/>.</description></item>
    ///   <item><description>If modified after indexing: log warning, publish event, return Stale.</description></item>
    ///   <item><description>Otherwise: log debug, return Valid status.</description></item>
    /// </list>
    /// Catches <see cref="UnauthorizedAccessException"/> and <see cref="IOException"/>
    /// and returns Error status with the exception message.
    /// </remarks>
    public async Task<CitationValidationResult> ValidateAsync(
        Citation citation,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(citation);

        _logger.LogDebug(
            "Validating citation for {DocumentPath}",
            citation.DocumentPath);

        try
        {
            // LOGIC: Check file existence at the citation's document path.
            // If the file has been deleted or moved, the citation is invalid.
            if (!File.Exists(citation.DocumentPath))
            {
                _logger.LogWarning(
                    "Citation invalid: {DocumentPath} not found",
                    citation.DocumentPath);

                var missingResult = new CitationValidationResult(
                    Citation: citation,
                    IsValid: false,
                    Status: CitationValidationStatus.Missing,
                    CurrentModifiedAt: null,
                    ErrorMessage: null);

                await PublishFailedEventAsync(missingResult, ct);
                return missingResult;
            }

            // LOGIC: Get the file's current modification timestamp.
            // FileInfo.LastWriteTimeUtc provides the most recent write time
            // without opening the file for reading.
            var fileInfo = new FileInfo(citation.DocumentPath);
            var currentModifiedAt = fileInfo.LastWriteTimeUtc;

            // LOGIC: Compare the file's modification time against the citation's
            // IndexedAt timestamp. If the file was modified after the document
            // was indexed, the citation content may be outdated.
            if (currentModifiedAt > citation.IndexedAt)
            {
                _logger.LogWarning(
                    "Citation stale: {DocumentPath} modified at {ModifiedAt}, indexed at {IndexedAt}",
                    citation.DocumentPath,
                    currentModifiedAt,
                    citation.IndexedAt);

                var staleResult = new CitationValidationResult(
                    Citation: citation,
                    IsValid: false,
                    Status: CitationValidationStatus.Stale,
                    CurrentModifiedAt: currentModifiedAt,
                    ErrorMessage: null);

                await PublishFailedEventAsync(staleResult, ct);
                return staleResult;
            }

            // LOGIC: File exists and has not been modified since indexing.
            // The citation accurately reflects the current file content.
            _logger.LogDebug(
                "Citation valid: {DocumentPath}",
                citation.DocumentPath);

            return new CitationValidationResult(
                Citation: citation,
                IsValid: true,
                Status: CitationValidationStatus.Valid,
                CurrentModifiedAt: currentModifiedAt,
                ErrorMessage: null);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            // LOGIC: File system errors (permission denied, I/O errors) are caught
            // and returned as Error status rather than propagated. This ensures that
            // validation failures in one citation do not break batch operations.
            _logger.LogError(ex,
                "Error validating citation for {DocumentPath}",
                citation.DocumentPath);

            return new CitationValidationResult(
                Citation: citation,
                IsValid: false,
                Status: CitationValidationStatus.Error,
                CurrentModifiedAt: null,
                ErrorMessage: ex.Message);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Batch validation flow:
    /// <list type="number">
    ///   <item><description>Materialize input to list for count logging.</description></item>
    ///   <item><description>Create <see cref="SemaphoreSlim"/> with <see cref="MaxParallelValidations"/>.</description></item>
    ///   <item><description>Launch all validations with throttled concurrency.</description></item>
    ///   <item><description>Await all tasks and collect results.</description></item>
    ///   <item><description>Log summary of stale and missing counts if any found.</description></item>
    /// </list>
    /// Results are returned in the same order as the input citations.
    /// </remarks>
    public async Task<IReadOnlyList<CitationValidationResult>> ValidateBatchAsync(
        IEnumerable<Citation> citations,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(citations);

        var citationList = citations.ToList();

        _logger.LogDebug(
            "Batch validating {Count} citations",
            citationList.Count);

        // LOGIC: Use SemaphoreSlim to throttle concurrent file I/O.
        // Without throttling, validating hundreds of citations simultaneously
        // could overwhelm the file system and degrade performance.
        using var semaphore = new SemaphoreSlim(MaxParallelValidations);
        var tasks = citationList.Select(async citation =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                return await ValidateAsync(citation, ct);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        // LOGIC: Log a summary of validation results for observability.
        // Only log when stale or missing citations are found to reduce noise.
        var staleCount = results.Count(r => r.IsStale);
        var missingCount = results.Count(r => r.IsMissing);

        if (staleCount > 0 || missingCount > 0)
        {
            _logger.LogInformation(
                "Batch validation complete: {Stale} stale, {Missing} missing of {Total}",
                staleCount, missingCount, citationList.Count);
        }

        return results;
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: License-gated validation wrapper:
    /// <list type="number">
    ///   <item><description>Check <see cref="ILicenseContext.IsFeatureEnabled"/> with
    ///     <see cref="FeatureCodes.CitationValidation"/>.</description></item>
    ///   <item><description>If not licensed: log debug, return null.</description></item>
    ///   <item><description>If licensed: delegate to <see cref="ValidateAsync"/>.</description></item>
    /// </list>
    /// Returning null signals the UI to hide stale indicators entirely for
    /// Core tier users, rather than showing them as valid.
    /// </remarks>
    public async Task<CitationValidationResult?> ValidateIfLicensedAsync(
        Citation citation,
        CancellationToken ct = default)
    {
        // LOGIC: Check license before performing file I/O.
        // Core tier users should not see stale indicators at all,
        // so we return null rather than a Valid result.
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.CitationValidation))
        {
            _logger.LogDebug(
                "Citation validation skipped: feature not licensed");
            return null;
        }

        return await ValidateAsync(citation, ct);
    }

    /// <summary>
    /// Publishes a <see cref="CitationValidationFailedEvent"/> for stale or missing citations.
    /// </summary>
    /// <param name="result">
    /// The validation result to include in the event.
    /// </param>
    /// <param name="ct">
    /// Cancellation token for the publish operation.
    /// </param>
    /// <remarks>
    /// LOGIC: Events are published synchronously (awaited) within the validation flow
    /// because downstream handlers may need to update UI state before the validation
    /// result is returned to the caller. This differs from CitationCreatedEvent
    /// which uses fire-and-forget publishing.
    /// </remarks>
    private async Task PublishFailedEventAsync(
        CitationValidationResult result,
        CancellationToken ct)
    {
        var evt = new CitationValidationFailedEvent(result, DateTime.UtcNow);
        await _mediator.Publish(evt, ct);
    }
}
