using NFHotel.Application.Ports;
using NFHotel.Infrastructure.Config;

namespace NFHotel.Infrastructure.Time;

/// <summary>
/// Systemets ur. Eneste implementering af <see cref="IClock"/> (A-05).
/// </summary>
/// <remarks>
/// <see cref="UtcNow"/> er maskinens ur i UTC; <see cref="Today"/> er hotellets
/// kalenderdato. Adskillelsen er hele pointen: tidsstempler skal være entydige på tværs af
/// sommertid, mens bookingperioder er lokale kalenderdatoer (B-08).
/// <para>
/// Typen er tilstandsløs bortset fra den uforanderlige tidszone og kan registreres som
/// singleton.
/// </para>
/// </remarks>
public sealed class SystemClock : IClock
{
    private readonly TimeZoneInfo _hotelTimeZone;

    /// <summary>
    /// Opretter uret.
    /// </summary>
    /// <param name="options">Tidsindstillingerne, herunder hotellets tidszone.</param>
    /// <exception cref="ArgumentNullException">Kastes hvis <paramref name="options"/> er <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">
    /// Kastes hvis tidszone-id'et ikke findes på maskinen. Kastes ved opstart, ikke ved
    /// første booking (fail fast).
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

    /// <summary>Slår tidszonen op og oversætter frameworkets fejl til en forståelig opstartsfejl.</summary>
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