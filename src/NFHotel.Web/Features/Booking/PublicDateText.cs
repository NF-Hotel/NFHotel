using System.Globalization;

namespace NFHotel.Web.Features.Booking;

/// <summary>
/// Date texts for the public part of the site: <c>d MMMM yyyy</c> (UI-Spec section 4).
/// </summary>
/// <remarks>
/// The month names are written here rather than coming from a <c>CultureInfo</c>, and that is a deliberate
/// choice. The culture of the server is not set anywhere in <c>Program.cs</c>, and a
/// <c>ToString("d MMMM yyyy")</c> would therefore give one language on one server and another on
/// the next — the same code, two outcomes, depending on the host machine. The site must also work
/// without ICU, in line with the requirement of app.css of no external fonts and no CDNs.
/// <para>
/// Only the public context uses this long form. The admin tables use the short
/// <c>dd MMM yyyy</c> from <c>AdminFormat</c>.
/// </para>
/// </remarks>
public static class PublicDateText
{
    /// <summary>Dash between two dates in a period. Not a hyphen.</summary>
    private const string RangeSeparator = " – ";

    /// <summary>Month names in order, written as English spelling prescribes.</summary>
    private static readonly string[] MonthNames =
    [
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    ];

    /// <summary>
    /// Writes one date as <c>12 October 2026</c>.
    /// </summary>
    /// <param name="date">The date.</param>
    /// <returns>The English text.</returns>
    public static string Long(DateOnly date) => string.Concat(
        date.Day.ToString(CultureInfo.InvariantCulture),
        " ",
        MonthNames[date.Month - 1],
        " ",
        date.Year.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Writes a period as <c>12 October 2026 – 15 October 2026</c>.
    /// </summary>
    /// <param name="start">The arrival date, inclusive.</param>
    /// <param name="end">The departure date, exclusive.</param>
    /// <returns>The English text.</returns>
    public static string Period(DateOnly start, DateOnly end) =>
        string.Concat(Long(start), RangeSeparator, Long(end));

    /// <summary>
    /// Writes a number of nights with the correct plural form.
    /// </summary>
    /// <param name="nights">Number of nights.</param>
    /// <returns>For example <c>1 night</c> or <c>3 nights</c>.</returns>
    public static string Nights(int nights) => string.Concat(
        nights.ToString(CultureInfo.InvariantCulture),
        nights == 1 ? " night" : " nights");
}
