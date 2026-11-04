using NFHotel.External.Config;

namespace NFHotel.Web.Common;

/// <summary>
/// Turns the moments the API stores into the clock time of the hotel and back (A-05, MIL-060), so a Manager plans a shift
/// in the time of the hotel whatever the time zone of the server.
/// </summary>
public sealed class HotelTimeZone
{
    private readonly TimeZoneInfo _zone;

    /// <summary>Creates the converter.</summary>
    /// <param name="options">The time settings of the hotel.</param>
    public HotelTimeZone(HotelTimeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _zone = TimeZoneInfo.FindSystemTimeZoneById(
            string.IsNullOrWhiteSpace(options.TimeZoneId) ? HotelTimeOptions.DefaultTimeZoneId : options.TimeZoneId);
    }

    /// <summary>Creates the converter for a zone, for tests.</summary>
    /// <param name="zone">The zone.</param>
    public HotelTimeZone(TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(zone);
        _zone = zone;
    }

    /// <summary>The moment as the clock of the hotel shows it.</summary>
    /// <param name="moment">The moment.</param>
    /// <returns>The local date and time with the offset of the hotel.</returns>
    public DateTimeOffset ToHotel(DateTimeOffset moment) => TimeZoneInfo.ConvertTime(moment, _zone);

    /// <summary>The moment of a date and a time on the clock of the hotel.</summary>
    /// <param name="date">The date.</param>
    /// <param name="time">The time.</param>
    /// <returns>The moment, with the offset of the hotel on that date.</returns>
    public DateTimeOffset AtHotel(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, _zone.GetUtcOffset(local));
    }
}
