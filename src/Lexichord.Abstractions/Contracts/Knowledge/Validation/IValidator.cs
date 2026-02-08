// =============================================================================
// File: IValidator.cs
// Project: Lexichord.Abstractions
// Description: Interface for pluggable document validators.
// =============================================================================
// LOGIC: Each validator declares its identity, supported modes, required
//   license tier, and the async validation operation. The ValidationEngine
//   uses these properties to filter which validators run for a given context.
//
// v0.6.5e: Validation Orchestrator (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation;

/// <summary>
/// Interface for pluggable document validators in the validation pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Each validator is a self-contained validation unit that inspects a document
/// and produces zero or more <see cref="ValidationFinding"/> instances. Validators
/// declare their <see cref="SupportedModes"/> and <see cref="RequiredLicenseTier"/>
/// so the <see cref="IValidationEngine"/> can filter which validators run.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe. The validation
/// engine may invoke multiple validators in parallel, and a single validator
/// instance may be invoked concurrently for different documents.
/// </para>
/// <para>
/// <b>Cancellation:</b> Implementations must respect the
/// <see cref="CancellationToken"/> to support timeout enforcement and
/// user-initiated cancellation.
/// </para>
/// <para>
/// <b>Error Isolation:</b> If a validator throws an exception, the engine
/// records the failure and continues with remaining validators. Validators
/// should prefer returning error findings over throwing exceptions.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5e as part of the Validation Orchestrator.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class SchemaComplianceValidator : IValidator
/// {
///     public string Id => "schema-compliance";
///     public string DisplayName => "Schema Compliance";
///     public ValidationMode SupportedModes => ValidationMode.All;
///     public LicenseTier RequiredLicenseTier => LicenseTier.Core;
///
///     public Task&lt;IReadOnlyList&lt;ValidationFinding&gt;&gt; ValidateAsync(
///         ValidationContext context, CancellationToken cancellationToken)
///     {
///         var findings = new List&lt;ValidationFinding&gt;();
///         // ... perform validation ...
///         return Task.FromResult&lt;IReadOnlyList&lt;ValidationFinding&gt;&gt;(findings);
///     }
/// }
/// </code>
/// </example>
public interface IValidator
{
    /// <summary>
    /// Gets the unique identifier for this validator.
    /// </summary>
    /// <remarks>
    /// Must be stable across application restarts. Used for finding attribution,
    /// logging, and configuration-based enable/disable.
    /// Convention: lowercase kebab-case (e.g., "schema-compliance", "axiom-evaluator").
    /// </remarks>
    string Id { get; }

    /// <summary>
    /// Gets the human-readable display name for this validator.
    /// </summary>
    /// <remarks>
    /// Shown in the UI validation panel and logs.
    /// </remarks>
    string DisplayName { get; }

    /// <summary>
    /// Gets the validation modes this validator supports.
    /// </summary>
    /// <remarks>
    /// A validator is only invoked if the requested <see cref="ValidationOptions.Mode"/>
    /// intersects with this value. Use <see cref="ValidationMode.All"/> for validators
    /// that should run in every mode.
    /// </remarks>
    ValidationMode SupportedModes { get; }

    /// <summary>
    /// Gets the minimum license tier required to use this validator.
    /// </summary>
    /// <remarks>
    /// Validators requiring a tier higher than the user's current tier are
    /// skipped and counted in <see cref="ValidationResult.ValidatorsSkipped"/>.
    /// </remarks>
    LicenseTier RequiredLicenseTier { get; }

    /// <summary>
    /// Validates the document described by the given context.
    /// </summary>
    /// <param name="context">
    /// The validation context containing the document and configuration.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token for timeout enforcement and user cancellation.
    /// Implementations must check this token periodically for long-running operations.
    /// </param>
    /// <returns>
    /// A read-only list of findings. An empty list indicates the document
    /// passed this validator's checks.
    /// </returns>
    Task<IReadOnlyList<ValidationFinding>> ValidateAsync(
        ValidationContext context,
        CancellationToken cancellationToken = default);
}
