using NFHotel.Domain.ValueObjects;

namespace NFHotel.Web.Common;

/// <summary>
/// Translates the states of the domain to badge classes from the design system.
/// </summary>
/// <remarks>
/// The counterpart of <see cref="DisplayText"/>: it owns the wording, this owns the appearance.
/// Both belong in Web, because domain models must not be filled with UI formatting
/// (Conventions.md). The class names are the contract from UI-Spec section 3 and are found in
/// <c>wwwroot/app.css</c>.
/// <para>
/// The <c>_</c> branches fall back to the neutral badge. They are there because C# requires that
/// a switch expression over an enum covers all int values — not because a state is missing.
/// </para>
/// </remarks>
public static class BadgeClass
{
    /// <summary>The base class that all modifiers build on.</summary>
    private const string Base = "badge";

    /// <summary>
    /// Gives the badge classes for the status of a booking.
    /// </summary>
    /// <param name="status">The status of the booking.</param>
    /// <returns>The class string, for example <c>"badge badge-confirmed"</c>.</returns>
    public static string For(BookingStatus status) => status switch
    {
        BookingStatus.Pending => $"{Base} badge-pending",
        BookingStatus.Confirmed => $"{Base} badge-confirmed",
        BookingStatus.CheckedIn => $"{Base} badge-checkedin",
        BookingStatus.CheckedOut => $"{Base} badge-checkedout",
        BookingStatus.Cancelled => $"{Base} badge-cancelled",
        _ => Base
    };

    /// <summary>
    /// Gives the badge classes for the bookability of a room.
    /// </summary>
    /// <param name="status">The status of the room.</param>
    /// <returns>Klassestrengen.</returns>
    /// <remarks>
    /// <see cref="RoomStatus.Maintenance"/> is temporary and shares its appearance with service,
    /// while <see cref="RoomStatus.OutOfService"/> is an administrative block and therefore
    /// gets the neutral "blocked" colour.
    /// </remarks>
    public static string For(RoomStatus status) => status switch
    {
        RoomStatus.Available => $"{Base} badge-confirmed",
        RoomStatus.Maintenance => $"{Base} badge-service",
        RoomStatus.OutOfService => $"{Base} badge-blocked",
        _ => Base
    };

    /// <summary>
    /// Gives the badge classes for the cleaning and service state of a room.
    /// </summary>
    /// <param name="status">The cleaning status of the room.</param>
    /// <returns>Klassestrengen.</returns>
    public static string For(HousekeepingStatus status) => status switch
    {
        HousekeepingStatus.Clean => $"{Base} badge-clean",
        HousekeepingStatus.DailyCleaningDue => $"{Base} badge-cleaning",
        HousekeepingStatus.DepartureCleaningDue => $"{Base} badge-cleaning",
        HousekeepingStatus.CleaningInProgress => $"{Base} badge-cleaning",
        HousekeepingStatus.ServiceRequired => $"{Base} badge-service",
        HousekeepingStatus.ServiceInProgress => $"{Base} badge-service",
        _ => Base
    };

    /// <summary>
    /// Gives the class for the severity of a message.
    /// </summary>
    /// <param name="kind">Alvorlighedsgraden.</param>
    /// <returns>The class string, for example <c>"alert alert-error"</c>.</returns>
    public static string For(AlertKind kind) => kind switch
    {
        AlertKind.Error => "alert alert-error",
        AlertKind.Ok => "alert alert-ok",
        AlertKind.Info => "alert alert-info",
        _ => "alert alert-info"
    };
}
