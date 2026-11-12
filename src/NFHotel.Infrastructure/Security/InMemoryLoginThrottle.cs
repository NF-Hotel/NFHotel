using System.Collections.Concurrent;

using NFHotel.Application.Ports;
using NFHotel.Infrastructure.Config;

namespace NFHotel.Infrastructure.Security;

/// <summary>
/// Counts failed sign-ins per user name in a fixed window (ADR-0005). The count lives in memory, so a restart of the
/// API clears it. The window starts at the first failure and does not slide, so a refused attempt does not extend it.
/// </summary>
public sealed class InMemoryLoginThrottle : ILoginThrottle
{
    /// <summary>How many failures are recorded between two sweeps of the windows that have run out.</summary>
    private const int FailuresPerSweep = 256;

    private readonly ConcurrentDictionary<string, Window> _windows = new(StringComparer.Ordinal);
    private readonly IClock _clock;
    private readonly int _maxFailures;
    private readonly TimeSpan _window;
    private int _failuresSinceStart;

    /// <summary>Creates the throttle.</summary>
    /// <param name="options">The number of failures and the length of the window.</param>
    /// <param name="clock">The source of the time, so tests can fix it.</param>
    public InMemoryLoginThrottle(StaffSecurityOptions options, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clock);

        _maxFailures = options.MaxFailures;
        _window = TimeSpan.FromMinutes(options.ThrottleWindowMinutes);
        _clock = clock;
    }

    /// <summary>
    /// The number of keys that have a window, run-out windows included until the next sweep. For tests and diagnostics:
    /// it shows that keys which fail once and are never tried again do not stay in memory (MIL-079).
    /// </summary>
    public int TrackedKeyCount => _windows.Count;

    /// <inheritdoc />
    public bool IsBlocked(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!_windows.TryGetValue(key, out var window))
        {
            return false;
        }

        if (_clock.UtcNow - window.Start >= _window)
        {
            _windows.TryRemove(new KeyValuePair<string, Window>(key, window));
            return false;
        }

        return window.Failures >= _maxFailures;
    }

    /// <inheritdoc />
    public void RecordFailure(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var now = _clock.UtcNow;
        _windows.AddOrUpdate(
            key,
            _ => new Window(now, 1),
            (_, current) => now - current.Start >= _window ? new Window(now, 1) : current with { Failures = current.Failures + 1 });

        // A key is otherwise dropped only when it is checked again after its window. A sign-in tries few names, but a
        // guest's lookup can be tried with any booking number (MIL-079), so the windows that have run out are swept now
        // and then, and the memory stays bounded by the failures of one window.
        if (Interlocked.Increment(ref _failuresSinceStart) % FailuresPerSweep == 0)
        {
            SweepRunOutWindows(now);
        }
    }

    /// <inheritdoc />
    public void Reset(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        _windows.TryRemove(key, out _);
    }

    /// <summary>Drops every window that has run out.</summary>
    /// <param name="now">The time of the failure that started the sweep.</param>
    private void SweepRunOutWindows(DateTimeOffset now)
    {
        foreach (KeyValuePair<string, Window> runOut in _windows.Where(entry => now - entry.Value.Start >= _window))
        {
            // Removes the pair only while it is unchanged, so a failure recorded meanwhile is kept.
            _windows.TryRemove(runOut);
        }
    }

    private sealed record Window(DateTimeOffset Start, int Failures);
}
