// =============================================================================
// File: IndexingErrorCategory.cs
// Project: Lexichord.Modules.RAG
// Description: Categorization of indexing failure types.
// Version: v0.4.7d
// =============================================================================
// LOGIC: Provides granular categorization for error handling and retry decisions.
//   - Categories map to specific exception types and HTTP status codes.
//   - Used by IndexingErrorCategorizer to determine IsRetryable.
// =============================================================================

namespace Lexichord.Modules.RAG.Models;

/// <summary>
/// Categorizes the type of error that caused a document indexing failure.
/// </summary>
/// <remarks>
/// <para>
/// Error categories enable targeted error handling and user-friendly messaging.
/// Some categories are inherently retryable (e.g., <see cref="RateLimit"/>),
/// while others require user intervention (e.g., <see cref="FileNotFound"/>).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7d as part of Indexing Errors.
/// </para>
/// </remarks>
public enum IndexingErrorCategory
{
    /// <summary>
    /// The error could not be classified into a known category.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The embedding API returned a rate limit error (HTTP 429).
    /// </summary>
    /// <remarks>Retryable after a delay.</remarks>
    RateLimit,

    /// <summary>
    /// A network-related error occurred (connection timeout, DNS failure, etc.).
    /// </summary>
    /// <remarks>Retryable after checking connectivity.</remarks>
    NetworkError,

    /// <summary>
    /// The embedding service is temporarily unavailable (HTTP 503).
    /// </summary>
    /// <remarks>Retryable after a delay.</remarks>
    ServiceUnavailable,

    /// <summary>
    /// The document content is invalid or cannot be processed (e.g., binary file).
    /// </summary>
    /// <remarks>Not retryable without user intervention.</remarks>
    InvalidContent,

    /// <summary>
    /// The file exceeds the maximum size limit for indexing.
    /// </summary>
    /// <remarks>Not retryable without splitting or excluding the file.</remarks>
    FileTooLarge,

    /// <summary>
    /// The source file was not found on disk.
    /// </summary>
    /// <remarks>Not retryable until file is restored.</remarks>
    FileNotFound,

    /// <summary>
    /// Access to the source file was denied.
    /// </summary>
    /// <remarks>Not retryable until permissions are fixed.</remarks>
    PermissionDenied,

    /// <summary>
    /// The document content exceeds the embedding model's token limit.
    /// </summary>
    /// <remarks>May be retryable with different chunking settings.</remarks>
    TokenLimitExceeded,

    /// <summary>
    /// The API key is missing, invalid, or expired.
    /// </summary>
    /// <remarks>Not retryable until API key is configured correctly.</remarks>
    ApiKeyInvalid
}
