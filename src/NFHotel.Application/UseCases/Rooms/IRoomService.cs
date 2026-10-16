using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;

namespace NFHotel.Application.UseCases.Rooms;

/// <summary>
/// Use cases for rum: stamdata, bookbarhed og rengøringsforløb.
/// </summary>
/// <remarks>
/// Der er <b>ingen generisk mål-status-metode</b> (A-07). Hver domæneovergang har sin egen
/// metode. En metode af formen <c>ChangeHousekeepingStatusAsync(newStatus)</c> ville tvinge
/// servicen til at oversætte (nuværende, ønsket) til et metodekald via en tabel — altså
/// reimplementere tilstandsmaskinen ét lag højere end den hører hjemme.
/// <para>
/// Alle overgange er idempotente for samme måltilstand (A-08): at markere et rum rent to
/// gange er en no-op, ikke en fejl. Mobilappen har ingen offline-understøttelse og retry'er
/// over ustabilt netværk; en retry må ikke give en fejl til rengøringspersonalet.
/// </para>
/// <para>
/// <b>Roller er dokumentation i Fase 1 (A-12).</b> Kolonnen "hvem må" i designet beskriver
/// hvem der <i>vil få lov</i> når autorisation bygges. Her kan enhver kalde enhver overgang.
/// </para>
/// </remarks>
public interface IRoomService
{
    // --------------------------------------------------------
    // LÆSNING
    // --------------------------------------------------------

