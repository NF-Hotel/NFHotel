namespace NFHotel.Infrastructure.Config;

/// <summary>
/// Indstillinger for hotellets tidszone (A-05).
/// </summary>
/// <remarks>
/// "I dag" i BR-06, BR-44 og BR-104 er hotellets kalenderdato — ikke serverens og ikke
/// UTC. Zonen er konfigurerbar, så et testmiljø og en fremtidig anden lokation ikke kræver
/// en kodeændring.
/// </remarks>
public sealed class HotelTimeOptions
{
    /// <summary>Konfigurationssektionen indstillingerne læses fra.</summary>
    public const string SectionName = "HotelTime";

    /// <summary>Nøglens navn i konfigurationen, uden sektionspræfiks.</summary>
    public const string TimeZoneIdName = "TimeZoneId";

    /// <summary>Tidszonen der bruges hvis konfigurationen ikke siger andet.</summary>
    public const string DefaultTimeZoneId = "Europe/Copenhagen";

    /// <summary>
    /// IANA- eller Windows-id for hotellets tidszone, fx <c>Europe/Copenhagen</c>.
    /// </summary>
    public string TimeZoneId { get; init; } = DefaultTimeZoneId;
}