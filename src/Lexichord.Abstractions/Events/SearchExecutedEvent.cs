using Lexichord.Abstractions.Contracts.Editor;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Domain event raised when search operations are executed.
/// </summary>
/// <remarks>
/// LOGIC: This event is published via MediatR to enable analytics,
/// logging, and cross-module search coordination. It is raised when:
/// - Replace All is executed
/// - Other search analytics are needed
/// </remarks>
/// <param name="DocumentId">The ID of the document being searched.</param>
/// <param name="SearchText">The search text used.</param>
/// <param name="MatchCount">Number of matches found or replacements made.</param>
/// <param name="Options">The search options used.</param>
public record SearchExecutedEvent(
    string DocumentId,
    string SearchText,
    int MatchCount,
    SearchOptions Options
) : DomainEventBase;
