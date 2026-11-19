using System.Globalization;
using System.Text;

using NFHotel.Domain.Attributes;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Guests;

/// <summary>
/// The list of countries that a guest chooses from (MIL-086 decision 2, ADR-0050): the 249 codes of ISO 3166-1 with
/// the English short name that the data of the hotel already stores. The data of the hotel is checked against the list
/// in the tests of the application, which can read the repository.
/// </summary>
public class CountriesTests
{
    /// <summary>Positive: 249 entries, as many as ISO 3166-1 assigns, each code and each name once.</summary>
    [Fact]
    public void All_TheCountriesOfIso3166_HasEachCodeAndNameOnce()
    {
        // Act
        IReadOnlyList<IsoCountry> all = Countries.All;

        // Assert
        Assert.Equal(249, all.Count);
        Assert.Equal(all.Count, all.Select(country => country.Alpha2Code).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(all.Count, all.Select(country => country.Name).Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>Equivalence: a code is two capital ASCII letters, and a name is trimmed and in normal form C.</summary>
    [Fact]
    public void All_CodesAndNames_HaveTheirForm()
    {
        // Act & Assert
        Assert.All(Countries.All, country =>
        {
            Assert.Matches("^[A-Z]{2}$", country.Alpha2Code);
            Assert.Equal(country.Name.Trim(), country.Name);
            Assert.True(country.Name.IsNormalized(NormalizationForm.FormC), country.Name);
            Assert.True(CountryNameAttribute.Accepts(country.Name), country.Name);
        });
    }

    /// <summary>Positive: the list is in the order of its names in the invariant culture, so a page shows it as it is.</summary>
    [Fact]
    public void All_Order_IsTheOrderOfTheNames()
    {
        // Arrange
        string[] names = Countries.All.Select(country => country.Name).ToArray();

        // Act
        string[] sorted = names.Order(StringComparer.Create(CultureInfo.InvariantCulture, ignoreCase: false)).ToArray();

        // Assert
        Assert.Equal(sorted, names);
    }

    /// <summary>
    /// Positive: the eight names that the published copy lacked or held in an older form are as ADR-0050 gives them,
    /// with their letters composed.
    /// </summary>
    [Theory]
    [InlineData("AX", "Åland Islands")]
    [InlineData("BL", "Saint Barthélemy")]
    [InlineData("CI", "Côte d'Ivoire")]
    [InlineData("CW", "Curaçao")]
    [InlineData("RE", "Réunion")]
    [InlineData("GB", "United Kingdom of Great Britain and Northern Ireland (the)")]
    [InlineData("TR", "Türkiye")]
    [InlineData("NL", "Netherlands (Kingdom of the)")]
    public void All_CorrectedEntries_HoldTheirNames(string code, string name)
    {
        // Act
        IsoCountry country = Assert.Single(Countries.All, entry => entry.Alpha2Code == code);

        // Assert
        Assert.Equal(name, country.Name);
    }

    /// <summary>Positive: the codes of the Nordic countries, the neighbours of the hotel, name their countries.</summary>
    [Theory]
    [InlineData("DK", "Denmark")]
    [InlineData("SE", "Sweden")]
    [InlineData("NO", "Norway")]
    [InlineData("FI", "Finland")]
    [InlineData("IS", "Iceland")]
    [InlineData("DE", "Germany")]
    public void All_Neighbours_HoldTheirNames(string code, string name)
    {
        // Act
        IsoCountry country = Assert.Single(Countries.All, entry => entry.Alpha2Code == code);

        // Assert
        Assert.Equal(name, country.Name);
    }
}
