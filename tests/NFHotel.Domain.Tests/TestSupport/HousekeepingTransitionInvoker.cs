using NFHotel.Domain.Entities;

namespace NFHotel.Domain.Tests.TestSupport;

/// <summary>
/// Oversætter en <see cref="HousekeepingTransition"/> til det tilsvarende metodekald på et rum.
/// </summary>
internal static class HousekeepingTransitionInvoker
{
    /// <summary>
    /// Udfører overgangen på rummet.
    /// </summary>
    /// <param name="room">Rummet overgangen udføres på.</param>
    /// <param name="transition">Overgangen der skal udføres.</param>
    internal static void Invoke(Room room, HousekeepingTransition transition)
    {
        switch (transition)
        {
            case HousekeepingTransition.MarkDailyCleaningDue:
                room.MarkDailyCleaningDue();
                break;

            case HousekeepingTransition.MarkDepartureCleaningDue:
                room.MarkDepartureCleaningDue();
                break;

            case HousekeepingTransition.StartCleaning:
                room.StartCleaning();
                break;

            case HousekeepingTransition.CompleteCleaning:
                room.CompleteCleaning();
                break;

            case HousekeepingTransition.ReportServiceNeeded:
                room.ReportServiceNeeded();
                break;

            case HousekeepingTransition.StartService:
                room.StartService();
                break;

            case HousekeepingTransition.CompleteServiceWithoutCleaning:
                room.CompleteService(requiresCleaning: false);
                break;

            case HousekeepingTransition.CompleteServiceRequiringCleaning:
                room.CompleteService(requiresCleaning: true);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(transition), transition, "Ukendt overgang.");
        }
    }
}