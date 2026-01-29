namespace Lexichord.Abstractions.Validation;

/// <summary>
/// Represents a single validation error with structured details.
/// </summary>
/// <param name="PropertyName">The name of the property that failed validation.</param>
/// <param name="ErrorMessage">A human-readable description of the validation failure.</param>
/// <param name="ErrorCode">An optional machine-readable error code for categorization.</param>
/// <param name="AttemptedValue">The value that was attempted but failed validation.</param>
/// <remarks>
/// DESIGN: This record provides a standardized structure for validation errors that:
///
/// 1. **Enables Structured Logging**: Error codes and property names allow for
///    machine-readable logging and metrics aggregation.
///
/// 2. **Supports Localization**: The ErrorCode can be used to look up localized
///    messages while preserving the default ErrorMessage.
///
/// 3. **Aids Debugging**: AttemptedValue helps developers understand what value
///    caused the validation failure.
///
/// 4. **Serializes Cleanly**: As a record, it serializes to JSON without special
///    handling for exception-related properties.
/// </remarks>
public sealed record ValidationError(
    string PropertyName,
    string ErrorMessage,
    string? ErrorCode = null,
    object? AttemptedValue = null);
