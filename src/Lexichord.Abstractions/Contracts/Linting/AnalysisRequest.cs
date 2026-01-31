namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Represents a document analysis request with snapshot semantics.
/// </summary>
/// <remarks>
/// LOGIC: Captures all information needed to perform a single analysis pass.
/// Immutable record ensures thread-safe sharing across the async pipeline.
/// The content is a snapshot taken at request time, so subsequent edits
/// don't affect in-flight analysis.
///
/// Version: v0.3.7a
/// </remarks>
/// <param name="DocumentId">Unique identifier for the document being analyzed.</param>
/// <param name="FilePath">Optional file path for filtering, logging, and display.</param>
/// <param name="Content">Document content snapshot at time of request.</param>
/// <param name="RequestedAt">Timestamp when the request was created.</param>
/// <param name="CancellationToken">Token for cooperative cancellation of the request.</param>
public sealed record AnalysisRequest(
    string DocumentId,
    string? FilePath,
    string Content,
    DateTimeOffset RequestedAt,
    CancellationToken CancellationToken = default)
{
    /// <summary>
    /// Creates a new analysis request with the current timestamp.
    /// </summary>
    /// <param name="documentId">Unique identifier for the document.</param>
    /// <param name="filePath">Optional file path.</param>
    /// <param name="content">Document content snapshot.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A new <see cref="AnalysisRequest"/> instance.</returns>
    /// <remarks>
    /// LOGIC: Factory method provides a convenient way to create requests
    /// with automatic timestamp capture. Use this for most request creation.
    /// </remarks>
    public static AnalysisRequest Create(
        string documentId,
        string? filePath,
        string content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documentId);
        ArgumentNullException.ThrowIfNull(content);

        return new AnalysisRequest(
            documentId,
            filePath,
            content,
            DateTimeOffset.UtcNow,
            cancellationToken);
    }

    /// <summary>
    /// Creates a new request with a different cancellation token.
    /// </summary>
    /// <param name="cancellationToken">The new cancellation token.</param>
    /// <returns>A new <see cref="AnalysisRequest"/> with the updated token.</returns>
    /// <remarks>
    /// LOGIC: Used when the buffer needs to provide its own cancellation
    /// token that it can control independently of the caller's token.
    /// </remarks>
    public AnalysisRequest WithCancellationToken(CancellationToken cancellationToken)
        => this with { CancellationToken = cancellationToken };
}
