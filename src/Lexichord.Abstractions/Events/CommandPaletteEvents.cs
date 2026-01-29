using Lexichord.Abstractions.Contracts.Commands;
using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// MediatR notification published when the Command Palette is opened.
/// </summary>
/// <param name="Mode">The mode the palette was opened in.</param>
/// <remarks>
/// LOGIC: Published via IMediator for observability and analytics.
/// Handlers can use this to track command palette usage patterns.
/// </remarks>
public record CommandPaletteOpenedEvent(PaletteMode Mode) : INotification;

/// <summary>
/// MediatR notification published when a command is executed from the palette.
/// </summary>
/// <param name="CommandId">The ID of the executed command.</param>
/// <param name="CommandTitle">The display title of the command.</param>
/// <param name="SearchQuery">The search query that found this command.</param>
/// <remarks>
/// LOGIC: Enables analytics on palette usage patterns, such as:
/// - Most frequently executed commands
/// - Common search patterns
/// - Time to find commands
/// </remarks>
public record CommandPaletteExecutedEvent(
    string CommandId,
    string CommandTitle,
    string? SearchQuery) : INotification;
