using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Errors;

/// <summary>
/// Samlet katalog over de fejlkoder Application-laget kan returnere (A-09).
/// </summary>
/// <remarks>
/// <b>Domænet ejer sine egne koder.</b> Konstanterne herunder <i>genbruger</i> dem
/// (<c>= BookingErrors.StartDateInPast</c>) frem for at opfinde parallelle koder — så kan de
/// to lag ikke drive fra hinanden, og en søgning på koden rammer både reglen og dens
/// oversættelse.
/// <para>
/// Koderne der ender på <c>.invalid_state</c> er dem
/// <see cref="InvalidStateTransitionException"/> genererer af entitetsnavn og metodenavn.
/// De står her, så UI og Fase 3's ledger-audit kan opregne dem uden at gætte formen.
/// </para>
/// <para>
/// Kun koderne der begynder med noget domænet ikke kender — "findes ikke", "rummet er
/// optaget", "samtidighedskonflikt" — er defineret her fra bunden. De hører til
/// use casen, ikke til invarianten.
/// </para>
/// </remarks>
public static class ErrorCodes
{
    /// <summary>Fejlkoder for bookinger.</summary>
    public static class Booking
    {
        /// <summary>Bookingen findes ikke (BR-115).</summary>
        public const string NotFound = "booking.not_found";

        /// <summary>Ankomstdatoen er ikke udfyldt (BR-101).</summary>
        public const string StartDateRequired = DateRange.StartRequired;

        /// <summary>Afrejsedatoen er ikke udfyldt (BR-102).</summary>
        public const string EndDateRequired = DateRange.EndRequired;

        /// <summary>Afrejsedatoen er ikke efter ankomstdatoen (BR-38, BR-43, BR-88, BR-103).</summary>
        public const string EndNotAfterStart = DateRange.EndNotAfterStart;

        /// <summary>Perioden starter før i dag (BR-44, BR-104, BR-108).</summary>
        public const string StartDateInPast = BookingErrors.StartDateInPast;

        /// <summary>Bookingen mangler en rumreference (BR-45, BR-105).</summary>
        public const string RoomRequired = BookingErrors.RoomRequired;

        /// <summary>Bookingen mangler en gæstereference (BR-106).</summary>
        public const string GuestRequired = BookingErrors.GuestRequired;

        /// <summary>Udtjekningstidspunktet er ikke efter indtjekningstidspunktet (BR-107).</summary>
        public const string CheckOutBeforeCheckIn = BookingErrors.CheckOutBeforeCheckIn;

        /// <summary>Udtjekningsdatoen ligger før ankomstdatoen.</summary>
        public const string CheckOutDateBeforeStartDate = BookingErrors.CheckOutDateBeforeStartDate;

        /// <summary>
        /// Udtjekningsdatoen ligger efter i dag.
        /// </summary>
        /// <remarks>
        /// En dato i fremtiden ville skubbe bookingens effektive slutdato frem og spærre
        /// rummet indtil da (A-01) — og tastefejlen kan ikke rettes bagefter, fordi en
        /// <c>CheckedOut</c> booking hverken kan omlægges eller annulleres.
        /// </remarks>
        public const string CheckOutDateInFuture = BookingErrors.CheckOutDateInFuture;

        /// <summary>Bookingen må ikke bekræftes fra sin nuværende status (BR-02, BR-121).</summary>
        public const string ConfirmNotAllowed = "booking.confirm.invalid_state";

        /// <summary>Bookingen må ikke tjekkes ind nu (BR-06, BR-07).</summary>
        public const string CheckInNotAllowed = "booking.check_in.invalid_state";

        /// <summary>Bookingen må ikke tjekkes ud (BR-09, BR-10).</summary>
        public const string CheckOutNotAllowed = "booking.check_out.invalid_state";

        /// <summary>Bookingen må ikke annulleres (BR-11, BR-122).</summary>
        public const string CancelNotAllowed = "booking.cancel.invalid_state";

