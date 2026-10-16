namespace NFHotel.Web.Features.Booking;

/// <summary>
/// Trinnene i kundens bookingflow på <c>/book</c>.
/// </summary>
/// <remarks>
/// Værdierne er stigende og sammenlignes med <c>&lt;</c> i trinindikatoren, så et trin før
/// det aktuelle tegnes som gennemført. Rækkefølgen er derfor en del af kontrakten — ikke
/// bare en opremsning.
/// <para>
/// Trinnet alene er ikke en tilladelse: <see cref="BookingFlowViewModel"/> kontrollerer
/// altid datoer og rumvalg igen, inden den går videre (BR-C-04). Ellers ville et trin, der
/// blev sat af en tidligere handling, kunne bære en bruger forbi en regel.
/// </para>
/// </remarks>
public enum BookingFlowStep
{
    /// <summary>Trin 1: ankomst- og afrejsedato.</summary>
    Dates = 0,

    /// <summary>Trin 2: valg blandt de rum der er ledige i perioden (BR-C-02).</summary>
    Room = 1,

    /// <summary>Trin 3: gæstens oplysninger, indtastet uden konto (BR-C-01).</summary>
    Guest = 2,

    /// <summary>Trin 4: kvitteringen for den oprettede booking (BR-C-05).</summary>
    Receipt = 3
}