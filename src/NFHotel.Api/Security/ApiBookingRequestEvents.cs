using NFHotel.Application.Ports;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Api.Security;

/// <summary>
/// The events of the booking request and its verification in the API (MIL-088, ADR-0052 decision 10 and ADR-0053
/// decisions 5 and 6): the use case says what happened, and this writes it once in the log of
/// <see cref="SecurityEvents.Category"/>, with the address of the caller of the request and no email, and once in the
/// counters of <see cref="SecurityMetrics"/>.
/// </summary>
/// <param name="caller">The address of the caller of the request.</param>
/// <param name="events">Writes the events of the booking flow.</param>
public sealed class ApiBookingRequestEvents(ICallerAddress caller, BookingFlowEvents events) : IBookingRequestEvents
{
    #region Fields

    private readonly ICallerAddress _caller = caller ?? throw new ArgumentNullException(nameof(caller));
    private readonly BookingFlowEvents _events = events ?? throw new ArgumentNullException(nameof(events));

    #endregion Fields

    #region Public methods

    /// <inheritdoc />
    public void RequestMade() => _events.BookingRequestMade(_caller.Address);

    /// <inheritdoc />
    public void VerificationFailed() => _events.VerificationFailed(_caller.Address);

    /// <inheritdoc />
    public void PublicBookingMade() => _events.PublicBookingMade(_caller.Address);

    /// <inheritdoc />
    public void LimitRefused(string limit) => _events.LimitRefused(limit, _caller.Address);

    /// <inheritdoc />
    public void MailSent(MailKind kind) => _events.MailSent(kind);

    /// <inheritdoc />
    public void MailNotSent(MailKind kind, string reason) => _events.MailNotSent(kind, reason);

    /// <inheritdoc />
    public void MailFuseBlown() => _events.MailFuseBlown();

    #endregion Public methods
}
