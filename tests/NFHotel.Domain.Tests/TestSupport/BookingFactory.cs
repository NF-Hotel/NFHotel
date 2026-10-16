using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.TestSupport;

/// <summary>
/// Bygger bookinger i en ønsket tilstand ud fra faste datoer.
/// </summary>
/// <remarks>
/// Alle datoer er hårdkodede — Domain tager "i dag" som parameter (A-05), så ingen test
/// behøver <c>DateTime.Now</c>. Tilstande nås kun gennem de rigtige overgange, så
/// fabrikken aldrig kan producere en booking domænet ikke selv kunne have produceret.
/// </remarks>
internal static class BookingFactory
{
    /// <summary>Dagens dato i alle bookingtests.</summary>
    internal static readonly DateOnly Today = new(2026, 1, 1);

    /// <summary>Standardankomst. Samme dag som <see cref="Today"/>, så check-in er lovligt (BR-06).</summary>
    internal static readonly DateOnly Arrival = new(2026, 1, 1);

    /// <summary>Standardafrejse (eksklusiv). Giver 9 overnatninger.</summary>
    internal static readonly DateOnly Departure = new(2026, 1, 10);

    /// <summary>Standardtidspunkt for indtjekning.</summary>
    internal static readonly DateTimeOffset CheckInMoment = new(2026, 1, 1, 15, 0, 0, TimeSpan.Zero);

    /// <summary>Standardtidspunkt for udtjekning på den bookede afrejsedag.</summary>
    internal static readonly DateTimeOffset CheckOutMoment = new(2026, 1, 10, 10, 0, 0, TimeSpan.Zero);

    /// <summary>Rummet alle standardbookinger ligger på.</summary>
    internal const int RoomId = 7;

    /// <summary>Gæsten alle standardbookinger hører til.</summary>
    internal const int GuestId = 3;

    /// <summary>Standardperioden 1.–10. januar.</summary>
    internal static DateRange StandardPeriod => new(Arrival, Departure);

    /// <summary>
    /// Opretter en booking i startstatus <see cref="BookingStatus.Pending"/> (BR-01).
    /// </summary>
    /// <returns>En ny booking på <see cref="RoomId"/> i <see cref="StandardPeriod"/>.</returns>
    internal static Booking Pending() => Booking.Create(StandardPeriod, RoomId, GuestId, Today);

    /// <summary>
    /// Opretter en booking og kører den frem til den ønskede status ad lovlig vej.
    /// </summary>
    /// <param name="status">Den ønskede sluttilstand.</param>
    /// <returns>En booking i <paramref name="status"/>.</returns>
    internal static Booking InStatus(BookingStatus status)
    {
        var booking = Pending();

        switch (status)
        {
            case BookingStatus.Pending:
                break;

            case BookingStatus.Confirmed:
                booking.Confirm();
                break;

            case BookingStatus.CheckedIn:
                booking.CheckIn(CheckInMoment, Today);
                break;

            case BookingStatus.CheckedOut:
                booking.CheckIn(CheckInMoment, Today);

                // Udtjekningsdatoen må ikke ligge efter "i dag", så hotellets dagsdato ved
                // udtjekningen er afrejsedagen — ikke ankomstdagen.
                booking.CheckOut(CheckOutMoment, Departure, Departure);
                break;

            case BookingStatus.Cancelled:
                booking.Cancel();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "Ukendt bookingstatus.");
        }

        return booking;
    }

    /// <summary>
    /// Opretter en booking hvor gæsten er rejst før den bookede afrejsedag (A-01).
    /// </summary>
    /// <param name="checkOutDate">
    /// Den hotel-lokale dato gæsten faktisk rejste. Bruges også som "i dag", fordi en
    /// udtjekning altid registreres på den dag den sker.
    /// </param>
    /// <returns>En udtjekket booking med <see cref="Booking.CheckOutDate"/> sat.</returns>
    internal static Booking CheckedOutOn(DateOnly checkOutDate)
    {
        var booking = Pending();
        booking.CheckIn(CheckInMoment, Today);

        // Udtjekningstidspunktet skal være strengt efter indtjekningen (BR-107); klokken 10
        // på afrejsedagen ligger efter indtjekningen kl. 15 på ankomstdagen i alle scenarier,
        // også ved udtjekning samme dag som ankomsten.
        var occurredAt = checkOutDate == Arrival
            ? CheckInMoment.AddHours(3)
            : new DateTimeOffset(checkOutDate.Year, checkOutDate.Month, checkOutDate.Day, 10, 0, 0, TimeSpan.Zero);

        booking.CheckOut(occurredAt, checkOutDate, checkOutDate);

        return booking;
    }

    /// <summary>
    /// Sætter primærnøglen på en booking der ikke er persisteret.
    /// </summary>
    /// <param name="booking">Bookingen der skal have et id.</param>
    /// <param name="bookingId">Det id EF Core ellers ville have tildelt.</param>
    /// <returns>Samme booking, med <see cref="Entity{TKey}.Id"/> sat.</returns>
    /// <remarks>
    /// <see cref="Entity{TKey}.Id"/> har privat setter, fordi kun EF Core må tildele den
    /// (A-15). BR-109's bookingnummerformat kan kun testes med et realistisk id, så
    /// nøglen sættes her via refleksion i stedet for at åbne setteren for produktionskode.
    /// </remarks>
    internal static Booking WithBookingId(Booking booking, int bookingId)
    {
        var setter = typeof(Booking).GetProperty(nameof(Booking.Id))!.GetSetMethod(nonPublic: true)!;
        setter.Invoke(booking, [bookingId]);

        return booking;
    }
}