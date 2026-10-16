using NFHotel.Domain.Errors;

namespace NFHotel.Domain.ValueObjects;

/// <summary>
/// En bookingperiode: ankomstdato (inklusiv) til afrejsedato (eksklusiv).
/// </summary>
/// <remarks>
/// Halvåbent interval <c>[Start, End)</c>, så afrejse og ankomst samme dag ikke er
/// overlap (B-03, BR-39). Typen samler reglerne BR-38, BR-43, BR-88, BR-101, BR-102,
/// BR-103, BR-109 og BR-117 ét sted.
/// <para>
/// Bemærk: <c>default(DateRange)</c> kan som for enhver struct konstrueres uden om
/// konstruktøren og har da <see cref="Start"/> = <see cref="End"/> = <c>default</c>.
/// Domænet producerer aldrig den værdi selv — entiteterne bygger altid perioden
/// gennem konstruktøren.
/// </para>
/// </remarks>
public readonly record struct DateRange
{
    /// <summary>Fejlkode: ankomstdatoen er ikke udfyldt (BR-101).</summary>
    public const string StartRequired = "date_range.start_required";

    /// <summary>Fejlkode: afrejsedatoen er ikke udfyldt (BR-102).</summary>
    public const string EndRequired = "date_range.end_required";

    /// <summary>Fejlkode: afrejsedatoen er ikke strengt efter ankomstdatoen (BR-38, BR-43, BR-88, BR-103).</summary>
    public const string EndNotAfterStart = "date_range.end_not_after_start";

    /// <summary>
    /// Opretter en periode.
    /// </summary>
    /// <param name="start">Ankomstdato, inklusiv.</param>
    /// <param name="end">Afrejsedato, eksklusiv. Skal være strengt efter <paramref name="start"/>.</param>
    /// <exception cref="DomainException">
    /// Kastes hvis en dato er <c>default</c> (BR-101, BR-102), eller hvis
    /// <paramref name="end"/> ikke er efter <paramref name="start"/> — 0 nætter er
    /// ikke tilladt (BR-38, BR-43, BR-88, BR-103).
    /// </exception>
    public DateRange(DateOnly start, DateOnly end)
    {
        if (start == default)
        {
            throw new DomainException(StartRequired);
        }

        if (end == default)
        {
            throw new DomainException(EndRequired);
        }

        if (end <= start)
        {
            throw new DomainException(EndNotAfterStart);
        }

        Start = start;
        End = end;
    }

    /// <summary>Ankomstdato, inklusiv.</summary>
    public DateOnly Start { get; }

    /// <summary>Afrejsedato, eksklusiv. Altid strengt større end <see cref="Start"/>.</summary>
    public DateOnly End { get; }

    /// <summary>Antal overnatninger. Erstatter det gamle <c>Booking.NumberOfNights</c> (BR-109, BR-117).</summary>
    public int Nights => End.DayNumber - Start.DayNumber;

    /// <summary>
    /// Halvåben overlapstest mod en anden periode (B-03, BR-39).
    /// </summary>
    /// <param name="other">Perioden der sammenlignes med.</param>
    /// <returns>Sand hvis perioderne deler mindst én nat.</returns>
    public bool Overlaps(DateRange other) => Start < other.End && End > other.Start;

    /// <summary>
    /// Afgør om en dato ligger i perioden.
    /// </summary>
    /// <param name="date">Datoen der undersøges.</param>
    /// <returns>Sand hvis <paramref name="date"/> ligger i <c>[Start, End)</c>.</returns>
    public bool Contains(DateOnly date) => date >= Start && date < End;

    /// <summary>
    /// Afgør om perioden ligger i nutid eller fremtid (BR-44, BR-104).
    /// </summary>
    /// <param name="today">Dagens dato i hotellets tidszone. Leveres af kaldet — Domain kender ikke uret.</param>
    /// <returns>Sand hvis perioden starter på eller efter <paramref name="today"/>.</returns>
    public bool StartsOnOrAfter(DateOnly today) => Start >= today;
}