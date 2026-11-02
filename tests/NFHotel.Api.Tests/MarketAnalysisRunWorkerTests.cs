using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NFHotel.Api.Startup;
using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.MarketAnalysis;
using NFHotel.TestSupport;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace NFHotel.Api.Tests;

/// <summary>The background worker that runs a market analysis taken from the queue (MIL-026, MIL-054 task 8).</summary>
public sealed class MarketAnalysisRunWorkerTests
{
    private readonly ServiceWorld _world = new();
    private readonly ChannelMarketAnalysisRunLauncher _launcher = new();
    private SignallingScopes _scopes = null!;

    private MarketAnalysisRunWorker Worker(IMarketAnalysisBookingQueries? bookings = null)
    {
        var services = new ServiceCollection()
            .AddScoped(_ => new MarketAnalysisRunner(
                bookings ?? _world.MarketAnalysisBookings,
                _world.MarketAnalysisClient,
                _world.MarketAnalysisRuns,
                _world.UnitOfWork,
                _world.MarketAnalysisTracker,
                _world.Clock))
            .BuildServiceProvider();
        _scopes = new SignallingScopes(services.GetRequiredService<IServiceScopeFactory>());

        return new MarketAnalysisRunWorker(_launcher, _scopes, _world.MarketAnalysisTracker, NullLogger<MarketAnalysisRunWorker>.Instance);
    }

    /// <summary>
    /// Queues a run and waits until the scope of the run has ended, so the worker has taken it. A stop then waits for the
    /// rest of the run, the catch included, before it returns; no test waits on time.
    /// </summary>
    private async Task<MarketAnalysisRunStateDto> RunAsync(MarketAnalysisRunWorker worker)
    {
        var runId = _world.MarketAnalysisTracker.TryBegin().Value.RunId;
        await worker.StartAsync(TestContext.Current.CancellationToken);

        _launcher.Launch(runId);
        await _scopes.Ended.Task.WaitAsync(TestContext.Current.CancellationToken);
        await worker.StopAsync(TestContext.Current.CancellationToken);

        return _world.MarketAnalysisTracker.Get(runId)!;
    }

    /// <summary>Makes the scopes of the worker and tells when the first one ended.</summary>
    private sealed class SignallingScopes(IServiceScopeFactory inner) : IServiceScopeFactory
    {
        public TaskCompletionSource Ended { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public IServiceScope CreateScope() => new SignallingScope(inner.CreateScope(), Ended);
    }

    private sealed class SignallingScope(IServiceScope inner, TaskCompletionSource ended) : IServiceScope, IAsyncDisposable
    {
        public IServiceProvider ServiceProvider => inner.ServiceProvider;

        public void Dispose()
        {
            inner.Dispose();
            ended.TrySetResult();
        }

        public async ValueTask DisposeAsync()
        {
            await ((IAsyncDisposable)inner).DisposeAsync();
            ended.TrySetResult();
        }
    }

    /// <summary>Positive: a launched run is executed in a scope of its own and completes.</summary>
    [Fact]
    public async Task Launch_WorkingRun_Completes()
    {
        using var worker = Worker();

        var state = await RunAsync(worker);

        Assert.Equal(MarketAnalysisRunStates.Completed, state.State);
        Assert.Equal(1, _world.MarketAnalysisClient.Calls);
    }

    /// <summary>Error handling: an unexpected exception is caught and written to the run as a failure.</summary>
    [Fact]
    public async Task Launch_RunThrows_IsMarkedFailed()
    {
        var bookings = Substitute.For<IMarketAnalysisBookingQueries>();
        bookings.GetBookingsAsync(Arg.Any<DateOnly?>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("the database is gone"));
        using var worker = Worker(bookings);

        var state = await RunAsync(worker);

        Assert.Equal(MarketAnalysisRunStates.Failed, state.State);
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.UnexpectedResponse, state.Error);
    }

    /// <summary>Asynchronous behaviour: a run that is cancelled is written as unavailable, not as an unexpected failure.</summary>
    [Fact]
    public async Task Launch_RunCancelled_IsMarkedUnavailable()
    {
        var bookings = Substitute.For<IMarketAnalysisBookingQueries>();
        bookings.GetBookingsAsync(Arg.Any<DateOnly?>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());
        using var worker = Worker(bookings);

        var state = await RunAsync(worker);

        Assert.Equal(ErrorCodes.MarketAnalysisRunner.Unavailable, state.Error);
    }

    /// <summary>Asynchronous behaviour: a worker with an empty queue stops cleanly when the API stops.</summary>
    [Fact]
    public async Task Stop_EmptyQueue_EndsWithoutError()
    {
        using var worker = Worker();
        await worker.StartAsync(TestContext.Current.CancellationToken);

        await worker.StopAsync(TestContext.Current.CancellationToken);

        // The loop starts on a thread of its own, so a stop that comes first ends it cancelled: either way it ends without a fault.
        Assert.True(worker.ExecuteTask!.IsCompleted);
        Assert.False(worker.ExecuteTask.IsFaulted);
    }
}
