using NFHotel.Domain.Entities;

namespace NFHotel.Domain.Repositories;

/// <summary>
/// Dataadgang for rum — entitetsdelen. Implementeres i Infrastructure.
/// </summary>
/// <remarks>
/// Repositoryet <b>beslutter ikke</b>: filtre er parametre, ikke hårdkodede valg (BR-110).
/// Skrivemetoderne markerer og persisterer ikke. Projektioner ligger i <c>IRoomQueries</c>
/// (Application).
/// </remarks>
public interface IRoomRepository : IRepository<Room, int>
{
    /// <summary>
    /// Afgør om værelsesnummeret allerede er i brug (BR-N-01).
    /// </summary>
    /// <param name="roomNumber">Værelsesnummeret der undersøges.</param>
    /// <param name="excludeRoomId">
    /// Rummet der redigeres, så det ikke kolliderer med sig selv. <c>null</c> ved oprettelse.
    /// </param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Sand hvis et andet rum har nummeret.</returns>
    Task<bool> RoomNumberExistsAsync(
        string roomNumber,
        int? excludeRoomId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Afgør om der findes bookinger på rummet (BR-78, BR-112).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Sand hvis mindst én booking peger på rummet.</returns>
    /// <remarks>
    /// Erstatter det gamle systems <c>catch (SqlException 547)</c>. Fremmednøglen bevares som
    /// sidste værn, men reglen står ikke længere i en catch-blok med brugervendt engelsk tekst.
    /// </remarks>
    Task<bool> HasAnyBookingsAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>Markerer et rum til sletning. Persisterer ikke.</summary>
    /// <param name="room">Rummet. Entiteten og ikke id'et, fordi servicen alligevel har hentet den.</param>
    void Remove(Room room);
}