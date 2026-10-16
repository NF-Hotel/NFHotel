namespace NFHotel.Domain.ValueObjects;

/// <summary>
/// Regler der hører til <see cref="BookingStatus"/>.
/// </summary>
public static class BookingStatusExtensions
{
    /// <summary>
    /// Afgør om en booking med den angivne status lægger beslag på rummet.
    /// </summary>
    /// <param name="status">Bookingens status.</param>
    /// <returns>Sand for alt undtagen <see cref="BookingStatus.Cancelled"/>.</returns>
    /// <remarks>
    /// Kun annullering frigiver rummet helt. En udtjekket booking blokerer stadig — men
    /// kun frem til sin effektive slutdato, så tidlig udtjekning alligevel frigiver de
    /// resterende nætter (A-01). Svarer til databasens <c>WHERE (status &lt;&gt; 4)</c>.
    /// </remarks>
    public static bool BlocksRoom(this BookingStatus status) => status != BookingStatus.Cancelled;
}