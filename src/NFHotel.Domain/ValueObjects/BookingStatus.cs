namespace NFHotel.Domain.ValueObjects;

/// <summary>
/// Bookingens livscyklus.
/// </summary>
/// <remarks>
/// Ordinalværdierne 0-4 er bevaret fra det gamle system, fordi de allerede findes som int
/// i eksisterende data (B-07). Værdierne må aldrig omnummereres uden en datamigrering —
/// databasens exclusion constraint refererer direkte til <c>status &lt;&gt; 4</c>.
/// </remarks>
public enum BookingStatus
{
    /// <summary>Oprettet, men ikke betalingsgaranteret. Startstatus for enhver booking (BR-01).</summary>
    Pending = 0,

    /// <summary>
    /// Betaling garanteret. Er IKKE en forudsætning for check-in — walk-in går direkte
    /// fra <see cref="Pending"/> til <see cref="CheckedIn"/> (B-01).
    /// </summary>
    Confirmed = 1,

    /// <summary>Gæsten er tjekket ind.</summary>
    CheckedIn = 2,

    /// <summary>
    /// Gæsten er tjekket ud. Blokerer rummet frem til bookingens effektive slutdato,
    /// ikke til den oprindelige <c>EndDate</c> (A-01).
    /// </summary>
    CheckedOut = 3,

    /// <summary>
    /// Annulleret. Soft-delete — rækken slettes aldrig (BR-13).
    /// Eneste status der frigiver rummet helt (A-01).
    /// </summary>
    Cancelled = 4
}