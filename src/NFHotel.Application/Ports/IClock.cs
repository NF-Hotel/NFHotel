namespace NFHotel.Application.Ports;

/// <summary>
/// Systemets ur. Eneste tilladte kilde til "nu" og "i dag" i Application-laget (A-05).
/// </summary>
/// <remarks>
/// Services må ALDRIG bruge <c>DateTime.Now</c>, <c>DateTime.UtcNow</c> eller
/// <c>DateOnly.FromDateTime(...)</c>. Uden ét fælles ur beregner hver service sin egen
/// "i dag", og BR-06, BR-44 og BR-104 er tilbage i det gamle systems problem, hvor
/// tidszonepolitikken lå spredt ud over ViewModels.
/// <para>
/// Implementeringen bor i Infrastructure (<c>SystemClock</c>) og kender hotellets
/// tidszone. Domain kender hverken ur eller tidszone — det er derfor entiteterne får
/// <c>occurredAt</c> og <c>today</c> som parametre.
/// </para>
/// </remarks>
public interface IClock
{
    /// <summary>Det aktuelle tidspunkt i UTC. Bruges til faktiske tidsstempler (B-08).</summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>
    /// Dagens dato i <b>hotellets</b> tidszone — ikke i UTC og ikke i serverens lokaltid.
    /// </summary>
    /// <remarks>
    /// Bookingperioder er kalenderdatoer, ikke tidsstempler (B-08). Omregningen fra
    /// tidsstempel til hotel-lokal dato er en politik, ikke en beregning, og hører derfor
    /// præcis ét sted: bag dette ur.
    /// </remarks>
    DateOnly Today { get; }
}