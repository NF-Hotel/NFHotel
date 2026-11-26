using NFHotel.Domain.ValueObjects;

namespace NFHotel.Api.Security;

/// <summary>
/// Writes the security events of the booking flow (MIL-087, ADR-0051 decision 6): each one once in the log of
/// <see cref="SecurityEvents.Category"/>, with the address of the caller and without any email or other typed value
/// (since MIL-089 no method takes an email), and once in the counter of <see cref="SecurityMetrics"/>. Since MIL-088
/// also the events of the booking request and of its mails.
/// </summary>
/// <param name="loggerFactory">The factory of the logger of <see cref="SecurityEvents.Category"/>.</param>
/// <param name="metrics">The counter of the events.</param>
public sealed class BookingFlowEvents(ILoggerFactory loggerFactory, SecurityMetrics metrics)
{
    #region Fields

    private readonly ILogger _logger = (loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory)))
        .CreateLogger(SecurityEvents.Category);

    private readonly SecurityMetrics _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));

    #endregion Fields

    #region Public methods

    /// <summary>Writes the event of a booking made on the public site.</summary>
    /// <param name="address">The address of the caller.</param>
    public void PublicBookingMade(string? address)
    {
        _logger.PublicBookingMade(address ?? SecurityEvents.None);
        _metrics.Count(SecurityEvents.PublicBookingMadeKind, null);
    }

    /// <summary>Writes the event of a guest sign-in refused for a wrong reference or email.</summary>
    /// <param name="address">The address of the caller.</param>
    public void GuestLookupFailed(string? address)
    {
        _logger.GuestLookupFailed(address ?? SecurityEvents.None);
        _metrics.Count(SecurityEvents.GuestLookupFailedKind, null);
    }

    /// <summary>Writes the event of a refusal by a limit.</summary>
    /// <param name="limit">The name of the limit, from <see cref="Application.Ports.LimitNames"/>.</param>
    /// <param name="address">The address of the caller.</param>
    public void LimitRefused(string limit, string? address)
    {
        ArgumentNullException.ThrowIfNull(limit);

        _logger.LimitRefused(limit, address ?? SecurityEvents.None);
        _metrics.Count(SecurityEvents.LimitRefusedKind, limit);
    }

    /// <summary>Writes the event of a booking request that was stored and whose mail went (MIL-088).</summary>
    /// <param name="address">The address of the caller.</param>
    public void BookingRequestMade(string? address)
    {
        _logger.BookingRequestMade(address ?? SecurityEvents.None);
        _metrics.Count(SecurityEvents.BookingRequestMadeKind, null);
    }

    /// <summary>Writes the event of a verification with a link that does not work: unknown, used or lapsed (MIL-088).</summary>
    /// <param name="address">The address of the caller.</param>
    public void VerificationFailed(string? address)
    {
        _logger.VerificationFailed(address ?? SecurityEvents.None);
        _metrics.Count(SecurityEvents.VerificationFailedKind, null);
    }

    /// <summary>Counts a mail that the relay took (MIL-088); no log line, because a mail is no security event.</summary>
    /// <param name="kind">The kind of the mail.</param>
    public void MailSent(MailKind kind) => _metrics.CountMail(SecurityEvents.MailNameOf(kind));

    /// <summary>Writes the event of a mail that the relay did not take or that the fuse held back (MIL-088).</summary>
    /// <param name="kind">The kind of the mail.</param>
    /// <param name="reason">The reason in a word; any other text is written as <c>other</c>.</param>
    public void MailNotSent(MailKind kind, string? reason)
    {
        _logger.MailNotSent(SecurityEvents.MailNameOf(kind), SecurityEvents.MailReasonOf(reason));
        _metrics.Count(SecurityEvents.MailNotSentKind, null);
    }

    /// <summary>Writes the event of a refusal by the fuse of the mails a day (MIL-088).</summary>
    public void MailFuseBlown()
    {
        _logger.MailFuseBlown();
        _metrics.Count(SecurityEvents.MailFuseBlownKind, null);
    }

    #endregion Public methods
}
