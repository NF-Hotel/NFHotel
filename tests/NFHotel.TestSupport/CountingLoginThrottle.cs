using NFHotel.Application.Ports;

namespace NFHotel.TestSupport;

/// <summary>
/// A throttle without a clock for tests that start many attempts at once: a key is refused after the given number of
/// failures until it is reset. Each call is one locked step, as the check and the count of the real throttle are.
/// </summary>
public sealed class CountingLoginThrottle : ILoginThrottle
{
    #region Fields

    private readonly Dictionary<string, int> _failures = new(StringComparer.Ordinal);
    private readonly object _lock = new();
    private readonly int _maxFailures;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the throttle.</summary>
    /// <param name="maxFailures">The number of failures after which a key is refused.</param>
    public CountingLoginThrottle(int maxFailures)
    {
        _maxFailures = maxFailures;
    }

    #endregion Constructors

    #region Public methods

    /// <inheritdoc />
    public bool IsBlocked(string key)
    {
        lock (_lock)
        {
            return _failures.GetValueOrDefault(key) >= _maxFailures;
        }
    }

    /// <inheritdoc />
    public void RecordFailure(string key)
    {
        lock (_lock)
        {
            _failures[key] = _failures.GetValueOrDefault(key) + 1;
        }
    }

    /// <inheritdoc />
    public bool TryCountAttempt(string key)
    {
        lock (_lock)
        {
            int failures = _failures.GetValueOrDefault(key);
            if (failures >= _maxFailures)
            {
                return false;
            }

            _failures[key] = failures + 1;
            return true;
        }
    }

    /// <inheritdoc />
    public void Reset(string key)
    {
        lock (_lock)
        {
            _failures.Remove(key);
        }
    }

    #endregion Public methods
}
