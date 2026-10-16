using NFHotel.Domain.Entities;

namespace NFHotel.Domain.Tests.TestSupport;

/// <summary>
/// Translates a <see cref="HousekeepingTransition"/> to the corresponding method call on a room.
/// </summary>
internal static class HousekeepingTransitionInvoker
{
    /// <summary>
    /// Performs the transition on the room.
    /// </summary>
    /// <param name="room">The room the transition is performed on.</param>
    /// <param name="transition">The transition to perform.</param>
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