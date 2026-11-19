using NFHotel.Application.Ports;
using NFHotel.Infrastructure.Config;

namespace NFHotel.Infrastructure.Security;

/// <summary>
/// The limits on the tries of the guest sign-in, in memory (ADR-0049 decision 6): failed tries per reference and per
/// caller address, each in a fixed window that starts at the first try, in two stores of a bounded size, apart from the
/// throttle of the staff sign-in.
/// </summary>
/// <remarks>
/// One lock guards both stores, so the check of both limits and the count in both are one step: tries sent at the same
/// time cannot pass the check before another is counted, and a try that one limit refuses is counted in neither store.
/// Each store drops its windows as <see cref="BoundedWindowStore{TKey}"/> says: when every window is at its limit, the
/// try of a new key is refused and counted in neither store, so a flood of new keys cannot free a key whose tries are
/// used up (review RC-418). A restart of the API clears both stores.
/// </remarks>
public sealed class InMemoryGuestSignInLimits : IGuestSignInLimits
{
    #region Fields

    private readonly Lock _lock = new();
    private readonly BoundedWindowStore<Guid> _references;
    private readonly BoundedWindowStore<string> _addresses;
    private readonly IClock _clock;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the limits.</summary>
    /// <param name="options">The limits, the length of the window and the size of each store.</param>
    /// <param name="clock">The source of the time, so tests can fix it.</param>
    /// <exception cref="ArgumentOutOfRangeException">A setting is below one.</exception>
    public InMemoryGuestSignInLimits(GuestLookupOptions options, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxFailuresPerReference, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxFailuresPerAddress, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.WindowMinutes, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxTrackedKeys, 1);

        TimeSpan window = TimeSpan.FromMinutes(options.WindowMinutes);
        _references = new BoundedWindowStore<Guid>(window, options.MaxTrackedKeys, options.MaxFailuresPerReference, EqualityComparer<Guid>.Default);
        _addresses = new BoundedWindowStore<string>(window, options.MaxTrackedKeys, options.MaxFailuresPerAddress, StringComparer.Ordinal);
        _clock = clock;
    }

    #endregion Constructors

    #region Properties

    /// <summary>The number of references that have a window. For tests and diagnostics: the store stays within its bound.</summary>
    public int TrackedReferenceCount
    {
        get
        {
            lock (_lock)
            {
                return _references.Count;
            }
        }
    }

    /// <summary>The number of addresses that have a window. For tests and diagnostics: the store stays within its bound.</summary>
    public int TrackedAddressCount
    {
        get
        {
            lock (_lock)
            {
                return _addresses.Count;
            }
        }
    }

    #endregion Properties

    #region Public methods

    /// <inheritdoc />
    public bool TryCount(Guid? reference, string address)
    {
        ArgumentNullException.ThrowIfNull(address);

        DateTimeOffset now = _clock.UtcNow;
        lock (_lock)
        {
            // A refused try leaves both windows as they are: it neither counts nor extends them.
            if (_addresses.Refuses(address, now) || (reference is Guid known && _references.Refuses(known, now)))
            {
                return false;
            }

            _addresses.Record(address, now);
            if (reference is Guid counted)
            {
                _references.Record(counted, now);
            }

            return true;
        }
    }

    /// <inheritdoc />
    public void Succeeded(Guid reference, string address)
    {
        ArgumentNullException.ThrowIfNull(address);

        DateTimeOffset now = _clock.UtcNow;
        lock (_lock)
        {
            _references.Clear(reference);
            _addresses.GiveBack(address, now);
        }
    }

    #endregion Public methods
}
