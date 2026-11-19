using System.Globalization;

namespace NFHotel.Web.Common;

/// <summary>
/// Writes a date as the value of an attribute of a date field, such as <c>min</c> and <c>max</c>: <c>yyyy-MM-dd</c>, the
/// form that HTML asks for, in every culture.
/// </summary>
public static class DateInput
{
    /// <summary>The format of a date in a date field of HTML.</summary>
    public const string Format = "yyyy-MM-dd";

    /// <summary>The date as a date field of HTML writes it.</summary>
    /// <param name="date">The date.</param>
    /// <returns>The date as <c>yyyy-MM-dd</c>.</returns>
    public static string Text(DateOnly date) => date.ToString(Format, CultureInfo.InvariantCulture);
}
