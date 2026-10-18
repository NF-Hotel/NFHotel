using System.Globalization;

namespace NFHotel.Domain.Tests.TestSupport;

/// <summary>
/// Parses fixed ISO dates to <see cref="DateOnly"/>.
/// </summary>
/// <remarks>
/// Dates cannot stand in <c>[InlineData]</c> (attribute arguments must be constants), so
/// the theories write them as <c>"2026-01-01"</c> and translate here. Culture independent and without
/// <c>DateTime.Now</c>, so the tests are deterministic.
/// </remarks>
internal static class TestDate
{
    private const string IsoFormat = "yyyy-MM-dd";

    /// <summary>
    /// Translates an ISO date to <see cref="DateOnly"/>.
    /// </summary>
    /// <param name="isoDate">The date in the form <c>yyyy-MM-dd</c>.</param>
    /// <returns>The corresponding date.</returns>
    internal static DateOnly Of(string isoDate) =>
        DateOnly.ParseExact(isoDate, IsoFormat, CultureInfo.InvariantCulture);
}