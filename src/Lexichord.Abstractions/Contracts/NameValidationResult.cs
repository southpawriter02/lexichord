namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Represents the result of validating a file or folder name.
/// </summary>
/// <param name="IsValid">Whether the name is valid.</param>
/// <param name="ErrorMessage">An error message if the name is invalid.</param>
/// <remarks>
/// LOGIC: Provides synchronous name validation results for real-time
/// UI feedback during inline editing operations.
/// </remarks>
public sealed record NameValidationResult(
    bool IsValid,
    string? ErrorMessage = null
)
{
    /// <summary>
    /// A cached valid result instance.
    /// </summary>
    public static readonly NameValidationResult Valid = new(true);

    /// <summary>
    /// Creates an invalid result with an error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>An invalid result.</returns>
    public static NameValidationResult Invalid(string message) => new(false, message);
}
