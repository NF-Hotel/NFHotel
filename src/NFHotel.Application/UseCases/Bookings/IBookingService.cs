using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;

namespace NFHotel.Application.UseCases.Bookings;

/// <summary>
/// Use cases for bookinger. Bærer flest forretningsregler af de tre services.
/// </summary>
/// <remarks>
/// Testen for hver eneste metode: <i>kan en konsol-app kalde den og få alle regler
/// håndhævet?</i> Ingen UI-typer, ingen dialoger, ingen <c>Debug.WriteLine</c> — 823 linjer
/// logik i én ViewModel var det gamle systems kernefejl (B-10).
/// <para>
/// Bemærk placeringen af <see cref="GetAvailableRoomsAsync"/>: den returnerer rum, men
/// reglen er en <i>bookingregel</i> (overlap i en periode). Lå den på <c>IRoomService</c>,
/// skulle den kende <c>IBookingRepository</c>, og så havde begge services begge repositories.
/// </para>
/// <para>
/// Der er ingen <c>SalesService</c> i Fase 1: BR-31 sætter altid omsætningen til 0, og der
/// findes ingen pris i domænet. En service der returnerer 0 er værre end ingen — den ligner
/// en implementering.
/// </para>
/// </remarks>
public interface IBookingService
{
    /// <summary>
    /// Henter én booking (BR-115).
    /// </summary>
    /// <param name="bookingId">Bookingens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Bookingens detaljer, eller <see cref="ErrorCodes.Booking.NotFound"/>.</returns>
    Task<Result<BookingDetailsDto>> GetByIdAsync(int bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Henter bookingoversigten (BR-23, BR-24, BR-25, BR-32, BR-111).
    /// </summary>
    /// <param name="query">Datovindue, fritekst, sortering og om annullerede skal med.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Oversigtens rækker, sorteret som forespurgt.</returns>
    /// <remarks>
    /// Periodefilteret er altid aktivt. Det retter fejlen hvor uge- og månedsvisningen slet
    /// ikke filtrerede og derfor hentede hele tabellen (BR-111).
    /// </remarks>
    Task<Result<IReadOnlyList<BookingListItemDto>>> GetOverviewAsync(
        BookingOverviewQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finder ledige rum i en periode (BR-34, BR-36, BR-37, BR-38, BR-39, BR-40, BR-110).
    /// </summary>
    /// <param name="checkInDate">Ankomstdato, inklusiv.</param>
    /// <param name="checkOutDate">Afrejsedato, eksklusiv.</param>
    /// <param name="excludeBookingId">
    /// Bookingen der redigeres, så den ikke spærrer for sig selv. <c>null</c> ved oprettelse.
    /// Parameteren manglede i det gamle system og gjorde BR-19 unødigt indviklet.
    /// </param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>De rum der både er bookbare (BR-110) og ledige i hele perioden.</returns>
    Task<Result<IReadOnlyList<AvailableRoomDto>>> GetAvailableRoomsAsync(
        DateOnly checkInDate,
        DateOnly checkOutDate,
        int? excludeBookingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opretter en booking (BR-01, BR-19, BR-41 til BR-49, BR-51, BR-101 til BR-106, B-03).
    /// </summary>
    /// <param name="request">Periode, rum og enten en eksisterende eller en ny gæst (BR-47).</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Den oprettede booking, eller de brudte regler.</returns>
    /// <remarks>
    /// Overlapstjekket er <b>nyt</b> — det fandtes hverken i det gamle systems
    /// <c>CreateBooking</c> eller i repoet, så dobbeltbooking var mulig (B-03).
    /// </remarks>
    Task<Result<BookingDetailsDto>> CreateAsync(
        CreateBookingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bekræfter en booking — betaling er garanteret (BR-02, BR-03, BR-05, BR-121).
    /// </summary>
    /// <param name="bookingId">Bookingens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Bookingen, eller <see cref="ErrorCodes.Booking.ConfirmNotAllowed"/>.</returns>
    /// <remarks>Bekræftelsesdialogen (BR-04) er UI og hører i Web.</remarks>
    Task<Result<BookingDetailsDto>> ConfirmAsync(int bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tjekker gæsten ind (BR-06, BR-07, BR-08).
    /// </summary>
    /// <param name="bookingId">Bookingens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Bookingen, eller <see cref="ErrorCodes.Booking.CheckInNotAllowed"/>.</returns>
    /// <remarks>
    /// Walk-in er tilladt: <c>Pending</c> går direkte til <c>CheckedIn</c> uden om
    /// <c>Confirmed</c> (B-01). Tidspunktet kommer fra uret, aldrig fra <c>DateTime.Now</c> (A-05).
    /// </remarks>
    Task<Result<BookingDetailsDto>> CheckInAsync(int bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tjekker gæsten ud (BR-09, BR-10, BR-107, A-01, A-03, BR-N-06).
    /// </summary>
    /// <param name="bookingId">Bookingens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Bookingen, eller <see cref="ErrorCodes.Booking.CheckOutNotAllowed"/>.</returns>
    /// <remarks>
    /// Sætter den hotel-lokale udtjekningsdato, hvilket frigiver resterende nætter ved tidlig
    /// udtjekning uden at røre den bookede slutdato (A-01), og sætter rummet til
    /// slutrengøring (BR-N-06).
    /// </remarks>
    Task<Result<BookingDetailsDto>> CheckOutAsync(int bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Annullerer en booking (BR-11, BR-13, BR-122).
    /// </summary>
    /// <param name="bookingId">Bookingens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Bookingen, eller <see cref="ErrorCodes.Booking.CancelNotAllowed"/>.</returns>
    /// <remarks>
    /// Soft-delete: status sættes, rækken bliver (BR-13). Det er den eneste overgang der
    /// frigiver rummet helt (A-01).
    /// </remarks>
    Task<Result<BookingDetailsDto>> CancelAsync(int bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Flytter en booking til nye datoer og/eller et nyt rum
    /// (BR-14, BR-17, BR-18, BR-19, BR-20, BR-108, BR-123, B-03).
    /// </summary>
    /// <param name="request">Bookingens id, den nye periode og rummet.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Den flyttede booking, eller de brudte regler.</returns>
    /// <remarks>
    /// Bruger samme overlapsfunktion som <see cref="CreateAsync"/>, hvilket lukker
    /// modstriden mellem BR-19 og BR-39.
    /// </remarks>
    Task<Result<BookingDetailsDto>> RescheduleAsync(
        RescheduleBookingRequest request,
        CancellationToken cancellationToken = default);
}