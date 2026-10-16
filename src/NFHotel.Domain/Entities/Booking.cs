using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Entities;

/// <summary>
/// En reservation af ét rum til én gæst i én periode. Aggregatrod.
/// </summary>
/// <remarks>
/// Bookingen kender kun sine egne felter. Regler der kræver kendskab til <i>andre</i>
/// bookinger — fx overlap ved oprettelse (BR-19) — hører i Application-laget, som kalder
/// <see cref="Booking.ConflictsWith"/>.
/// <para>
/// Domænet kender ikke uret: "i dag" (<c>today</c>) og faktiske tidsstempler
/// (<c>occurredAt</c>) leveres altid som parametre. Det gør tilstandsmaskinen deterministisk
/// testbar og holder tidszonepolitikken ét sted i Application (A-05).
/// </para>
/// </remarks>
public sealed class Booking : Entity<int>
{
    private const string EntityName = "booking";
    private const string BookingNumberPrefix = "FLZ-";
    private const string BookingNumberFormat = "D6";

    /// <summary>Til EF Core. Brug <see cref="Create"/> i kode.</summary>
    private Booking()
    {
    }

    /// <summary>Ankomstdato uden klokkeslæt (B-08).</summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>
    /// Afrejsedato uden klokkeslæt, eksklusiv (B-08).
    /// </summary>
    /// <remarks>
    /// Den <i>bookede</i> slutdato. Ændres ALDRIG af en udtjekning (A-01) — den er
    /// historik. Det er <see cref="EffectiveEndDate"/> der styrer belægningen.
    /// </remarks>
    public DateOnly EndDate { get; private set; }

    /// <summary>Faktisk indtjekningstidspunkt. Null indtil check-in (B-08).</summary>
    public DateTimeOffset? CheckInTime { get; private set; }

    /// <summary>Faktisk udtjekningstidspunkt. Null indtil check-ud (B-08).</summary>
    public DateTimeOffset? CheckOutTime { get; private set; }

    /// <summary>
    /// Den hotel-lokale dato gæsten faktisk tjekkede ud. Null indtil check-ud.
    /// </summary>
    /// <remarks>
    /// Selvstændigt felt, ikke afledt af <see cref="CheckOutTime"/>: omregningen
    /// tidsstempel → hotel-lokal dato er en tidszonepolitik som hverken Domain eller
    /// databasen kan udføre deterministisk (A-02, A-03). Datoen leveres derfor udefra.
    /// </remarks>
    public DateOnly? CheckOutDate { get; private set; }

    /// <summary>Bookingens livscyklustilstand. Ændres kun via metoderne på klassen.</summary>
    public BookingStatus Status { get; private set; }

    /// <summary>Fremmednøgle til rummet. Altid større end 0 (BR-45, BR-105).</summary>
    public int RoomId { get; private set; }

    /// <summary>Fremmednøgle til gæsten. Altid større end 0 (BR-106).</summary>
    public int GuestId { get; private set; }

    /// <summary>
    /// Navigation til rummet. Kun udfyldt når kaldet har bedt om den; brug den aldrig som guard.
    /// </summary>
    public Room? Room { get; private set; }

    /// <summary>
    /// Navigation til gæsten. Kun udfyldt når kaldet har bedt om den; brug den aldrig som guard.
    /// </summary>
    public Guest? Guest { get; private set; }

    /// <summary>Den bookede periode som value object. Historik — bruges ikke til overlapstjek.</summary>
    public DateRange Period => new(StartDate, EndDate);

