using NFHotel.Application.Ports;

namespace NFHotel.TestSupport;

/// <summary>
/// The limits of the guest sign-in without a clock, for tests that start many tries at once (ADR-0049 decision 6): a
/// reference and an address are refused after the given number of failed tries until a success clears the reference.
/// Each call is one locked step, as the check and the count of the real limits are.
/// </summary>
public sealed class CountingGuestSignInLimits : IGuestSignInLimits
{
    #region Fields

    private readonly Dictionary<string, int> _failures = new(StringComparer.Ordinal);
    private readonly object _lock = new();
    private readonly int _maxPerReference;
    private readonly int _maxPerAddress;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the limits.</summary>
    /// <param name="maxPerReference">The failed tries after which a reference is refused.</param>
    /// <param name="maxPerAddress">The failed tries after which an address is refused.</param>
    public CountingGuestSignInLimits(int maxPerReference, int maxPerAddress)
    {
        _maxPerReference = maxPerReference;
        _maxPerAddress = maxPerAddress;
    }

    #endregion Constructors

    #region Public methods

    /// <inheritdoc />
    public bool TryCount(Guid? reference, string address)
    {
        ArgumentNullException.ThrowIfNull(address);

        lock (_lock)
        {
            string addressKey = AddressKey(address);
            if (_failures.GetValueOrDefault(addressKey) >= _maxPerAddress
                || (reference is Guid known && _failures.GetValueOrDefault(ReferenceKey(known)) >= _maxPerReference))
            {
                return false;
            }

            _failures[addressKey] = _failures.GetValueOrDefault(addressKey) + 1;
            if (reference is Guid counted)
            {
                _failures[ReferenceKey(counted)] = _failures.GetValueOrDefault(ReferenceKey(counted)) + 1;
            }

            return true;
        }
    }

    /// <inheritdoc />
    public void Succeeded(Guid reference, string address)
    {
        ArgumentNullException.ThrowIfNull(address);

        lock (_lock)
        {
            _failures.Remove(ReferenceKey(reference));
            string addressKey = AddressKey(address);
            if (_failures.GetValueOrDefault(addressKey) > 0)
            {
                _failures[addressKey]--;
            }
        }
    }

    /// <summary>The failed tries that are counted against an address.</summary>
    /// <param name="address">The address.</param>
    /// <returns>The count.</returns>
    public int FailuresOf(string address)
    {
        lock (_lock)
        {
            return _failures.GetValueOrDefault(AddressKey(address));
        }
    }

    /// <summary>The failed tries that are counted against a reference.</summary>
    /// <param name="reference">The reference.</param>
    /// <returns>The count.</returns>
    public int FailuresOf(Guid reference)
    {
        lock (_lock)
        {
            return _failures.GetValueOrDefault(ReferenceKey(reference));
        }
    }

    #endregion Public methods

    #region Private methods

    private static string AddressKey(string address) => "address:" + address;

    private static string ReferenceKey(Guid reference) => "reference:" + reference.ToString("D");

    #endregion Private methods
}
