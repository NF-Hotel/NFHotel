using System.Globalization;

namespace NFHotel.Domain.Tests.TestSupport;

/// <summary>
/// Parser faste ISO-datoer til <see cref="DateOnly"/>.
/// </summary>
/// <remarks>
/// Datoer kan ikke stå i <c>[InlineData]</c> (attributargumenter skal være konstanter), så
/// theories skriver dem som <c>"2026-01-01"</c> og oversætter her. Kulturuafhængigt og uden
/// <c>DateTime.Now</c>, så testene er deterministiske.
/// </remarks>
internal static class TestDate
{
    private const string IsoFormat = "yyyy-MM-dd";

    /// <summary>
    /// Oversætter en ISO-dato til <see cref="DateOnly"/>.
    /// </summary>
    /// <param name="isoDate">Datoen på formen <c>yyyy-MM-dd</c>.</param>
    /// <returns>Den tilsvarende dato.</returns>
    internal static DateOnly Of(string isoDate) =>
        DateOnly.ParseExact(isoDate, IsoFormat, CultureInfo.InvariantCulture);
}