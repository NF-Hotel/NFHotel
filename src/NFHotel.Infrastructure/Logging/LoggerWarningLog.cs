using Microsoft.Extensions.Logging;

using NFHotel.Application.Ports;

namespace NFHotel.Infrastructure.Logging;

/// <summary>Writes the warnings of the Application layer to the host's <see cref="ILogger"/>.</summary>
public sealed partial class LoggerWarningLog : IWarningLog
{
    #region Fields

    private readonly ILogger<LoggerWarningLog> _logger;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the log.</summary>
    /// <param name="logger">The logger of the host.</param>
    public LoggerWarningLog(ILogger<LoggerWarningLog> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    #endregion Constructors

    #region Public methods

    /// <inheritdoc />
    public void Warn(string message) => Warning(_logger, message);

    #endregion Public methods

    #region Private methods

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Message}")]
    private static partial void Warning(ILogger logger, string message);

    #endregion Private methods
}
