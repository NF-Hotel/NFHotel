using NFHotel.Domain.Errors;

namespace NFHotel.Domain.ValueObjects;

/// <summary>
/// A booking period: arrival date (inclusive) to departure date (exclusive).
/// </summary>
/// <remarks>
/// Half-open interval <c>[Start, End)</c>, so departure and arrival on the same day are not an
/// overlap (B-03, BR-39). The type gathers the rules BR-38, BR-43, BR-88, BR-101, BR-102,
/// BR-103, BR-109 and BR-117 in one place.
/// <para>
/// Note: <c>default(DateRange)</c> can, as with any struct, be constructed bypassing
/// the constructor and then has <see cref="Start"/> = <see cref="End"/> = <c>default</c>.
/// The domain never produces that value itself — the entities always build the period
/// through the constructor.
/// </para>
/// </remarks>
public readonly record struct DateRange
{
    /// <summary>Error code: the arrival date is not filled in (BR-101).</summary>
    public const string StartRequired = "date_range.start_required";

    /// <summary>Error code: the departure date is not filled in (BR-102).</summary>
    public const string EndRequired = "date_range.end_required";

    /// <summary>Error code: the departure date is not strictly after the arrival date (BR-38, BR-43, BR-88, BR-103).</summary>
    public const string EndNotAfterStart = "date_range.end_not_after_start";

    /// <summary>
    /// Creates a period.
    /// </summary>
    /// <param name="start">Arrival date, inclusive.</param>
    /// <param name="end">Departure date, exclusive. Must be strictly after <paramref name="start"/>.</param>
    /// <exception cref="DomainException">
    /// Thrown if a date is <c>default</c> (BR-101, BR-102), or if
    /// <paramref name="end"/> is not after <paramref name="start"/> — 0 nights is
    /// not allowed (BR-38, BR-43, BR-88, BR-103).
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

    /// <summary>Arrival date, inclusive.</summary>
    public DateOnly Start { get; }

    /// <summary>Departure date, exclusive. Always strictly greater than <see cref="Start"/>.</summary>
    public DateOnly End { get; }

    /// <summary>Number of nights. Replaces the old <c>Booking.NumberOfNights</c> (BR-109, BR-117).</summary>
    public int Nights => End.DayNumber - Start.DayNumber;

    /// <summary>
    /// Half-open overlap test against another period (B-03, BR-39).
    /// </summary>
    /// <param name="other">The period to compare with.</param>
    /// <returns>True if the periods share at least one night.</returns>
    public bool Overlaps(DateRange other) => Start < other.End && End > other.Start;

    /// <summary>
    /// Decides whether a date lies in the period.
    /// </summary>
    /// <param name="date">The date being checked.</param>
    /// <returns>True if <paramref name="date"/> lies in <c>[Start, End)</c>.</returns>
    public bool Contains(DateOnly date) => date >= Start && date < End;

    /// <summary>
    /// Decides whether the period lies in the present or the future (BR-44, BR-104).
    /// </summary>
    /// <param name="today">Today's date in the hotel's time zone. Supplied by the caller — the domain does not know the clock.</param>
    /// <returns>True if the period starts on or after <paramref name="today"/>.</returns>
    public bool StartsOnOrAfter(DateOnly today) => Start >= today;
}
