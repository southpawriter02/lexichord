using Lexichord.Abstractions.Contracts.Linting;
using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Published when a linting operation begins for a document.
/// </summary>
/// <remarks>
/// LOGIC: Enables UI feedback (spinner, "Linting..." indicator).
/// Published at the start of actual scanning, after debounce completes.
///
/// Version: v0.2.3a
/// </remarks>
/// <param name="DocumentId">The document being linted.</param>
/// <param name="Timestamp">When the lint operation started.</param>
public sealed record LintingStartedEvent(
    string DocumentId,
    DateTimeOffset Timestamp
) : DomainEventBase, INotification;

/// <summary>
/// Published when a linting operation completes successfully or is cancelled.
/// </summary>
/// <remarks>
/// LOGIC: Enables UI update with results (badge count, gutter markers).
/// Contains the full LintResult with violations and timing.
///
/// Version: v0.2.3a
/// </remarks>
/// <param name="Result">The complete lint result.</param>
public sealed record LintingCompletedEvent(
    LintResult Result
) : DomainEventBase, INotification;

/// <summary>
/// Published when a linting operation encounters an error.
/// </summary>
/// <remarks>
/// LOGIC: Enables error logging and user notification.
/// Errors are non-fatal - document remains subscribed.
///
/// Version: v0.2.3a
/// </remarks>
/// <param name="DocumentId">The document that failed to lint.</param>
/// <param name="ErrorMessage">Human-readable error description.</param>
/// <param name="Exception">The underlying exception (if available).</param>
public sealed record LintingErrorEvent(
    string DocumentId,
    string ErrorMessage,
    Exception? Exception = null
) : DomainEventBase, INotification;
