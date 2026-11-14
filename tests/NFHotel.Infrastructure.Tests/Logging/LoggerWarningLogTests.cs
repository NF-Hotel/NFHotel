using Microsoft.Extensions.Logging;

using NFHotel.Infrastructure.Logging;

namespace NFHotel.Infrastructure.Tests.Logging;

/// <summary>The adapter of the warnings of the Application layer (MIL-055, RC-266 N2): one entry at the level Warning, with the text as it was given.</summary>
public sealed class LoggerWarningLogTests
{
    #region Warn

    /// <summary>Positive: a warning is written once, at the level Warning, with the text it was given.</summary>
    [Fact]
    public void Warn_Text_WritesOneWarningWithTheText()
    {
        // Arrange
        const string Text = "The price per night of booking 42 could not be stored; the booking is kept without a price. DbUpdateException";
        ListLogger logger = new();
        LoggerWarningLog log = new(logger);

        // Act
        log.Warn(Text);

        // Assert
        LogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal((LogLevel.Warning, Text), (entry.Level, entry.Text));
    }

    /// <summary>Edge case: braces in the text are written as they are; the text is a value of the entry, not its template.</summary>
    [Fact]
    public void Warn_TextWithBraces_WritesTheTextAsGiven()
    {
        // Arrange
        ListLogger logger = new();
        LoggerWarningLog log = new(logger);

        // Act
        log.Warn("Booking {42} has no {Price}");

        // Assert
        Assert.Equal("Booking {42} has no {Price}", Assert.Single(logger.Entries).Text);
    }

    /// <summary>Negative: the adapter refuses to be made without a logger.</summary>
    [Fact]
    public void Constructor_NoLogger_ThrowsArgumentNullException()
    {
        // Act and assert: the null-forgiving operator passes the null that the constructor must refuse.
        Assert.Throws<ArgumentNullException>(() => new LoggerWarningLog(null!));
    }

    #endregion

    #region Helpers

    /// <summary>One entry of the log: its level and its formatted text.</summary>
    private sealed record LogEntry(LogLevel Level, string Text);

    /// <summary>A log that keeps the level and the formatted text of each entry.</summary>
    private sealed class ListLogger : ILogger<LoggerWarningLog>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
    }

    #endregion
}
