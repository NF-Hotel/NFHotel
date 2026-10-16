using NFHotel.Domain.ValueObjects;

namespace NFHotel.Web.Common;

/// <summary>
/// Oversætter domænets tilstande til badge-klasser fra designsystemet.
/// </summary>
/// <remarks>
/// Modstykket til <see cref="DisplayText"/>: den ejer ordlyden, denne ejer udseendet.
/// Begge hører i Web, fordi domænemodeller ikke må fyldes med UI-formatering
/// (Conventions.md). Klassenavnene er kontrakten fra UI-Spec afsnit 3 og findes i
/// <c>wwwroot/app.css</c>.
/// <para>
/// <c>_</c>-grenene falder tilbage til den neutrale badge. De er der fordi C# kræver at
/// et switch-udtryk over en enum dækker alle int-værdier — ikke fordi en tilstand mangler.
/// </para>
/// </remarks>
public static class BadgeClass
{
    /// <summary>Basisklassen som alle modifiers bygger på.</summary>
    private const string Base = "badge";

    /// <summary>
    /// Giver badge-klasserne for en bookings status.
    /// </summary>
    /// <param name="status">Bookingens status.</param>
    /// <returns>Klassestrengen, fx <c>"badge badge-confirmed"</c>.</returns>
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
    /// Giver badge-klasserne for et rums bookbarhed.
    /// </summary>
    /// <param name="status">Rummets status.</param>
    /// <returns>Klassestrengen.</returns>
    /// <remarks>
    /// <see cref="RoomStatus.Maintenance"/> er midlertidig og deler udseende med service,
    /// mens <see cref="RoomStatus.OutOfService"/> er en administrativ spærring og derfor
    /// får den neutrale "blokeret"-farve.
    /// </remarks>
    public static string For(RoomStatus status) => status switch
    {
        RoomStatus.Available => $"{Base} badge-confirmed",
        RoomStatus.Maintenance => $"{Base} badge-service",
        RoomStatus.OutOfService => $"{Base} badge-blocked",
        _ => Base
    };

    /// <summary>
    /// Giver badge-klasserne for et rums rengørings- og servicestand.
    /// </summary>
    /// <param name="status">Rummets rengøringsstatus.</param>
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
    /// Giver klassen for en beskeds alvorlighedsgrad.
    /// </summary>
    /// <param name="kind">Alvorlighedsgraden.</param>
    /// <returns>Klassestrengen, fx <c>"alert alert-error"</c>.</returns>
    public static string For(AlertKind kind) => kind switch
    {
        AlertKind.Error => "alert alert-error",
        AlertKind.Ok => "alert alert-ok",
        AlertKind.Info => "alert alert-info",
        _ => "alert alert-info"
    };
}