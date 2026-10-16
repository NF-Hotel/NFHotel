using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.TestSupport;

/// <summary>
/// Bygger rum i en ønsket rengøringstilstand.
/// </summary>
/// <remarks>
/// Tilstandene nås udelukkende gennem de lovlige overgange, så fabrikken ikke kan
/// producere en tilstand rummet ikke selv kunne have nået.
/// </remarks>
internal static class RoomFactory
{
    /// <summary>Standardværelsesnummer.</summary>
    internal const string RoomNumber = "101";

    /// <summary>Standardetage. Skal være > 0 (BR-98).</summary>
    internal const int Floor = 1;

    /// <summary>Standardkapacitet. Skal være > 0 (BR-100).</summary>
    internal const int Capacity = 2;

    /// <summary>
    /// Opretter et nyt rum: bookbart og rent (BR-81).
    /// </summary>
    /// <returns>Et gyldigt, ikke-persisteret rum.</returns>
    internal static Room Create() => Room.Create(RoomNumber, Floor, RoomSize.Double, Capacity);

    /// <summary>
    /// Opretter et rum og kører det frem til den ønskede rengøringstilstand.
    /// </summary>
    /// <param name="status">Den ønskede rengøringstilstand.</param>
    /// <returns>Et rum i <paramref name="status"/>.</returns>
    internal static Room WithHousekeeping(HousekeepingStatus status)
    {
        var room = Create();

        switch (status)
        {
            case HousekeepingStatus.Clean:
                break;

            case HousekeepingStatus.DailyCleaningDue:
                room.MarkDailyCleaningDue();
                break;

            case HousekeepingStatus.DepartureCleaningDue:
                room.MarkDepartureCleaningDue();
                break;

            case HousekeepingStatus.CleaningInProgress:
                room.MarkDepartureCleaningDue();
                room.StartCleaning();
                break;

            case HousekeepingStatus.ServiceRequired:
                room.ReportServiceNeeded();
                break;

            case HousekeepingStatus.ServiceInProgress:
                room.ReportServiceNeeded();
                room.StartService();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "Ukendt rengøringstilstand.");
        }

        return room;
    }
}