namespace NFHotel.Web.Security;

/// <summary>
/// The open requests to the circuits of the pages, counted per caller (ADR-0051 decision 8, SAR-002 BF-02). A circuit
/// holds one request to <c>/_blazor</c> open as long as it lives, its web socket, so the open requests of a caller are
/// its circuits. A caller is a key of <see cref="NFHotel.Application.UseCases.CallerAddressKey"/>: an IPv4 address as
/// itself, an IPv6 address by its first 64 bits, as the limits of the API count.
/// </summary>
/// <remarks>
/// A caller has an entry only while it has an open request, so there are never more entries than open requests, which
/// nginx and Kestrel bound. The count lives in the memory of the web application; a restart ends every circuit and
/// clears it.
/// </remarks>
public sealed class CircuitLimit
{
    #region Fields

    /// <summary>
    /// The open requests to <c>/_blazor</c> that one caller may have at once. A tab of a browser holds one, so 50 leave
    /// room for the guests and the staff behind one address, such as the network of the hotel.
    /// </summary>
    public const int OpenPerCaller = 50;

    private readonly Dictionary<string, int> _open = new(StringComparer.Ordinal);
    private readonly Lock _lock = new();

    #endregion Fields

    #region Properties

    /// <summary>The number of callers with an open request. For tests and diagnostics: a caller without one is forgotten.</summary>
    public int CallerCount
    {
        get
        {
            lock (_lock)
            {
                return _open.Count;
            }
        }
    }

    #endregion Properties

    #region Public methods

    /// <summary>Counts an open request of a caller, unless the caller already has <see cref="OpenPerCaller"/>.</summary>
    /// <param name="caller">The key of the caller.</param>
    /// <returns><see langword="true"/> when the request is counted; <see langword="false"/> when the limit refuses it.</returns>
    public bool TryOpen(string caller)
    {
        ArgumentNullException.ThrowIfNull(caller);

        lock (_lock)
        {
            int open = _open.GetValueOrDefault(caller);
            if (open >= OpenPerCaller)
            {
                return false;
            }

            _open[caller] = open + 1;
            return true;
        }
    }

    /// <summary>Ends an open request of a caller that <see cref="TryOpen"/> counted.</summary>
    /// <param name="caller">The key of the caller.</param>
    /// <exception cref="InvalidOperationException">The caller has no open request.</exception>
    public void Close(string caller)
    {
        ArgumentNullException.ThrowIfNull(caller);

        lock (_lock)
        {
            if (!_open.TryGetValue(caller, out int open))
            {
                throw new InvalidOperationException("The caller has no open request to close.");
            }

            if (open == 1)
            {
                _open.Remove(caller);
            }
            else
            {
                _open[caller] = open - 1;
            }
        }
    }

    /// <summary>The open requests of a caller.</summary>
    /// <param name="caller">The key of the caller.</param>
    /// <returns>The number; 0 for a caller without one.</returns>
    public int OpenOf(string caller)
    {
        ArgumentNullException.ThrowIfNull(caller);

        lock (_lock)
        {
            return _open.GetValueOrDefault(caller);
        }
    }

    #endregion Public methods
}
