namespace NFHotel.Application.Ports;

/// <summary>
/// The clock of the system. The only allowed source of "now" and "today" in the Application layer (A-05).
/// </summary>
/// <remarks>
/// Services must NEVER use <c>DateTime.Now</c>, <c>DateTime.UtcNow</c> or
/// <c>DateOnly.FromDateTime(...)</c>. Without one common clock every service computes its own
/// "today", and BR-06, BR-44 and BR-104 are back in the old system's problem, where
/// the time zone policy was spread across ViewModels.
/// <para>
/// The implementation lives in Infrastructure (<c>SystemClock</c>) and knows the hotel's
/// time zone. Domain knows neither clock nor time zone — which is why the entities get
/// <c>occurredAt</c> and <c>today</c> as parameters.
/// </para>
/// </remarks>
public interface IClock
{
    /// <summary>The current moment in UTC. Used for actual timestamps (B-08).</summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>
    /// Today's date in the <b>hotel's</b> time zone — not in UTC and not in the server's local time.
    /// </summary>
    /// <remarks>
    /// Booking periods are calendar dates, not timestamps (B-08). The conversion from
    /// timestamp to hotel-local date is a policy, not a calculation, and therefore belongs in
    /// exactly one place: behind this clock.
    /// </remarks>
    DateOnly Today { get; }
}