namespace NFHotel.Application.Ports;

/// <summary>
/// The reasons, each in a word, why the relay did not take a mail (MIL-088, ADR-0053 decision 5). The event
/// <c>mail_not_sent</c> holds one of them, never the address or the text of the relay.
/// </summary>
public static class MailFailureReasons
{
    #region Fields

    /// <summary>The relay or the forwarder could not be reached.</summary>
    public const string Connect = "connect";

    /// <summary>The encrypted connection failed: no STARTTLS, or a certificate that does not hold.</summary>
    public const string Tls = "tls";

    /// <summary>The relay refused the account.</summary>
    public const string Authentication = "auth";

    /// <summary>The relay refused the from address, the address of the mail or the mail itself.</summary>
    public const string Refused = "refused";

    /// <summary>The send took longer than its limit.</summary>
    public const string Timeout = "timeout";

    /// <summary>Anything else.</summary>
    public const string Other = "other";

    /// <summary>The mail was not sent because the fuse of the mails a day is blown (ADR-0053 decision 6).</summary>
    public const string Fuse = "fuse";

    #endregion Fields

    #region Properties

    /// <summary>Every reason, so an event can refuse a text that is none of them.</summary>
    public static IReadOnlyList<string> All { get; } = [Connect, Tls, Authentication, Refused, Timeout, Other, Fuse];

    #endregion Properties
}
