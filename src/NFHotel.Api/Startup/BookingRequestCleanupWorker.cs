using NFHotel.Application.UseCases.Bookings;

namespace NFHotel.Api.Startup;

/// <summary>
/// Runs the clean-up of the booking requests in the background of the API (MIL-088, ADR-0052 decision 7): once at the
/// start, so a request that lapsed while the API was down goes at once, and then every <see cref="Interval"/>. Each run
/// has a scope of its own. A run that fails is logged and the next run tries again; it never stops the API.
/// </summary>
public sealed partial class BookingRequestCleanupWorker : BackgroundService
{
    #region Fields

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<BookingRequestCleanupWorker> _logger;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the worker.</summary>
    /// <param name="scopes">Makes the scope of a run.</param>
    /// <param name="logger">The log.</param>
    public BookingRequestCleanupWorker(IServiceScopeFactory scopes, ILogger<BookingRequestCleanupWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(scopes);
        ArgumentNullException.ThrowIfNull(logger);

        _scopes = scopes;
        _logger = logger;
    }

    #endregion Constructors

    #region Properties

    /// <summary>The time between two runs (ADR-0052 decision 7).</summary>
    public static TimeSpan Interval { get; } = TimeSpan.FromMinutes(10);

    #endregion Properties

    #region Public methods

    /// <summary>Runs the clean-up once in a scope of its own; a failure is logged, not thrown.</summary>
    /// <param name="stoppingToken">Stops the run when the API stops.</param>
    /// <returns>A task that completes when the run is over.</returns>
    /// <exception cref="OperationCanceledException">The API is stopping.</exception>
    public async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using AsyncServiceScope scope = _scopes.CreateAsyncScope();
            IBookingRequestCleanup cleanup = scope.ServiceProvider.GetRequiredService<IBookingRequestCleanup>();
            (int requests, int mails) = await cleanup.RunAsync(stoppingToken).ConfigureAwait(false);
            LogCleaned(_logger, requests, mails);
        }
#pragma warning disable CA1031 // A background worker has no caller to throw to: the failure is logged and the next run tries again.
        catch (Exception exception) when (exception is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
#pragma warning restore CA1031
        {
            // The stop of the API fails the filter and ends the worker; any other failure, a timeout too, is logged.
            LogFailed(_logger, exception);
        }
    }

    #endregion Public methods

    #region Protected methods

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(Interval);
        try
        {
            do
            {
                await RunOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The API is stopping; the next start runs the clean-up again.
        }
    }

    #endregion Protected methods

    #region Private methods

    [LoggerMessage(EventId = 8701, Level = LogLevel.Debug,
        Message = "The clean-up deleted {Requests} lapsed booking requests and {Mails} old records of mails.")]
    private static partial void LogCleaned(ILogger logger, int requests, int mails);

    [LoggerMessage(EventId = 8702, Level = LogLevel.Error,
        Message = "The clean-up of the booking requests failed; the next run tries again.")]
    private static partial void LogFailed(ILogger logger, Exception exception);

    #endregion Private methods
}
