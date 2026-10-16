using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Rules;

/// <summary>
/// Statsløse bookingregler. Rene funktioner uden database- eller tidsafhængighed, så de
/// kan unit-testes direkte (B-03).
/// </summary>
/// <remarks>
/// Klassen ejer definitionen af overlap (A-04). Infrastructure genererer databasens
/// exclusion constraint ud fra den samme definition, og en paritetstest asserter at
/// C#-funktionen og SQL-constrainten er enige om et sæt kendte tilfælde. Uden den test
/// driver de fra hinanden.
/// </remarks>
public static class BookingRules
{
    /// <summary>Fejlkode: perioden starter før i dag (BR-44, BR-104, BR-108).</summary>
    public const string StartDateInPast = "booking.start_date_in_past";

    /// <summary>Fejlkode: bookingen mangler en rumreference (BR-45, BR-105).</summary>
    public const string RoomRequired = "booking.room_required";

    /// <summary>Fejlkode: bookingen mangler en gæstereference (BR-106).</summary>
    public const string GuestRequired = "booking.guest_required";

    /// <summary>Fejlkode: udtjekningstidspunktet er ikke efter indtjekningstidspunktet (BR-107).</summary>
    public const string CheckOutBeforeCheckIn = "booking.check_out_before_check_in";

    /// <summary>Fejlkode: udtjekningsdatoen ligger før ankomstdatoen.</summary>
    public const string CheckOutDateBeforeStartDate = "booking.check_out_date_before_start_date";

    /// <summary>
    /// Fejlkode: udtjekningsdatoen ligger efter i dag. En dato i fremtiden ville spærre
    /// rummet frem til den (A-01) og kunne ikke rettes bagefter.
    /// </summary>
    public const string CheckOutDateInFuture = "booking.check_out_date_in_future";

    /// <summary>
    /// Halvåben overlapstest mellem to perioder (B-03, BR-39).
    /// </summary>
    /// <param name="a">Første periode.</param>
    /// <param name="b">Anden periode.</param>
    /// <returns>Sand hvis perioderne deler mindst én nat. Afrejse- og ankomstdag samme dag er IKKE overlap.</returns>
    public static bool Overlaps(DateRange a, DateRange b) => a.Overlaps(b);

    /// <summary>
    /// Afgør om en booking med den angivne status lægger beslag på rummet.
    /// </summary>
    /// <param name="status">Bookingens status.</param>
    /// <returns>Sand for alt undtagen <see cref="BookingStatus.Cancelled"/>.</returns>
    /// <remarks>
    /// Kun annullering frigiver rummet helt. En udtjekket booking blokerer stadig — men
    /// kun frem til sin effektive slutdato, så tidlig udtjekning alligevel frigiver de
    /// resterende nætter (A-01). Svarer til databasens <c>WHERE (status &lt;&gt; 4)</c>.
    /// </remarks>
    public static bool BlocksRoom(BookingStatus status) => status != BookingStatus.Cancelled;

    /// <summary>
    /// Afgør om en eksisterende booking er i konflikt med en ønsket periode på et rum (BR-19).
    /// </summary>
    /// <param name="existing">Den eksisterende booking der undersøges.</param>
    /// <param name="roomId">Rummet den ønskede periode gælder.</param>
    /// <param name="desiredPeriod">Den ønskede periode.</param>
    /// <returns>Sand hvis den eksisterende booking spærrer for den ønskede periode.</returns>
    /// <remarks>
    /// Sammenligningen sker mod <see cref="Booking.EffectivePeriod"/>, ikke
    /// <see cref="Booking.Period"/> (A-01): en gæst der rejser tidligt skal frigive de
    /// resterende nætter. Kaldet er ansvarligt for at ekskludere bookingen selv ved
    /// redigering.
    /// </remarks>
    public static bool Conflicts(Booking existing, int roomId, DateRange desiredPeriod)
    {
        ArgumentNullException.ThrowIfNull(existing);

        return existing.RoomId == roomId
            && BlocksRoom(existing.Status)
            && Overlaps(existing.EffectivePeriod, desiredPeriod);
    }
}