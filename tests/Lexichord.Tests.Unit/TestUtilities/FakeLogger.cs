using Microsoft.Extensions.Logging;

namespace Lexichord.Tests.Unit.TestUtilities;

/// <summary>
/// A fake logger that captures log entries for test assertions.
/// </summary>
/// <typeparam name="T">The type being logged.</typeparam>
public class FakeLogger<T> : ILogger<T>
{
    private readonly List<LogEntry> _logs = new();

    /// <summary>
    /// Gets the captured log entries.
    /// </summary>
    public IReadOnlyList<LogEntry> Logs => _logs.AsReadOnly();

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        _logs.Add(new LogEntry(logLevel, message, exception));
    }

    /// <summary>
    /// Clears all captured log entries.
    /// </summary>
    public void Clear() => _logs.Clear();

    /// <summary>
    /// Represents a captured log entry.
    /// </summary>
    public record LogEntry(LogLevel Level, string Message, Exception? Exception);
}
