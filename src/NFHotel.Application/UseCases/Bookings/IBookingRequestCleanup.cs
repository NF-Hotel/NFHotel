namespace NFHotel.Application.UseCases.Bookings;

/// <summary>
/// The clean-up of the booking requests (MIL-088, ADR-0052 decision 7): deletes the requests whose link lapsed, with their
/// personal data, and the records of mails older than a day. A timed job runs it every 10 minutes.
/// </summary>
public interface IBookingRequestCleanup
{
    #region Public methods

    /// <summary>Deletes the lapsed requests and the old records of mails, by the clock of the application.</summary>
    /// <param name="cancellationToken">Cancels the run.</param>
    /// <returns>The number of requests and the number of records deleted.</returns>
    Task<(int Requests, int Mails)> RunAsync(CancellationToken cancellationToken = default);

    #endregion Public methods
}
