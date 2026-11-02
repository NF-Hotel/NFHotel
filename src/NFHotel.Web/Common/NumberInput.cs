using System.Globalization;

namespace NFHotel.Web.Common;

/// <summary>Reads the numbers that staff type into a field: invariant culture, a dot as the decimal mark.</summary>
public static class NumberInput
{
    /// <summary>Parses a decimal number; thousands separators, spaces and exponents are refused.</summary>
    /// <param name="text">The text typed.</param>
    /// <param name="value">The number, when the text is one.</param>
    /// <returns><c>true</c> when the text is a number.</returns>
    public static bool TryParseDecimal(string? text, out decimal value) =>
        decimal.TryParse(
            text,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out value);

    /// <summary>Writes a number as the field shows it: two decimals and a dot.</summary>
    /// <param name="value">The number.</param>
    /// <returns>The text, for example <c>89.50</c>.</returns>
    public static string Format(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
}
