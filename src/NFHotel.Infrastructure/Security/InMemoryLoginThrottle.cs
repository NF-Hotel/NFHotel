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
    private readonly ConcurrentDictionary<string, Window> _windows = new(StringComparer.Ordinal);
    private readonly IClock _clock;
    private readonly int _maxFailures;
    private readonly TimeSpan _window;

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
    }

    /// <inheritdoc />
    public void Reset(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        _windows.TryRemove(key, out _);
    }

    private sealed record Window(DateTimeOffset Start, int Failures);
}
