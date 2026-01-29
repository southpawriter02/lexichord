namespace Lexichord.Host.Services;

using System.Collections.Concurrent;
using System.Diagnostics;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Central registry for all application commands.
/// </summary>
/// <remarks>
/// LOGIC (v0.1.5a): Singleton service that maintains a thread-safe dictionary
/// of all registered commands. Provides lookup, execution, and event publishing.
///
/// Thread Safety:
/// - Uses ConcurrentDictionary for atomic add/remove
/// - Snapshot iteration for GetAll operations
/// - Event handlers invoked on calling thread
///
/// MediatR Integration:
/// - Publishes CommandRegisteredMediatREvent on registration
/// - Publishes CommandExecutedMediatREvent on execution
/// - Fire-and-forget with exception handling
/// </remarks>
public class CommandRegistry : ICommandRegistry
{
    private readonly ConcurrentDictionary<string, CommandDefinition> _commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly IMediator _mediator;
    private readonly ILogger<CommandRegistry> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandRegistry"/> class.
    /// </summary>
    /// <param name="mediator">MediatR for event publishing.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public CommandRegistry(IMediator mediator, ILogger<CommandRegistry> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public int CommandCount => _commands.Count;

    /// <inheritdoc/>
    public event EventHandler<CommandRegisteredEventArgs>? CommandRegistered;

    /// <inheritdoc/>
    public event EventHandler<CommandUnregisteredEventArgs>? CommandUnregistered;

    /// <inheritdoc/>
    public event EventHandler<CommandExecutedEventArgs>? CommandExecuted;

    /// <inheritdoc/>
    public void Register(CommandDefinition command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // LOGIC: Validate command definition
        var errors = command.Validate();
        if (errors.Count > 0)
        {
            var message = $"Invalid command definition: {string.Join("; ", errors)}";
            _logger.LogError(message);
            throw new ArgumentException(message, nameof(command));
        }

        // LOGIC: Atomic add-or-fail to detect duplicates
        if (!_commands.TryAdd(command.Id, command))
        {
            var message = $"Command with ID '{command.Id}' is already registered.";
            _logger.LogError(message);
            throw new ArgumentException(message, nameof(command));
        }

        _logger.LogDebug("Registered command: {CommandId} ({Title})", command.Id, command.Title);

        // LOGIC: Raise .NET event for synchronous subscribers
        CommandRegistered?.Invoke(this, new CommandRegisteredEventArgs { Command = command });

        // LOGIC: Publish MediatR event (fire-and-forget with exception handling)
        PublishEventAsync(new CommandRegisteredMediatREvent(command.Id, command.Title, command.Category));
    }

    /// <inheritdoc/>
    public void RegisterRange(IEnumerable<CommandDefinition> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        // LOGIC: Enumerate once and validate all before registering any
        var commandList = commands.ToList();

        // Validate all commands first
        foreach (var command in commandList)
        {
            if (command is null)
                throw new ArgumentException("Command collection contains null entries.", nameof(commands));

            var errors = command.Validate();
            if (errors.Count > 0)
            {
                throw new ArgumentException(
                    $"Invalid command '{command.Id}': {string.Join("; ", errors)}",
                    nameof(commands));
            }

            if (_commands.ContainsKey(command.Id))
            {
                throw new ArgumentException(
                    $"Command with ID '{command.Id}' is already registered.",
                    nameof(commands));
            }

            // Check for duplicates within the batch
            if (commandList.Count(c => c.Id.Equals(command.Id, StringComparison.OrdinalIgnoreCase)) > 1)
            {
                throw new ArgumentException(
                    $"Duplicate command ID '{command.Id}' in batch.",
                    nameof(commands));
            }
        }

        // LOGIC: All validated, now register
        foreach (var command in commandList)
        {
            Register(command);
        }
    }

