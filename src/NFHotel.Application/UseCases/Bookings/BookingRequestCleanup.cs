using NFHotel.Application.Ports;
using NFHotel.Domain.Repositories;

namespace NFHotel.Application.UseCases.Bookings;

/// <summary>
/// Deletes the lapsed booking requests and the old records of mails (MIL-088, ADR-0052 decision 7), by the clock of the
/// application, so a test runs it with a fixed clock.
/// </summary>
public sealed class BookingRequestCleanup : IBookingRequestCleanup
{
    #region Fields

    private readonly IBookingRequestRepository _requests;
    private readonly ISentMailRepository _sentMails;
    private readonly IClock _clock;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the clean-up.</summary>
    /// <param name="requests">The booking requests.</param>
    /// <param name="sentMails">The records of the mails sent.</param>
    /// <param name="clock">The clock of the application (A-05).</param>
    public BookingRequestCleanup(IBookingRequestRepository requests, ISentMailRepository sentMails, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(sentMails);
        ArgumentNullException.ThrowIfNull(clock);

        _requests = requests;
        _sentMails = sentMails;
        _clock = clock;
    }

    #endregion Constructors

    #region Properties

    /// <summary>How long a record of a mail is kept: the longest window that counts it, a day.</summary>
    public static TimeSpan MailRecordLifetime { get; } = TimeSpan.FromHours(24);

    #endregion Properties

    #region Public methods

    /// <inheritdoc />
    public async Task<(int Requests, int Mails)> RunAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = _clock.UtcNow;
        int requests = await _requests.DeleteLapsedAsync(now, cancellationToken).ConfigureAwait(false);
        int mails = await _sentMails.DeleteOlderThanAsync(now - MailRecordLifetime, cancellationToken).ConfigureAwait(false);

        return (requests, mails);
    }

    #endregion Public methods
}
