using NFHotel.Application.Ports;
using NFHotel.Infrastructure.Config;

namespace NFHotel.Infrastructure.Security;

/// <summary>
/// The counts per caller address of the searches for free rooms and of the public booking requests, in the memory of
/// the API (MIL-087, ADR-0051 decisions 2 and 5): one bounded store for the minute of the searches and one each for the
/// hour and the day of the booking requests. A restart clears them.
/// </summary>
/// <remarks>
/// One lock guards the three stores, so the check of the limits of a request and its count are one step: requests sent
/// at the same time cannot all pass the check before any of them is counted, and a booking request that one window
/// refuses is counted in neither. Each window is fixed and starts at the first request of its address. The stores drop
/// windows as <see cref="BoundedWindowStore{TKey}"/> says: a window below its limit before one at it, so a flood of new
/// addresses cannot free an address whose count is used up while the store has another window to drop; and when every
/// window of a full store is at its limit, the window that runs out first, so senders with more addresses than a store
/// holds cannot make the limits refuse every other address (review RC-428). The limit per email is not here: it is
/// counted in the database, and its number is <see cref="ConfiguredPublicBookingEmailLimit"/>.
/// </remarks>
public sealed class InMemorySenderLimits : ISenderLimits
{
    #region Fields

    private readonly Lock _lock = new();
    private readonly BoundedWindowStore<string> _searches;
    private readonly BoundedWindowStore<string> _bookingHours;
    private readonly BoundedWindowStore<string> _bookingDays;
    private readonly IClock _clock;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the limits.</summary>
    /// <param name="options">The numbers of the limits and the size of each store.</param>
    /// <param name="clock">The source of the time, so tests can fix it.</param>
    /// <exception cref="ArgumentOutOfRangeException">A setting is below one, or a limit is above <see cref="BoundedWindowStore{TKey}.MaxLimit"/>.</exception>
    public InMemorySenderLimits(SenderLimitOptions options, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.SearchesPerMinute, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.BookingRequestsPerHour, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.BookingRequestsPerDay, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxTrackedKeys, 1);

        _searches = new BoundedWindowStore<string>(SearchWindow, options.MaxTrackedKeys, options.SearchesPerMinute, StringComparer.Ordinal, dropsUsedUpWindowsWhenFull: true);
        _bookingHours = new BoundedWindowStore<string>(BookingHourWindow, options.MaxTrackedKeys, options.BookingRequestsPerHour, StringComparer.Ordinal, dropsUsedUpWindowsWhenFull: true);
        _bookingDays = new BoundedWindowStore<string>(BookingDayWindow, options.MaxTrackedKeys, options.BookingRequestsPerDay, StringComparer.Ordinal, dropsUsedUpWindowsWhenFull: true);
        _clock = clock;
    }

    #endregion Constructors

    #region Properties

    /// <summary>The window of the limit <see cref="LimitNames.Search"/>.</summary>
    public static TimeSpan SearchWindow { get; } = TimeSpan.FromMinutes(1);

    /// <summary>The window of the limit <see cref="LimitNames.BookingHour"/>.</summary>
    public static TimeSpan BookingHourWindow { get; } = TimeSpan.FromHours(1);

    /// <summary>The window of the limit <see cref="LimitNames.BookingDay"/>.</summary>
    public static TimeSpan BookingDayWindow { get; } = TimeSpan.FromDays(1);

    /// <summary>The number of addresses with a window of searches. For tests and diagnostics: the store stays within its bound.</summary>
    public int TrackedSearchAddressCount => CountOf(_searches);

    /// <summary>The number of addresses with a window of an hour of booking requests, within the bound like the others.</summary>
    public int TrackedBookingHourAddressCount => CountOf(_bookingHours);

    /// <summary>The number of addresses with a window of a day of booking requests, within the bound like the others.</summary>
    public int TrackedBookingDayAddressCount => CountOf(_bookingDays);

    #endregion Properties

    #region Public methods

    /// <inheritdoc />
    public SenderLimitDecision TryCountSearch(string addressKey)
    {
        ArgumentNullException.ThrowIfNull(addressKey);

        DateTimeOffset now = _clock.UtcNow;
        lock (_lock)
        {
            if (_searches.Refuses(addressKey, now))
            {
                return SenderLimitDecision.Refused(LimitNames.Search, _searches.RetryAfter(addressKey, now));
            }

            _searches.Record(addressKey, now);
            return SenderLimitDecision.Allowed;
        }
    }

    /// <inheritdoc />
    public SenderLimitDecision TryCountBookingRequest(string addressKey)
    {
        ArgumentNullException.ThrowIfNull(addressKey);

        DateTimeOffset now = _clock.UtcNow;
        lock (_lock)
        {
            TimeSpan hourWait = _bookingHours.Refuses(addressKey, now) ? _bookingHours.RetryAfter(addressKey, now) : TimeSpan.Zero;
            TimeSpan dayWait = _bookingDays.Refuses(addressKey, now) ? _bookingDays.RetryAfter(addressKey, now) : TimeSpan.Zero;
            if (hourWait > TimeSpan.Zero || dayWait > TimeSpan.Zero)
            {
                // A request that both windows refuse waits for both, so the answer names the one with the longer wait.
                return dayWait >= hourWait
                    ? SenderLimitDecision.Refused(LimitNames.BookingDay, dayWait)
                    : SenderLimitDecision.Refused(LimitNames.BookingHour, hourWait);
            }

            _bookingHours.Record(addressKey, now);
            _bookingDays.Record(addressKey, now);
            return SenderLimitDecision.Allowed;
        }
    }

    /// <inheritdoc />
    public void GiveBackBookingRequest(string addressKey)
    {
        ArgumentNullException.ThrowIfNull(addressKey);

        DateTimeOffset now = _clock.UtcNow;
        lock (_lock)
        {
            _bookingHours.GiveBack(addressKey, now);
            _bookingDays.GiveBack(addressKey, now);
        }
    }

    #endregion Public methods

    #region Private methods

    private int CountOf(BoundedWindowStore<string> store)
    {
        lock (_lock)
        {
            return store.Count;
        }
    }

    #endregion Private methods
}
