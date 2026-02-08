// =============================================================================
// File: ValidatorRegistry.cs
// Project: Lexichord.Modules.Knowledge
// Description: Registry for managing pluggable document validators.
// =============================================================================
// LOGIC: Thread-safe registry for validator instances. Validators are registered
//   by ID and resolved based on validation mode and license tier. Uses
//   ConcurrentDictionary for thread-safe access from parallel registrations.
//
// Filtering Logic:
//   1. Validator must be enabled (IsEnabled == true)
//   2. Validator must support the requested mode (SupportedModes & mode != 0)
//   3. Validator's RequiredLicenseTier must be <= user's license tier
//
// v0.6.5e: Validation Orchestrator (CKVS Phase 3a)
// Dependencies: IValidator (v0.6.5e), LicenseTier (v0.0.4c), ILogger<T> (v0.0.3b)
// =============================================================================

using System.Collections.Concurrent;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Validation;

/// <summary>
/// Thread-safe registry for managing pluggable document validators.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ValidatorRegistry"/> is the central store for all registered
/// <see cref="IValidator"/> instances. It provides registration, retrieval,
/// and filtering based on <see cref="ValidationMode"/> and <see cref="LicenseTier"/>.
/// </para>
/// <para>
/// <b>Thread Safety:</b> All operations are thread-safe via
/// <see cref="ConcurrentDictionary{TKey, TValue}"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5e as part of the Validation Orchestrator.
/// </para>
/// </remarks>
internal sealed class ValidatorRegistry
{
    private readonly ConcurrentDictionary<string, ValidatorInfo> _validators = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<ValidatorRegistry> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidatorRegistry"/> class.
    /// </summary>
    /// <param name="logger">Logger for registration and filtering diagnostics.</param>
    public ValidatorRegistry(ILogger<ValidatorRegistry> logger)
    {
        _logger = logger;
        _logger.LogDebug("ValidatorRegistry initialized");
    }

    /// <summary>
    /// Gets the total number of registered validators.
    /// </summary>
    public int Count => _validators.Count;

    /// <summary>
    /// Registers a validator with optional priority.
    /// </summary>
    /// <param name="validator">The validator to register. Must not be null.</param>
    /// <param name="priority">
    /// Priority for ordering. Higher values = higher priority. Default is 0.
    /// </param>
    /// <returns>
    /// <c>true</c> if the validator was registered successfully;
    /// <c>false</c> if a validator with the same ID is already registered.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="validator"/> is null.</exception>
    public bool Register(IValidator validator, int priority = 0)
    {
        ArgumentNullException.ThrowIfNull(validator);

        var info = new ValidatorInfo(validator, priority);

        // LOGIC: TryAdd ensures we don't overwrite existing registrations.
        // Duplicate IDs are logged as warnings but do not throw.
        if (_validators.TryAdd(validator.Id, info))
        {
            _logger.LogInformation(
                "Registered validator '{ValidatorId}' (display: '{DisplayName}', priority: {Priority}, " +
                "modes: {SupportedModes}, requiredTier: {RequiredTier})",
                validator.Id,
                validator.DisplayName,
                priority,
                validator.SupportedModes,
                validator.RequiredLicenseTier);
            return true;
        }

        _logger.LogWarning(
            "Validator '{ValidatorId}' is already registered. Duplicate registration ignored",
            validator.Id);
        return false;
    }

    /// <summary>
    /// Gets all validators applicable for the given mode and license tier.
    /// </summary>
    /// <param name="mode">The validation mode to filter by.</param>
    /// <param name="licenseTier">The current user's license tier.</param>
    /// <returns>
    /// Validators that are enabled, support the requested mode, and have a
    /// <see cref="IValidator.RequiredLicenseTier"/> at or below the given tier.
    /// Ordered by priority descending (highest priority first).
    /// </returns>
    public IReadOnlyList<IValidator> GetApplicableValidators(
        ValidationMode mode,
        LicenseTier licenseTier)
    {
        _logger.LogDebug(
            "Resolving applicable validators for mode={Mode}, licenseTier={LicenseTier} " +
            "(total registered: {TotalCount})",
            mode, licenseTier, _validators.Count);

        var applicable = new List<(IValidator Validator, int Priority)>();
        var skippedMode = 0;
        var skippedLicense = 0;
        var skippedDisabled = 0;

        foreach (var (id, info) in _validators)
        {
            // LOGIC: Step 1 — Skip disabled validators.
            if (!info.IsEnabled)
            {
                skippedDisabled++;
                _logger.LogTrace(
                    "Skipping validator '{ValidatorId}': disabled",
                    id);
                continue;
            }

            // LOGIC: Step 2 — Check mode compatibility using bitwise AND.
            if ((info.Validator.SupportedModes & mode) == 0)
            {
                skippedMode++;
                _logger.LogTrace(
                    "Skipping validator '{ValidatorId}': mode {ValidatorModes} does not support {RequestedMode}",
                    id, info.Validator.SupportedModes, mode);
                continue;
            }

            // LOGIC: Step 3 — Check license tier.
            if (info.Validator.RequiredLicenseTier > licenseTier)
            {
                skippedLicense++;
                _logger.LogTrace(
                    "Skipping validator '{ValidatorId}': requires {RequiredTier} but user has {CurrentTier}",
                    id, info.Validator.RequiredLicenseTier, licenseTier);
                continue;
            }

            applicable.Add((info.Validator, info.Priority));
        }

        // LOGIC: Sort by priority descending — highest priority validators first.
        var result = applicable
            .OrderByDescending(v => v.Priority)
            .Select(v => v.Validator)
            .ToList();

        _logger.LogDebug(
            "Resolved {ApplicableCount} applicable validators " +
            "(skipped: {SkippedMode} mode, {SkippedLicense} license, {SkippedDisabled} disabled)",
            result.Count, skippedMode, skippedLicense, skippedDisabled);

        return result;
    }

    /// <summary>
    /// Gets the count of validators that would be skipped for the given mode and license tier.
    /// </summary>
    /// <param name="mode">The validation mode to filter by.</param>
    /// <param name="licenseTier">The current user's license tier.</param>
    /// <returns>The number of registered validators that would not run.</returns>
    public int GetSkippedCount(ValidationMode mode, LicenseTier licenseTier)
    {
        return _validators.Count - GetApplicableValidators(mode, licenseTier).Count;
    }
}
