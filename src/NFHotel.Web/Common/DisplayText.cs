using NFHotel.Domain.ValueObjects;

namespace NFHotel.Web.Common;

/// <summary>
/// Danske visningstekster for domænets enums.
/// </summary>
/// <remarks>
/// Ren visningsformatering og derfor Web-lagets ansvar — domænemodeller må ikke fyldes med
/// UI-formatering (Conventions.md). Samme princip som <see cref="ErrorMessages"/>: domænet
/// ejer værdien, Web ejer ordlyden.
/// <para>
/// <c>_</c>-grenen falder tilbage til enum-navnet. Den er der fordi C# kræver at et
/// switch-udtryk over en enum er udtømmende for <i>alle</i> int-værdier — ikke for at
/// dække en glemt status. Tilføjes en ny værdi i domænet, viser skærmen dens engelske navn
/// indtil teksten skrives her, frem for at kaste midt i en tabel.
/// </para>
/// </remarks>
public static class DisplayText
{
    /// <summary>
    /// Teksten der vises hvor en liste er tom.
    /// </summary>
    public const string NoData = "Nothing to show here yet.";

    /// <summary>
    /// Beskriver en bookings livscyklustilstand.
    /// </summary>
    /// <param name="status">Bookingens status.</param>
    /// <returns>Den danske tekst.</returns>
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
    /// Beskriver et rums bookbarhed (B-04).
    /// </summary>
    /// <param name="status">Rummets status.</param>
    /// <returns>Den danske tekst.</returns>
    public static string Describe(RoomStatus status) => status switch
    {
        RoomStatus.Available => "In service",
        RoomStatus.OutOfService => "Out of service",
        RoomStatus.Maintenance => "Maintenance",
        _ => status.ToString()
    };

    /// <summary>
    /// Beskriver et rums rengøringsforløb (B-04, BR-N-06).
    /// </summary>
    /// <param name="status">Rummets rengøringsstatus.</param>
    /// <returns>Den danske tekst.</returns>
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
    /// Beskriver en rumstørrelse.
    /// </summary>
    /// <param name="size">Rumstørrelsen.</param>
    /// <returns>Den danske tekst.</returns>
    public static string Describe(RoomSize size) => size switch
    {
        RoomSize.Single => "Single room",
        RoomSize.Double => "Double room",
        RoomSize.Suite => "Suite",
        _ => size.ToString()
    };
}