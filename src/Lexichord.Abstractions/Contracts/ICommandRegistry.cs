namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Central registry for all application commands.
/// </summary>
/// <remarks>
/// LOGIC: The CommandRegistry is a singleton service that:
///
/// 1. **Stores Commands**: Maintains a dictionary of CommandDefinitions
///    keyed by their unique ID.
///
/// 2. **Provides Lookup**: Allows retrieval of commands by ID, category,
///    or as a complete list.
///
/// 3. **Handles Execution**: Executes commands with parameter passing,
///    CanExecute checking, and error handling.
///
/// 4. **Publishes Events**: Notifies subscribers when commands are
///    registered, unregistered, or executed.
///
/// Registration Timing:
/// - Built-in commands registered during Host startup
/// - Module commands registered during IModule.InitializeAsync()
/// - Dynamic commands can be registered/unregistered at runtime
///
/// Thread Safety:
/// - Uses ConcurrentDictionary for thread-safe access
/// - Event handlers may be invoked on any thread
/// </remarks>
public interface ICommandRegistry
{
    /// <summary>
    /// Registers a new command with the registry.
    /// </summary>
    /// <param name="command">The command definition to register.</param>
    /// <exception cref="ArgumentNullException">Thrown if command is null.</exception>
    /// <exception cref="ArgumentException">Thrown if command ID is empty or duplicate.</exception>
    /// <remarks>
    /// LOGIC: Registration flow:
    /// 1. Validate command is not null
    /// 2. Validate command.Id is not empty
    /// 3. Check for duplicate ID (throws if exists)
    /// 4. Add to internal dictionary
    /// 5. Raise CommandRegistered event
    /// 6. Publish CommandRegisteredEvent to MediatR
    /// </remarks>
    void Register(CommandDefinition command);

    /// <summary>
    /// Registers multiple commands at once.
    /// </summary>
    /// <param name="commands">The commands to register.</param>
    /// <exception cref="ArgumentNullException">Thrown if commands is null.</exception>
    /// <exception cref="ArgumentException">Thrown if any command fails validation.</exception>
    /// <remarks>
    /// LOGIC: Convenience method for registering multiple commands.
    /// All-or-nothing: if any command fails, none are registered.
    /// </remarks>
    void RegisterRange(IEnumerable<CommandDefinition> commands);

    /// <summary>
    /// Removes a command from the registry.
    /// </summary>
    /// <param name="commandId">The ID of the command to remove.</param>
    /// <returns>True if command was found and removed, false otherwise.</returns>
    /// <remarks>
    /// LOGIC: Used for dynamic command management.
    /// Raises CommandUnregistered event on success.
    /// </remarks>
    bool Unregister(string commandId);

    /// <summary>
    /// Gets all registered commands.
    /// </summary>
    /// <returns>Read-only list of all commands, ordered by category then title.</returns>
    /// <remarks>
    /// LOGIC: Returns a snapshot of current commands.
    /// Safe to enumerate while other operations occur.
    /// </remarks>
    IReadOnlyList<CommandDefinition> GetAllCommands();

    /// <summary>
    /// Gets commands filtered by category.
    /// </summary>
    /// <param name="category">The category to filter by (case-insensitive).</param>
    /// <returns>Commands in the specified category, ordered by title.</returns>
    IReadOnlyList<CommandDefinition> GetCommandsByCategory(string category);

    /// <summary>
    /// Gets all unique categories.
    /// </summary>
    /// <returns>List of category names, alphabetically sorted.</returns>
    IReadOnlyList<string> GetCategories();

    /// <summary>
    /// Gets a specific command by ID.
    /// </summary>
    /// <param name="commandId">The command ID to look up.</param>
    /// <returns>The command, or null if not found.</returns>
    CommandDefinition? GetCommand(string commandId);

    /// <summary>
    /// Checks if a command with the given ID exists.
    /// </summary>
    /// <param name="commandId">The command ID to check.</param>
    /// <returns>True if command exists.</returns>
    bool HasCommand(string commandId);

    /// <summary>
    /// Attempts to execute a command by ID.
    /// </summary>
    /// <param name="commandId">The command ID to execute.</param>
    /// <param name="parameter">Optional parameter to pass to the command.</param>
    /// <returns>True if command was found and executed successfully.</returns>
    /// <remarks>
    /// LOGIC: Execution flow:
    /// 1. Look up command by ID
    /// 2. If not found, log warning and return false
    /// 3. Check CanExecute (if defined)
    /// 4. If CanExecute returns false, return false
    /// 5. Start timing
    /// 6. Execute command (wrapped in try/catch)
    /// 7. Stop timing
    /// 8. Raise CommandExecuted event
    /// 9. Publish CommandExecutedEvent to MediatR
    /// 10. Return true (or false if exception thrown)
    ///
    /// Exception Handling:
    /// - Exceptions from Execute are caught and logged
    /// - Exception stored in CommandExecutedEventArgs
    /// - Method returns false on exception
    /// </remarks>
    bool TryExecute(string commandId, object? parameter = null);

    /// <summary>
    /// Checks if a command can currently execute.
    /// </summary>
    /// <param name="commandId">The command ID to check.</param>
    /// <returns>True if command exists and can execute.</returns>
    /// <remarks>
    /// LOGIC: Returns false if:
    /// - Command doesn't exist
    /// - Command.CanExecute returns false
    /// </remarks>
    bool CanExecute(string commandId);

    /// <summary>
    /// Gets the total count of registered commands.
    /// </summary>
    int CommandCount { get; }

    /// <summary>
    /// Event raised when a command is registered.
    /// </summary>
    event EventHandler<CommandRegisteredEventArgs>? CommandRegistered;

    /// <summary>
    /// Event raised when a command is unregistered.
    /// </summary>
    event EventHandler<CommandUnregisteredEventArgs>? CommandUnregistered;

    /// <summary>
    /// Event raised when a command is executed.
    /// </summary>
    event EventHandler<CommandExecutedEventArgs>? CommandExecuted;
}

/// <summary>
/// Event args for command registration.
/// </summary>
public class CommandRegisteredEventArgs : EventArgs
{
    /// <summary>
    /// Gets the registered command.
    /// </summary>
    public required CommandDefinition Command { get; init; }
}

/// <summary>
/// Event args for command unregistration.
/// </summary>
public class CommandUnregisteredEventArgs : EventArgs
{
    /// <summary>
    /// Gets the ID of the unregistered command.
    /// </summary>
    public required string CommandId { get; init; }
}

/// <summary>
/// Event args for command execution.
/// </summary>
public class CommandExecutedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the executed command ID.
    /// </summary>
    public required string CommandId { get; init; }

    /// <summary>
    /// Gets the command title.
    /// </summary>
    public required string CommandTitle { get; init; }

    /// <summary>
    /// Gets the parameter passed to the command.
    /// </summary>
    public object? Parameter { get; init; }

    /// <summary>
    /// Gets whether execution succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the exception if execution failed.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets the execution duration.
    /// </summary>
    public required TimeSpan Duration { get; init; }
}
