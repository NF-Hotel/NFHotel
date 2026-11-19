namespace NFHotel.Infrastructure.Config;

/// <summary>
/// The numbers of the limits on what one caller can ask of the booking flow (MIL-087, ADR-0051 decisions 1 to 3), read
/// from the section <c>SenderLimits</c>. The defaults are the table "Limits of the booking flow" of
/// <c>docs/refactor/BusinessRules.md</c>, which a test holds them to.
/// </summary>
public sealed class SenderLimitOptions
{
    #region Fields

    /// <summary>The section of the configuration.</summary>
    public const string SectionName = "SenderLimits";

    #endregion Fields

    #region Properties

    /// <summary>The searches for free rooms that one caller address may send in a minute.</summary>
    public int SearchesPerMinute { get; init; } = 60;

    /// <summary>The public booking requests that one caller address may send in an hour.</summary>
    public int BookingRequestsPerHour { get; init; } = 5;

    /// <summary>The public booking requests that one caller address may send in a day.</summary>
    public int BookingRequestsPerDay { get; init; } = 20;

    /// <summary>The coming bookings made on the public site that one email may hold.</summary>
    public int ComingPublicBookingsPerEmail { get; init; } = 3;

    /// <summary>The most keys that each store of counts per address holds.</summary>
    public int MaxTrackedKeys { get; init; } = 10_000;

    #endregion Properties
}