    /// <summary>
    /// Henter ét rum (BR-115).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Rummets detaljer, eller <see cref="ErrorCodes.Room.NotFound"/>.</returns>
    Task<Result<RoomDetailsDto>> GetByIdAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Henter alle rum.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Alle rum som listerækker.</returns>
    Task<Result<IReadOnlyList<RoomListItemDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Søger rum med filter (BR-74, BR-76).
    /// </summary>
    /// <param name="filter">Filteret. Et tomt filter svarer til "ryd filter" og giver alle rum.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>De matchende rum.</returns>
    Task<Result<IReadOnlyList<RoomListItemDto>>> SearchAsync(
        RoomFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Henter filterets valgmuligheder (BR-75, BR-126).
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Etager fra data; størrelse, status og rengøringsstand fra enums.</returns>
    Task<Result<RoomFilterOptionsDto>> GetFilterOptionsAsync(CancellationToken cancellationToken = default);

    // --------------------------------------------------------
    // STAMDATA
    // --------------------------------------------------------

    /// <summary>
    /// Opretter et rum (BR-81, BR-84, BR-97, BR-98, BR-99, BR-100, BR-113, BR-N-01).
    /// </summary>
    /// <param name="request">Rummets stamdata.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Det oprettede rum, eller alle valideringsfejl på én gang.</returns>
    Task<Result<RoomDetailsDto>> CreateAsync(
        CreateRoomRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retter et rums stamdata (BR-82, BR-84, BR-97 til BR-100, BR-113, BR-N-01).
    /// </summary>
    /// <param name="request">De nye stamdata. Bærer ikke status (A-08).</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Det opdaterede rum, eller alle valideringsfejl på én gang.</returns>
    Task<Result<RoomDetailsDto>> UpdateAsync(
        UpdateRoomRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sletter et rum (BR-78, BR-112).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Succes, eller <see cref="ErrorCodes.Room.HasBookings"/> hvis rummet er i brug.</returns>
    Task<Result> DeleteAsync(int roomId, CancellationToken cancellationToken = default);

    // --------------------------------------------------------
    // BOOKBARHED (RoomStatus) — B-04, A-07, A-08
    // --------------------------------------------------------

    /// <summary>
    /// Tager rummet ud af drift på ubestemt tid (BR-110, BR-126). Rummet kan ikke bookes.
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Rummets detaljer efter overgangen. Er en no-op hvis rummet allerede er ude af drift.</returns>
    Task<Result<RoomDetailsDto>> TakeOutOfServiceAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Spærrer rummet midlertidigt på grund af en fejl der gør det ubeboeligt (BR-110, BR-126).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Rummets detaljer efter overgangen. Er en no-op hvis rummet allerede er spærret.</returns>
    Task<Result<RoomDetailsDto>> SendToMaintenanceAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gør rummet bookbart igen (BR-110, BR-126). Rører ikke rengøringsstanden (B-04).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Rummets detaljer efter overgangen. Er en no-op hvis rummet allerede er bookbart.</returns>
    Task<Result<RoomDetailsDto>> ReturnToServiceAsync(int roomId, CancellationToken cancellationToken = default);

    // --------------------------------------------------------
    // RENGØRING OG SERVICE (HousekeepingStatus) — BR-N-06, A-07
    // --------------------------------------------------------

    /// <summary>
    /// Markerer at det beboede rum afventer daglig rengøring (BR-N-06).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>
    /// Rummets detaljer, eller
    /// <see cref="ErrorCodes.Room.MarkDailyCleaningDueNotAllowed"/> hvis rummet afventer
    /// slutrengøring eller er under rengøring eller service — slutrengøring må ikke nedgraderes.
    /// </returns>
    Task<Result<RoomDetailsDto>> MarkDailyCleaningDueAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Markerer at rummet afventer slutrengøring efter afrejse (BR-N-06).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>
    /// Rummets detaljer, eller
    /// <see cref="ErrorCodes.Room.MarkDepartureCleaningDueNotAllowed"/> hvis rummet er under
    /// rengøring eller service.
    /// </returns>
    /// <remarks>
    /// Kaldes også automatisk af <c>BookingService.CheckOutAsync</c> umiddelbart efter en
    /// udtjekning; denne use case findes for de gange personalet gør det manuelt.
    /// </remarks>
    Task<Result<RoomDetailsDto>> MarkDepartureCleaningDueAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rengøringspersonalet kvitterer og går i gang (BR-N-06).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>
    /// Rummets detaljer, eller <see cref="ErrorCodes.Room.StartCleaningNotAllowed"/> hvis
    /// rummet ikke afventer rengøring.
    /// </returns>
    Task<Result<RoomDetailsDto>> StartCleaningAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rengøringen er færdig; rummet er rent (BR-N-06).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>
    /// Rummets detaljer, eller <see cref="ErrorCodes.Room.CompleteCleaningNotAllowed"/> hvis
    /// rengøringen ikke er i gang.
    /// </returns>
    Task<Result<RoomDetailsDto>> CompleteCleaningAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rapporterer at rummet skal ses af en servicetekniker (BR-N-06).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>
    /// Rummets detaljer, eller <see cref="ErrorCodes.Room.ReportServiceNeededNotAllowed"/>
    /// hvis service allerede er i gang.
    /// </returns>
    /// <remarks>
    /// Spærrer ikke rummet for booking — det kræver <see cref="SendToMaintenanceAsync"/> (B-04).
    /// </remarks>
    Task<Result<RoomDetailsDto>> ReportServiceNeededAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Serviceteknikeren kvitterer og går i gang (BR-N-06).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>
    /// Rummets detaljer, eller <see cref="ErrorCodes.Room.StartServiceNotAllowed"/> hvis
    /// rummet ikke afventer service.
    /// </returns>
    Task<Result<RoomDetailsDto>> StartServiceAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Servicen er afsluttet (BR-N-06).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="requiresCleaning">
    /// Sand hvis arbejdet har efterladt rummet snavset; rummet sættes da til slutrengøring
    /// i stedet for rent.
    /// </param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>
    /// Rummets detaljer, eller <see cref="ErrorCodes.Room.CompleteServiceNotAllowed"/> hvis
    /// servicen ikke er i gang.
    /// </returns>
    Task<Result<RoomDetailsDto>> CompleteServiceAsync(
        int roomId,
        bool requiresCleaning,
        CancellationToken cancellationToken = default);
}