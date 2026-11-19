namespace NFHotel.Application.Ports;

/// <summary>
/// The limits on the tries of the guest sign-in (ADR-0049 decision 6): failed tries are counted per reference and per
/// caller address, apart from the throttle of the staff sign-in, and a try over either limit is refused.
/// </summary>
public interface IGuestSignInLimits
{
    /// <summary>
    /// Counts one try against the reference and the address unless either has used up its tries in its window, as one
    /// atomic step, so that tries sent at the same time cannot all pass the check before any of them is counted. The
    /// try counts as a failure until <see cref="Succeeded"/> takes it back.
    /// </summary>
    /// <param name="reference">
    /// The reference as read, whether a booking has it or not; <see langword="null"/> for an unreadable reference, which
    /// counts against the address alone.
    /// </param>
    /// <param name="address">The address of the caller.</param>
    /// <returns>
    /// <see langword="true"/> when the try is counted and may go on; <see langword="false"/> when it must be refused, and
    /// nothing is counted.
    /// </returns>
    bool TryCount(Guid? reference, string address);

    /// <summary>
    /// Takes back a try that succeeded: clears the window of the reference, and gives the address its try back without
    /// clearing its window, so that one's own booking cannot reset the count of an address.
    /// </summary>
    /// <param name="reference">The reference that signed in.</param>
    /// <param name="address">The address of the caller.</param>
    void Succeeded(Guid reference, string address);
}
