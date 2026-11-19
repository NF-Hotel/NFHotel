namespace NFHotel.Infrastructure.Security;

/// <summary>
/// The fixed windows of one kind of key in memory, in a store of a bounded size (ADR-0049 decision 6, ADR-0051 decision
/// 5): a window starts at the first count of its key, runs for a fixed length and holds at most the limit.
/// </summary>
/// <remarks>
/// The store keeps its windows in the order in which they started, and grouped by their counts. A new window first drops
/// the windows that have run out, which lead that order. A full store then drops the window with the fewest counts, of
/// those the one that reached its count first, and never a window at its limit while another is below it. When every
/// window is at its limit, the owner chooses (ADR-0051 decision 5, review RC-428): a store of references refuses the new
/// key and counts it nowhere, so a flood of new keys cannot free a reference whose tries are used up (review RC-418); a
/// store of caller addresses drops the window that runs out first, so senders with more addresses than the store holds
/// cannot make it refuse every other address. The store is not thread-safe on its own: its owner holds one lock around
/// every call.
/// </remarks>
/// <typeparam name="TKey">The kind of key: a reference or the key of a caller address.</typeparam>
internal sealed class BoundedWindowStore<TKey>
    where TKey : notnull
{
    #region Fields

    /// <summary>
    /// The highest limit of a window (review RC-428 F11): the store keeps a list for every count up to its limit, so a
    /// limit in the millions is a mistake of the setup, refused when the store is made.
    /// </summary>
    public const int MaxLimit = 100_000;

    private readonly Dictionary<TKey, Window> _windows;
    private readonly LinkedList<Window> _byStart = new();
    private readonly LinkedList<Window>[] _byCount;
    private readonly TimeSpan _length;
    private readonly int _capacity;
    private readonly int _limit;
    private readonly bool _dropsUsedUpWindowsWhenFull;

    #endregion Fields

    #region Constructors

    /// <summary>Creates an empty store.</summary>
    /// <param name="length">The length of a window.</param>
    /// <param name="capacity">The most keys that the store holds a window for.</param>
    /// <param name="limit">The most counts in one window, at most <see cref="MaxLimit"/>.</param>
    /// <param name="comparer">Compares two keys.</param>
    /// <param name="dropsUsedUpWindowsWhenFull">
    /// Whether a full store whose windows are all at their limit drops the window that runs out first for a new key, as a
    /// store of caller addresses does; otherwise it refuses the new key, as the store of references does.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The length is not positive, the capacity or the limit is below one, or the limit is above <see cref="MaxLimit"/>.
    /// </exception>
    public BoundedWindowStore(TimeSpan length, int capacity, int limit, IEqualityComparer<TKey> comparer, bool dropsUsedUpWindowsWhenFull)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(length, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, MaxLimit);
        ArgumentNullException.ThrowIfNull(comparer);

        _length = length;
        _capacity = capacity;
        _limit = limit;
        _dropsUsedUpWindowsWhenFull = dropsUsedUpWindowsWhenFull;
        _windows = new Dictionary<TKey, Window>(comparer);
        _byCount = new LinkedList<Window>[limit + 1];
        for (int count = 0; count <= limit; count++)
        {
            _byCount[count] = new LinkedList<Window>();
        }
    }

    #endregion Constructors

    #region Properties

    /// <summary>The number of keys that have a window.</summary>
    public int Count => _windows.Count;

    #endregion Properties

    #region Public methods

    /// <summary>
    /// Tells whether one more count of the key would be refused: its window is at the limit, or, in a store that refuses
    /// new keys, the key has no window and every window of a full store is at its limit, so there is no room that would
    /// not free a used-up key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="now">The time of the request.</param>
    /// <returns><see langword="true"/> when the count is refused.</returns>
    public bool Refuses(TKey key, DateTimeOffset now)
    {
        DropRunOut(now);
        if (_windows.TryGetValue(key, out Window? window))
        {
            return window.Counted >= _limit;
        }

        return !_dropsUsedUpWindowsWhenFull && _windows.Count >= _capacity && LeastProtective() is null;
    }

    /// <summary>
    /// The time until a key that <see cref="Refuses"/> refuses can be counted again: the rest of its window, or for a new
    /// key in a full store the rest of the window that runs out first, which frees the first room.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="now">The time of the request.</param>
    /// <returns>The time to wait; zero for an empty store.</returns>
    public TimeSpan RetryAfter(TKey key, DateTimeOffset now)
    {
        DropRunOut(now);
        Window? window = _windows.TryGetValue(key, out Window? own) ? own : _byStart.First?.Value;
        return window is null ? TimeSpan.Zero : window.Start + _length - now;
    }

    /// <summary>Counts one in the window of the key, or in a new window that starts now.</summary>
    /// <param name="key">The key.</param>
    /// <param name="now">The time of the request.</param>
    public void Record(TKey key, DateTimeOffset now)
    {
        DropRunOut(now);
        if (_windows.TryGetValue(key, out Window? window))
        {
            Move(window, Math.Min(window.Counted + 1, _limit));
            return;
        }

        if (_windows.Count >= _capacity
            && (LeastProtective() ?? (_dropsUsedUpWindowsWhenFull ? _byStart.First?.Value : null)) is { } dropped)
        {
            Remove(dropped);
        }

        Window created = new(key, now);
        _byStart.AddLast(created.StartNode);
        _byCount[created.Counted].AddLast(created.CountNode);
        _windows[key] = created;
    }

    /// <summary>Forgets the window of the key.</summary>
    /// <param name="key">The key.</param>
    public void Clear(TKey key)
    {
        if (_windows.TryGetValue(key, out Window? window))
        {
            Remove(window);
        }
    }

    /// <summary>Takes one count back from the window of the key and keeps the window; a window that has run out is left alone.</summary>
    /// <param name="key">The key.</param>
    /// <param name="now">The time of the request.</param>
    public void GiveBack(TKey key, DateTimeOffset now)
    {
        if (_windows.TryGetValue(key, out Window? window) && !HasRunOut(window, now) && window.Counted > 0)
        {
            Move(window, window.Counted - 1);
        }
    }

    #endregion Public methods

    #region Private methods

    private bool HasRunOut(Window window, DateTimeOffset now) => now - window.Start >= _length;

    // The windows that have run out lead the order of their start, so they go first.
    private void DropRunOut(DateTimeOffset now)
    {
        while (_byStart.First is { } first && HasRunOut(first.Value, now))
        {
            Remove(first.Value);
        }
    }

    // The window whose loss gives the least back: the fewest counts, of those the one that reached its count first;
    // never a window at its limit, so a key whose count is used up stays refused until its window runs out.
    private Window? LeastProtective()
    {
        for (int count = 0; count < _limit; count++)
        {
            if (_byCount[count].First is { } first)
            {
                return first.Value;
            }
        }

        return null;
    }

    private void Move(Window window, int count)
    {
        _byCount[window.Counted].Remove(window.CountNode);
        window.Counted = count;
        _byCount[count].AddLast(window.CountNode);
    }

    private void Remove(Window window)
    {
        _windows.Remove(window.Key);
        _byStart.Remove(window.StartNode);
        _byCount[window.Counted].Remove(window.CountNode);
    }

    #endregion Private methods

    #region Nested types

    /// <summary>The window of one key: when it started, the counts in it, and its place in the two orders.</summary>
    private sealed class Window
    {
        public Window(TKey key, DateTimeOffset start)
        {
            Key = key;
            Start = start;
            StartNode = new LinkedListNode<Window>(this);
            CountNode = new LinkedListNode<Window>(this);
        }

        public TKey Key { get; }

        public DateTimeOffset Start { get; }

        public int Counted { get; set; } = 1;

        public LinkedListNode<Window> StartNode { get; }

        public LinkedListNode<Window> CountNode { get; }
    }

    #endregion Nested types
}
