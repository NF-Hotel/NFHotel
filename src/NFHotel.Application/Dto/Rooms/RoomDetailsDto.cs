using NFHotel.Application.UseCases.Rooms;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Rooms;

/// <summary>
/// Et rum med alt hvad detaljeskærmen har brug for.
/// </summary>
/// <param name="RoomId">Rummets id.</param>
/// <param name="RoomNumber">Værelsesnummer.</param>
/// <param name="Floor">Etage.</param>
/// <param name="Size">Værelsestype.</param>
/// <param name="Capacity">Antal personer rummet kan rumme.</param>
/// <param name="Status">Bookbarhed (B-04).</param>
/// <param name="HousekeepingStatus">Rengørings- og servicestand (B-04).</param>
/// <param name="IsBookable">
/// Om rummet overhovedet må udlejes (BR-110). Beregnet af domænets
/// <see cref="Room.IsBookable"/> — Web må ikke udlede den af <paramref name="Status"/> selv.
/// </param>
/// <param name="HasBookings">
/// Om der findes bookinger på rummet. Bærer sletteguarden (BR-78, BR-112) op i UI'et, så
/// advarslen kan vises inden knappen trykkes i stedet for som en fejl bagefter.
/// </param>
public sealed record RoomDetailsDto(
    int RoomId,
    string RoomNumber,
    int Floor,
    RoomSize Size,
    int Capacity,
    RoomStatus Status,
    HousekeepingStatus HousekeepingStatus,
    bool IsBookable,
    bool HasBookings)
{
    /// <summary>
    /// Hvilke overgange der er lovlige lige nu (A-07), så Web kan tegne knapper uden at kende
    /// en eneste overgang.
    /// </summary>
    /// <remarks>
    /// Beregnet property og ikke en konstruktørparameter, fordi detaljevisningen hentes som
    /// SQL-projektion i repositoryet (A-11): databasen kan ikke fylde et
    /// <see cref="RoomActionsDto"/> i rækken. Udtrykkene ligger derfor ét sted i Application —
    /// <see cref="RoomMapping.ToActions(RoomStatus, HousekeepingStatus)"/> — som både denne
    /// property og entitetsstien bruger.
    /// </remarks>
    public RoomActionsDto Actions => RoomMapping.ToActions(Status, HousekeepingStatus);
}