        /// <summary>Bookingen må ikke flyttes (BR-14, BR-123).</summary>
        public const string RescheduleNotAllowed = "booking.reschedule.invalid_state";

        /// <summary>Rummet er allerede optaget i den ønskede periode (BR-19, B-03).</summary>
        /// <remarks>
        /// Samme kode uanset om servicetjekket eller databasens exclusion constraint
        /// stoppede den — forskellen er kun synlig i loggen.
        /// </remarks>
        public const string RoomNotAvailable = "booking.room_not_available";

        /// <summary>Rummet kan ikke udlejes: det er ude af drift eller spærret (BR-110).</summary>
        public const string RoomNotBookable = "booking.room_not_bookable";

        /// <summary>Der er hverken valgt en eksisterende gæst eller angivet en ny — eller begge dele (BR-47).</summary>
        public const string GuestSelectionInvalid = "booking.guest_selection_invalid";

        /// <summary>Bookingen blev ændret af en anden imens. Hent igen og prøv forfra.</summary>
        public const string ConcurrencyConflict = "booking.concurrency_conflict";
    }

    /// <summary>Fejlkoder for rum.</summary>
    public static class Room
    {
        /// <summary>Rummet findes ikke (BR-115).</summary>
        public const string NotFound = "room.not_found";

        /// <summary>Værelsesnummer mangler (BR-97).</summary>
        public const string RoomNumberRequired = RoomErrors.RoomNumberRequired;

        /// <summary>Værelsesnummeret er for langt (A-10, BR-N-03).</summary>
        public const string RoomNumberTooLong = RoomErrors.RoomNumberTooLong;

        /// <summary>Etagen er ikke større end 0 (BR-98).</summary>
        public const string FloorInvalid = RoomErrors.FloorInvalid;

        /// <summary>Værelsestypen er ikke en defineret værdi (BR-99).</summary>
        public const string SizeInvalid = RoomErrors.SizeInvalid;

        /// <summary>Kapaciteten er ikke større end 0 (BR-100).</summary>
        public const string CapacityInvalid = RoomErrors.CapacityInvalid;

        /// <summary>Et andet rum har allerede det værelsesnummer (BR-N-01).</summary>
        public const string RoomNumberAlreadyExists = "room.room_number_already_exists";

        /// <summary>Rummet kan ikke slettes, fordi der findes bookinger på det (BR-78, BR-112).</summary>
        public const string HasBookings = "room.has_bookings";

        /// <summary>Rummet kan ikke sættes til daglig rengøring fra sin nuværende stand (BR-N-06).</summary>
        public const string MarkDailyCleaningDueNotAllowed = "room.mark_daily_cleaning_due.invalid_state";

        /// <summary>Rummet kan ikke sættes til slutrengøring fra sin nuværende stand (BR-N-06).</summary>
        public const string MarkDepartureCleaningDueNotAllowed = "room.mark_departure_cleaning_due.invalid_state";

        /// <summary>Rengøringen kan ikke påbegyndes: rummet afventer ikke rengøring (BR-N-06).</summary>
        public const string StartCleaningNotAllowed = "room.start_cleaning.invalid_state";

        /// <summary>Rengøringen kan ikke afsluttes: den er ikke i gang (BR-N-06).</summary>
        public const string CompleteCleaningNotAllowed = "room.complete_cleaning.invalid_state";

        /// <summary>Service kan ikke rapporteres: den er allerede i gang (BR-N-06).</summary>
        public const string ReportServiceNeededNotAllowed = "room.report_service_needed.invalid_state";

        /// <summary>Servicen kan ikke påbegyndes: rummet afventer ikke service (BR-N-06).</summary>
        public const string StartServiceNotAllowed = "room.start_service.invalid_state";

        /// <summary>Servicen kan ikke afsluttes: den er ikke i gang (BR-N-06).</summary>
        public const string CompleteServiceNotAllowed = "room.complete_service.invalid_state";

