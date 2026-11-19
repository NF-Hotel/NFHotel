using System.Reflection;

using Microsoft.AspNetCore.Components;

using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;
using NFHotel.Web.Features.Booking;

namespace NFHotel.Web.Tests;

/// <summary>
/// The rows of the address of the booking page in the table "Validation rules of the booking flow" hold against the
/// page (MIL-086 decision 1, criterion 2, ADR-0050): every value that the page reads from its address has a row, every
/// such row names a value that the page reads, and every type and method of the last column exists. The rows of the
/// API are held by the tests of the API.
/// </summary>
public sealed class BookingAddressRulesTableTests
{
    private const string BookingPageRowPrefix = "/book?";

    // The assemblies whose types the last column of a row of the page may name: the page and the rules of the domain.
    private static readonly Assembly[] RuleAssemblies =
    [
        typeof(BookingFlowViewModel).Assembly,
        typeof(Countries).Assembly,
    ];

    /// <summary>Positive and negative: the values of the address of the booking page and the rows of the table are the same set.</summary>
    [Fact]
    public void Table_RowsOfTheBookingPage_AreTheValuesThatThePageReads()
    {
        // Arrange
        string[] read = typeof(Book).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.GetCustomAttribute<SupplyParameterFromQueryAttribute>()?.Name)
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();

        // Act
        string[] rows = BookingPageRows()
            .Select(row => row.Field[BookingPageRowPrefix.Length..])
            .Order(StringComparer.Ordinal)
            .ToArray();

        // Assert
        Assert.NotEmpty(read);
        Assert.Equal(read, rows);
    }

    /// <summary>Negative: a type or a method of the last column of a row of the page that does not exist fails.</summary>
    [Fact]
    public void Table_EveryRuleOfTheBookingPage_Exists()
    {
        // Act
        string[] missing = BookingPageRows()
            .SelectMany(row => ValidationRulesTable.NamesOfCode(row.RuleInTheCode))
            .Where(name => !ValidationRulesTable.Exists(name, RuleAssemblies))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        // Assert
        Assert.True(missing.Length == 0, "The last column names code that does not exist: " + string.Join(", ", missing));
    }

    private static IEnumerable<ValidationRule> BookingPageRows() =>
        ValidationRulesTable.Read().Where(row => row.Field.StartsWith(BookingPageRowPrefix, StringComparison.Ordinal));
}
