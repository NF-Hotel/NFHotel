using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

namespace NFHotel.Api.Tests;

/// <summary>Keeps every log entry that the API writes, so a test can read the security events (MIL-086 decision 6).</summary>
public sealed class RecordingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();

    /// <summary>The entries written so far, oldest first.</summary>
    public IReadOnlyList<LogEntry> Entries => _entries.ToArray();

    /// <summary>Forgets the entries written so far.</summary>
    public void Clear() => _entries.Clear();

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new Logger(categoryName, _entries);

    /// <inheritdoc />
    public void Dispose()
    {
        // Nothing to release.
    }

    /// <summary>One entry: its category, its level, the formatted message and the values of its template.</summary>
    /// <param name="Category">The category of the logger.</param>
    /// <param name="Level">The level.</param>
    /// <param name="Message">The formatted message.</param>
    /// <param name="Values">The named values of the message template.</param>
    public sealed record LogEntry(string Category, LogLevel Level, string Message, IReadOnlyList<KeyValuePair<string, object?>> Values)
    {
        /// <summary>The message and every value as text, to search for a value that must not be there.</summary>
        public string AllText => Message + " " + string.Join(" ", Values.Select(pair => $"{pair.Key}={pair.Value}"));
    }

    private sealed class Logger(string category, ConcurrentQueue<LogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            IReadOnlyList<KeyValuePair<string, object?>> values = state as IReadOnlyList<KeyValuePair<string, object?>> ?? [];
            entries.Enqueue(new LogEntry(category, logLevel, formatter(state, exception), values));
        }
    }
}
