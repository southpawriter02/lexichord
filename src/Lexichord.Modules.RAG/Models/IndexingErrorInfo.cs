// =============================================================================
// File: IndexingErrorInfo.cs
// Project: Lexichord.Modules.RAG
// Description: Detailed information about a document indexing failure.
// Version: v0.4.7d
// =============================================================================
// LOGIC: Encapsulates all error context for UI display and retry decisions.
//   - IsRetryable computed from Category for transient errors.
//   - SuggestedAction provides user-facing guidance per category.
// =============================================================================

namespace Lexichord.Modules.RAG.Models;

/// <summary>
/// Encapsulates detailed information about a document indexing failure.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IndexingErrorInfo"/> provides a rich view of indexing failures
/// for display in the Index Status View and for making retry decisions.
/// </para>
/// <para>
/// <b>Usage:</b> Created by <see cref="Services.IndexingErrorCategorizer"/>
/// when processing <see cref="Indexing.DocumentIndexingFailedEvent"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7d as part of Indexing Errors.
/// </para>
/// </remarks>
/// <param name="DocumentId">The unique identifier of the failed document.</param>
/// <param name="FilePath">The file path of the failed document.</param>
/// <param name="Message">User-friendly error message.</param>
/// <param name="Timestamp">When the error occurred.</param>
/// <param name="RetryCount">Number of retry attempts made.</param>
/// <param name="Category">The categorized error type.</param>
public record IndexingErrorInfo(
    Guid DocumentId,
    string FilePath,
    string Message,
    DateTimeOffset Timestamp,
    int RetryCount,
    IndexingErrorCategory Category)
{
    /// <summary>
    /// Gets whether this error type is potentially recoverable through retry.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Transient errors like rate limits, network issues, and service unavailability
    /// are considered retryable. Persistent errors like file not found or invalid
    /// content require user intervention.
    /// </para>
    /// </remarks>
    public bool IsRetryable => Category is
        IndexingErrorCategory.RateLimit or
        IndexingErrorCategory.NetworkError or
        IndexingErrorCategory.ServiceUnavailable;

    /// <summary>
    /// Gets a user-friendly action suggestion based on the error category.
    /// </summary>
    public string SuggestedAction => Category switch
    {
        IndexingErrorCategory.RateLimit => "Wait a few minutes and retry",
        IndexingErrorCategory.NetworkError => "Check your internet connection and retry",
        IndexingErrorCategory.ServiceUnavailable => "The service is temporarily down. Try again later",
        IndexingErrorCategory.InvalidContent => "This file type may not be supported for indexing",
        IndexingErrorCategory.FileTooLarge => "Consider splitting the file into smaller parts",
        IndexingErrorCategory.FileNotFound => "Check that the file exists and retry",
        IndexingErrorCategory.PermissionDenied => "Check file permissions and retry",
        IndexingErrorCategory.TokenLimitExceeded => "Try using a different chunking strategy",
        IndexingErrorCategory.ApiKeyInvalid => "Check your API key configuration in Settings",
        IndexingErrorCategory.Unknown or _ => "Try re-indexing the document"
    };

    /// <summary>
    /// Creates an <see cref="IndexingErrorInfo"/> from exception details.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="filePath">The file path.</param>
    /// <param name="message">The error message.</param>
    /// <param name="category">The error category.</param>
    /// <param name="retryCount">Current retry count.</param>
    /// <returns>A new <see cref="IndexingErrorInfo"/> instance.</returns>
    public static IndexingErrorInfo Create(
        Guid documentId,
        string filePath,
        string message,
        IndexingErrorCategory category,
        int retryCount = 0)
    {
        return new IndexingErrorInfo(
            documentId,
            filePath,
            message,
            DateTimeOffset.UtcNow,
            retryCount,
            category);
    }
}
