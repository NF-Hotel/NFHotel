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
/// Each store keeps its windows in the order in which they started. A new window first drops the windows that have run
/// out, which lead that order, and a full store then drops the window that started first, which is the nearest to its
/// end, so the least protection is lost. A restart of the API clears both stores.
/// </remarks>
public sealed class InMemoryGuestSignInLimits : IGuestSignInLimits
{
    #region Fields

    private readonly object _lock = new();
    private readonly Store<Guid> _references;
    private readonly Store<string> _addresses;
    private readonly IClock _clock;
    private readonly int _maxPerReference;
    private readonly int _maxPerAddress;

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
        _references = new Store<Guid>(window, options.MaxTrackedKeys, EqualityComparer<Guid>.Default);
        _addresses = new Store<string>(window, options.MaxTrackedKeys, StringComparer.Ordinal);
        _maxPerReference = options.MaxFailuresPerReference;
        _maxPerAddress = options.MaxFailuresPerAddress;
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
            if (_addresses.FailuresOf(address, now) >= _maxPerAddress
                || (reference is Guid known && _references.FailuresOf(known, now) >= _maxPerReference))
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

    /// <summary>The windows of one kind of key, in the order in which they started; not thread-safe on its own.</summary>
    /// <typeparam name="TKey">The kind of key: a reference or an address.</typeparam>
    private sealed class Store<TKey>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, LinkedListNode<Window>> _windows;
        private readonly LinkedList<Window> _byStart = new();
        private readonly TimeSpan _length;
        private readonly int _capacity;

        public Store(TimeSpan length, int capacity, IEqualityComparer<TKey> comparer)
        {
            _length = length;
            _capacity = capacity;
            _windows = new Dictionary<TKey, LinkedListNode<Window>>(comparer);
        }

        public int Count => _windows.Count;

        /// <summary>The failed tries in the window of the key; a window that has run out holds none.</summary>
        public int FailuresOf(TKey key, DateTimeOffset now) =>
            _windows.TryGetValue(key, out LinkedListNode<Window>? node) && !HasRunOut(node.Value, now) ? node.Value.Failures : 0;

        /// <summary>Counts one failed try in the window of the key, or in a new window that starts now.</summary>
        public void CountFailure(TKey key, DateTimeOffset now)
        {
            if (_windows.TryGetValue(key, out LinkedListNode<Window>? node))
            {
                if (!HasRunOut(node.Value, now))
                {
                    node.Value.Failures++;
                    return;
                }

                Remove(node);
            }

            MakeRoom(now);
            _windows[key] = _byStart.AddLast(new Window(key, now));
        }

        /// <summary>Forgets the window of the key.</summary>
        public void Clear(TKey key)
        {
            if (_windows.TryGetValue(key, out LinkedListNode<Window>? node))
            {
                Remove(node);
            }
        }

        /// <summary>Takes one failed try back from the window of the key and keeps the window.</summary>
        public void GiveBack(TKey key, DateTimeOffset now)
        {
            if (_windows.TryGetValue(key, out LinkedListNode<Window>? node) && !HasRunOut(node.Value, now) && node.Value.Failures > 0)
            {
                node.Value.Failures--;
            }
        }

        private bool HasRunOut(Window window, DateTimeOffset now) => now - window.Start >= _length;

        // The windows that have run out lead the order, so they go first; a full store then drops the oldest window.
        private void MakeRoom(DateTimeOffset now)
        {
            while (_byStart.First is { } first && HasRunOut(first.Value, now))
            {
                Remove(first);
            }

            while (_windows.Count >= _capacity && _byStart.First is { } oldest)
            {
                Remove(oldest);
            }
        }

        private void Remove(LinkedListNode<Window> node)
        {
            _windows.Remove(node.Value.Key);
            _byStart.Remove(node);
        }

        /// <summary>The window of one key: when it started and the failed tries in it.</summary>
        private sealed class Window(TKey key, DateTimeOffset start)
        {
            public TKey Key { get; } = key;

            public DateTimeOffset Start { get; } = start;

            public int Failures { get; set; } = 1;
        }
    }

    #endregion Nested types
}
