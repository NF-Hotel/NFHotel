using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Ports;

/// <summary>
/// The security events of the booking request and its verification (MIL-088, ADR-0052 decision 10 and ADR-0053 decisions
/// 5 and 6). The use case writes them where it decides, because only it knows which limit refused and whether a mail went;
/// the API writes each event once in its log, with the address of the caller and without any email (ADR-0051 decision
/// 6 since MIL-089), and counts it. No method takes an email, so none can reach the log.
/// </summary>
public interface IBookingRequestEvents
{
    #region Public methods

    /// <summary>A booking request was stored and its mail sent.</summary>
    void RequestMade();

    /// <summary>A verification came with a link that does not work: unknown, used or lapsed.</summary>
    void VerificationFailed();

    /// <summary>A verification made a booking: the event of a booking made on the public site.</summary>
    void PublicBookingMade();

    /// <summary>A limit per mailbox refused a request.</summary>
    /// <param name="limit">The name of the limit, from <see cref="LimitNames"/>.</param>
    void LimitRefused(string limit);

    /// <summary>
    /// The relay took a mail. Not a security event: the API counts the mails of each kind, so the alert
    /// <c>MailVolumeHigh</c> sees the volume before the fuse (ADR-0053 decision 6).
    /// </summary>
    /// <param name="kind">The kind of the mail.</param>
    void MailSent(MailKind kind);

    /// <summary>The relay did not take a mail, or the fuse held it back.</summary>
    /// <param name="kind">The kind of the mail.</param>
    /// <param name="reason">The reason in a word, from <see cref="MailFailureReasons"/>.</param>
    void MailNotSent(MailKind kind, string reason);

    /// <summary>The fuse of the mails a day refused a request.</summary>
    void MailFuseBlown();

    #endregion Public methods
}
