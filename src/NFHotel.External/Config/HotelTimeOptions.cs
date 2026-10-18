namespace NFHotel.External.Config;

/// <summary>
/// Indstillinger for hotellets tidszone (A-05).
/// </summary>
/// <remarks>
/// "Today" in BR-06, BR-44 and BR-104 is the hotel's calendar date — not the server's and not
/// UTC. The zone is configurable, so a test environment and a future second location do not require
/// a code change.
/// </remarks>
public sealed class HotelTimeOptions
{
    /// <summary>The configuration section the settings are read from.</summary>
    public const string SectionName = "HotelTime";

    /// <summary>The name of the key in the configuration, without section prefix.</summary>
    public const string TimeZoneIdName = "TimeZoneId";

    /// <summary>The time zone used if the configuration says nothing else.</summary>
    public const string DefaultTimeZoneId = "Europe/Copenhagen";

    /// <summary>
    /// IANA or Windows id of the hotel's time zone, for example <c>Europe/Copenhagen</c>.
    /// </summary>
    public string TimeZoneId { get; init; } = DefaultTimeZoneId;
}