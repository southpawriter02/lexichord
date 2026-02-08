// =============================================================================
// File: IValidationEngine.cs
// Project: Lexichord.Abstractions
// Description: Interface for the document validation orchestrator.
// =============================================================================
// LOGIC: The validation engine is the single entry point for running document
//   validation. It resolves applicable validators from the registry, executes
//   them via the pipeline, and returns an aggregated result. Consumers inject
//   this interface without needing to know about individual validators.
//
// v0.6.5e: Validation Orchestrator (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation;

/// <summary>
/// Interface for the document validation orchestrator.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IValidationEngine"/> is the single entry point for all
/// document validation. It orchestrates the validation lifecycle:
/// </para>
/// <list type="number">
///   <item>Resolves applicable validators from the validator registry based on
///   the requested <see cref="ValidationMode"/> and <see cref="LicenseTier"/>.</item>
///   <item>Executes applicable validators in parallel via the validation pipeline.</item>
///   <item>Aggregates findings and returns a unified <see cref="ValidationResult"/>.</item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe. Multiple callers
/// may invoke <see cref="ValidateDocumentAsync"/> concurrently.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5e as part of the Validation Orchestrator.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class DocumentSaveHandler(IValidationEngine engine)
/// {
///     public async Task OnSaveAsync(Document doc, CancellationToken ct)
///     {
///         var context = ValidationContext.Create(doc.Id, doc.Type, doc.Content,
///             new ValidationOptions(Mode: ValidationMode.OnSave));
///         var result = await engine.ValidateDocumentAsync(context, ct);
///         if (!result.IsValid)
///         {
///             // Show validation errors to user
///         }
///     }
/// }
/// </code>
/// </example>
public interface IValidationEngine
{
    /// <summary>
    /// Validates a document using all applicable validators.
    /// </summary>
    /// <param name="context">
    /// The validation context containing the document and configuration.
    /// The <see cref="ValidationContext.Options"/> determines which validators
    /// run and how the validation pass behaves.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token for aborting the validation pass.
    /// </param>
    /// <returns>
    /// An aggregated <see cref="ValidationResult"/> containing all findings
    /// from all validators that ran, plus metadata about skipped validators
    /// and total duration.
    /// </returns>
    Task<ValidationResult> ValidateDocumentAsync(
        ValidationContext context,
        CancellationToken cancellationToken = default);
}
