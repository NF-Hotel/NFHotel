namespace NFHotel.External.Tests.MarketAnalysis;

/// <summary>
/// A clock for the poll loop of the client: a timer fires at once and moves the clock forward by its delay, so a test that
/// follows a run for minutes of poll intervals takes no time and can see every delay that was asked for.
/// </summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    private DateTimeOffset _now = new(2026, 10, 29, 10, 0, 0, TimeSpan.Zero);

    /// <summary>The delay of each timer that was made, in order.</summary>
    public List<TimeSpan> Delays { get; } = [];

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => _now;

    /// <inheritdoc />
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        Delays.Add(dueTime);
        _now += dueTime;

        // The callback runs on the pool, as a real timer does, after the caller has the timer in hand.
        ThreadPool.QueueUserWorkItem(_ => callback(state));

        return new FiredTimer();
    }

    private sealed class FiredTimer : ITimer
    {
        public bool Change(TimeSpan dueTime, TimeSpan period) => true;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
