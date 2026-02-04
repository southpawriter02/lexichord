// -----------------------------------------------------------------------
// <copyright file="ChatOptionsValidationException.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation.Results;

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Exception thrown when <see cref="ChatOptions"/> validation fails.
/// </summary>
/// <remarks>
/// <para>
/// This exception wraps FluentValidation failures into a strongly-typed exception
/// that can be caught and handled by callers. Use the <see cref="Errors"/> property
/// to access detailed validation error information.
/// </para>
/// <para>
/// The exception message is formatted as a bulleted list of validation errors
/// for easy display in logs or user interfaces.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// try
/// {
///     var options = resolver.Resolve(requestOptions, "openai");
/// }
/// catch (ChatOptionsValidationException ex)
/// {
///     foreach (var error in ex.Errors)
///     {
///         Console.WriteLine($"  {error.Property}: {error.Message}");
///     }
/// }
/// </code>
/// </example>
public class ChatOptionsValidationException : ChatCompletionException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatOptionsValidationException"/> class
    /// with a collection of validation failures.
    /// </summary>
    /// <param name="failures">The validation failures from FluentValidation.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="failures"/> is null.
    /// </exception>
    public ChatOptionsValidationException(IEnumerable<ValidationFailure> failures)
        : base(FormatMessage(failures))
    {
        ArgumentNullException.ThrowIfNull(failures, nameof(failures));

        Errors = failures
            .Select(f => new ValidationError(
                f.PropertyName,
                f.ErrorMessage,
                f.ErrorCode,
                f.AttemptedValue))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatOptionsValidationException"/> class
    /// with a collection of validation errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="errors"/> is null.
    /// </exception>
    public ChatOptionsValidationException(IEnumerable<ValidationError> errors)
        : base(FormatMessage(errors))
    {
        ArgumentNullException.ThrowIfNull(errors, nameof(errors));

        Errors = errors.ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    /// <value>A read-only collection of <see cref="ValidationError"/> instances.</value>
    public IReadOnlyList<ValidationError> Errors { get; }

    /// <summary>
    /// Gets a value indicating whether there are any validation errors.
    /// </summary>
    /// <value>True if there is at least one error; otherwise, false.</value>
    public bool HasErrors => Errors.Count > 0;

    /// <summary>
    /// Gets errors for a specific property.
    /// </summary>
    /// <param name="propertyName">The property name to filter by.</param>
    /// <returns>A collection of errors for the specified property.</returns>
    public IEnumerable<ValidationError> GetErrorsForProperty(string propertyName)
        => Errors.Where(e => e.Property.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Formats a collection of validation failures into a user-friendly message.
    /// </summary>
    /// <param name="failures">The validation failures.</param>
    /// <returns>A formatted message string.</returns>
    private static string FormatMessage(IEnumerable<ValidationFailure> failures)
    {
        ArgumentNullException.ThrowIfNull(failures, nameof(failures));

        var errorMessages = failures.Select(f => $"  • {f.PropertyName}: {f.ErrorMessage}");
        return $"ChatOptions validation failed:\n{string.Join("\n", errorMessages)}";
    }

    /// <summary>
    /// Formats a collection of validation errors into a user-friendly message.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A formatted message string.</returns>
    private static string FormatMessage(IEnumerable<ValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors, nameof(errors));

        var errorMessages = errors.Select(e => $"  • {e.Property}: {e.Message}");
        return $"ChatOptions validation failed:\n{string.Join("\n", errorMessages)}";
    }
}

/// <summary>
/// Represents a single validation error for a <see cref="ChatOptions"/> property.
/// </summary>
/// <param name="Property">The name of the property that failed validation.</param>
/// <param name="Message">The human-readable error message.</param>
/// <param name="ErrorCode">The machine-readable error code for programmatic handling.</param>
/// <param name="AttemptedValue">The value that was attempted, for diagnostic purposes.</param>
/// <example>
/// <code>
/// var error = new ValidationError(
///     Property: "Temperature",
///     Message: "Temperature must be between 0.0 and 2.0.",
///     ErrorCode: "TEMPERATURE_OUT_OF_RANGE",
///     AttemptedValue: 3.5
/// );
/// </code>
/// </example>
public record ValidationError(
    string Property,
    string Message,
    string? ErrorCode,
    object? AttemptedValue);
