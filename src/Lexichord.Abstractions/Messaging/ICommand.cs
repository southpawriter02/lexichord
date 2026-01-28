namespace Lexichord.Abstractions.Messaging;

/// <summary>
/// Marker interface for commands that change application state.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the handler.</typeparam>
/// <remarks>
/// LOGIC: Commands represent an intent to CHANGE state. Key characteristics:
///
/// 1. **Single Handler**: Each command type MUST have exactly ONE handler.
///    MediatR will throw if zero or multiple handlers exist.
///
/// 2. **Return Value**: Commands return a response indicating the result
///    (e.g., created entity ID, success/failure result).
///
/// 3. **Side Effects**: Commands MAY cause side effects (database writes,
///    file changes, event publications).
///
/// 4. **Dispatch**: Commands are dispatched via IMediator.Send().
///
/// Example:
/// <code>
/// public record CreateDocumentCommand : ICommand&lt;DocumentId&gt;
/// {
///     public string Title { get; init; }
///     public string Content { get; init; }
/// }
/// </code>
/// </remarks>
public interface ICommand<out TResponse> : MediatR.IRequest<TResponse>
{
}

/// <summary>
/// Marker interface for commands that do not return a value.
/// </summary>
/// <remarks>
/// LOGIC: Use this for commands where success is indicated by the absence
/// of an exception. Internally returns MediatR.Unit.
///
/// Example:
/// <code>
/// public record DeleteDocumentCommand : ICommand
/// {
///     public DocumentId Id { get; init; }
/// }
/// </code>
/// </remarks>
public interface ICommand : ICommand<MediatR.Unit>
{
}