    /// <summary>
    /// Den slutdato bookingen faktisk lægger beslag på rummet til (A-01):
    /// <c>GREATEST(StartDate + 1 dag, COALESCE(CheckOutDate, EndDate))</c>.
    /// </summary>
    /// <remarks>
    /// Tidlig udtjekning frigiver dermed automatisk de resterende nætter, uden at
    /// <see cref="EndDate"/> røres. <c>StartDate + 1</c> er gulvet, så en udtjekning samme
    /// dag som ankomsten ikke giver en tom periode — databasens genererede kolonne
    /// <c>effective_end_date</c> beregner præcis det samme udtryk.
    /// </remarks>
    public DateOnly EffectiveEndDate
    {
        get
        {
            var actualEnd = CheckOutDate ?? EndDate;
            var minimumEnd = StartDate.AddDays(1);

            return actualEnd > minimumEnd ? actualEnd : minimumEnd;
        }
    }

    /// <summary>
    /// Den periode bookingen faktisk blokerer rummet i: <c>[StartDate, EffectiveEndDate)</c>.
    /// Dette er periodetypen overlapstjek skal bruge — ikke <see cref="Period"/> (A-01).
    /// </summary>
    public DateRange EffectivePeriod => new(StartDate, EffectiveEndDate);

    /// <summary>Antal bookede overnatninger (BR-109, BR-117).</summary>
    public int NumberOfNights => Period.Nights;

    /// <summary>
    /// Vist bookingnummer, fx <c>FLZ-000123</c> (BR-109). Afledt, aldrig persisteret.
    /// </summary>
    public string BookingNumber => $"{BookingNumberPrefix}{Id.ToString(BookingNumberFormat)}";

    /// <summary>Sand hvis bookingen må bekræftes (BR-02, BR-121).</summary>
    public bool CanConfirm => Status == BookingStatus.Pending;

    /// <summary>Sand hvis bookingen må tjekkes ud (BR-09).</summary>
    public bool CanCheckOut =>
        Status == BookingStatus.CheckedIn && CheckInTime is not null && CheckOutTime is null;

    /// <summary>Sand hvis bookingen må annulleres (BR-11, BR-122).</summary>
    public bool CanCancel => Status is BookingStatus.Pending or BookingStatus.Confirmed;

    /// <summary>Sand hvis bookingens datoer og rum må ændres (BR-14, BR-123, A-06).</summary>
    public bool CanReschedule => Status is BookingStatus.Pending or BookingStatus.Confirmed;

    /// <summary>
    /// Afgør om bookingen må tjekkes ind på den angivne dato (BR-06).
    /// </summary>
    /// <param name="today">Dagens dato i hotellets tidszone.</param>
    /// <returns>Sand hvis check-in er tilladt. Bemærk B-01: <c>Confirmed</c> er ikke et krav.</returns>
    public bool CanCheckIn(DateOnly today) =>
        (Status is BookingStatus.Pending or BookingStatus.Confirmed)
        && CheckInTime is null
        && StartDate <= today;

    /// <summary>
    /// Opretter en ny booking med status <see cref="BookingStatus.Pending"/> (BR-01, BR-48).
    /// </summary>
    /// <param name="period">Bookingperioden. Skal starte på eller efter <paramref name="today"/>.</param>
    /// <param name="roomId">Rummets id. Skal være større end 0.</param>
    /// <param name="guestId">Gæstens id. Skal være større end 0.</param>
    /// <param name="today">Dagens dato i hotellets tidszone. Leveres af kaldet — Domain kender ikke uret.</param>
    /// <returns>En ny, gyldig booking der endnu ikke er persisteret.</returns>
    /// <exception cref="DomainException">
    /// Ved startdato i fortiden (BR-44, BR-104), manglende rum (BR-45, BR-105) eller
    /// manglende gæst (BR-106).
    /// </exception>
    public static Booking Create(DateRange period, int roomId, int guestId, DateOnly today)
    {
        GuardPeriodIsNotInThePast(period, today);
        GuardRoomId(roomId);
        GuardGuestId(guestId);

        return new Booking
        {
            StartDate = period.Start,
            EndDate = period.End,
            RoomId = roomId,
            GuestId = guestId,
            Status = BookingStatus.Pending
        };
    }

