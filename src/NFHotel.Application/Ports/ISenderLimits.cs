namespace NFHotel.Application.Ports;

/// <summary>
/// The limits on what one caller can ask of the booking flow per caller address (MIL-087, ADR-0051 decisions 2 and 5):
/// the searches for free rooms and the public booking requests. The counts are keys of <c>CallerAddressKey</c>; the limit
/// per email is <see cref="IPublicBookingEmailLimit"/>.
/// </summary>
public interface ISenderLimits
{
    #region Public methods

    /// <summary>
    /// Counts one search for free rooms against the address unless its window is used up, as one atomic step, so that
    /// searches sent at the same time cannot all pass the check before any of them is counted.
    /// </summary>
    /// <param name="addressKey">The key of the caller address.</param>
    /// <returns>Whether the search may go on, and when not, the time until the window has room again.</returns>
    SenderLimitDecision TryCountSearch(string addressKey);

    /// <summary>
    /// Counts one public booking request against the hour and the day of the address unless either is used up, as one
    /// atomic step; a refused request counts in neither window.
    /// </summary>
    /// <param name="addressKey">The key of the caller address.</param>
    /// <returns>Whether the request may go on, and when not, the limit that refused it and the time to wait.</returns>
    SenderLimitDecision TryCountBookingRequest(string addressKey);

    /// <summary>
    /// Gives back a booking request that a rule of the use case refused, so that a refused value does not use up a
    /// count (decision 2); a window that has run out in between is left alone.
    /// </summary>
    /// <param name="addressKey">The key of the caller address.</param>
    void GiveBackBookingRequest(string addressKey);

    #endregion Public methods
}
