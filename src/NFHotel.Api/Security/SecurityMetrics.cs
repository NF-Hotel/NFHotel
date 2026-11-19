using System.Diagnostics.Metrics;

namespace NFHotel.Api.Security;

/// <summary>
/// The counter of the security events of the booking flow (MIL-087, ADR-0051 decision 6): <c>nfhotel.security.events</c>
/// on the meter <c>NFHotel.Security</c>, with the labels <c>kind</c> and <c>limit</c>. In Prometheus it is
/// <c>nfhotel_security_events_total</c>.
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

    private readonly Counter<long> _events;

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

    #endregion Public methods
}
