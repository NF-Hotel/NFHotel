using System.Globalization;

namespace NFHotel.Web.Features.Admin;

/// <summary>
/// Formatting of dates and times in admin tables.
/// </summary>
/// <remarks>
/// UI-Spec section 4: dates are written <c>dd MMM yyyy</c> in tables. The format is in one place,
/// so a column cannot come to write the date differently from the neighbouring column.
/// <para>
/// <see cref="CultureInfo.InvariantCulture"/> is used deliberately: the month abbreviation must be
/// the same English one regardless of which culture the server happens to run with, and a pure
/// number format like 01.02.2026 is read differently by Danish and American guests.
/// </para>
/// </remarks>
public static class AdminFormat
{
    /// <summary>The format dates are written with in admin (UI-Spec section 4).</summary>
    private const string DatePattern = "dd MMM yyyy";

    /// <summary>Format for a time of day without a date.</summary>
    private const string TimePattern = "HH:mm";

    /// <summary>The text shown where an optional date is not set.</summary>
    private const string Missing = "—";

    /// <summary>
    /// Writes a date as <c>dd MMM yyyy</c>.
    /// </summary>
    /// <param name="value">Datoen.</param>
    /// <returns>The formatted date.</returns>
    public static string Date(DateOnly value) =>
        value.ToString(DatePattern, CultureInfo.InvariantCulture);

    /// <summary>
    /// Writes an optional date as <c>dd MMM yyyy</c>.
    /// </summary>
    /// <param name="value">The date, or <c>null</c>.</param>
    /// <returns>The formatted date, or a dash.</returns>
    public static string Date(DateOnly? value) => value is null ? Missing : Date(value.Value);

    /// <summary>
    /// Writes a time as <c>dd MMM yyyy HH:mm</c>.
    /// </summary>
    /// <param name="value">The time, or <c>null</c>.</param>
    /// <returns>The formatted time, or a dash.</returns>
    public static string DateAndTime(DateTimeOffset? value) => value is null
        ? Missing
        : value.Value.ToString($"{DatePattern} {TimePattern}", CultureInfo.InvariantCulture);
}