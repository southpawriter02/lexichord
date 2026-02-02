// =============================================================================
// File: IndexingErrorCategorizer.cs
// Project: Lexichord.Modules.RAG
// Description: Static service for categorizing indexing exceptions.
// Version: v0.4.7d
// =============================================================================
// LOGIC: Maps exceptions to IndexingErrorCategory via pattern matching.
//   - Inspects HttpRequestException for HTTP status codes.
//   - Uses exception type matching for FileNotFoundException, etc.
//   - Falls back to message inspection for edge cases.
// =============================================================================

using System.Net;
using System.Net.Http;
using Lexichord.Modules.RAG.Models;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Static service for categorizing indexing exceptions into actionable categories.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IndexingErrorCategorizer"/> maps raw exceptions to
/// <see cref="IndexingErrorCategory"/> values, enabling targeted error handling,
/// retry decisions, and user-friendly messaging.
/// </para>
/// <para>
/// <b>Pattern Matching Strategy:</b>
/// </para>
/// <list type="number">
///   <item>Check for <see cref="HttpRequestException"/> with specific status codes.</item>
///   <item>Check for file system exceptions (<see cref="FileNotFoundException"/>, etc.).</item>
///   <item>Inspect exception message for common patterns.</item>
///   <item>Fall back to <see cref="IndexingErrorCategory.Unknown"/>.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.4.7d as part of Indexing Errors.
/// </para>
/// </remarks>
public static class IndexingErrorCategorizer
{
    /// <summary>
    /// Categorizes an exception into an <see cref="IndexingErrorCategory"/>.
    /// </summary>
    /// <param name="exception">The exception to categorize.</param>
    /// <returns>The determined <see cref="IndexingErrorCategory"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method performs pattern matching in the following priority order:
    /// </para>
    /// <list type="number">
    ///   <item>HTTP status code detection (429, 503, 401).</item>
    ///   <item>Exception type detection (FileNotFoundException, etc.).</item>
    ///   <item>Message pattern inspection (rate limit, too large).</item>
    ///   <item>Default to Unknown.</item>
    /// </list>
    /// </remarks>
    public static IndexingErrorCategory Categorize(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            // HTTP-based errors with status codes
            HttpRequestException { StatusCode: HttpStatusCode.TooManyRequests } =>
                IndexingErrorCategory.RateLimit,

            HttpRequestException { StatusCode: HttpStatusCode.ServiceUnavailable } =>
                IndexingErrorCategory.ServiceUnavailable,

            HttpRequestException { StatusCode: HttpStatusCode.Unauthorized } =>
                IndexingErrorCategory.ApiKeyInvalid,

            HttpRequestException { StatusCode: HttpStatusCode.Forbidden } =>
                IndexingErrorCategory.ApiKeyInvalid,

            // Generic network errors
            HttpRequestException =>
                IndexingErrorCategory.NetworkError,

            // File system errors
            FileNotFoundException =>
                IndexingErrorCategory.FileNotFound,

            DirectoryNotFoundException =>
                IndexingErrorCategory.FileNotFound,

            UnauthorizedAccessException =>
                IndexingErrorCategory.PermissionDenied,

            // Message-based pattern matching
            _ when ContainsIgnoreCase(exception.Message, "rate limit") =>
                IndexingErrorCategory.RateLimit,

            _ when ContainsIgnoreCase(exception.Message, "too large") =>
                IndexingErrorCategory.FileTooLarge,

            _ when ContainsIgnoreCase(exception.Message, "token limit") =>
                IndexingErrorCategory.TokenLimitExceeded,

            _ when ContainsIgnoreCase(exception.Message, "max tokens") =>
                IndexingErrorCategory.TokenLimitExceeded,

            _ when ContainsIgnoreCase(exception.Message, "invalid content") =>
                IndexingErrorCategory.InvalidContent,

            _ when ContainsIgnoreCase(exception.Message, "unsupported") =>
                IndexingErrorCategory.InvalidContent,

            _ when ContainsIgnoreCase(exception.Message, "api key") =>
                IndexingErrorCategory.ApiKeyInvalid,

            _ => IndexingErrorCategory.Unknown
        };
    }

    /// <summary>
    /// Generates a user-friendly error message based on the exception and category.
    /// </summary>
    /// <param name="exception">The original exception.</param>
    /// <param name="category">The categorized error type.</param>
    /// <returns>A user-friendly error message.</returns>
    public static string GetUserFriendlyMessage(Exception exception, IndexingErrorCategory category)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return category switch
        {
            IndexingErrorCategory.RateLimit =>
                "Rate limit exceeded. Please wait before retrying.",

            IndexingErrorCategory.NetworkError =>
                "Network error occurred. Check your internet connection.",

            IndexingErrorCategory.ServiceUnavailable =>
                "The embedding service is temporarily unavailable.",

            IndexingErrorCategory.InvalidContent =>
                "The document content could not be processed.",

            IndexingErrorCategory.FileTooLarge =>
                "The file is too large to index.",

            IndexingErrorCategory.FileNotFound =>
                $"File not found: {Path.GetFileName(GetFilePathFromException(exception))}",

            IndexingErrorCategory.PermissionDenied =>
                "Access denied to the file.",

            IndexingErrorCategory.TokenLimitExceeded =>
                "The document exceeds the maximum token limit.",

            IndexingErrorCategory.ApiKeyInvalid =>
                "API key is invalid or missing. Check your settings.",

            IndexingErrorCategory.Unknown or _ =>
                TruncateMessage($"Indexing failed: {exception.Message}", 200)
        };
    }

    /// <summary>
    /// Creates an <see cref="IndexingErrorInfo"/> from an exception.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="filePath">The file path.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="retryCount">Current retry count.</param>
    /// <returns>A fully populated <see cref="IndexingErrorInfo"/>.</returns>
    public static IndexingErrorInfo CreateErrorInfo(
        Guid documentId,
        string filePath,
        Exception exception,
        int retryCount = 0)
    {
        var category = Categorize(exception);
        var message = GetUserFriendlyMessage(exception, category);

        return IndexingErrorInfo.Create(
            documentId,
            filePath,
            message,
            category,
            retryCount);
    }

    #region Private Helpers

    private static bool ContainsIgnoreCase(string source, string value) =>
        source.Contains(value, StringComparison.OrdinalIgnoreCase);

    private static string GetFilePathFromException(Exception exception)
    {
        // Try to extract file path from FileNotFoundException
        if (exception is FileNotFoundException fnf && !string.IsNullOrEmpty(fnf.FileName))
        {
            return fnf.FileName;
        }

        return "unknown file";
    }

    private static string TruncateMessage(string message, int maxLength)
    {
        if (string.IsNullOrEmpty(message))
            return string.Empty;

        return message.Length <= maxLength
            ? message
            : string.Concat(message.AsSpan(0, maxLength - 3), "...");
    }

    #endregion
}
