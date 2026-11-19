using NFHotel.Domain.Attributes;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

namespace NFHotel.Application.Tests.Architecture;

/// <summary>
/// The list of countries stores what the data of the hotel already uses (MIL-086 task 2, ADR-0050): every country of
/// the booking data of the hotel is a name of <see cref="Countries"/>, written the same way, so the market analysis
/// keeps grouping a country under one name.
/// </summary>
public sealed class HotelCountryDataTests
{
    // The data has no country for most of its rows and writes "Unknown" for them; that is no country and is not offered.
    private const string NoCountry = "Unknown";

    /// <summary>Positive: each country of column 15 of the booking data of the hotel is a name of the list.</summary>
    [Fact]
    public void HotelData_EveryCountry_IsANameOfTheList()
    {
        // Arrange
        var path = Path.Join(RepositoryRoot.Find(), "src", "NFHotel.DataAnalysis", "data", "nf_hotel_bookings.csv");
        var lines = File.ReadAllLines(path);
        var header = lines[0].Split(';');
        var column = Array.IndexOf(header, "country");

        // Act
        var countries = lines
            .Skip(1)
            .Select(line => line.Split(';')[column])
            .Where(country => country != NoCountry)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        // Assert
        Assert.Equal(14, column);
        Assert.Equal(12, countries.Length);
        Assert.All(countries, country => Assert.True(CountryNameAttribute.Accepts(country), country));
    }
}
