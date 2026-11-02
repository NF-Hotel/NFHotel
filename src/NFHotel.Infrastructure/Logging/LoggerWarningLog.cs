using Microsoft.Extensions.Logging;

using NFHotel.Application.Ports;

namespace NFHotel.Infrastructure.Logging;

/// <summary>Writes the warnings of the Application layer to the host's <see cref="ILogger"/>.</summary>
public sealed partial class LoggerWarningLog : IWarningLog
{
    private readonly ILogger<LoggerWarningLog> _logger;

    /// <summary>Creates the log.</summary>
    /// <param name="logger">The logger of the host.</param>
    public LoggerWarningLog(ILogger<LoggerWarningLog> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    /// <inheritdoc />
    public void Warn(string message) => Warning(_logger, message);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Message}")]
    private static partial void Warning(ILogger logger, string message);
}
