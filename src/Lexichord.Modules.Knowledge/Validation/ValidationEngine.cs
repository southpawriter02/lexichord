// =============================================================================
// File: ValidationEngine.cs
// Project: Lexichord.Modules.Knowledge
// Description: Orchestrates document validation using registered validators.
// =============================================================================
// LOGIC: The ValidationEngine is the single entry point for document validation.
//   It coordinates the ValidatorRegistry (which validators to run) with the
//   ValidationPipeline (how to run them). The lifecycle is:
//   1. Receive ValidationContext from caller
//   2. Query ValidatorRegistry for applicable validators (mode + license filter)
//   3. Delegate to ValidationPipeline for parallel execution
//   4. Return aggregated ValidationResult
//
// v0.6.5e: Validation Orchestrator (CKVS Phase 3a)
// Dependencies: ValidatorRegistry (v0.6.5e), ValidationPipeline (v0.6.5e),
//               ILogger<T> (v0.0.3b)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Validation;

/// <summary>
/// Orchestrates document validation using registered validators.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ValidationEngine"/> implements <see cref="IValidationEngine"/>
/// and serves as the single entry point for all document validation. It coordinates:
/// </para>
/// <list type="bullet">
///   <item><see cref="ValidatorRegistry"/>: resolves which validators are applicable
///   for the requested mode and license tier.</item>
///   <item><see cref="ValidationPipeline"/>: executes applicable validators in parallel
///   with timeout enforcement and error isolation.</item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This class is thread-safe. Multiple callers can invoke
/// <see cref="ValidateDocumentAsync"/> concurrently.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5e as part of the Validation Orchestrator.
/// </para>
/// </remarks>
internal sealed class ValidationEngine : IValidationEngine
{
    private readonly ValidatorRegistry _registry;
    private readonly ValidationPipeline _pipeline;
    private readonly ILogger<ValidationEngine> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationEngine"/> class.
    /// </summary>
    /// <param name="registry">The validator registry for resolving applicable validators.</param>
    /// <param name="pipeline">The validation pipeline for executing validators.</param>
    /// <param name="logger">Logger for orchestration diagnostics.</param>
    public ValidationEngine(
        ValidatorRegistry registry,
        ValidationPipeline pipeline,
        ILogger<ValidationEngine> logger)
    {
        _registry = registry;
        _pipeline = pipeline;
        _logger = logger;

        _logger.LogDebug(
            "ValidationEngine initialized (registered validators: {Count})",
            registry.Count);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Orchestration steps:
    /// </para>
    /// <list type="number">
    ///   <item>Log the incoming validation request with context metadata.</item>
    ///   <item>Query the <see cref="ValidatorRegistry"/> for applicable validators
    ///   based on the <see cref="ValidationOptions.Mode"/> and
    ///   <see cref="ValidationOptions.LicenseTier"/>.</item>
    ///   <item>Delegate to the <see cref="ValidationPipeline"/> for parallel execution.</item>
    ///   <item>Log the result summary and return.</item>
    /// </list>
    /// </remarks>
    public async Task<ValidationResult> ValidateDocumentAsync(
        ValidationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "Starting document validation for '{DocumentId}' (type: {DocumentType}, " +
            "mode: {Mode}, licenseTier: {LicenseTier})",
            context.DocumentId,
            context.DocumentType,
            context.Options.Mode,
            context.Options.LicenseTier);

        // LOGIC: Step 1 — Resolve applicable validators from the registry.
        var applicableValidators = _registry.GetApplicableValidators(
            context.Options.Mode,
            context.Options.LicenseTier);

        var skippedCount = _registry.GetSkippedCount(
            context.Options.Mode,
            context.Options.LicenseTier);

        _logger.LogDebug(
            "Resolved {ApplicableCount} applicable validators, {SkippedCount} skipped " +
            "for document '{DocumentId}'",
            applicableValidators.Count,
            skippedCount,
            context.DocumentId);

        // LOGIC: Step 2 — Delegate to the pipeline for parallel execution.
        var result = await _pipeline.ExecuteAsync(
            applicableValidators,
            context,
            skippedCount,
            cancellationToken);

        // LOGIC: Step 3 — Log result summary.
        if (result.IsValid)
        {
            _logger.LogInformation(
                "Document '{DocumentId}' passed validation ({ValidatorsRun} validators, " +
                "{WarningCount} warnings, {Duration}ms)",
                context.DocumentId,
                result.ValidatorsRun,
                result.WarningCount,
                result.Duration.TotalMilliseconds);
        }
        else
        {
            _logger.LogWarning(
                "Document '{DocumentId}' failed validation: {ErrorCount} errors, " +
                "{WarningCount} warnings ({ValidatorsRun} validators, {Duration}ms)",
                context.DocumentId,
                result.ErrorCount,
                result.WarningCount,
                result.ValidatorsRun,
                result.Duration.TotalMilliseconds);
        }

        return result;
    }
}
