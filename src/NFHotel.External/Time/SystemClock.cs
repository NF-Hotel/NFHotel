using NFHotel.Application.Ports;
using NFHotel.External.Config;

namespace NFHotel.External.Time;

/// <summary>
/// The clock of the system. The only implementation of <see cref="IClock"/> (A-05).
/// </summary>
/// <remarks>
/// <see cref="UtcNow"/> is the machine's clock in UTC; <see cref="Today"/> is the hotel's
/// calendar date. The separation is the whole point: timestamps must be unambiguous across
/// daylight saving time, while booking periods are local calendar dates (B-08).
/// <para>
/// The type is stateless apart from the immutable time zone and can be registered as
/// singleton.
/// </para>
/// </remarks>
public sealed class SystemClock : IClock
{
    private readonly TimeZoneInfo _hotelTimeZone;

    /// <summary>
    /// Opretter uret.
    /// </summary>
    /// <param name="options">The time settings, including the hotel's time zone.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the time zone id does not exist on the machine. Thrown at start-up, not at the
    /// first booking (fail fast).
    /// </exception>
    public SystemClock(HotelTimeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _hotelTimeZone = ResolveTimeZone(options.TimeZoneId);
    }

    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(UtcNow, _hotelTimeZone).DateTime);

    /// <summary>Looks up the time zone and translates the framework's error to an understandable start-up error.</summary>
    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        var id = string.IsNullOrWhiteSpace(timeZoneId)
            ? HotelTimeOptions.DefaultTimeZoneId
            : timeZoneId;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new InvalidOperationException(
                $"Tidszonen '{id}' fra "
                + $"'{HotelTimeOptions.SectionName}:{HotelTimeOptions.TimeZoneIdName}' findes ikke på denne maskine.",
                exception);
        }
    }
}