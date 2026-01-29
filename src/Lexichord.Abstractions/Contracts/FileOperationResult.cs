namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Represents the result of a file operation.
/// </summary>
/// <remarks>
/// LOGIC: Provides a structured result pattern for file operations,
/// enabling the caller to determine success/failure and access
/// relevant error information when operations fail.
///
/// Usage pattern:
/// <code>
/// var result = await fileService.CreateFileAsync(parent, name);
/// if (result.Success)
///     // Use result.ResultPath
/// else
///     // Handle result.Error and result.ErrorMessage
/// </code>
/// </remarks>
public sealed record FileOperationResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the resulting path after the operation (for create/rename).
    /// </summary>
    public string? ResultPath { get; init; }

    /// <summary>
    /// Gets a human-readable error message when the operation fails.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the categorized error type when the operation fails.
    /// </summary>
    public FileOperationError? Error { get; init; }

    /// <summary>
    /// Creates a successful result with an optional result path.
    /// </summary>
    /// <param name="resultPath">The path created or modified by the operation.</param>
    /// <returns>A success result.</returns>
    public static FileOperationResult Succeeded(string? resultPath = null) =>
        new() { Success = true, ResultPath = resultPath };

    /// <summary>
    /// Creates a failed result with an error type and message.
    /// </summary>
    /// <param name="error">The categorized error type.</param>
    /// <param name="message">A human-readable error message.</param>
    /// <returns>A failure result.</returns>
    public static FileOperationResult Failed(FileOperationError error, string message) =>
        new() { Success = false, Error = error, ErrorMessage = message };
}
