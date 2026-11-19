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
/// Each store keeps its windows in the order in which they started, and grouped by their failed tries. A new window
/// first drops the windows that have run out, which lead that order. A full store then drops the window with the fewest
/// failed tries, of those the one that reached its count first, and never a window at its limit: when every window is at
/// its limit, the try of a new key is refused and counted in neither store. So a flood of new keys cannot free a key
/// whose tries are used up (review RC-418). A restart of the API clears both stores.
/// </remarks>
public sealed class InMemoryGuestSignInLimits : IGuestSignInLimits
{
    #region Fields

    private readonly object _lock = new();
    private readonly Store<Guid> _references;
    private readonly Store<string> _addresses;
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
        _references = new Store<Guid>(window, options.MaxTrackedKeys, options.MaxFailuresPerReference, EqualityComparer<Guid>.Default);
        _addresses = new Store<string>(window, options.MaxTrackedKeys, options.MaxFailuresPerAddress, StringComparer.Ordinal);
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

            _addresses.CountFailure(address, now);
            if (reference is Guid counted)
            {
                _references.CountFailure(counted, now);
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

    #region Nested types

    /// <summary>
    /// The windows of one kind of key, in the order in which they started and grouped by their failed tries; not
    /// thread-safe on its own.
    /// </summary>
    /// <typeparam name="TKey">The kind of key: a reference or an address.</typeparam>
    private sealed class Store<TKey>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, Window> _windows;
        private readonly LinkedList<Window> _byStart = new();
        private readonly LinkedList<Window>[] _byFailures;
        private readonly TimeSpan _length;
        private readonly int _capacity;
        private readonly int _limit;

        public Store(TimeSpan length, int capacity, int limit, IEqualityComparer<TKey> comparer)
        {
            _length = length;
            _capacity = capacity;
            _limit = limit;
            _windows = new Dictionary<TKey, Window>(comparer);
            _byFailures = new LinkedList<Window>[limit + 1];
            for (int failures = 0; failures <= limit; failures++)
            {
                _byFailures[failures] = new LinkedList<Window>();
            }
        }

        public int Count => _windows.Count;

        /// <summary>
        /// Tells whether a failed try of the key would be refused: its window is at the limit, or the key has no window and
        /// every window of a full store is at its limit, so there is no room that would not free a used-up key.
        /// </summary>
        public bool Refuses(TKey key, DateTimeOffset now)
        {
            DropRunOut(now);
            if (_windows.TryGetValue(key, out Window? window))
            {
                return window.Failures >= _limit;
            }

            return _windows.Count >= _capacity && LeastProtective() is null;
        }

        /// <summary>Counts one failed try in the window of the key, or in a new window that starts now.</summary>
        public void CountFailure(TKey key, DateTimeOffset now)
        {
            DropRunOut(now);
            if (_windows.TryGetValue(key, out Window? window))
            {
                Move(window, Math.Min(window.Failures + 1, _limit));
                return;
            }

            if (_windows.Count >= _capacity && LeastProtective() is { } dropped)
            {
                Remove(dropped);
            }

            Window created = new(key, now);
            _byStart.AddLast(created.StartNode);
            _byFailures[created.Failures].AddLast(created.FailuresNode);
            _windows[key] = created;
        }

        /// <summary>Forgets the window of the key.</summary>
        public void Clear(TKey key)
        {
            if (_windows.TryGetValue(key, out Window? window))
            {
                Remove(window);
            }
        }

        /// <summary>Takes one failed try back from the window of the key and keeps the window.</summary>
        public void GiveBack(TKey key, DateTimeOffset now)
        {
            if (_windows.TryGetValue(key, out Window? window) && !HasRunOut(window, now) && window.Failures > 0)
            {
                Move(window, window.Failures - 1);
            }
        }

        private bool HasRunOut(Window window, DateTimeOffset now) => now - window.Start >= _length;

        // The windows that have run out lead the order of their start, so they go first and hold no failed try.
        private void DropRunOut(DateTimeOffset now)
        {
            while (_byStart.First is { } first && HasRunOut(first.Value, now))
            {
                Remove(first.Value);
            }
        }

        // The window whose loss gives the least back: the fewest failed tries, of those the one that reached its count
        // first; never a window at its limit, so a key whose tries are used up stays refused until its window runs out.
        private Window? LeastProtective()
        {
            for (int failures = 0; failures < _limit; failures++)
            {
                if (_byFailures[failures].First is { } first)
                {
                    return first.Value;
                }
            }

            return null;
        }

        private void Move(Window window, int failures)
        {
            _byFailures[window.Failures].Remove(window.FailuresNode);
            window.Failures = failures;
            _byFailures[failures].AddLast(window.FailuresNode);
        }

        private void Remove(Window window)
        {
            _windows.Remove(window.Key);
            _byStart.Remove(window.StartNode);
            _byFailures[window.Failures].Remove(window.FailuresNode);
        }

        /// <summary>The window of one key: when it started, the failed tries in it, and its place in the two orders.</summary>
        private sealed class Window
        {
            public Window(TKey key, DateTimeOffset start)
            {
                Key = key;
                Start = start;
                StartNode = new LinkedListNode<Window>(this);
                FailuresNode = new LinkedListNode<Window>(this);
            }

            public TKey Key { get; }

            public DateTimeOffset Start { get; }

            public int Failures { get; set; } = 1;

            public LinkedListNode<Window> StartNode { get; }

            public LinkedListNode<Window> FailuresNode { get; }
        }
    }

    #endregion Nested types
}
