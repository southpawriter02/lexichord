namespace Lexichord.Abstractions.Validation;

/// <summary>
/// Exception thrown when one or more validation errors occur during request processing.
/// </summary>
/// <remarks>
/// DESIGN: This exception is designed for:
///
/// 1. **Aggregation**: Collects all validation errors from multiple validators
///    into a single exception, enabling batch error reporting.
///
/// 2. **Structured Data**: Provides errors as <see cref="ValidationError"/> records
///    for easy serialization and programmatic access.
///
/// 3. **Clean Serialization**: Unlike raw FluentValidation exceptions, this class
///    avoids serialization issues with internal FluentValidation types.
///
/// 4. **Pipeline Integration**: Thrown by <c>ValidationBehavior</c> when request
///    validation fails, allowing upstream handlers to catch and process.
/// </remarks>
public sealed class ValidationException : Exception
{
    /// <summary>
    /// Gets the collection of validation errors that caused this exception.
    /// </summary>
    public IReadOnlyCollection<ValidationError> Errors { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationException"/> with the specified errors.
    /// </summary>
    /// <param name="errors">The validation errors that caused the exception.</param>
    public ValidationException(IEnumerable<ValidationError> errors)
        : base(BuildMessage(errors))
    {
        Errors = errors.ToList().AsReadOnly();
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationException"/> from FluentValidation failures.
    /// </summary>
    /// <param name="failures">The FluentValidation failure objects to convert.</param>
    /// <remarks>
    /// LOGIC: This constructor provides a convenient way to create a ValidationException
    /// directly from FluentValidation results, extracting the relevant properties.
    /// </remarks>
    public ValidationException(IEnumerable<FluentValidation.Results.ValidationFailure> failures)
        : this(failures.Select(f => new ValidationError(
            f.PropertyName,
            f.ErrorMessage,
            f.ErrorCode,
            f.AttemptedValue)))
    {
    }

    /// <summary>
    /// Builds a summary message from the collection of validation errors.
    /// </summary>
    private static string BuildMessage(IEnumerable<ValidationError> errors)
    {
        var errorList = errors.ToList();

        if (errorList.Count == 0)
        {
            return "One or more validation errors occurred.";
        }

        if (errorList.Count == 1)
        {
            var error = errorList[0];
            return $"Validation failed for '{error.PropertyName}': {error.ErrorMessage}";
        }

        var propertyNames = string.Join(", ", errorList.Select(e => e.PropertyName).Distinct());
        return $"Validation failed for {errorList.Count} errors on properties: {propertyNames}";
    }
}
