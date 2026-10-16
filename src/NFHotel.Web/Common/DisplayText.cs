using NFHotel.Domain.ValueObjects;

namespace NFHotel.Web.Common;

/// <summary>
/// Danish display texts for the enums of the domain.
/// </summary>
/// <remarks>
/// Pure display formatting and therefore the responsibility of the Web layer — domain models must not be filled with
/// UI formatting (Conventions.md). The same principle as <see cref="ErrorMessages"/>: the domain
/// owns the value, Web owns the wording.
/// <para>
/// The <c>_</c> branch falls back to the enum name. It is there because C# requires that a
/// switch expression over an enum is exhaustive for <i>all</i> int values — not to
/// cover a forgotten status. If a new value is added in the domain, the screen shows its English name
/// until the text is written here, rather than throwing in the middle of a table.
/// </para>
/// </remarks>
public static class DisplayText
{
    /// <summary>
    /// The text shown where a list is empty.
    /// </summary>
    public const string NoData = "Nothing to show here yet.";

    /// <summary>
    /// Describes the life cycle state of a booking.
    /// </summary>
    /// <param name="status">Bookingens status.</param>
    /// <returns>The Danish text.</returns>
    public static string Describe(BookingStatus status) => status switch
    {
        BookingStatus.Pending => "Pending",
        BookingStatus.Confirmed => "Confirmed",
        BookingStatus.CheckedIn => "Checked in",
        BookingStatus.CheckedOut => "Checked out",
        BookingStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    /// <summary>
    /// Describes the bookability of a room (B-04).
    /// </summary>
    /// <param name="status">Rummets status.</param>
    /// <returns>The Danish text.</returns>
    public static string Describe(RoomStatus status) => status switch
    {
        RoomStatus.Available => "In service",
        RoomStatus.OutOfService => "Out of service",
        RoomStatus.Maintenance => "Maintenance",
        _ => status.ToString()
    };

    /// <summary>
    /// Describes the cleaning flow of a room (B-04, BR-N-06).
    /// </summary>
    /// <param name="status">The cleaning status of the room.</param>
    /// <returns>The Danish text.</returns>
    public static string Describe(HousekeepingStatus status) => status switch
    {
        HousekeepingStatus.Clean => "Clean",
        HousekeepingStatus.DailyCleaningDue => "Daily cleaning due",
        HousekeepingStatus.DepartureCleaningDue => "Departure cleaning due",
        HousekeepingStatus.CleaningInProgress => "Cleaning in progress",
        HousekeepingStatus.ServiceRequired => "Service needed",
        HousekeepingStatus.ServiceInProgress => "Service in progress",
        _ => status.ToString()
    };

    /// <summary>
    /// Describes a room size.
    /// </summary>
    /// <param name="size">The room size.</param>
    /// <returns>The Danish text.</returns>
    public static string Describe(RoomSize size) => size switch
    {
        RoomSize.Single => "Single room",
        RoomSize.Double => "Double room",
        RoomSize.Suite => "Suite",
        _ => size.ToString()
    };
}