    /// <inheritdoc/>
    public bool Unregister(string commandId)
    {
        if (string.IsNullOrWhiteSpace(commandId))
            return false;

        if (!_commands.TryRemove(commandId, out var removed))
        {
            _logger.LogDebug("Unregister failed: command '{CommandId}' not found", commandId);
            return false;
        }

        _logger.LogDebug("Unregistered command: {CommandId}", commandId);

        // LOGIC: Raise .NET event
        CommandUnregistered?.Invoke(this, new CommandUnregisteredEventArgs { CommandId = commandId });

        return true;
    }

    /// <inheritdoc/>
    public IReadOnlyList<CommandDefinition> GetAllCommands()
    {
        // LOGIC: Return snapshot ordered by category, then title
        return _commands.Values
            .OrderBy(c => c.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <inheritdoc/>
    public IReadOnlyList<CommandDefinition> GetCommandsByCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return [];

        return _commands.Values
            .Where(c => c.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetCategories()
    {
        return _commands.Values
            .Select(c => c.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <inheritdoc/>
    public CommandDefinition? GetCommand(string commandId)
    {
        if (string.IsNullOrWhiteSpace(commandId))
            return null;

        return _commands.TryGetValue(commandId, out var command) ? command : null;
    }

    /// <inheritdoc/>
    public bool HasCommand(string commandId)
    {
        if (string.IsNullOrWhiteSpace(commandId))
            return false;

        return _commands.ContainsKey(commandId);
    }

    /// <inheritdoc/>
    public bool TryExecute(string commandId, object? parameter = null)
    {
        // LOGIC: Look up command
        if (!_commands.TryGetValue(commandId, out var command))
        {
            _logger.LogWarning("Command not found: {CommandId}", commandId);
            return false;
        }

        // LOGIC: Check CanExecute predicate
        if (command.CanExecute is not null)
        {
            try
            {
                if (!command.CanExecute())
                {
                    _logger.LogDebug("Command '{CommandId}' cannot execute (CanExecute returned false)", commandId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CanExecute threw exception for command '{CommandId}'", commandId);
                return false;
            }
        }

        // LOGIC: Execute with timing
        var stopwatch = Stopwatch.StartNew();
        Exception? executionException = null;
        var success = false;

        try
        {
            command.Execute(parameter);
            success = true;
            _logger.LogDebug("Executed command: {CommandId} in {ElapsedMs}ms", commandId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            executionException = ex;
            _logger.LogError(ex, "Command '{CommandId}' threw exception during execution", commandId);
        }

        stopwatch.Stop();

        // LOGIC: Raise .NET event
        var eventArgs = new CommandExecutedEventArgs
        {
            CommandId = commandId,
            CommandTitle = command.Title,
            Parameter = parameter,
            Success = success,
            Exception = executionException,
            Duration = stopwatch.Elapsed
        };
        CommandExecuted?.Invoke(this, eventArgs);

        // LOGIC: Publish MediatR event (default to Programmatic source since we don't have source info)
        PublishEventAsync(new CommandExecutedMediatREvent(
            commandId,
            command.Title,
            CommandSource.Programmatic,
            stopwatch.Elapsed.TotalMilliseconds,
            success));

        return success;
    }

    /// <inheritdoc/>
    public bool CanExecute(string commandId)
    {
        if (!_commands.TryGetValue(commandId, out var command))
            return false;

        if (command.CanExecute is null)
            return true;

        try
        {
            return command.CanExecute();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CanExecute threw exception for command '{CommandId}'", commandId);
            return false;
        }
    }

    /// <summary>
    /// Fire-and-forget event publishing with exception handling.
    /// </summary>
    private void PublishEventAsync<TEvent>(TEvent @event) where TEvent : INotification
    {
        // LOGIC: Don't await - fire and forget
        // Exceptions are caught and logged to prevent disruption
        _ = Task.Run(async () =>
        {
            try
            {
                await _mediator.Publish(@event);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish {EventType}", typeof(TEvent).Name);
            }
        });
    }
}
