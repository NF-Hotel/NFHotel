using NFHotel.Application.Errors;
using NFHotel.Application.UseCases.MarketAnalysis;

namespace NFHotel.Api.Startup;

/// <summary>
/// Runs the market analysis in the background of the API (MIL-026). The call to the markets API takes minutes, so a start must
/// not wait for it: the worker takes a run from the queue, gives it a scope of its own, because the scope of the request ended
/// with the response, and runs it. Every outcome, a failure included, is written to the <see cref="MarketAnalysisRunTracker"/>
/// that the page reads.
/// </summary>
public sealed class MarketAnalysisRunWorker : BackgroundService
{
    private readonly ChannelMarketAnalysisRunLauncher _launcher;
    private readonly IServiceScopeFactory _scopes;
    private readonly MarketAnalysisRunTracker _tracker;
    private readonly ILogger<MarketAnalysisRunWorker> _logger;

    /// <summary>Creates the worker.</summary>
    /// <param name="launcher">The queue of runs.</param>
    /// <param name="scopes">Makes the scope of a run.</param>
    /// <param name="tracker">The state of the run.</param>
    /// <param name="logger">The log.</param>
    public MarketAnalysisRunWorker(
        ChannelMarketAnalysisRunLauncher launcher,
        IServiceScopeFactory scopes,
        MarketAnalysisRunTracker tracker,
        ILogger<MarketAnalysisRunWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(launcher);
        ArgumentNullException.ThrowIfNull(scopes);
        ArgumentNullException.ThrowIfNull(tracker);
        ArgumentNullException.ThrowIfNull(logger);

        _launcher = launcher;
        _scopes = scopes;
        _tracker = tracker;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var runId in _launcher.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                await RunAsync(runId, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // The API is stopping: a run that was going on is marked failed by RunAsync, and nothing else is waiting.
        }
    }

    private async Task RunAsync(Guid runId, CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var runner = scope.ServiceProvider.GetRequiredService<MarketAnalysisRunner>();
            await runner.ExecuteAsync(runId, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("The market analysis run {RunId} was cancelled because the API is stopping.", runId);
            _tracker.MarkFailed(runId, ErrorCodes.MarketAnalysisRunner.Unavailable);
        }
#pragma warning disable CA1031 // A background worker has no caller to throw to: the failure is logged and written to the run.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            _logger.LogError(exception, "The market analysis run {RunId} failed.", runId);
            _tracker.MarkFailed(runId, ErrorCodes.MarketAnalysisRunner.UnexpectedResponse);
        }
    }
}