        /// <summary>Rummet blev ændret af en anden imens. Hent igen og prøv forfra.</summary>
        public const string ConcurrencyConflict = "room.concurrency_conflict";
    }

    /// <summary>Fejlkoder for helligdagskalenderen hos analysetjenesten.</summary>
    /// <remarks>
    /// Koderne beskriver hvad kalderen kan gøre ved fejlen, ikke HTTP-detaljerne:
    /// <see cref="NotConfigured"/> og <see cref="Unauthorized"/> er en driftsfejl,
    /// <see cref="Unavailable"/> er værd at prøve igen senere.
    /// </remarks>
    public static class Holidays
    {
        /// <summary>Tjenestens adresse eller API-nøgle er ikke sat i konfigurationen.</summary>
        public const string NotConfigured = "holidays.not_configured";

        /// <summary>Tjenesten afviste API-nøglen (HTTP 401/403).</summary>
        public const string Unauthorized = "holidays.unauthorized";

        /// <summary>Tjenesten kunne ikke nås, svarede ikke i tide eller fejlede (netværk, timeout, HTTP 5xx).</summary>
        public const string Unavailable = "holidays.unavailable";

        /// <summary>Årstallene er ugyldige (fx første år efter sidste år), eller tjenesten afviste dem (HTTP 400/422).</summary>
        public const string InvalidRequest = "holidays.invalid_request";

        /// <summary>Tjenesten svarede noget vi ikke kan bruge (uventet status eller ulæseligt indhold).</summary>
        public const string UnexpectedResponse = "holidays.unexpected_response";
    }

    /// <summary>Fejlkoder for gæster.</summary>
    public static class Guest
    {
        /// <summary>Gæsten findes ikke (BR-115).</summary>
        public const string NotFound = "guest.not_found";

        /// <summary>Fornavn mangler (BR-52, BR-91).</summary>
        public const string FirstNameRequired = GuestErrors.FirstNameRequired;

        /// <summary>Fornavnet er for langt (A-10, BR-N-03).</summary>
        public const string FirstNameTooLong = GuestErrors.FirstNameTooLong;

        /// <summary>Efternavn mangler (BR-53, BR-92).</summary>
        public const string LastNameRequired = GuestErrors.LastNameRequired;

        /// <summary>Efternavnet er for langt (A-10, BR-N-03).</summary>
        public const string LastNameTooLong = GuestErrors.LastNameTooLong;

        /// <summary>E-mail mangler (BR-93).</summary>
        public const string EmailRequired = GuestErrors.EmailRequired;

        /// <summary>E-mailen mangler '@' eller '.' (BR-55, BR-93).</summary>
        public const string EmailInvalid = GuestErrors.EmailInvalid;

        /// <summary>E-mailen er for lang (A-10, BR-N-03).</summary>
        public const string EmailTooLong = GuestErrors.EmailTooLong;

        /// <summary>Telefonnummer mangler (BR-54, BR-94).</summary>
        public const string PhoneNumberRequired = GuestErrors.PhoneNumberRequired;

        /// <summary>Telefonnummeret er for langt (A-10, BR-N-03).</summary>
        public const string PhoneNumberTooLong = GuestErrors.PhoneNumberTooLong;

        /// <summary>Land mangler (BR-56, BR-95).</summary>
        public const string CountryRequired = GuestErrors.CountryRequired;

        /// <summary>Landet er for langt (A-10, BR-N-03).</summary>
        public const string CountryTooLong = GuestErrors.CountryTooLong;

        /// <summary>Pasnummeret er for langt (A-10, BR-N-03).</summary>
        public const string PassportNumberTooLong = GuestErrors.PassportNumberTooLong;

        /// <summary>Gæsten kan ikke slettes, fordi der findes bookinger på hende (BR-N-02).</summary>
        public const string HasBookings = "guest.has_bookings";

        /// <summary>Gæsten blev ændret af en anden imens. Hent igen og prøv forfra.</summary>
        public const string ConcurrencyConflict = "guest.concurrency_conflict";
    }
}