    /// <summary>
    /// Bekræfter bookingen — betaling er garanteret (BR-02, BR-03, BR-05).
    /// Bekræftelse er ikke en forudsætning for check-in (B-01).
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">
    /// Kastes hvis status ikke er præcis <see cref="BookingStatus.Pending"/>.
    /// </exception>
    public void Confirm()
    {
        if (!CanConfirm)
        {
            throw InvalidStateTransitionException.For(EntityName, nameof(Confirm), Status);
        }

        Status = BookingStatus.Confirmed;
    }

    /// <summary>
    /// Tjekker gæsten ind (BR-06, BR-07). Tilladt fra både <see cref="BookingStatus.Pending"/>
    /// og <see cref="BookingStatus.Confirmed"/> (B-01).
    /// </summary>
    /// <param name="occurredAt">Det faktiske indtjekningstidspunkt.</param>
    /// <param name="today">
    /// Dagens dato i hotellets tidszone. Leveres separat, fordi omregningen fra tidsstempel
    /// til lokal kalenderdato er en tidszonepolitik der hører i Application-laget.
    /// </param>
    /// <exception cref="InvalidStateTransitionException">
    /// Kastes hvis status hverken er Pending eller Confirmed, hvis <see cref="CheckInTime"/>
    /// allerede er sat, eller hvis <see cref="StartDate"/> ligger efter <paramref name="today"/>.
    /// </exception>
    public void CheckIn(DateTimeOffset occurredAt, DateOnly today)
    {
        if (!CanCheckIn(today))
        {
            throw InvalidStateTransitionException.For(EntityName, nameof(CheckIn), Status);
        }

        CheckInTime = occurredAt;
        Status = BookingStatus.CheckedIn;
    }

    /// <summary>
    /// Tjekker gæsten ud (BR-09, BR-10).
    /// </summary>
    /// <param name="occurredAt">
    /// Det faktiske udtjekningstidspunkt. Skal være strengt efter <see cref="CheckInTime"/> (BR-107).
    /// </param>
    /// <param name="checkOutDate">
    /// Den hotel-lokale dato udtjekningen skete på (A-03). Indgår i
    /// <see cref="EffectiveEndDate"/> og frigiver dermed resterende nætter ved tidlig
    /// udtjekning, uden at <see cref="EndDate"/> ændres (A-01).
    /// </param>
    /// <param name="today">
    /// Dagens dato i hotellets tidszone — parallelt med <see cref="CheckIn"/>. Leveres af
    /// kaldet, fordi Domain aldrig selv må kende "i dag".
    /// </param>
    /// <exception cref="InvalidStateTransitionException">
    /// Kastes hvis status ikke er <see cref="BookingStatus.CheckedIn"/>, eller hvis
    /// <see cref="CheckOutTime"/> allerede er sat.
    /// </exception>
    /// <exception cref="DomainException">
    /// Kastes hvis <paramref name="occurredAt"/> ikke er efter <see cref="CheckInTime"/>
    /// (BR-107), eller hvis <paramref name="checkOutDate"/> ligger før
    /// <see cref="StartDate"/> eller efter <paramref name="today"/>.
    /// </exception>
    public void CheckOut(DateTimeOffset occurredAt, DateOnly checkOutDate, DateOnly today)
    {
        if (!CanCheckOut)
        {
            throw InvalidStateTransitionException.For(EntityName, nameof(CheckOut), Status);
        }

        if (occurredAt <= CheckInTime)
        {
            throw new DomainException(BookingErrors.CheckOutBeforeCheckIn);
        }

        if (checkOutDate < StartDate)
        {
            throw new DomainException(BookingErrors.CheckOutDateBeforeStartDate);
        }

        // En udtjekningsdato i fremtiden kan ikke rettes bagefter: en CheckedOut booking
        // kan hverken omlægges eller annulleres, og datoen ville spærre rummet frem til
        // den via EffectiveEndDate (A-01). Derfor en øvre grænse, ikke kun en nedre.
        if (checkOutDate > today)
        {
            throw new DomainException(BookingErrors.CheckOutDateInFuture);
        }

        CheckOutTime = occurredAt;
        CheckOutDate = checkOutDate;
        Status = BookingStatus.CheckedOut;
    }

