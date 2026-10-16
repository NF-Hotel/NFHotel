using NFHotel.Application.Dto.Rooms;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Ports;

/// <summary>
/// Læsning af rum som projektioner (A-11). Implementeres i Infrastructure.
/// </summary>
/// <remarks>
/// Entiteter hentes kun på kommandostier, via <c>IRoomRepository</c> i Domain. Filtre er
/// parametre, ikke hårdkodede valg — kalderen afgør hvad "tilgængelig" betyder (BR-110).
/// </remarks>
public interface IRoomQueries
{
    /// <summary>
    /// Henter et rums detaljer som projektion.
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Detaljerne, eller <c>null</c> hvis rummet ikke findes (BR-115).</returns>
    Task<RoomDetailsDto?> GetDetailsAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Henter alle rum som listerækker.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Alle rum, sorteret på værelsesnummer.</returns>
    Task<IReadOnlyList<RoomListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Henter de rum der matcher filteret (BR-74, BR-76).
    /// </summary>
    /// <param name="filter">Filteret. Et tomt filter betyder alle rum.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>De matchende rum. Filtreringen sker i databasen, ikke i hukommelsen (BR-111).</returns>
    Task<IReadOnlyList<RoomListItemDto>> GetByFilterAsync(
        RoomFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Henter rum med den angivne bookbarhed som tilgængelighedsrækker.
    /// </summary>
    /// <param name="status">Statussen kalderen ønsker. Kalderen — ikke repositoryet — vælger (BR-110).</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Rummene, sorteret på værelsesnummer.</returns>
    Task<IReadOnlyList<AvailableRoomDto>> GetByStatusAsync(
        RoomStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Henter de etager der faktisk findes rum på (BR-75).
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Etagenumre uden dubletter, stigende.</returns>
    Task<IReadOnlyList<int>> GetDistinctFloorsAsync(CancellationToken cancellationToken = default);
}