using NFHotel.Application.Dto.Rooms;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Rooms;

/// <summary>
/// Eksplicit mapping fra <see cref="Room"/> til rummets DTO'er (A-14).
/// </summary>
/// <remarks>
/// Håndskrevet og uden mapping-bibliotek. Feltvalgene er beslutninger, ikke boilerplate —
/// konventionsbaseret mapping ville kopiere alt hvad der tilfældigvis hedder det samme, og
/// dermed træffe dem for os.
/// <para>
/// Mapping bruges kun på <b>kommandostier</b>, hvor entiteten alligevel er hentet.
/// Læsestier projicerer direkte i repositoryet (A-11), så en manglende <c>Include</c> ikke
/// kan give tavst tomme felter.
/// </para>
/// </remarks>
public static class RoomMapping
{
    /// <summary>
    /// Mapper et rum til en listerække.
    /// </summary>
    /// <param name="room">Rummet.</param>
    /// <returns>Listerækken.</returns>
    public static RoomListItemDto ToListItem(this Room room)
    {
        ArgumentNullException.ThrowIfNull(room);

        return new RoomListItemDto(
            room.Id,
            room.RoomNumber,
            room.Floor,
            room.Size,
            room.Capacity,
            room.Status,
            room.HousekeepingStatus);
    }

    /// <summary>
    /// Mapper et rum til detaljevisningen.
    /// </summary>
    /// <param name="room">Rummet.</param>
    /// <param name="hasBookings">
    /// Om der findes bookinger på rummet. Leveres udefra, fordi det er et opslag i en anden
    /// tabel og ikke en egenskab ved rummet.
    /// </param>
    /// <returns>Detaljevisningen. <c>IsBookable</c> kommer fra domænet (BR-110).</returns>
    public static RoomDetailsDto ToDetails(this Room room, bool hasBookings)
    {
        ArgumentNullException.ThrowIfNull(room);

        return new RoomDetailsDto(
            room.Id,
            room.RoomNumber,
            room.Floor,
            room.Size,
            room.Capacity,
            room.Status,
            room.HousekeepingStatus,
            room.IsBookable,
            hasBookings);
    }

    /// <summary>
    /// Beregner hvilke overgange der er lovlige på et rum lige nu (A-07).
    /// </summary>
    /// <param name="status">Rummets bookbarhed.</param>
    /// <param name="housekeepingStatus">Rummets rengørings- og servicestand.</param>
    /// <returns>Handlingerne.</returns>
    /// <remarks>
    /// Prædikaterne spejler <see cref="Room.CanTakeOutOfService"/> og de øvrige
    /// <c>Room.Can*</c>-properties, som overgangsmetoderne selv bruger som guard. De kan ikke
    /// kaldes direkte, fordi både listen og detaljevisningen projiceres i repositoryet og
    /// aldrig materialiserer entiteten (A-11).
    /// <para>
    /// <b>Dette er den eneste kopi i Application</b> — begge DTO'ers <c>Actions</c> og
    /// <see cref="ToActions(Room)"/> kalder den samme funktion, så læse- og kommandostien ikke
    /// kan blive uenige. Samme opstilling som <c>BookingMapping.ToActions</c>, og en
    /// paritetstest mod domænet skal pinne den.
    /// </para>
    /// </remarks>
    public static RoomActionsDto ToActions(RoomStatus status, HousekeepingStatus housekeepingStatus) =>
        new(
            CanTakeOutOfService: status != RoomStatus.OutOfService,
            CanSendToMaintenance: status != RoomStatus.Maintenance,
            CanReturnToService: status != RoomStatus.Available,
            CanMarkDailyCleaningDue:
                housekeepingStatus is HousekeepingStatus.Clean or HousekeepingStatus.ServiceRequired,
            CanMarkDepartureCleaningDue:
                housekeepingStatus is HousekeepingStatus.Clean
                    or HousekeepingStatus.DailyCleaningDue
                    or HousekeepingStatus.ServiceRequired,
            CanStartCleaning:
                housekeepingStatus is HousekeepingStatus.DailyCleaningDue
                    or HousekeepingStatus.DepartureCleaningDue,
            CanCompleteCleaning: housekeepingStatus == HousekeepingStatus.CleaningInProgress,
            CanReportServiceNeeded:
                housekeepingStatus is not (HousekeepingStatus.ServiceRequired
                    or HousekeepingStatus.ServiceInProgress),
            CanStartService: housekeepingStatus == HousekeepingStatus.ServiceRequired,
            CanCompleteService: housekeepingStatus == HousekeepingStatus.ServiceInProgress);

    /// <summary>
    /// Beregner de lovlige overgange for en rum-entitet (A-07).
    /// </summary>
    /// <param name="room">Rummet.</param>
    /// <returns>Handlingerne.</returns>
    public static RoomActionsDto ToActions(this Room room)
    {
        ArgumentNullException.ThrowIfNull(room);

        return ToActions(room.Status, room.HousekeepingStatus);
    }

    /// <summary>
    /// Mapper et rum til tilgængelighedslistens række.
    /// </summary>
    /// <param name="room">Rummet.</param>
    /// <returns>Rækken uden status — listen indeholder kun ledige rum.</returns>
    public static AvailableRoomDto ToAvailableRoom(this Room room)
    {
        ArgumentNullException.ThrowIfNull(room);

        return new AvailableRoomDto(
            room.Id,
            room.RoomNumber,
            room.Floor,
            room.Size,
            room.Capacity);
    }
}