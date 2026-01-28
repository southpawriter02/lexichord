namespace Lexichord.Abstractions.Messaging;

/// <summary>
/// Marker interface for queries that read application state.
/// </summary>
/// <typeparam name="TResponse">The type of data returned by the handler.</typeparam>
/// <remarks>
/// LOGIC: Queries represent an intent to READ state. Key characteristics:
///
/// 1. **Single Handler**: Each query type MUST have exactly ONE handler.
///
/// 2. **No Side Effects**: Queries MUST NOT cause side effects. The same
///    query with the same parameters should return the same result
///    (within a consistent read context).
///
/// 3. **Return Value**: Queries always return data (DTOs, view models, etc.).
///
/// 4. **Dispatch**: Queries are dispatched via IMediator.Send() (same as commands).
///
/// Example:
/// <code>
/// public record GetDocumentByIdQuery : IQuery&lt;DocumentDto&gt;
/// {
///     public DocumentId Id { get; init; }
/// }
/// </code>
/// </remarks>
public interface IQuery<out TResponse> : MediatR.IRequest<TResponse>
{
}
