using MediatR;

namespace Lexichord.Abstractions.Messaging;

/// <summary>
/// Command to close a document.
/// </summary>
/// <param name="DocumentId">The document identifier.</param>
/// <param name="Force">If true, bypasses CanCloseAsync check.</param>
public sealed record CloseDocumentCommand(string DocumentId, bool Force = false) : IRequest<bool>;

/// <summary>
/// Command to close all documents.
/// </summary>
/// <param name="Force">If true, bypasses CanCloseAsync for all documents.</param>
/// <param name="SkipPinned">If true, skips pinned documents.</param>
public sealed record CloseAllDocumentsCommand(bool Force = false, bool SkipPinned = true) : IRequest<bool>;

/// <summary>
/// Command to close all documents except one.
/// </summary>
/// <param name="ExceptDocumentId">The document to keep open.</param>
/// <param name="Force">If true, bypasses CanCloseAsync for all documents.</param>
/// <param name="SkipPinned">If true, skips pinned documents.</param>
public sealed record CloseAllButThisCommand(string ExceptDocumentId, bool Force = false, bool SkipPinned = true) : IRequest<bool>;

/// <summary>
/// Command to close documents to the right of a reference document.
/// </summary>
/// <param name="DocumentId">The reference document.</param>
/// <param name="Force">If true, bypasses CanCloseAsync check.</param>
public sealed record CloseToTheRightCommand(string DocumentId, bool Force = false) : IRequest<bool>;

/// <summary>
/// Command to pin or unpin a document.
/// </summary>
/// <param name="DocumentId">The document identifier.</param>
/// <param name="Pin">True to pin; false to unpin.</param>
public sealed record PinDocumentCommand(string DocumentId, bool Pin) : IRequest;
