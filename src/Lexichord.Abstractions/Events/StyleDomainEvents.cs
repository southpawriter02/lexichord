using Lexichord.Abstractions.Contracts;
using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Published when a style sheet is successfully reloaded from file.
/// </summary>
/// <remarks>
/// LOGIC: Enables consumers to react to live style sheet updates.
/// The watcher publishes this after successful YAML parsing and engine update.
///
/// Version: v0.2.1d
/// </remarks>
/// <param name="FilePath">Path to the reloaded style configuration file.</param>
/// <param name="NewStyleSheet">The newly loaded style sheet.</param>
/// <param name="PreviousStyleSheet">The style sheet that was replaced.</param>
/// <param name="ReloadSource">Indicates what triggered the reload.</param>
public record StyleSheetReloadedEvent(
    string FilePath,
    StyleSheet NewStyleSheet,
    StyleSheet PreviousStyleSheet,
    StyleReloadSource ReloadSource
) : DomainEventBase, INotification;

/// <summary>
/// Published when the style configuration watcher encounters an error.
/// </summary>
/// <remarks>
/// LOGIC: Enables error logging and user notification for watcher failures.
/// Published on YAML parsing errors, file access errors, or watcher failures.
///
/// Version: v0.2.1d
/// </remarks>
/// <param name="FilePath">Path to the file that caused the error (if applicable).</param>
/// <param name="ErrorMessage">Human-readable error description.</param>
/// <param name="Exception">The underlying exception (if available).</param>
/// <param name="ErrorType">Classification of the error for handling.</param>
public record StyleWatcherErrorEvent(
    string? FilePath,
    string ErrorMessage,
    Exception? Exception,
    StyleWatcherErrorType ErrorType
) : DomainEventBase, INotification;

/// <summary>
/// Published when the terminology lexicon is modified (create, update, delete, reactivate).
/// </summary>
/// <remarks>
/// LOGIC: Enables cache invalidation and UI updates when terms change.
/// Handler failures are logged but do not fail the main operation.
///
/// Version: v0.2.2d
/// </remarks>
/// <param name="ChangeType">The type of change that occurred.</param>
/// <param name="TermId">The unique identifier of the affected term.</param>
/// <param name="TermPattern">The pattern of the affected term (for logging).</param>
/// <param name="Category">The category of the affected term.</param>
/// <param name="AffectedCount">Number of items affected (usually 1).</param>
public sealed record LexiconChangedEvent(
    LexiconChangeType ChangeType,
    Guid TermId,
    string? TermPattern = null,
    string? Category = null,
    int AffectedCount = 1
) : DomainEventBase, INotification;

/// <summary>
/// Indicates what triggered a style sheet reload.
/// </summary>
public enum StyleReloadSource
{
    /// <summary>File was created (new file detected).</summary>
    FileCreated,

    /// <summary>File was modified (content changed).</summary>
    FileModified,

    /// <summary>File was renamed (moved into watched path).</summary>
    FileRenamed,

    /// <summary>Manual reload requested via ForceReloadAsync.</summary>
    ManualReload
}

/// <summary>
/// Classifies style watcher errors for handling.
/// </summary>
public enum StyleWatcherErrorType
{
    /// <summary>YAML syntax error in the configuration file.</summary>
    YamlParseError,

    /// <summary>Schema validation failure (invalid structure).</summary>
    SchemaValidationError,

    /// <summary>File not found or access denied.</summary>
    FileAccessError,

    /// <summary>FileSystemWatcher internal error (buffer overflow, etc.).</summary>
    WatcherInternalError,

    /// <summary>Unknown or unclassified error.</summary>
    Unknown
}
