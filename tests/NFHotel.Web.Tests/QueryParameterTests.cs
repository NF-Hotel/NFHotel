using System.Reflection;

using Microsoft.AspNetCore.Components;

namespace NFHotel.Web.Tests;

/// <summary>
/// Every query parameter of the web application is text that its page reads itself (MIL-067 task 3). Blazor answers
/// an error page when a value of the address does not parse as the type of the parameter, so a parameter of another
/// type lets anyone make the page fail at will (ZAP 30003 on the booking page).
/// </summary>
public sealed class QueryParameterTests
{
    private const BindingFlags DeclaredProperties =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    #region The scan

    /// <summary>Positive: the scan sees the query parameters of the pages, public and private alike, so the rule has something to check.</summary>
    [Fact]
    public void QueryParameters_OfTheWebApplication_AreFound()
    {
        // Arrange and act
        var found = QueryParameters().Select(Name).ToArray();

        // Assert
        Assert.Contains("NFHotel.Web.Features.Booking.Book.GuestsQuery", found);
        Assert.Contains("NFHotel.Web.Features.Admin.Login.Login.ReturnTo", found);
    }

    #endregion

    #region The rule

    /// <summary>Negative: a query parameter of any type but text fails the rule, and the failure names the page, the property and its type.</summary>
    [Fact]
    public void QueryParameters_OfEveryPage_AreText()
    {
        // Arrange and act
        var typed = QueryParameters()
            .Where(parameter => parameter.Property.PropertyType != typeof(string))
            .Select(parameter => $"{Name(parameter)} is {TypeName(parameter.Property.PropertyType)}")
            .ToArray();

        // Assert
        if (typed.Length > 0)
        {
            Assert.Fail($"Read these query parameters as text and parse them in their page: {string.Join("; ", typed)}.");
        }
    }

    #endregion

    #region Helpers

    private static IEnumerable<(Type Page, PropertyInfo Property)> QueryParameters() =>
        typeof(Program).Assembly.GetTypes()
            .SelectMany(type => type.GetProperties(DeclaredProperties), (type, property) => (Page: type, Property: property))
            .Where(parameter => parameter.Property.IsDefined(typeof(SupplyParameterFromQueryAttribute), inherit: false));

    private static string Name((Type Page, PropertyInfo Property) parameter) =>
        $"{parameter.Page.FullName}.{parameter.Property.Name}";

    private static string TypeName(Type type) =>
        Nullable.GetUnderlyingType(type) is { } inner ? $"{inner.Name}?" : type.Name;

    #endregion
}
