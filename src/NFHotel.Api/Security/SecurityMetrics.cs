using System.Diagnostics.Metrics;

namespace NFHotel.Api.Security;

/// <summary>
/// The counter of the security events of the booking flow (MIL-087, ADR-0051 decision 6): <c>nfhotel.security.events</c>
/// on the meter <c>NFHotel.Security</c>, with the labels <c>kind</c> and <c>limit</c>. In Prometheus it is
/// <c>nfhotel_security_events_total</c>. Beside it the counter of the mails that the relay took,
/// <c>nfhotel.mails</c> with the label <c>kind</c> (MIL-088, ADR-0053 decision 6), <c>nfhotel_mails_total</c> in
/// Prometheus: a mail is no security event, but its volume is what the alert <c>MailVolumeHigh</c> watches.
/// </summary>
public sealed class SecurityMetrics
{
    #region Fields

    /// <summary>The name of the meter, which the telemetry of the services sends on.</summary>
    public const string MeterName = "NFHotel.Security";

    /// <summary>The name of the counter.</summary>
    public const string EventsCounterName = "nfhotel.security.events";

    /// <summary>The label with the kind of the event.</summary>
    public const string KindTag = "kind";

    /// <summary>The label with the name of the limit of a refusal.</summary>
    public const string LimitTag = "limit";

    /// <summary>The name of the counter of the mails that the relay took (MIL-088).</summary>
    public const string MailsCounterName = "nfhotel.mails";

    private readonly Counter<long> _events;
    private readonly Counter<long> _mails;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the counter.</summary>
    /// <param name="meterFactory">The factory of the meters of the host.</param>
    public SecurityMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);

        // The factory owns the meter and disposes of it with the host.
        Meter meter = meterFactory.Create(MeterName);
        _events = meter.CreateCounter<long>(
            EventsCounterName,
            unit: "{event}",
            description: "The security events of the booking flow, by kind and by the limit of a refusal.");
        _mails = meter.CreateCounter<long>(
            MailsCounterName,
            unit: "{mail}",
            description: "The mails of the booking flow that the relay took, by kind.");
    }

    #endregion Constructors

    #region Public methods

    /// <summary>Counts one event.</summary>
    /// <param name="kind">The kind of the event.</param>
    /// <param name="limit">The name of the limit of a refusal, or <see langword="null"/>.</param>
    public void Count(string kind, string? limit)
    {
        ArgumentNullException.ThrowIfNull(kind);

        // An event that no limit refused has no label limit, so its series has the label kind only.
        if (limit is null)
        {
            _events.Add(1, new KeyValuePair<string, object?>(KindTag, kind));
            return;
        }

        _events.Add(1, new KeyValuePair<string, object?>(KindTag, kind), new KeyValuePair<string, object?>(LimitTag, limit));
    }

    /// <summary>Counts one mail that the relay took.</summary>
    /// <param name="mail">The name of the kind of the mail, from <see cref="SecurityEvents.MailNames"/>.</param>
    public void CountMail(string mail)
    {
        ArgumentNullException.ThrowIfNull(mail);

        _mails.Add(1, new KeyValuePair<string, object?>(KindTag, mail));
    }

    /// <summary>
    /// Gives the series of the mails a value of 0 once the telemetry runs (MIL-088, ADR-0053 decisions 5 and 6). A series
    /// that a counter starts with its first event has no earlier value, so <c>increase</c> in Prometheus does not see
    /// that event, and the alerts <c>MailNotSent</c> and <c>MailFuseBlown</c> fire on the first one; with the 0 they do.
    /// </summary>
    public void StartAtZero()
    {
        _events.Add(0, new KeyValuePair<string, object?>(KindTag, SecurityEvents.MailNotSentKind));
        _events.Add(0, new KeyValuePair<string, object?>(KindTag, SecurityEvents.MailFuseBlownKind));
        foreach (string mail in SecurityEvents.MailNames)
        {
            _mails.Add(0, new KeyValuePair<string, object?>(KindTag, mail));
        }
    }

    #endregion Public methods
}
