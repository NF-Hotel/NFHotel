using System.Globalization;

namespace NFHotel.Web.Features.Booking;

/// <summary>
/// Datotekster til den offentlige del af sitet: <c>d MMMM yyyy</c> (UI-Spec afsnit 4).
/// </summary>
/// <remarks>
/// Månedsnavnene står her frem for at komme fra en <c>CultureInfo</c>, og det er et bevidst
/// valg. Serverens kultur er ikke sat nogen steder i <c>Program.cs</c>, og en
/// <c>ToString("d MMMM yyyy")</c> ville derfor give ét sprog på én server og et andet på
/// den næste — samme kode, to udfald, afhængigt af værtsmaskinen. Sitet skal desuden virke
/// uden ICU, på linje med app.css' krav om ingen eksterne fonte og ingen CDN'er.
/// <para>
/// Kun den offentlige kontekst bruger denne lange form. Admin-tabellerne bruger den korte
/// <c>dd MMM yyyy</c> fra <c>AdminFormat</c>.
/// </para>
/// </remarks>
public static class PublicDateText
{
    /// <summary>Tankestreg mellem to datoer i en periode. Ikke en bindestreg.</summary>
    private const string RangeSeparator = " – ";

    /// <summary>Månedsnavne i rækkefølge, skrevet som engelsk retskrivning foreskriver.</summary>
    private static readonly string[] MonthNames =
    [
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    ];

    /// <summary>
    /// Skriver én dato som <c>12 October 2026</c>.
    /// </summary>
    /// <param name="date">Datoen.</param>
    /// <returns>Den engelske tekst.</returns>
    public static string Long(DateOnly date) => string.Concat(
        date.Day.ToString(CultureInfo.InvariantCulture),
        " ",
        MonthNames[date.Month - 1],
        " ",
        date.Year.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Skriver en periode som <c>12 October 2026 – 15 October 2026</c>.
    /// </summary>
    /// <param name="start">Ankomstdatoen, inklusiv.</param>
    /// <param name="end">Afrejsedatoen, eksklusiv.</param>
    /// <returns>Den engelske tekst.</returns>
    public static string Period(DateOnly start, DateOnly end) =>
        string.Concat(Long(start), RangeSeparator, Long(end));

    /// <summary>
    /// Skriver et antal overnatninger med korrekt flertalsform.
    /// </summary>
    /// <param name="nights">Antal nætter.</param>
    /// <returns>Fx <c>1 night</c> eller <c>3 nights</c>.</returns>
    public static string Nights(int nights) => string.Concat(
        nights.ToString(CultureInfo.InvariantCulture),
        nights == 1 ? " night" : " nights");
}