using System.Reflection;

namespace NFHotel.Application.Tests.Architecture;

/// <summary>
/// The website holds no pricing rule of its own (MIL-071): the market analysis service works out the suggestions, and the application
/// layer only presents them and applies the limits of the Manager. A season, or a rule that makes suggestions, must not come back.
/// </summary>
public sealed class NoPriceRuleTests
{
    private static readonly Assembly ApplicationAssembly = typeof(UseCases.Result).Assembly;

    /// <summary>Negative: no type of the application layer is named after a season or after a rule that makes price suggestions.</summary>
    [Fact]
    public void Application_Types_HaveNoSeasonAndNoSuggestionRule()
    {
        // Act
        string[] offenders =
        [
            .. ApplicationAssembly.GetTypes()
                .Where(type => type.Name.Contains("Season", StringComparison.Ordinal)
                    || (type.Name.Contains("Suggestion", StringComparison.Ordinal) && type.Name.Contains("Rule", StringComparison.Ordinal))
                    || type.Name.StartsWith("OccupancyObservation", StringComparison.Ordinal) && type.Name != "OccupancyObservationReader")
                .Select(type => type.FullName ?? type.Name),
        ];

        // Assert
        Assert.True(offenders.Length == 0, $"The application layer holds a pricing rule again: {string.Join(", ", offenders)}");
    }

    /// <summary>Negative: no method of the application layer reads the monthly occupancy to work out a high or a low season.</summary>
    [Fact]
    public void Application_Methods_DoNotComputeASeasonOrASuggestion()
    {
        // Act
        string[] offenders =
        [
            .. ApplicationAssembly.GetTypes()
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Select(method => (Type: type, Method: method)))
                .Where(item => item.Method.Name is "ByCalendarMonth" or "Suggest" or "ComputeSeason")
                .Select(item => $"{item.Type.FullName}.{item.Method.Name}"),
        ];

        // Assert
        Assert.True(offenders.Length == 0, $"The application layer computes a season or a suggestion again: {string.Join(", ", offenders)}");
    }
}
