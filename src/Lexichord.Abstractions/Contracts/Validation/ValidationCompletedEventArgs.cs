// =============================================================================
// File: ValidationCompletedEventArgs.cs
// Project: Lexichord.Abstractions
// Description: Event arguments for validation completion notification.
// =============================================================================
// LOGIC: Provides detailed information about a completed validation run,
//   including the result, which validators succeeded, and which failed.
//
// v0.7.5f: Issue Aggregator (Unified Validation Feature)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Event arguments for <see cref="IUnifiedValidationService.ValidationCompleted"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This class provides information about a completed validation:
/// <list type="bullet">
///   <item><description>The document path and full validation result</description></item>
///   <item><description>Which validators completed successfully</description></item>
///   <item><description>Which validators failed or timed out</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage:</b> Subscribe to <see cref="IUnifiedValidationService.ValidationCompleted"/>
/// to receive notifications when validation finishes. This enables UI components
/// to update their displays with fresh results.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5f as part of the Unified Validation feature.
/// </para>
/// </remarks>
public class ValidationCompletedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the path to the validated document.
    /// </summary>
    public required string DocumentPath { get; init; }

    /// <summary>
    /// Gets the validation result.
    /// </summary>
    public required UnifiedValidationResult Result { get; init; }

    /// <summary>
    /// Gets the list of validators that completed successfully.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Validator names are: "StyleLinter", "GrammarLinter", "ValidationEngine".
    /// </remarks>
    public IReadOnlyList<string> SuccessfulValidators { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets validators that failed or timed out, with error messages.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Maps validator name to its error message. Empty if all succeeded.
    /// </remarks>
    public IReadOnlyDictionary<string, string> FailedValidators { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Gets whether all validators completed successfully.
    /// </summary>
    public bool AllValidatorsSucceeded => FailedValidators.Count == 0;

    /// <summary>
    /// Gets whether any validators failed.
    /// </summary>
    public bool HasFailures => FailedValidators.Count > 0;

    /// <summary>
    /// Gets the timestamp when validation completed.
    /// </summary>
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Creates a success event args with no failures.
    /// </summary>
    /// <param name="documentPath">Path to the validated document.</param>
    /// <param name="result">The validation result.</param>
    /// <param name="successfulValidators">Names of validators that completed.</param>
    /// <returns>A new <see cref="ValidationCompletedEventArgs"/> instance.</returns>
    public static ValidationCompletedEventArgs Success(
        string documentPath,
        UnifiedValidationResult result,
        IReadOnlyList<string> successfulValidators) =>
        new()
        {
            DocumentPath = documentPath,
            Result = result,
            SuccessfulValidators = successfulValidators,
            FailedValidators = new Dictionary<string, string>()
        };

    /// <summary>
    /// Creates an event args with some failures.
    /// </summary>
    /// <param name="documentPath">Path to the validated document.</param>
    /// <param name="result">The validation result (partial).</param>
    /// <param name="successfulValidators">Names of validators that completed.</param>
    /// <param name="failedValidators">Validators that failed with error messages.</param>
    /// <returns>A new <see cref="ValidationCompletedEventArgs"/> instance.</returns>
    public static ValidationCompletedEventArgs WithFailures(
        string documentPath,
        UnifiedValidationResult result,
        IReadOnlyList<string> successfulValidators,
        IReadOnlyDictionary<string, string> failedValidators) =>
        new()
        {
            DocumentPath = documentPath,
            Result = result,
            SuccessfulValidators = successfulValidators,
            FailedValidators = failedValidators
        };
}
