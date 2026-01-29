namespace Lexichord.Abstractions.Events;

using Lexichord.Abstractions.Contracts.Editor;
using MediatR;

/// <summary>
/// Event published when a document is saved.
/// </summary>
/// <param name="DocumentId">Unique identifier for the document.</param>
/// <param name="FilePath">The saved file path.</param>
/// <param name="BytesWritten">Number of bytes written.</param>
/// <param name="Duration">Time taken for the save.</param>
/// <param name="SavedAt">When the save completed.</param>
public record DocumentSavedEvent(
    string DocumentId,
    string FilePath,
    long BytesWritten,
    TimeSpan Duration,
    DateTimeOffset SavedAt
) : INotification;

/// <summary>
/// Event published when a save operation fails.
/// </summary>
/// <param name="DocumentId">Unique identifier for the document.</param>
/// <param name="FilePath">The attempted file path.</param>
/// <param name="ErrorCode">The error code.</param>
/// <param name="ErrorMessage">The error message.</param>
/// <param name="FailedAt">When the failure occurred.</param>
public record DocumentSaveFailedEvent(
    string DocumentId,
    string FilePath,
    SaveErrorCode ErrorCode,
    string ErrorMessage,
    DateTimeOffset FailedAt
) : INotification;
