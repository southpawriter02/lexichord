// =============================================================================
// File: ValidationPipeline.cs
// Project: Lexichord.Modules.Knowledge
// Description: Executes validators in parallel with timeout and error isolation.
// =============================================================================
// LOGIC: Given a list of validators and a context, runs all validators
//   concurrently via Task.WhenAll. Each validator is wrapped with:
//   - Per-validator timeout via CancellationTokenSource.CreateLinkedTokenSource
//   - Exception isolation: caught exceptions become Error findings
//   - Timeout handling: timeout becomes a specific Error finding
//   Aggregates all findings into a single ValidationResult.
//
// v0.6.5e: Validation Orchestrator (CKVS Phase 3a)
// Dependencies: IValidator (v0.6.5e), ValidationResult (v0.6.5e),
//               ILogger<T> (v0.0.3b)
// =============================================================================

using System.Diagnostics;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Validation;

/// <summary>
/// Executes validators in parallel with timeout enforcement and error isolation.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ValidationPipeline"/> is responsible for the actual concurrent
/// execution of validators. Each validator runs as an independent task with its
/// own timeout. Exceptions from individual validators are caught and converted
/// to error findings — a failing validator never crashes the entire pipeline.
/// </para>
/// <para>
/// <b>Timeout Behavior:</b> Each validator receives a linked cancellation token
/// that fires at the configured timeout. If a validator does not respect the
/// token, the pipeline still continues — it records the timeout as a finding
/// and moves on.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5e as part of the Validation Orchestrator.
/// </para>
/// </remarks>
internal sealed class ValidationPipeline
{
    private readonly ILogger<ValidationPipeline> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationPipeline"/> class.
    /// </summary>
    /// <param name="logger">Logger for execution diagnostics.</param>
    public ValidationPipeline(ILogger<ValidationPipeline> logger)
    {
        _logger = logger;
        _logger.LogDebug("ValidationPipeline initialized");
    }

    /// <summary>
    /// Executes the given validators in parallel against the provided context.
    /// </summary>
    /// <param name="validators">The validators to execute. Must not be null.</param>
    /// <param name="context">The validation context. Must not be null.</param>
    /// <param name="skippedCount">Number of validators that were skipped (for result metadata).</param>
    /// <param name="cancellationToken">External cancellation token.</param>
    /// <returns>
    /// An aggregated <see cref="ValidationResult"/> containing findings from all validators.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="validators"/> or <paramref name="context"/> is null.
    /// </exception>
    public async Task<ValidationResult> ExecuteAsync(
        IReadOnlyList<IValidator> validators,
        ValidationContext context,
        int skippedCount = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(validators);
        ArgumentNullException.ThrowIfNull(context);

        // LOGIC: Short-circuit if no validators to run.
        if (validators.Count == 0)
        {
            _logger.LogDebug(
                "No validators to execute for document '{DocumentId}'",
                context.DocumentId);
            return ValidationResult.Valid(validatorsRun: 0, validatorsSkipped: skippedCount);
        }

        _logger.LogInformation(
            "Starting validation pipeline for document '{DocumentId}' " +
            "with {ValidatorCount} validators (timeout: {Timeout})",
            context.DocumentId,
            validators.Count,
            context.Options.EffectiveTimeout);

        var stopwatch = Stopwatch.StartNew();
        var timeout = context.Options.EffectiveTimeout;

        // LOGIC: Create tasks for all validators, each wrapped with timeout and error handling.
        var tasks = validators.Select(validator =>
            ExecuteValidatorAsync(validator, context, timeout, cancellationToken));

        // LOGIC: Execute all validator tasks in parallel.
        var results = await Task.WhenAll(tasks);

        stopwatch.Stop();

        // LOGIC: Flatten all findings from all validators into a single list.
        var allFindings = results
            .SelectMany(r => r)
            .ToList();

        // LOGIC: Apply MaxFindings cap if configured.
        if (context.Options.MaxFindings.HasValue &&
            allFindings.Count > context.Options.MaxFindings.Value)
        {
            _logger.LogDebug(
                "Capping findings from {TotalCount} to {MaxFindings} for document '{DocumentId}'",
                allFindings.Count, context.Options.MaxFindings.Value, context.DocumentId);
            allFindings = allFindings.Take(context.Options.MaxFindings.Value).ToList();
        }

        _logger.LogInformation(
            "Validation pipeline completed for document '{DocumentId}' in {Duration}ms: " +
            "{FindingCount} findings ({ErrorCount} errors, {WarningCount} warnings, {InfoCount} info)",
            context.DocumentId,
            stopwatch.ElapsedMilliseconds,
            allFindings.Count,
            allFindings.Count(f => f.Severity == ValidationSeverity.Error),
            allFindings.Count(f => f.Severity == ValidationSeverity.Warning),
            allFindings.Count(f => f.Severity == ValidationSeverity.Info));

        return ValidationResult.WithFindings(
            allFindings,
            validatorsRun: validators.Count,
            validatorsSkipped: skippedCount,
            duration: stopwatch.Elapsed);
    }

    /// <summary>
    /// Executes a single validator with timeout enforcement and error isolation.
    /// </summary>
    /// <param name="validator">The validator to execute.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="timeout">Maximum time allowed for this validator.</param>
    /// <param name="externalToken">External cancellation token.</param>
    /// <returns>The findings from this validator, or an error finding on failure.</returns>
    private async Task<IReadOnlyList<ValidationFinding>> ExecuteValidatorAsync(
        IValidator validator,
        ValidationContext context,
        TimeSpan timeout,
        CancellationToken externalToken)
    {
        _logger.LogDebug(
            "Executing validator '{ValidatorId}' for document '{DocumentId}'",
            validator.Id, context.DocumentId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // LOGIC: Create a linked cancellation token that fires at the timeout
            // or when the external token is cancelled — whichever comes first.
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            timeoutCts.CancelAfter(timeout);

            var findings = await validator.ValidateAsync(context, timeoutCts.Token);

            stopwatch.Stop();
            _logger.LogDebug(
                "Validator '{ValidatorId}' completed in {Duration}ms with {FindingCount} findings",
                validator.Id, stopwatch.ElapsedMilliseconds, findings.Count);

            return findings;
        }
        catch (OperationCanceledException) when (!externalToken.IsCancellationRequested)
        {
            // LOGIC: Timeout — the per-validator timeout fired, not the external token.
            stopwatch.Stop();
            _logger.LogWarning(
                "Validator '{ValidatorId}' timed out after {Duration}ms for document '{DocumentId}'",
                validator.Id, stopwatch.ElapsedMilliseconds, context.DocumentId);

            return new[]
            {
                ValidationFinding.Error(
                    validator.Id,
                    "VALIDATOR_TIMEOUT",
                    $"Validator '{validator.DisplayName}' timed out after {timeout.TotalMilliseconds}ms")
            };
        }
        catch (OperationCanceledException)
        {
            // LOGIC: External cancellation — propagate without converting to finding.
            _logger.LogDebug(
                "Validator '{ValidatorId}' cancelled by external request",
                validator.Id);
            throw;
        }
        catch (Exception ex)
        {
            // LOGIC: Unexpected exception — convert to error finding for isolation.
            stopwatch.Stop();
            _logger.LogError(ex,
                "Validator '{ValidatorId}' threw an exception for document '{DocumentId}': {Message}",
                validator.Id, context.DocumentId, ex.Message);

            return new[]
            {
                ValidationFinding.Error(
                    validator.Id,
                    "VALIDATOR_ERROR",
                    $"Validator '{validator.DisplayName}' failed: {ex.Message}")
            };
        }
    }
}
