using System.Globalization;

namespace NFHotel.Application.UseCases;

/// <summary>The one way a price or a number of the pricing rule is written into an audit detail: two decimals, a dot.</summary>
internal static class MoneyText
{
    /// <summary>Writes a number with two decimals in the invariant culture.</summary>
    /// <param name="value">The number.</param>
    /// <returns>The text, for example <c>100.00</c>.</returns>
    public static string Format(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
}