    /// <summary>
    /// Annullerer bookingen (BR-11, BR-13). Soft-delete — rækken slettes aldrig.
    /// Eneste overgang der frigiver rummet helt (A-01).
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">
    /// Kastes hvis status hverken er Pending eller Confirmed. En booking der er tjekket
    /// ind eller ud kan ikke annulleres.
    /// </exception>
    public void Cancel()
    {
        if (!CanCancel)
        {
            throw InvalidStateTransitionException.For(EntityName, nameof(Cancel), Status);
        }

        Status = BookingStatus.Cancelled;
    }

    /// <summary>
    /// Flytter bookingen til en ny periode og/eller et nyt rum (BR-14, BR-17, BR-18, BR-20).
    /// </summary>
    /// <param name="newPeriod">Den nye periode. Skal starte på eller efter <paramref name="today"/> (BR-108).</param>
    /// <param name="newRoomId">Det nye rums id. Skal være større end 0.</param>
    /// <param name="today">Dagens dato i hotellets tidszone.</param>
    /// <exception cref="InvalidStateTransitionException">
    /// Kastes hvis status hverken er Pending eller Confirmed.
    /// </exception>
    /// <exception cref="DomainException">
    /// Kastes hvis den nye periode starter i fortiden (BR-108) eller rummets id ikke er gyldigt.
    /// </exception>
    /// <remarks>
    /// Kontrollerer IKKE overlap mod andre bookinger — det kræver kendskab til andre
    /// bookinger og hører derfor i Application-laget (BR-19).
    /// </remarks>
    public void Reschedule(DateRange newPeriod, int newRoomId, DateOnly today)
    {
        if (!CanReschedule)
        {
            throw InvalidStateTransitionException.For(EntityName, nameof(Reschedule), Status);
        }

        GuardPeriodIsNotInThePast(newPeriod, today);
        GuardRoomId(newRoomId);

        StartDate = newPeriod.Start;
        EndDate = newPeriod.End;
        RoomId = newRoomId;
    }

    /// <summary>Afviser en periode der starter før i dag (BR-44, BR-104, BR-108).</summary>
    private static void GuardPeriodIsNotInThePast(DateRange period, DateOnly today)
    {
        if (!period.StartsOnOrAfter(today))
        {
            throw new DomainException(BookingErrors.StartDateInPast);
        }
    }

    /// <summary>Afviser en manglende rumreference (BR-45, BR-105).</summary>
    private static void GuardRoomId(int roomId)
    {
        if (roomId <= 0)
        {
            throw new DomainException(BookingErrors.RoomRequired);
        }
    }

    /// <summary>Afviser en manglende gæstereference (BR-106).</summary>
    private static void GuardGuestId(int guestId)
    {
        if (guestId <= 0)
        {
            throw new DomainException(BookingErrors.GuestRequired);
        }
    }

    /// <summary>
    /// Afgør om denne booking er i konflikt med en ønsket periode på et rum (BR-19).
    /// </summary>
    /// <param name="roomId">Rummet den ønskede periode gælder.</param>
    /// <param name="desiredPeriod">Den ønskede periode.</param>
    /// <returns>Sand hvis denne booking spærrer for den ønskede periode.</returns>
    /// <remarks>
    /// Sammenligningen sker mod <see cref="EffectivePeriod"/>, ikke <see cref="Period"/>
    /// (A-01): en gæst der rejser tidligt skal frigive de resterende nætter. Kaldet er
    /// ansvarligt for at ekskludere bookingen selv ved redigering. Perioden er halvåben
    /// (B-03, BR-39): afrejse og ankomst samme dag er ikke overlap.
    /// </remarks>
    public bool ConflictsWith(int roomId, DateRange desiredPeriod) =>
        RoomId == roomId
        && Status.BlocksRoom()
        && EffectivePeriod.Overlaps(desiredPeriod);
}